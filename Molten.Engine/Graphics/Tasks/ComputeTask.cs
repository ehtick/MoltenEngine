namespace Molten.Graphics;

public struct ComputeTask : IGpuTask<ComputeTask>
{
    internal Shader Shader;

    internal Vector3UI Groups;

    public event GpuTaskHandler<ComputeTask> OnCompleted;

    public static bool Process(GpuCommandList cmd, ref ComputeTask t)
    {
        cmd.Dispatch(t.Shader, t.Groups);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(ref this, success);
    }
}
