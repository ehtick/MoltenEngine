namespace Molten.Graphics;

internal struct BufferGetStreamTask : IGpuTask<BufferGetStreamTask>
{
    internal GpuBuffer Buffer;

    internal uint ByteOffset;

    internal GpuMapType MapType;

    internal GpuBuffer Staging;

    /// <summary>A callback to interact with the retrieved stream.</summary>
    internal event Action<GpuBuffer, GpuStream> OnStreamOpened;

    public static bool Validate(ref BufferGetStreamTask t) => true;

    public static bool Process(GpuCommandList cmd, ref BufferGetStreamTask t)
    {
        using (GpuStream stream = cmd.MapResource(t.Buffer, 0, t.ByteOffset, t.MapType))
            t.OnStreamOpened?.Invoke(t.Buffer, stream);

        return true;
    }

    public void Complete(bool success) { }
}
