namespace Molten.Graphics;
internal struct TextureSetDataTask : IGpuTask<TextureSetDataTask>
{
    internal GpuTexture Texture;
    internal TextureData Data;
    internal uint DestArrayIndex;
    internal uint DestLevelIndex;
    internal uint ArrayCount;
    internal uint LevelCount;
    internal uint ArrayStartIndex;
    internal uint LevelStartIndex;

    public event GpuTaskCallback OnCompleted;


    public static bool Validate(ref TextureSetDataTask t)
    {
        return true;
    }

    public unsafe static bool Process(GpuCommandList cmd, ref TextureSetDataTask t)
    {
        TextureSlice level;

        for (uint a = 0; a < t.ArrayCount; a++)
        {
            for (uint m = 0; m < t.LevelCount; m++)
            {
                uint slice = t.ArrayStartIndex + a;
                uint mip = t.LevelStartIndex + m;
                uint dataID = t.Data.GetLevelID(mip, slice);
                level = t.Data.Levels[dataID];

                if (level.TotalBytes == 0)
                    continue;

                uint destArray = t.DestArrayIndex + a;
                uint destLevel = t.DestLevelIndex + m;

                TextureSetSubResourceTask<byte> subTask = new(level.Data, 1, 0, level.TotalBytes, true);
                subTask.Texture = t.Texture;
                subTask.StartIndex = 0;
                subTask.ArrayIndex = destArray;
                subTask.MipLevel = destLevel;
                subTask.Pitch = level.Pitch;
                TextureSetSubResourceTask<byte>.Process(cmd, ref subTask);
            }
        }

        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
