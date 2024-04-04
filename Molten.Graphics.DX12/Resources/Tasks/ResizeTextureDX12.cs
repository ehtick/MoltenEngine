namespace Molten.Graphics.DX12;
internal struct ResizeTextureDX12 : IGpuTask<ResizeTextureDX12>
{
    internal TextureDX12 Texture;

    internal TextureDimensions NewDimensions;

    internal GpuResourceFormat NewFormat;

    internal GpuTaskCallback OnCompleted;

    public static bool Process(GpuCommandList cmd, ref ResizeTextureDX12 t)
    {
        t.Texture.ProcessResize(cmd, ref t);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
