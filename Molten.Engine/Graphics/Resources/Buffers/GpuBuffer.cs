namespace Molten.Graphics;

public abstract class GpuBuffer : GpuResource
{
    List<GpuBuffer> _allocations;

    /// <summary>
    /// Creates a new instance of <see cref="GpuBuffer"/>.
    /// </summary>
    /// <param name="device">The <see cref="GpuDevice"/> that the buffer is bound to.</param>
    /// <param name="stride">The number of bytes per buffer element, in bytes.</param>
    /// <param name="numElements">The number of elements in the buffer.</param>
    /// <param name="flags">Resource flags which define how the buffer can be used.</param>
    /// <param name="type">The type of buffer.</param>
    /// <param name="alignment">The alignment of the buffer, in bytes.</param>
    protected GpuBuffer(GpuDevice device, uint stride, ulong numElements, GpuResourceFlags flags, GpuBufferType type, uint alignment) :
        base(device, flags)
    {
        _allocations = new List<GpuBuffer>();
        ResourceFormat = GpuResourceFormat.Unknown;
        BufferType = type;
        Stride = stride;
        ElementCount = numElements;
        SizeInBytes = stride * numElements;
        Alignment = alignment;
    }

    /// <summary>
    /// Allocates a <see cref="GpuBuffer"/> as a sub-buffer within the current <see cref="GpuBuffer"/>.
    /// </summary>
    /// <param name="offsetBytes">The number of bytes from the start of the buffer to start the sub-buffer.</param>
    /// <param name="stride">The stride of the data to be contained within the sub-buffer</param>
    /// <param name="numElements">The number of elements to be contained within the sub-buffer</param>
    /// <param name="alignment">The alignment of the sub-buffer data.</param>
    /// <param name="flags">The resource flags of the sub-buffer.</param>
    /// <param name="type">The type of the sub-buffer.</param>
    /// <returns></returns>
    public GpuBuffer Allocate(ulong offsetBytes, uint stride, ulong numElements, GpuResourceFlags flags, GpuBufferType type, uint alignment = 1)
    {
        GpuBuffer subBuffer = OnAllocateSubBuffer(offsetBytes, stride, numElements, flags, type, alignment);
        _allocations.Add(subBuffer);
        return subBuffer;
    }

    public unsafe GpuBuffer Allocate<T>(ulong offsetBytes, ulong numElements, GpuResourceFlags flags, GpuBufferType type, uint alignment = 1)
        where T : unmanaged
    {
        return Allocate(offsetBytes, (uint)sizeof(T), numElements, flags, type, alignment);
    }

    /// <summary>Re-locates the current <see cref="GpuBuffer"/> to another location within it's parent buffer by offsetting it by the provided number of bytes.</summary>
    /// <param name="deltaBytes">The number of bytes to offset the current <see cref="GpuBuffer"/>.</param>
    public bool Seek(long deltaBytes)
    {
        if(ParentBuffer == null)
            throw new InvalidOperationException("Cannot seek a buffer that has no parent buffer.");

        return SetLocation((ulong)((long)Offset + deltaBytes), SizeInBytes);
    }

    public unsafe bool Seek<T>(uint numElements)
        where T : unmanaged
    {
       return Seek(numElements * sizeof(T));
    }

    /// <summary>
    /// Sets data on a <see cref="GpuBuffer"/> based on the given <see cref="GpuPriority"/>.
    /// </summary>
    /// <typeparam name="T">The type of data to be set.</typeparam>
    /// <param name="priority"></param>
    /// <param name="cmd"></param>
    /// <param name="data"></param>
    /// <param name="completeCallback"></param>
    public void SetData<T>(GpuPriority priority, GpuCommandList cmd, T[] data, GpuTaskCallback completeCallback = null)
        where T : unmanaged
    {
        SetData(priority, cmd, data, 0, (uint)data.Length, 0, completeCallback);
    }

    /// <summary>
    /// Sets data on a <see cref="GpuBuffer"/> based on the given <see cref="GpuPriority"/>.
    /// </summary>
    /// <typeparam name="T">The type of data to be set.</typeparam>
    /// <param name="priority"></param>
    /// <param name="cmd"></param>
    /// <param name="data"></param>
    /// <param name="startIndex">The start index within <paramref name="data"/> to copy.</param>
    /// <param name="elementCount"></param>
    /// <param name="byteOffset">The start location within the buffer to start copying from, in bytes.</param>
    /// <param name="completeCallback"></param>
    public void SetData<T>(GpuPriority priority, GpuCommandList cmd, T[] data, ulong startIndex, ulong elementCount, uint byteOffset = 0, 
        GpuTaskCallback completeCallback = null)
        where T : unmanaged
    {
        if (!Flags.Has(GpuResourceFlags.UploadMemory))
            throw new Exception("Cannot set data on a non-writable buffer.");

        BufferSetTask<T> task = new();
        task.ByteOffset = byteOffset;
        task.ElementCount = elementCount;
        task.Buffer = this;
        task.OnCompleted += completeCallback;

        // If executing immediately, we don't need to keep a copy of the data.
        if (priority == GpuPriority.Immediate)
        {
            task.Data = data;
            task.DataStartIndex = (uint)startIndex;
        }
        else
        {
            // Only copy the part we need from the source data, starting from startIndex.
            task.Data = new T[data.Length];
            task.DataStartIndex = 0;
            Array.Copy(data, (long)startIndex, task.Data, 0, (long)elementCount);
        }

        Device.Tasks.Push(priority, ref task, cmd);
    }

