using Molten.Collections;

namespace Molten.Graphics;

public class GpuDiscardBuffer<T> : GpuObject
    where T : unmanaged
{

    class BufferPair
    {
        internal GpuBuffer Buffer;

        internal GpuBuffer Ring;

        internal ulong NextOffset = 0;
    }

    class Frame : GpuObject
    {
        internal List<BufferPair> Pairs = new();

        internal Frame(GpuDevice device) : base(device) { }

        protected override void OnGraphicsRelease()
        {
            for (int i = Pairs.Count - 1; i >= 0; i--)
                Pairs[i].Buffer.Dispose(true);
        }
    }

    GpuFrameBuffer<Frame> _frames;
    ulong _maxAllocationSize;
    Frame _curFrame;
    uint _stride;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="device"></param>
    /// <param name="bufferType"></param>
    /// <param name="flags"></param>
    /// <param name="format"></param>
    /// <param name="blockCapacity">The capacity of each ring buffer within the discard buffer, in bytes.</param>
    /// <exception cref="NotImplementedException"></exception>
    internal unsafe GpuDiscardBuffer(GpuDevice device, 
        GpuBufferType bufferType, 
        GpuResourceFlags flags, 
        GpuResourceFormat format,
        ulong blockCapacity) : 
        base(device)
    {
        BufferType = bufferType;
        Flags = flags;
        Format = format;
        _stride = (uint)sizeof(T);
        _maxAllocationSize = Math.Min(blockCapacity, (1024 * 1024) * 128); // 128 MB - TODO: Get limit from hardware capabilities

        throw new NotImplementedException("Intialize buffer with the provided initial capacity");
    }

    private void Allocate()
    {
        BufferPair pair = new BufferPair();

        uint numElements = (uint)(_maxAllocationSize / _stride);
        pair.Buffer = Device.Resources.CreateBuffer<T>(BufferType, Flags, numElements, Format, 1);
        pair.Ring = pair.Buffer.Allocate<T>(0, 1, Flags, BufferType, 1);

        _curFrame.Pairs.Add(pair);
    }

    public GpuBuffer Get(uint numElements, uint alignment)
    {
        ulong neededBytes = numElements * _stride;
        if (neededBytes > _maxAllocationSize)
            throw new InvalidOperationException($"Requested buffer size exceeds the maximum allocation size of {_maxAllocationSize:N0} bytes.");

        BufferPair pair = null;

        // Check all existing buffer pairs for required capacity.
        for (int i = 0; i < _curFrame.Pairs.Count; i++)
        {
            pair = _curFrame.Pairs[i];
            ulong newOffset = EngineUtil.Align(pair.NextOffset, alignment);

            // Check if we've hit the last ring buffer and need to allocate a new one.
            if (!pair.Ring.SetLocation(newOffset, neededBytes) && i == (_curFrame.Pairs.Count - 1))
            {
                Allocate();
                continue;
            }

            pair.NextOffset = newOffset + neededBytes;
        }

        return pair.Ring;
    }

    internal void Prepare()
    {
        Frame frame = _frames.Prepare();

        if (_curFrame != frame)
        {
            _curFrame = frame;

            if (_curFrame.Pairs.Count == 0)
                Allocate();

            for (int i = 0; i < frame.Pairs.Count; i++)
                frame.Pairs[i].NextOffset = 0;
        }
    }

    protected override void OnGraphicsRelease()
    {
        _frames.Dispose(true);
    }

    public GpuBufferType BufferType { get; }

    public GpuResourceFlags Flags { get; }

    public GpuResourceFormat Format { get; }
}
