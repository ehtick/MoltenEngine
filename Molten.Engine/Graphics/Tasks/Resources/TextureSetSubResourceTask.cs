using Molten.Graphics.Textures;

namespace Molten.Graphics;

public struct TextureSetSubResourceTask<T> : IGpuTask<TextureSetSubResourceTask<T>>
    where T: unmanaged
{
    public GpuTexture Texture;

    public uint MipLevel;

    public unsafe byte* Data;

    public uint StartIndex;

    public uint Pitch;

    public uint ArrayIndex;

    public uint NumElements { get; private set; }

    public uint NumBytes { get; private set; }

    public uint Stride { get; private set; }

    bool _immediate;

    public ResourceRegion? Region;

    public GpuTaskCallback OnCompleted;

    internal unsafe TextureSetSubResourceTask(void* data, uint stride, uint startIndex, uint numElements, bool immediate)
    {
        Stride = stride;
        NumElements = numElements;
        NumBytes = Stride * NumElements;
        _immediate= immediate;

        if (immediate)
        {
            Data = (byte*)EngineUtil.Alloc(NumBytes);
            void* ptrStart = (byte*)data + (startIndex * stride);
            Buffer.MemoryCopy(ptrStart, Data, NumBytes, NumBytes);
        }
        else
        {
            Data = (byte*)data + (startIndex * stride);
        }
    }


    public static bool Validate(ref TextureSetSubResourceTask<T> t)
    {
        if (t.Texture.IsBlockCompressed)
        {
            if (t.Region != null)
                throw new NotImplementedException("Region-based SetData on block-compressed texture is currently unsupported!");
        }

        return true;
    }

    public unsafe static bool Process(GpuCommandList cmd, ref TextureSetSubResourceTask<T> t)
    {
        // Calculate size of a single array slice
        uint arraySliceBytes = 0;
        uint blockSize = 8; // default block size
        GpuTexture tex = t.Texture;
        uint levelWidth = tex.Width;
        uint levelHeight = tex.Height;
        uint levelDepth = tex.Depth;

        if (tex.IsBlockCompressed)
        {
            blockSize = BCHelper.GetBlockSize(tex.ResourceFormat);

            // Collect total level size.
            for (uint i = 0; i < tex.MipMapCount; i++)
            {
                arraySliceBytes += BCHelper.GetBCLevelSize(levelWidth, levelHeight, blockSize) * levelDepth;
                levelWidth = Math.Max(1, levelWidth / 2);
                levelHeight = Math.Max(1, levelHeight / 2);
                levelDepth = Math.Max(1, levelDepth / 2);
            }
        }
        else
        {
            // TODO: This is invalid if the format isn't 32bpp/4-bytes-per-pixel/RGBA.
            for (uint i = 0; i < tex.MipMapCount; i++)
            {
                arraySliceBytes += (levelWidth * levelHeight * 4) * levelDepth; //4 color channels. 1 byte each. Width * height * colorByteSize.
                levelWidth = Math.Max(1, levelWidth / 2);
                levelHeight = Math.Max(1, levelHeight / 2);
                levelDepth = Math.Max(1, levelDepth / 2);
            }
        }

        //======DATA TRANSFER===========
        ulong stride = (ulong)sizeof(T);
        ulong startBytes = t.StartIndex * stride;
        ulong numBytes = t.NumElements * stride;
        byte* ptrData = t.Data;
        ptrData += startBytes;

        uint subResourceIndex = (tex.MipMapCount * t.ArrayIndex) + t.MipLevel;

        if (tex.Flags.Has(GpuResourceFlags.UploadMemory))
        {
            using (GpuStream stream = cmd.MapResource(tex, subResourceIndex, 0, GpuMapType.Write))
            {
                // Are we constrained to an area of the texture?
                if (t.Region != null)
                {
                    ResourceRegion area = t.Region.Value;
                    ulong areaPitch = stride * area.Width;
                    ulong sliceBytes = areaPitch * area.Height;
                    uint aX = area.Left;
                    uint aY = area.Top;
                    uint aZ = area.Front;

                    for (uint y = aY, end = area.Bottom; y < end; y++)
                    {
                        stream.Position = (long)((sliceBytes * aZ) + (t.Pitch * aY) + (aX * stride));
                        stream.WriteRange(ptrData, areaPitch);
                        ptrData += areaPitch;
                        aY++;
                    }
                }
                else
                {
                    stream.WriteRange(ptrData, numBytes);
                }
            }

            cmd.Profiler.ResourceMapCalls++;
        }
        else
        {
            if (tex.IsBlockCompressed)
            {
                // Calculate mip-map level size.
                levelWidth = Math.Max(1, tex.Width >> (int)t.MipLevel);
                uint bcPitch = BCHelper.GetBCPitch(levelWidth, blockSize);


                GpuBuffer stagingBuffer = cmd.Device.UploadBuffer.Get<byte>(arraySliceBytes);

                // TODO support copy flags (DX11.1 feature)
                cmd.UpdateResource(stagingBuffer, subResourceIndex, null, ptrData, bcPitch, arraySliceBytes);
            }
            else
            {
                if (t.Region != null)
                {
                    ulong regionPitch = stride * t.Region.Value.Width;
                    GpuBuffer stagingBuffer = cmd.Device.UploadBuffer.Get<byte>(numBytes);     
                    cmd.UpdateResource(stagingBuffer, 0, t.Region.Value, ptrData, regionPitch, numBytes);
                }
                else
                {
                    GpuBuffer stagingBuffer = cmd.Device.UploadBuffer.Get<byte>(arraySliceBytes);
                    cmd.UpdateResource(stagingBuffer, subResourceIndex, null, ptrData, t.Pitch, arraySliceBytes);
                }
            }
        }

        tex.Version++;
        return true;
    }

    public unsafe void Complete(bool success)
    {
        OnCompleted?.Invoke(success);

        if (!_immediate)
            EngineUtil.Free(ref Data);
    }
}