    /// <summary>Retrieves data from a <see cref="GpuBuffer"/>.</summary>
    /// <param name="priority">The priority of the operation</param>
    /// <param name="cmd"></param>
    /// <param name="destination">The destination array. Must be big enough to contain the retrieved data.</param>
    /// <param name="startIndex">The start index within the destination array at which to place the retrieved data.</param>
    /// <param name="count">The number of elements to retrieve</param>
    /// <param name="byteOffset">The start location within the buffer to start copying from, in bytes.</param>
    /// <param name="completionCallback">A callback to run once the operation is completed.</param>
    public void GetData<T>(GpuPriority priority, GpuCommandList cmd, T[] destination, uint startIndex, uint count, ulong byteOffset, Action<T[]> completionCallback = null)
        where T : unmanaged
    {
        if (!Flags.Has(GpuResourceFlags.DownloadMemory))
            throw new GpuResourceException(this, "Cannot use GetData() on a non-readable buffer.");

        if (destination.Length < count)
            throw new ArgumentException("The provided destination array is not large enough.");

        BufferGetTask<T> task = new();
        task.ByteOffset = byteOffset;
        task.Count = count;
        task.DestArray = destination;
        task.DestIndex = startIndex;
        task.OnGetData += completionCallback;
        task.Buffer = this;
        Device.Tasks.Push(priority, ref task, cmd);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="stride"></param>
    /// <param name="numElements"></param>
    /// <param name="flags"></param>
    /// <param name="type"></param>
    /// <param name="alignment"></param>
    /// <returns></returns>
    protected abstract GpuBuffer OnAllocateSubBuffer(ulong offset, uint stride, ulong numElements, GpuResourceFlags flags, GpuBufferType type, uint alignment);

    /// <summary>
    /// Re-points the current <see cref="GpuBuffer"/> to another location within its underlying GPU buffer resource.
    /// </summary>
    /// <param name="offset">The number of bytes from the start of the underlying GPU buffer.</param>
    /// <param name="numBytes">The number of bytes required.</param>
    /// <returns>True if the update succeeded or false if the location or capacity could not be updated. e.g. Due to reaching or exceeding the end of the buffer.</returns>
    public abstract bool SetLocation(ulong offset, ulong numBytes);

    protected override void OnGraphicsRelease()
    {
        for (int i = _allocations.Count - 1; i >= 0; i--)
            _allocations[i].Dispose(true);

        ParentBuffer?._allocations.Remove(this);
    }

    /// <summary>
    /// Gets the stride (byte size) of each element within the current <see cref="GpuBuffer"/>.
    /// </summary>
    public uint Stride { get; }

    /// <summary>
    /// Gets the number of elements that the current <see cref="GpuBuffer"/> can store.
    /// </summary>
    public ulong ElementCount { get; }

    /// <summary>
    /// Gets the total size of the buffer, in bytes.
    /// </summary>
    public override ulong SizeInBytes { get; protected set; }

    /// <summary>
    /// Gets the type of the current <see cref="GpuBuffer"/>.
    /// </summary>
    public GpuBufferType BufferType { get; }

    /// <summary>
    /// Gets the vertex input layout of the current <see cref="GpuBuffer"/>, if any.
    /// <para>This property is only set if the current <see cref="BufferType"/> is <see cref="GpuBufferType.Vertex"/>.</para>
    /// </summary>
    public ShaderIOLayout VertexLayout { get; internal set; }

    /// <summary>
    /// Gets the offset of the current <see cref="GpuBuffer"/> within its parent <see cref="GpuBuffer"/>.
    /// <para>If the buffer has no parent, this value should always be 0.</para>
    /// </summary>
    public ulong Offset { get; protected set; }

    /// <summary>
    /// Gets the expected alignment of the current <see cref="GpuBuffer"/>.
    /// </summary>
    public uint Alignment { get; private set; }

    /// <summary>
    /// Gets the parent <see cref="GpuBuffer"/> of the current <see cref="GpuBuffer"/>, if any.
    /// </summary>
    public GpuBuffer ParentBuffer { get; protected set; }

    /// <summary>
    /// Gets a list of all sub-allocated <see cref="GpuBuffer"/> that were allocated by the current <see cref="GpuBuffer"/>.
    /// </summary>
    internal IReadOnlyList<GpuBuffer> Allocations => _allocations;

    /// <summary>
    /// Gets the constant/uninform bindings for the current <see cref="GpuBuffer"/>. 
    /// If the <see cref="BufferType"/> is not <see cref="GpuBufferType.Constant"/>, this value will be null.
    /// </summary>
    public GpuConstantData ConstantData { get; init; }
}
