namespace Molten.Graphics;

internal struct GenerateMipMapsTask : IGpuTask<GenerateMipMapsTask>
{
    public GpuTexture Texture;

    public GpuTaskCallback OnCompleted;

    public static bool Process(GpuCommandList cmd, ref GenerateMipMapsTask t)
    {
        cmd.OnGenerateMipmaps(t.Texture);
        t.Texture.Version++;
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
