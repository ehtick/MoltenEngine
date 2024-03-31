namespace Molten.Graphics;

public struct ResourceCopyTask : IGpuTask<ResourceCopyTask>
{
    public GpuResource Source;

    public GpuResource Destination;

    public event GpuTaskHandler OnCompleted;

    public static bool Process(GpuCommandList cmd, ref ResourceCopyTask t)
    {
        t.Source.Apply(cmd);
        cmd.CopyResource(t.Source, t.Destination);

        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
