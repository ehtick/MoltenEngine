namespace Molten.Graphics;
internal abstract class GpuTaskBank
{
    internal abstract void Process(GpuCommandList cmd, uint taskIndex);

    internal int BankIndex { get; init; }
}

internal class GpuTaskBank<T> : GpuTaskBank
    where T : struct, IGpuTask<T>
{
    T[] _tasks = new T[8];

    uint _nextIndex = 0;
    Stack<uint> _free = new();

    internal GpuTaskBank(int bankIndex)
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