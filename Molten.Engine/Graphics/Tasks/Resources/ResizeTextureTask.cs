namespace Molten.Graphics.DX12;
public struct ResizeTextureTask : IGpuTask<ResizeTextureTask>
{
    public GpuTexture Texture;

    public TextureDimensions NewDimensions;

    public GpuResourceFormat NewFormat;

    public GpuTaskCallback OnCompleted;

    public static bool Process(GpuCommandList cmd, ref ResizeTextureTask t)
    {
        t.Texture.ProcessResize(cmd, ref t);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
