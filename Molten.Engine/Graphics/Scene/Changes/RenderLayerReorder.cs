namespace Molten.Graphics;

/// <summary>A <see cref="RenderLayerReorder"/> for changing the draw order of a <see cref="LayerRenderData"/> instance.</summary>
internal struct RenderLayerReorder : IGpuTask<RenderLayerReorder>
{
    public LayerRenderData LayerData;
    public SceneRenderData SceneData;
    public ReorderMode Mode;

    public GpuTaskCallback OnCompleted;

    public static bool Process(GpuCommandList cmd, ref RenderLayerReorder t)
    {
        int indexOf = t.SceneData.Layers.IndexOf(t.LayerData);
        if (indexOf > -1)
        {
            t.SceneData.Layers.RemoveAt(indexOf);

            switch (t.Mode)
            {
                case ReorderMode.PushBackward:
                    t.SceneData.Layers.Insert(Math.Max(0, indexOf - 1), t.LayerData);
                    break;

                case ReorderMode.BringToFront:
                    t.SceneData.Layers.Add(t.LayerData);
                    break;

                case ReorderMode.PushForward:
                    if (indexOf + 1 < t.SceneData.Layers.Count)
                        t.SceneData.Layers.Insert(indexOf + 1, t.LayerData);
                    else
                        t.SceneData.Layers.Add(t.LayerData);
                    break;

                case ReorderMode.SendToBack:
                    t.SceneData.Layers.Insert(0, t.LayerData);
                    break;
            }
        }

        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
