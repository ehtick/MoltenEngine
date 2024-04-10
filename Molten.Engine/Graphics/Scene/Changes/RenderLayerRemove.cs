namespace Molten.Graphics;

/// <summary>A <see cref="RenderLayerRemove"/> for removing <see cref="LayerRenderData"/> fron a <see cref="SceneRenderData"/> instance.</summary>
internal struct RenderLayerRemove : IGpuTask<RenderLayerRemove>
{
    public SceneRenderData SceneData;

    public LayerRenderData LayerData;

    public GpuTaskCallback OnCompleted;

    public static bool Validate(ref RenderLayerRemove t) => true;

    public static bool Process(GpuCommandList cmd, ref RenderLayerRemove t)
    {
        t.SceneData.Layers.Remove(t.LayerData);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
