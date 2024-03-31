namespace Molten.Graphics;

/// <summary>A task for adding <see cref="SceneRenderData"/> to the renderer.</summary>
internal struct RenderAddScene : IGpuTask<RenderAddScene>
{
    public SceneRenderData Data;

    public static bool Process(GpuCommandList cmd, ref RenderAddScene t)
    {
        cmd.Device.Renderer.Scenes.Add(t.Data);
        return true;
    }

    public void Complete(bool success) { }
}
