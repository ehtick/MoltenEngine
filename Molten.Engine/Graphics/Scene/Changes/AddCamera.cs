namespace Molten.Graphics;

/// <summary>A task for adding a <see cref="RenderCamera"/> to a scene.</summary>
internal struct AddCamera : IGpuTask<AddCamera>
{
    public RenderCamera Camera;
    public SceneRenderData Data;

    public GpuTaskCallback OnCompleted;

    public static bool Validate(ref AddCamera t) => true;

    public static bool Process(GpuCommandList cmd, ref AddCamera t)
    {
        t.Data.Cameras.Add(t.Camera);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
