using Molten.Cache;
using Molten.Collections;

namespace Molten.Graphics;

/// <summary>
/// The base class for an API-specific implementation of a graphics device, which provides command/resource access to a GPU.
/// </summary>
public abstract partial class GpuDevice : EngineObject
{
    private class TaskQueue
    {
        internal ThreadedQueue<ulong> Tasks = new();
        internal GpuFrameBuffer<GpuCommandList> Cmd;
    }

    public delegate void FrameBufferSizeChangedHandler(uint oldSize, uint newSize);

    /// <summary>Occurs when a connected <see cref="IDisplayOutput"/> is activated on the current <see cref="GpuDevice"/>.</summary>
    public event DisplayOutputChanged OnOutputActivated;

    /// <summary>Occurs when a connected <see cref="IDisplayOutput"/> is deactivated on the current <see cref="GpuDevice"/>.</summary>
    public event DisplayOutputChanged OnOutputDeactivated;

    /// <summary>
    /// Invoked when the frame-buffer size is changed for the current <see cref="GpuDevice"/>.
    /// </summary>
    public event FrameBufferSizeChangedHandler OnFrameBufferSizeChanged;

    long _allocatedVRAM;
    uint _frameIndex;
    uint _newFrameBufferSize;
    uint _maxStagingSize;

    TaskQueue[] _taskQueues;
    List<GpuTaskBank> _taskBanks;
    Dictionary<Type, GpuTaskBank> _taskBanksByType;
    Interlocker _taskLocker;
    ThreadedList<GpuObject> _disposals;

    /// <summary>
    /// Creates a new instance of <see cref="GpuDevice"/>.
    /// </summary>
    /// <param name="renderer">The <see cref="RenderService"/> that the new graphics device will be bound to.</param>
    /// <param name="manager">The <see cref="GpuManager"/> that the device will be bound to.</param>
    protected GpuDevice(RenderService renderer, GpuManager manager)
    {
        Settings = renderer.Settings.Graphics;
        Renderer = renderer;
        Manager = manager;
        Log = renderer.Log;
        Profiler = new GraphicsDeviceProfiler();

        Cache = new ObjectCache();
        _disposals = new ThreadedList<GpuObject>();
        _maxStagingSize = (uint)ByteMath.FromMegabytes(renderer.Settings.Graphics.FrameStagingSize);

        SettingValue<FrameBufferMode> bufferingMode = renderer.Settings.Graphics.FrameBufferMode;
        BufferingMode_OnChanged(bufferingMode.Value, bufferingMode.Value);
        bufferingMode.OnChanged += BufferingMode_OnChanged;
    }

    public bool Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException("Cannot initialize a GraphicsDevice that has already been initialized.");

        CheckFrameBufferSize();

        if (OnInitialize())
        {
            IsInitialized = true;
            CheckFrameBufferSize();

            _taskLocker = new Interlocker();
            _taskQueues = new TaskQueue[2];
            _taskBanks = new List<GpuTaskBank>();
            _taskBanksByType = new Dictionary<Type, GpuTaskBank>();

            for (int i = 0; i < _taskQueues.Length; i++)
            {
                _taskQueues[i] = new TaskQueue();
                _taskQueues[i].Cmd = new GpuFrameBuffer<GpuCommandList>(this, (gpu) => gpu.GetCommandList());
            }
        }
        else
        {
            Log.Error($"Failed to initialize {this.Name}");
        }

