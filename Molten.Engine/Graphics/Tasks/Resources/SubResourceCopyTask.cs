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

    public event GpuTaskCallback OnCompleted;

    public static bool Validate(ref SubResourceCopyTask t) => true;

    public static bool Process(GpuCommandList cmd, ref SubResourceCopyTask t)
    {
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
