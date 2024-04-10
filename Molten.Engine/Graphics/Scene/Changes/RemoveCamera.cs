namespace Molten.Graphics;

/// <summary>A task for removing a <see cref="RenderCamera"/> from a scene.</summary>
internal struct RemoveCamera : IGpuTask<RemoveCamera>
{
    public RenderCamera Camera;
    public SceneRenderData Data;

    public GpuTaskCallback OnCompleted;

    public static bool Validate(ref RemoveCamera t) => true;

    public static bool Process(GpuCommandList cmd, ref RemoveCamera t)
    {
        t.Data.Cameras.Remove(t.Camera);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
