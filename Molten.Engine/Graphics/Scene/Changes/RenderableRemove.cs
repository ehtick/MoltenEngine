namespace Molten.Graphics;

/// <summary>A task for removing a <see cref="Renderable"/> from the root of a scene.</summary>
internal struct RenderableRemove : IGpuTask<RenderableRemove>
{
    public Renderable Renderable;

    public ObjectRenderData Data;

    public LayerRenderData LayerData;

    public GpuTaskCallback OnCompleted;

    public static bool Process(GpuCommandList cmd, ref RenderableRemove t)
    {
        RenderDataBatch batch;
        if (t.LayerData.Renderables.TryGetValue(t.Renderable, out batch))
            batch.Remove(t.Data);
        else
            return false;

        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
