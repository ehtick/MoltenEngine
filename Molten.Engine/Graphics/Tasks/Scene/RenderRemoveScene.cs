namespace Molten.Graphics;

/// <summary>A <see cref="RenderRemoveScene"/> for adding a <see cref="SceneObject"/> to the root of a scene.</summary>
internal struct RenderRemoveScene : IGpuTask<RenderRemoveScene>
{
    public SceneRenderData Data;

    public static bool Process(GpuCommandList cmd, ref RenderRemoveScene t)
    {
        cmd.Device.Renderer.Scenes.Remove(t.Data);
        return true;
    }

    public void Complete(bool success) { }
}
