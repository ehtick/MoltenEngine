using Molten.Graphics.Textures;

namespace Molten.Graphics;

internal struct BufferGetTask<T> : IGpuTask<BufferGetTask<T>>
    where T : unmanaged
{
    internal GpuBuffer Buffer;

    /// <summary>The number of bytes to offset the change, from the start of the provided <see cref="SrcSegment"/>.</summary>
    internal ulong ByteOffset;

    /// <summary>The number of elements to be copied.</summary>
    internal uint Count;

    /// <summary>The first index at which to start placing the retrieved data within <see cref="DestArray"/>.</summary>
    internal uint DestIndex;

    /// <summary>The destination array to store the retrieved data.</summary>
    internal T[] DestArray;

    /// <summary>
    /// Invoked when data retrieval has been completed.
    /// </summary>
    public event Action<T[]> OnGetData;

    public static bool Process(GpuCommandList cmd, ref BufferGetTask<T> t)
    {
        t.DestArray ??= new T[t.Count];
        ulong actualOffset = t.Buffer.Offset + t.ByteOffset;

        // Now set the structured variable's data
        using (GpuStream stream = cmd.MapResource(t.Buffer, 0, actualOffset, GpuMapType.Read))
            stream.ReadRange(t.DestArray, t.DestIndex, t.Count);

        t.OnGetData?.Invoke(t.DestArray);

        return true;
    }

    public void Complete(bool success) { }
}
