using Molten.Collections;

namespace Molten.Graphics;

public class GpuTaskManager : IDisposable
{
    private abstract class TaskBank
    {
        internal abstract void Process(GpuCommandList cmd, uint taskIndex);

        internal int BankIndex { get; init; }
    }

    private class TaskBank<T> : TaskBank
        where T : struct, IGpuTask<T>
    {
        T[] _tasks = new T[8];
        
        uint _nextIndex = 0;
        Stack<uint> _free = new();

        internal TaskBank(int bankIndex)
        {
            BankIndex = bankIndex;
        }

        internal override void Process(GpuCommandList cmd, uint taskIndex)
        {
            ref T task = ref _tasks[taskIndex];
            bool success = T.Process(cmd, ref task);
            task.Complete(success);

            _tasks[taskIndex] = default;
            _free.Push(taskIndex);
        }

        internal uint Enqueue(ref T task)
        {
            uint index;

            if (_free.Count > 0)
            {
                index = _free.Pop();
            }
            else
            {
                if (_nextIndex >= _tasks.Length)
                    Array.Resize(ref _tasks, _tasks.Length * 2);

                index = _nextIndex++;
            }

            _tasks[index] = task;
            return index;
        }
    }


    private class TaskQueue
    {
        internal ThreadedQueue<ulong> Tasks = new();
        internal GpuFrameBuffer<GpuCommandList> Cmd;
    }

    TaskQueue[] _queues;
    List<TaskBank> _banks;
    Dictionary<Type, TaskBank> _banksByType;
    GpuDevice _device;
    Interlocker _locker;

    internal GpuTaskManager(GpuDevice device)
    {
        _device = device;
        _locker = new Interlocker();
        _queues = new TaskQueue[2];

        _banks = new List<TaskBank>();
        _banksByType = new Dictionary<Type, TaskBank>();

        for(int i = 0; i < _queues.Length; i++)
        {
            _queues[i] = new TaskQueue();
            _queues[i].Cmd = new GpuFrameBuffer<GpuCommandList>(_device, (gpu) => gpu.GetCommandList());
        }
    }

    /// <summary>
    /// Pushes a <see cref="IGpuTask{T}"/> to the specified priority queue in the current <see cref="GpuTaskManager"/>.
    /// </summary>
    /// <param name="priority">The priority of the task.</param>
    /// <param name="task"></param>
    /// <param name="cmd"></param>
    public void Push<T>(GpuPriority priority, ref T task, GpuCommandList cmd)
        where T : struct, IGpuTask<T>
    {
        if (priority == GpuPriority.Immediate)
        {
            if (cmd == null)
                throw new ArgumentNullException("A command list must be provided when using GpuPriority.Immediate.");

            bool success = T.Process(cmd, ref task);
            task.Complete(success);
        }
        else
        {
            TaskQueue priorityQueue = _queues[(int)priority];
            TaskBank<T> bank;

            _locker.Lock();
            if (!_banksByType.TryGetValue(typeof(T), out TaskBank tb))
            {
                int bankIndex = _banks.Count;
                bank = new TaskBank<T>(bankIndex);
                _banks.Add(tb);
                _banksByType.Add(typeof(T), tb);
            }
            else
            {
                bank = tb as TaskBank<T>;
            }

            uint taskIndex = bank.Enqueue(ref task);
            ulong queueIndex = ((ulong)bank.BankIndex << 32) | taskIndex;
            _locker.Unlock();

            priorityQueue.Tasks.Enqueue(queueIndex);
        }
    }

    /// <summary>
    /// Pushes a compute-based shader as a task.
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="shader">The compute shader to be run inside the task.</param>
    /// <param name="groupsX">The number of X compute thread groups.</param>
    /// <param name="groupsY">The number of Y compute thread groups.</param>
    /// <param name="groupsZ">The number of Z compute thread groups.</param>
    /// <param name="callback">A callback to run once the task is completed.</param>
    public void Push(GpuPriority priority, Shader shader, uint groupsX, uint groupsY, uint groupsZ, GpuTaskHandler<ComputeTask> callback = null)
    {
        Push(priority, shader, new Vector3UI(groupsX, groupsY, groupsZ), callback);
    }

    public void Push(GpuPriority priority, Shader shader, Vector3UI groups, GpuTaskHandler<ComputeTask> callback = null)
    {
        ComputeTask task = new();
        task.Shader = shader;
        task.Groups = groups;
        task.OnCompleted += callback;
        Push(priority, ref task);
    }

    public void Dispose()
    {
        foreach (TaskQueue queue in _queues)
            queue.Cmd.Dispose();
    }

    /// <summary>
    /// Processes all tasks held in the manager for the specified priority queue, for the current <see cref="GpuTaskManager"/>.
    /// </summary>
    /// <param name="priority">The priority of the task.</param>
    internal void Process(GpuPriority priority)
    {
        if (priority == GpuPriority.Immediate)
            throw new InvalidOperationException("Cannot process immediate priority tasks, as these are not queueable.");

        // TODO Implement "AllowBatching" property on RenderTask to allow multiple tasks to be processed in a single Begin()-End() command block
        //      Tasks that don't allow batching will:
        //       - Be executed in individual Begin()-End() command blocks
        //       - Be executed on the next available compute device queue
        //       - May not finish in the order they were requested due to task size, queue size and device performance.

        TaskQueue queue = _queues[(int)priority];
        GpuCommandList cmd = queue.Cmd.Prepare();

        cmd.Begin();
        cmd.BeginEvent($"Process queued '{priority}' tasks");

        while (queue.Tasks.TryDequeue(out ulong queueIndex))
        {
            _locker.Lock();
            uint bankIndex = (uint)(queueIndex >> 32);
            uint taskIndex = (uint)(queueIndex & 0xFFFFFFFF);

            TaskBank bank = _banks[(int)bankIndex];
            bank.Process(cmd, taskIndex);
            _locker.Unlock();
        }

        cmd.EndEvent();
        cmd.End();
        _device.Execute(cmd);
    }
}
