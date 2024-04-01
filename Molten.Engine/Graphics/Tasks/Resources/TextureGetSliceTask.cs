namespace Molten.Graphics;

internal struct TextureGetSliceTask : IGpuTask<TextureGetSliceTask>
{
    public GpuTexture Texture;

    public Action<TextureSlice> OnGetData;

    public uint MipMapLevel;

    public uint ArrayIndex;

    public GpuMapType MapType;

    public static bool Process(GpuCommandList cmd, ref TextureGetSliceTask t)
    {
        TextureSlice slice = TextureSlice.FromTextureSlice(cmd, t.Texture, t.MipMapLevel, t.ArrayIndex, t.MapType);

        // Return resulting data
        t.OnGetData?.Invoke(slice);
        return true;
    }

    public void Complete(bool success) { }
}
