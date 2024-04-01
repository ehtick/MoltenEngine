namespace Molten.Graphics;

public struct TextureResizeTask : IGpuTask<TextureResizeTask>
{
    public GpuTexture Texture;

    public TextureDimensions NewDimensions;

    public GpuResourceFormat NewFormat;

    public GpuTaskCallback OnCompleted;

    public static bool Process(GpuCommandList cmd, ref TextureResizeTask t)
    {
        t.Texture.ResizeTextureImmediate(cmd, t.NewDimensions, t.NewFormat);
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }}