        return IsInitialized;
    }

    /// <summary>
    /// Pushes a <see cref="IGpuTask{T}"/> to the specified priority queue in the current <see cref="GpuTaskManager"/>.
    /// </summary>
    /// <param name="priority">The priority of the task.</param>
    /// <param name="task"></param>
    /// <param name="cmd">The command list to use if excuting a task with <see cref="GpuPriority.Immediate"/>. 
    /// If the task is not executed with immediate priority, the command list parameter is ignored.</param>
    public void PushTask<T>(GpuPriority priority, ref T task, GpuCommandList cmd)
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
            TaskQueue priorityQueue = _taskQueues[(int)priority];
            GpuTaskBank<T> bank;

            _taskLocker.Lock();
            if (!_taskBanksByType.TryGetValue(typeof(T), out GpuTaskBank tb))
            {
                int bankIndex = _taskBanks.Count;
                bank = new GpuTaskBank<T>(bankIndex);
                _taskBanks.Add(tb);
                _taskBanksByType.Add(typeof(T), tb);
            }
            else
            {
                bank = tb as GpuTaskBank<T>;
            }

            uint taskIndex = bank.Enqueue(ref task);
            ulong queueIndex = ((ulong)bank.BankIndex << 32) | taskIndex;
            _taskLocker.Unlock();

            priorityQueue.Tasks.Enqueue(queueIndex);
        }
    }

    /// <summary>
    /// Pushes a compute-based shader as a task.
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="cmd">The command list to use if excuting a task with <see cref="GpuPriority.Immediate"/>. 
    /// If the task is not executed with immediate priority, the command list parameter is ignored.</param>
    /// <param name="shader">The compute shader to be run inside the task.</param>
    /// <param name="groupsX">The number of X compute thread groups.</param>
    /// <param name="groupsY">The number of Y compute thread groups.</param>
    /// <param name="groupsZ">The number of Z compute thread groups.</param>
    /// <param name="callback">A callback to run once the task is completed.</param>
    public void PushTask(GpuPriority priority, GpuCommandList cmd, Shader shader, uint groupsX, uint groupsY, uint groupsZ, GpuTaskCallback callback = null)
    {
        PushTask(priority, cmd, shader, new Vector3UI(groupsX, groupsY, groupsZ), callback);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="cmd">The command list to use if excuting a task with <see cref="GpuPriority.Immediate"/>. 
    /// If the task is not executed with immediate priority, the command list parameter is ignored.</param>
    /// <param name="shader"></param>
    /// <param name="groups"></param>
    /// <param name="callback"></param>
    public void PushTask(GpuPriority priority, GpuCommandList cmd, Shader shader, Vector3UI groups, GpuTaskCallback callback = null)
    {
        ComputeTask task = new();
        task.Shader = shader;
        task.Groups = groups;
        task.OnCompleted = callback;
        PushTask(priority, ref task, cmd);
    }

    /// <summary>
    /// Gets whether or not the provided <see cref="GpuFormatSupportFlags"/> are supported
    /// by the current <see cref="GpuDevice"/> for the specified <see cref="GpuResourceFormat"/>.
    /// </summary>
    /// <param name="format">The <see cref="GpuResourceFormat"/> to check for support.</param>
    /// <param name="flags">The support flags to be checked.</param>
    /// <returns></returns>
    public bool IsFormatSupported(GpuResourceFormat format, GpuFormatSupportFlags flags)
    {
        if(flags == GpuFormatSupportFlags.None)
            throw new Exception("Cannot check for support with no flags.");

        GpuFormatSupportFlags support = GetFormatSupport(format);
        return (support & flags) == flags;
    }

    public abstract GpuFormatSupportFlags GetFormatSupport(GpuResourceFormat format);

    private void BufferingMode_OnChanged(FrameBufferMode oldValue, FrameBufferMode newValue)
    {
        SettingValue<FrameBufferMode> bufferingMode = Settings.FrameBufferMode;

        // Does the buffer mode exceed the minimum?
        _newFrameBufferSize = bufferingMode.Value switch
        {
            FrameBufferMode.Triple => 3,
            FrameBufferMode.Quad => 4,
            _ => 2,
        };
    }

    protected void InvokeOutputActivated(IDisplayOutput output)
    {
        OnOutputActivated?.Invoke(output);
    }

    protected void InvokeOutputDeactivated(IDisplayOutput output)
    {
        OnOutputDeactivated?.Invoke(output);
    }

    /// <summary>
    /// Activates a <see cref="IDisplayOutput"/> on the current <see cref="GpuDevice"/>.
    /// </summary>
    /// <param name="output">The output to be activated.</param>
    public abstract void AddActiveOutput(IDisplayOutput output);

    /// <summary>
    /// Deactivates a <see cref="IDisplayOutput"/> from the current <see cref="GpuDevice"/>. It will still be listed in <see cref="Outputs"/>, if attached.
    /// </summary>
    /// <param name="output">The output to be deactivated.</param>
    public abstract void RemoveActiveOutput(IDisplayOutput output);

    /// <summary>
    /// Removes all active <see cref="IDisplayOutput"/> from the current <see cref="GpuDevice"/>. They will still be listed in <see cref="Outputs"/>, if attached.
    /// </summary>
    public abstract void RemoveAllActiveOutputs();

    private void DisposeMarkedObjects(uint framesToWait, ulong frameID)
    {
        // Are we disposing before the render thread has started?
        _disposals.ForReverse(1, (index, obj) =>
        {
            ulong age = frameID - obj.ReleaseFrameID;
            if (age >= framesToWait)
            {
                obj.GraphicsRelease();
                _disposals.RemoveAt(index);
            }
        });
    }

    internal void MarkForRelease(GpuObject obj)
    {
        if (IsDisposed)
            throw new ObjectDisposedException("GraphicsDevice has already been disposed, so it cannot mark GraphicsObject instances for release.");

        obj.ReleaseFrameID = Renderer.FrameID;
        _disposals.Add(obj);
    }

    protected override void OnDispose(bool immediate)
    {
        foreach (TaskQueue queue in _taskQueues)
            queue.Cmd.Dispose();

        // Dispose of any registered output services.
        Resources.OutputSurfaces.For(0, (index, surface) =>
        {
            surface.Dispose();
            return false;
        });

        DisposeMarkedObjects(0,0);
    }

    /// <summary>Track a VRAM allocation.</summary>
    /// <param name="bytes">The number of bytes that were allocated.</param>
    public void AllocateVRAM(long bytes)
    {
        Interlocked.Add(ref _allocatedVRAM, bytes);
    }

    /// <summary>Track a VRAM deallocation.</summary>
    /// <param name="bytes">The number of bytes that were deallocated.</param>
    public void DeallocateVRAM(long bytes)
    {
        Interlocked.Add(ref _allocatedVRAM, -bytes);
    }

    private void CheckFrameBufferSize()
    {
        // Do we need to resize the number of buffered frames?
        if (_newFrameBufferSize != FrameBufferSize)
        {
            // Only trigger event if resizing and not initializing. CurrentFrameBufferSize is 0 when uninitialized.
            if (FrameBufferSize > 0)
                OnFrameBufferSizeChanged?.Invoke(FrameBufferSize, _newFrameBufferSize);

            FrameBufferSize = _newFrameBufferSize;
        }
    }

    protected abstract bool OnInitialize();

    /// <summary>Returns a new (or recycled) <see cref="GpuCommandList"/> which can be used to record GPU commands.</summary>
    /// <param name="flags">The flags to apply to the underlying command segment.</param>   
    public abstract GpuCommandList GetCommandList(GpuCommandListFlags flags = GpuCommandListFlags.None);

    /// <summary>
    /// Executes the provided <see cref="GpuCommandList"/> on the current <see cref="GpuCommandQueue"/>.
    /// </summary>
    /// <param name="cmd"></param>
    public abstract void Execute(GpuCommandList cmd);

    /// <summary>
    /// Resets the provided <see cref="GpuCommandList"/> so that it can be re-used.
    /// </summary>
    /// <param name="cmd"></param>
    public abstract void Reset(GpuCommandList cmd);

    /// <summary>
    /// Forces a CPU-side wait on the current thread until the provided <see cref="GpuFence"/> is signaled by the GPU.
    /// </summary>
    /// <param name="fence">The fence to wait on.</param>
    /// <param name="nsTimeout">An optional timeout, in nanoseconds.</param>
    /// <returns></returns>
    public abstract bool Wait(GpuFence fence, ulong nsTimeout = ulong.MaxValue);

    /// <summary>
    /// Processes all tasks held in the manager for the specified priority queue, for the current <see cref="GpuDevice"/>.
    /// </summary>
    /// <param name="priority">The priority of the task.</param>
    /// <param name="endCallback"></param>
    private void ProcessTasks(GpuPriority priority, Action<GpuCommandList> endCallback)
    {
        if (priority == GpuPriority.Immediate)
            throw new InvalidOperationException("Cannot process immediate priority tasks, as these are not queueable.");

        // TODO Implement "AllowBatching" property on RenderTask to allow multiple tasks to be processed in a single Begin()-End() command block
        //      Tasks that don't allow batching will:
        //       - Be executed in individual Begin()-End() command blocks
        //       - Be executed on the next available compute device queue
        //       - May not finish in the order they were requested due to task size, queue size and device performance.

        TaskQueue queue = _taskQueues[(int)priority];
        GpuCommandList cmd = queue.Cmd.Prepare();

        cmd.Begin();
        cmd.BeginEvent($"Process queued '{priority}' tasks");

        while (queue.Tasks.TryDequeue(out ulong queueIndex))
        {
            _taskLocker.Lock();
            uint bankIndex = (uint)(queueIndex >> 32);
            uint taskIndex = (uint)(queueIndex & 0xFFFFFFFF);

            GpuTaskBank bank = _taskBanks[(int)bankIndex];
            bank.Process(cmd, taskIndex);
            _taskLocker.Unlock();
        }

        cmd.EndEvent();

        endCallback?.Invoke(cmd);
        cmd.End();     
        Execute(cmd);
    }

    internal void BeginFrame(uint disposalNumFrames, ulong frameID)
    {
        DisposeMarkedObjects(disposalNumFrames, frameID);
        CheckFrameBufferSize();
        ProcessTasks(GpuPriority.StartOfFrame, (cmd) =>
        {
            OnBeginFrame(cmd, Resources.OutputSurfaces);
        });
    }

    internal void EndFrame(Timing time)
    {
        ProcessTasks(GpuPriority.EndOfFrame, (cmd) =>
        {
            OnEndFrame(cmd, Resources.OutputSurfaces);
        });

        _frameIndex = (_frameIndex + 1U) % FrameBufferSize;
    }

    protected abstract void OnBeginFrame(GpuCommandList cmd, IReadOnlyThreadedList<ISwapChainSurface> surfaces);

    protected abstract void OnEndFrame(GpuCommandList cmd, IReadOnlyThreadedList<ISwapChainSurface> surfaces);

    /// <summary>
    /// Gets the amount of VRAM that has been allocated on the current <see cref="GpuDevice"/>. 
    /// <para>For a software or integration device, this may be system memory (RAM).</para>
    /// </summary>
    internal long AllocatedVRAM => _allocatedVRAM;

    /// <summary>
    /// Gets the <see cref="Logger"/> that is bound to the current <see cref="GpuDevice"/> for outputting information.
    /// </summary>
    public Logger Log { get; }

    /// <summary>
    /// Gets the <see cref="GraphicsSettings"/> bound to the current <see cref="GpuDevice"/>.
    /// </summary>
    public GraphicsSettings Settings { get; }

    /// <summary>
    /// Gets the <see cref="GpuManager"/> that owns the current <see cref="GpuDevice"/>.
    /// </summary>
    public GpuManager Manager { get; }

    /// <summary>
    /// Gets the <see cref="RenderService"/> that created and owns the current <see cref="GpuDevice"/> instance.
    /// </summary>
    public RenderService Renderer { get; }

    /// <summary>Gets the machine-local device ID of the current <see cref="GpuDevice"/>.</summary>
    public abstract DeviceID ID { get; }

    /// <summary>The hardware vendor.</summary>
    public abstract DeviceVendor Vendor { get; }

    /// <summary>
    /// Gets the <see cref="GpuDeviceType"/> of the current <see cref="GpuDevice"/>.
    /// </summary>
    public abstract GpuDeviceType Type { get; }

    /// <summary>Gets a list of all <see cref="IDisplayOutput"/> devices attached to the current <see cref="GpuDevice"/>.</summary>
    public abstract IReadOnlyList<IDisplayOutput> Outputs { get; }

    /// <summary>Gets a list of all active <see cref="IDisplayOutput"/> devices attached to the current <see cref="GpuDevice"/>.
    /// <para>Active outputs are added via <see cref="AddActiveOutput(IDisplayOutput)"/>.</para></summary>
    public abstract IReadOnlyList<IDisplayOutput> ActiveOutputs { get; }

    /// <summary>
    /// Gets the capabilities of the current <see cref="GpuDevice"/>.
    /// </summary>
    public GpuCapabilities Capabilities { get; protected set; }

    /// <summary>
    /// Gets the vertex format cache which stores <see cref="ShaderIOLayout"/> instances to help avoid the need to generate multiple instances of the same formats.
    /// </summary>
    public abstract ShaderLayoutCache LayoutCache { get; }

    /// <summary>
    /// Gets the profiler attached to the current device.
    /// </summary>
    public GraphicsDeviceProfiler Profiler { get; } 

    /// <summary>
    /// Gets the current frame-buffer size. The value will be between 1 and <see cref="GraphicsSettings.FrameBufferMode"/>, from <see cref="Settings"/>.
    /// </summary>
    public uint FrameBufferSize { get; private set; }

    /// <summary>
    /// Gets the current frame buffer image index. The value will be between 0 and <see cref="GraphicsSettings.FrameBufferMode"/> - 1, from <see cref="Settings"/>.
    /// </summary>
    public uint FrameBufferIndex => _frameIndex;

    /// <summary>
    /// Gets the maximum size of a frame's staging buffer, in bytes.
    /// </summary>
    public uint MaxStagingBufferSize => _maxStagingSize;

    /// <summary>
    /// Gets whether or not the current <see cref="GpuDevice"/> is initialized.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the <see cref="ObjectCache"/> that is bound to the current <see cref="GpuDevice"/>.
    /// </summary>
    public ObjectCache Cache { get; }

    /// <summary>
    /// Gets the <see cref="GpuResourceManager"/> implementation for the current <see cref="GpuDevice"/>. 
    /// </summary>
    public abstract GpuResourceManager Resources { get; }
}
