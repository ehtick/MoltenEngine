namespace Molten.Graphics.DX12;

internal struct SurfaceClearTaskDX12 : IGpuTask<SurfaceClearTaskDX12>
{
    public TextureDX12 Surface;

    public Color Color;

    public GpuTaskCallback OnCompleted;

    public static bool Validate(ref SurfaceClearTaskDX12 t) => true;

    public static bool Process(GpuCommandList cmd, ref SurfaceClearTaskDX12 t)
    {
        t.Surface.Apply(cmd);
        (cmd as CommandListDX12).ClearDSV(t.Surface, t.Color);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
