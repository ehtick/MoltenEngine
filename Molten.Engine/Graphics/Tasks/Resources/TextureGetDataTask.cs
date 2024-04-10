using Molten.Graphics.Textures;

namespace Molten.Graphics;

internal struct TextureGetDataTask : IGpuTask<TextureGetDataTask>
{
    public GpuTexture Texture;

    public Action<TextureData> OnGetData;

    public GpuMapType MapType;

    public static bool Validate(ref TextureGetDataTask t) => true;

    public static bool Process(GpuCommandList cmd, ref TextureGetDataTask t)
    {
        GpuTexture tex = t.Texture;
        TextureData data = new TextureData(tex.Width, tex.Height, tex.Depth, tex.MipMapCount, tex.ArraySize)
        {
            Flags = tex.Flags,
            Format = tex.ResourceFormat,
            HighestMipMap = 0,
            IsCompressed = tex.IsBlockCompressed,
        };

        uint blockSize = BCHelper.GetBlockSize(tex.ResourceFormat);
        uint expectedRowPitch = 4 * tex.Width; // 4-bytes per pixel * Width.
        uint expectedSlicePitch = expectedRowPitch * tex.Height;

        // Iterate over each array slice.
        for (uint a = 0; a < tex.ArraySize; a++)
        {
            // Iterate over all mip-map levels of the array slice.
            for (uint i = 0; i < tex.MipMapCount; i++)
            {
                uint subID = (a * tex.MipMapCount) + i;
                data.Levels[subID] = TextureSlice.FromTextureSlice(cmd, tex, i, a, t.MapType);
            }
        }

        // TODO Perform CPU sync here.
        t.OnGetData?.Invoke(data);
        return true;
    }

    public void Complete(bool success) { }
}
