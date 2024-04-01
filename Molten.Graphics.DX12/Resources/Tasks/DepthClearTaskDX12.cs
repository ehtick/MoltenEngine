namespace Molten.Graphics.DX12;

internal struct DepthClearTaskDX12 : IGpuTask<DepthClearTaskDX12>
{
    public DepthSurfaceDX12 Surface;

    public float DepthValue;

    public byte StencilValue;

    public DepthClearFlags ClearFlags;

    public GpuTaskCallback OnCompleted;

    public static bool Process(GpuCommandList cmd, ref DepthClearTaskDX12 t)
    {
        t.Surface.Apply(cmd);
        (cmd as CommandListDX12).Clear(t.Surface, t.DepthValue, t.StencilValue, t.ClearFlags);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
