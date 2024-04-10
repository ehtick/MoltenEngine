namespace Molten.Graphics;

/// <summary>A <see cref="RenderLayerAdd"/> for adding <see cref="LayerRenderData"/> to the a<see cref="SceneRenderData"/> instance.</summary>
internal struct RenderLayerAdd : IGpuTask<RenderLayerAdd>
{
    public SceneRenderData SceneData;

    public LayerRenderData LayerData;

    public GpuTaskCallback OnCompleted;

    public static bool Validate(ref RenderLayerAdd t) => true;

    public static bool Process(GpuCommandList cmd, ref RenderLayerAdd t)
    {
        t.SceneData.Layers.Add(t.LayerData);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
