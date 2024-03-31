namespace Molten.Graphics;
internal struct TextureSetDataTask : IGpuTask<TextureSetDataTask>
{
    internal GpuTexture Resource;
    internal TextureData Data;
    internal uint DestArrayIndex;
    internal uint DestLevelIndex;
    internal uint ArrayCount;
    internal uint LevelCount;
    internal uint ArrayStartIndex;
    internal uint LevelStartIndex;

    public event GpuTaskHandler OnCompleted;

    public unsafe static bool Process(GpuCommandList cmd, ref TextureSetDataTask t)
    {
        TextureSlice level;

        TextureSetSubResourceTask<byte> subTask = new();
        subTask.Resource = t.Resource;
        subTask.StartIndex = 0;

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
                
                subTask.ArrayIndex = destArray;
                subTask.MipLevel = destLevel;
                subTask.InitializeImmediate(level.Data, 1, 0, level.TotalBytes);
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
