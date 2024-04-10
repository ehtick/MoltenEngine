namespace Molten.Graphics;

internal struct ComputeTask : IGpuTask<ComputeTask>
{
    internal Shader Shader;

    internal Vector3UI Groups;

    public GpuTaskCallback OnCompleted;

    public static bool Validate(ref ComputeTask t) => true;

    public static bool Process(GpuCommandList cmd, ref ComputeTask t)
    {
        cmd.Dispatch(t.Shader, t.Groups);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
