using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Molten.Graphics;

internal struct BufferSetTask<T> : IGpuTask<BufferSetTask<T>>
    where T : unmanaged
{
    public GpuBuffer Buffer;

    /// <summary>The number of bytes to offset the change, from the start of the provided <see cref="Segment"/>.</summary>
    internal ulong ByteOffset;

    /// <summary>The number of elements to be copied.</summary>
    internal ulong ElementCount;

    internal uint DataStartIndex;

    /// <summary>The data to be set.</summary>
    internal T[] Data;

    public event GpuTaskHandler OnCompleted;

    public static bool Process(GpuCommandList cmd, ref BufferSetTask<T> t)
    {
        ulong actualOffset = t.Buffer.Offset + t.ByteOffset;

        using (GpuStream stream = cmd.MapResource(t.Buffer, 0, actualOffset, GpuMapType.Write))
            stream.WriteRange(t.Data, t.DataStartIndex, t.ElementCount);

        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
