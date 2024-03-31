namespace Molten.Graphics;

public struct SubResourceCopyTask : IGpuTask<SubResourceCopyTask>
{
    public GpuResource Source;

    public ResourceRegion? SrcRegion;

    public uint SrcSubResource;

    /// <summary>The start offset within the resource.
    /// <para>For a buffer, only the X dimension needs to be set equal to the number of bytes to offset.</para>
    /// <para>For textures, this will vary depending on the number of texture dimensions.</para></summary>
    public Vector3UI DestStart;

    public GpuResource Destination;

    public uint DestSubResource;

    public event GpuTaskHandler OnCompleted;

    public static bool Process(GpuCommandList cmd, ref SubResourceCopyTask t)
    {
        if (!t.Destination.Flags.IsGpuWritable())
            throw new ResourceCopyException(t.Source, t.Destination, "The destination resource must have GPU write access for writing the copied data.");

        if (t.Source is GpuBuffer buffer && buffer.BufferType == GpuBufferType.Staging)
            t.Source.Apply(cmd);

        cmd.CopyResourceRegion(t.Source, t.SrcSubResource, t.SrcRegion, t.Destination, t.DestSubResource, t.DestStart);
        cmd.Profiler.SubResourceCopyCalls++;

        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
