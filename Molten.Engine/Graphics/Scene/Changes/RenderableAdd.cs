namespace Molten.Graphics;

/// <summary>A task for adding a <see cref="Renderable"/> to the root of a scene.</summary>
internal struct RenderableAdd : IGpuTask<RenderableAdd>
{
    public Renderable Renderable;

    public ObjectRenderData Data;

    public LayerRenderData LayerData;

    public GpuTaskCallback OnCompleted;

    public static bool Process(GpuCommandList cmd, ref RenderableAdd t)
    {
        RenderDataBatch batch;
        if (!t.LayerData.Renderables.TryGetValue(t.Renderable, out batch))
        {
            batch = new RenderDataBatch();
            t.LayerData.Renderables.Add(t.Renderable, batch);
        }

        batch.Add(t.Data);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
