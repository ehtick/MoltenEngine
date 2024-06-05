using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Molten.Graphics;

internal struct BufferSetTask<T> : IGpuTask<BufferSetTask<T>>
    where T : unmanaged
{
    public GpuBuffer Buffer;

    /// <summary>The number of bytes to offset the change.</summary>
    internal ulong ByteOffset;

    /// <summary>The number of elements to be copied.</summary>
    internal ulong ElementCount;

    internal uint DataStartIndex;

    /// <summary>The data to be set.</summary>
    internal T[] Data;

    public GpuTaskCallback OnCompleted;

    public static bool Validate(ref BufferSetTask<T> t) => true;

    public static bool Process(GpuCommandList cmd, ref BufferSetTask<T> t)
    {
        using (GpuStream stream = cmd.MapResource(t.Buffer, 0, t.ByteOffset, GpuMapType.Write))
            stream.WriteRange(t.Data, t.DataStartIndex, t.ElementCount);

        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
