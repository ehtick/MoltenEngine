using Molten.Graphics.DX12;
using Molten.Graphics.Textures;

namespace Molten.Graphics;

/// <summary>
/// A delegate for texture event handlers.
/// </summary>
/// <param name="texture">The texture instance that triggered the event.</param>
public delegate void TextureHandler<T>(T texture) where T : ITexture;

public abstract class GpuTexture : GpuResource, ITexture
{
    TextureDimensions _dimensions;
    TextureDimensions _pendingDimensions;
    GpuResourceFormat _format;

    /// <summary>
    /// Invoked when the current <see cref="GpuTexture"/> is resized.
    /// </summary>
    public event TextureHandler<ITexture> OnResize;

    /// <summary>
    /// Creates a new instance of <see cref="GpuTexture"/>.
    /// </summary>
    /// <param name="device">The <see cref="GpuTexture"/> that the buffer is bound to.</param>
    /// <param name="dimensions">The dimensions of the texture.</param>
    /// <param name="format">The <see cref="GpuResourceFormat"/> of the texture.</param>
    /// <param name="flags">Resource flags which define how the texture can be used.</param>
    /// <param name="name">The name of the texture. This is mainly used for debug purposes.</param>
    /// <exception cref="ArgumentException"></exception>
    protected GpuTexture(GpuDevice device, ref TextureDimensions dimensions, GpuResourceFormat format, GpuResourceFlags flags, string name)
        : base(device, flags)
    {
        if(dimensions.IsCubeMap && dimensions.ArraySize % 6 != 0)
            throw new ArgumentException("The array size of a cube map must be a multiple of 6.", nameof(dimensions.ArraySize));

        LastFrameResizedID = device.Renderer.FrameID;
        ValidateFlags();

        MSAASupport msaaSupport = MSAASupport.NotSupported; // TODO re-support. _renderer.Device.Features.GetMSAASupport(format, aaLevel);
        _dimensions = dimensions;

        Name = string.IsNullOrWhiteSpace(name) ? $"{GetType().Name}_{Width}x{Height}" : name;

        MultiSampleLevel = dimensions.MultiSampleLevel > AntiAliasLevel.Invalid ? dimensions.MultiSampleLevel : AntiAliasLevel.None;
        SampleQuality = msaaSupport != MSAASupport.NotSupported ? dimensions.SampleQuality : MSAAQuality.Default;
        ResourceFormat = format;
    }

    private void ValidateFlags()
    {
        // Validate RT mip-maps
        if (Flags.Has(GpuResourceFlags.MipMapGeneration))
        {
            if (Flags.Has(GpuResourceFlags.DenyShaderAccess) || !(this is IRenderSurface2D))
                throw new GpuResourceException(this, "Mip-map generation is only available on render-surface shader resources.");
        }

        // Only staging resources have CPU-write access.
        if (Flags.Has(GpuResourceFlags.UploadMemory))
        {
            if (!Flags.Has(GpuResourceFlags.DenyShaderAccess))
                throw new GpuResourceException(this, "Staging textures cannot allow shader access. Add GraphicsResourceFlags.NoShaderAccess flag.");
        }
    }

    public void Resize(GpuPriority priority, GpuCommandList cmd,
    uint newWidth,
    uint newHeight,
    uint newMipMapCount = 0,
    uint newArraySizeOrDepth = 0,
    GpuResourceFormat newFormat = GpuResourceFormat.Unknown,
    GpuTaskCallback completeCallback = null)
    {
        TextureDimensions dim = Dimensions;
        dim.Width = newWidth;
        dim.Height = newHeight;
        dim.MipMapCount = newMipMapCount;

        if (this is ITexture3D)
        {
            dim.Depth = newArraySizeOrDepth > 0 ? newArraySizeOrDepth : Depth;
            dim.ArraySize = 1;
        }
        else
        {
            dim.ArraySize = newArraySizeOrDepth > 0 ? newArraySizeOrDepth : ArraySize;
            dim.Depth = 1;
        }

        if (dim != _pendingDimensions)
        {
            _pendingDimensions = dim;

            ResourceFormat = newFormat == GpuResourceFormat.Unknown ? ResourceFormat : newFormat;
            ResizeTextureTask task = new ResizeTextureTask()
            {
                Texture = this,
                NewDimensions = dim,
                NewFormat = newFormat,
                OnCompleted = completeCallback
            };
            Device.PushTask(priority, ref task, cmd);
        }
    }

    protected internal abstract void ProcessResize(GpuCommandList cmd, ref ResizeTextureTask t);

    protected void InvokeOnResize()
    {
        OnResize?.Invoke(this);
    }

    /// <summary>Copies data fom the provided <see cref="TextureData"/> instance into the current texture.</summary>
    /// <param name="priority">The priority of the operation.</param>
    /// <param name="cmd">The command list used when executing the operation immediately. Can be null if not using <see cref="GpuPriority.Immediate"/>.</param>
    /// <param name="data"></param>
    /// <param name="levelStartIndex">The starting mip-map index within the provided <see cref="TextureData"/>.</param>
    /// <param name="arrayStartIndex">The starting array slice index within the provided <see cref="TextureData"/>.</param>
    /// <param name="levelCount">The number of mip-map levels to copy per array slice, from the provided <see cref="TextureData"/>.</param>
    /// <param name="arrayCount">The number of array slices to copy from the provided <see cref="TextureData"/>.</param>
    /// <param name="destLevelIndex">The mip-map index within the current texture to start copying to.</param>
    /// <param name="destArrayIndex">The array slice index within the current texture to start copying to.</param>
    /// <param name="completeCallback">A callback to invoke once the data has been transferred to the GPU.</param>
    public unsafe void SetData(GpuPriority priority, GpuCommandList cmd, TextureData data, uint levelStartIndex = 0, uint arrayStartIndex = 0,
        uint levelCount = 0, uint arrayCount = 0,
        uint destLevelIndex = 0, uint destArrayIndex = 0,
        GpuTaskCallback completeCallback = null)
    {
        TextureSetDataTask task = new();
        task.Data = data;
        task.Texture = this;
        task.LevelStartIndex = levelStartIndex;
        task.ArrayStartIndex = arrayStartIndex;
        task.LevelCount = levelCount;
        task.ArrayCount = arrayCount;
        task.DestLevelIndex = destLevelIndex;
        task.DestArrayIndex = destArrayIndex;
        task.OnCompleted += completeCallback;

        Device.PushTask(priority, ref task, cmd);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="priority">The priority of the operation.</param>
    /// <param name="cmd"></param>
    /// <param name="data"></param>
    /// <param name="mipIndex"></param>
    /// <param name="arraySlice"></param>
    /// <param name="completeCallback">A callback to invoke once the data has been transferred to the GPU.</param>
    public unsafe void SetSubResourceData(GpuPriority priority, GpuCommandList cmd, TextureSlice data, uint mipIndex, uint arraySlice, GpuTaskCallback completeCallback = null)
    {
        bool immediate = priority == GpuPriority.Immediate;
        TextureSetSubResourceTask<byte> task = new(data.Data, 1, 0, data.TotalBytes, immediate);
        task.Pitch = data.Pitch;
        task.Texture = this;
        task.ArrayIndex = arraySlice;
        task.MipLevel = mipIndex;
        task.OnCompleted = completeCallback;

        Device.PushTask(priority, ref task, cmd);
    }

    public unsafe void SetSubResourceData<T>(GpuPriority priority, GpuCommandList cmd, uint level, T[] data, uint startIndex, uint count, uint pitch, uint arrayIndex,
        GpuTaskCallback completeCallback = null)
        where T : unmanaged
    {
        bool immediate = priority == GpuPriority.Immediate;

        fixed (T* ptrData = data)
        {
            TextureSetSubResourceTask<T> task = new(ptrData, (uint)sizeof(T), startIndex, count, immediate)
            {
                Pitch = pitch,
                ArrayIndex = arrayIndex,
                MipLevel = level,
                Texture = this,
                OnCompleted = completeCallback,
            };

            Device.PushTask(priority, ref task, cmd);
        }
    }

    public unsafe void SetSubResourceData<T>(GpuPriority priority, GpuCommandList cmd, ResourceRegion area, T[] data, uint bytesPerPixel, uint level, uint arrayIndex = 0,
        GpuTaskCallback completeCallback = null)
        where T : unmanaged
    {
        fixed (T* ptrData = data)
            SetSubResourceData(priority, cmd, area, ptrData, (uint)data.Length, bytesPerPixel, level, arrayIndex, completeCallback);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The type of data to be sent to the GPU texture.</typeparam>
    /// <param name="priority">The priority of the operation.</param>
    /// <param name="cmd"></param>
    /// <param name="region"></param>
    /// <param name="data"></param>
    /// <param name="numElements"></param>
    /// <param name="bytesPerPixel"></param>
    /// <param name="level"></param>
    /// <param name="arrayIndex"></param>
    /// <param name="completeCallback">A callback to invoke once the resize operation has been completed.</param>
    /// <exception cref="Exception"></exception>
    public unsafe void SetSubResourceData<T>(GpuPriority priority, GpuCommandList cmd, ResourceRegion region, T* data,
        uint numElements, uint bytesPerPixel, uint level, uint arrayIndex = 0,
        GpuTaskCallback completeCallback = null)
        where T : unmanaged
    {
        uint texturePitch = region.Width * bytesPerPixel;
        uint pixels = region.Width * region.Height;
        uint expectedBytes = pixels * bytesPerPixel;
        uint dataBytes = (uint)(numElements * sizeof(T));

        if (pixels != numElements)
            throw new Exception($"The provided data does not match the provided area of {region.Width}x{region.Height}. Expected {expectedBytes} bytes. {dataBytes} bytes were provided.");

        // Do a bounds check
        ResourceRegion texBounds = new ResourceRegion(0, 0, 0, Width, Height, Depth);
        if (!texBounds.Contains(region))
            throw new Exception("The provided area would go outside of the current texture's bounds.");

        bool immediate = priority == GpuPriority.Immediate;
        TextureSetSubResourceTask<T> task = new(data, (uint)sizeof(T), 0, numElements, immediate);
        task.Texture = this;
        task.Pitch = texturePitch;
        task.StartIndex = 0;
        task.ArrayIndex = arrayIndex;
        task.MipLevel = level;
        task.Region = region;
        task.OnCompleted += completeCallback;
        Device.PushTask(priority, ref task, cmd);
    }

    public unsafe void SetSubResourceData<T>(GpuPriority priority, GpuCommandList cmd, uint level, T* data, uint startIndex, uint count, uint pitch, uint arrayIndex = 0,
        GpuTaskCallback completeCallback = null)
        where T : unmanaged
    {
        bool immediate = priority == GpuPriority.Immediate;
        TextureSetSubResourceTask<T> task = new(data, (uint)sizeof(T), startIndex, count, immediate);
        task.Pitch = pitch;
        task.Texture = this;
        task.ArrayIndex = arrayIndex;
        task.MipLevel = level;
        task.OnCompleted += completeCallback;
        Device.PushTask(priority, ref task, cmd);
    }

    /// <inheritdoc/>
    public void GetData(GpuPriority priority, GpuCommandList cmd, Action<TextureData> callback)
    {
        TextureGetDataTask task = new();
        task.OnGetData = callback;
        Device.PushTask( priority, ref task, cmd);
    }

    public void GetSubResourceData(GpuPriority priority, GpuCommandList cmd, uint mipLevel, uint arrayIndex, Action<TextureSlice> callback)
    {
        TextureGetSliceTask task = new();
        task.OnGetData = callback;
        task.Texture = this;
        task.MipMapLevel = mipLevel;
        task.ArrayIndex = arrayIndex;
        Device.PushTask(priority, ref task, cmd);
    }

    /// <summary>Generates mip maps for the texture via the current <see cref="GpuTexture"/>, if allowed.</summary>
    /// <param name="priority">The priority of the copy operation.</param>
    /// <param name="cmd">The command list that will execute the operation.</param>
    /// <param name="callback">A callback to run once the operation has completed.</param>
    public void GenerateMipMaps(GpuPriority priority, GpuCommandList cmd, GpuTaskCallback callback = null)
    {
        if (!Flags.Has(GpuResourceFlags.MipMapGeneration))
            throw new Exception("Cannot generate mip-maps for texture. Must have flag: TextureFlags.AllowMipMapGeneration.");

        GenerateMipMapsTask task = new();
        task.Texture = this;
        task.OnCompleted += callback;
        Device.PushTask(priority, ref task, cmd);
    }

    /// <summary>Gets whether or not the texture is using a supported block-compressed format.</summary>
    public bool IsBlockCompressed { get; protected set; }

    /// <summary>Gets the width of the texture.</summary>
    public uint Width => _dimensions.Width;

    /// <summary>Gets the height of the texture.</summary>
    public uint Height => _dimensions.Height;

    /// <summary>Gets the depth of the texture. For a 3D texture this is the number of slices.</summary>
    public uint Depth => _dimensions.Depth;

    /// <summary>Gets the number of mip map levels in the texture.</summary>
    public uint MipMapCount => _dimensions.MipMapCount;

    /// <summary>Gets the number of array slices in the texture. For a cube-map, this value will a multiple of 6. For example, a cube map with 2 array elements will have 12 array slices.</summary>
    public uint ArraySize => _dimensions.ArraySize;

    /// <summary>
    /// Gets the dimensions of the texture.
    /// </summary>
    public TextureDimensions Dimensions
    {
        get => _dimensions;
        protected set => _dimensions = value;
    }

    /// <summary>
    /// Gets the pending dimensions of the texture. This is the dimensions that the texture should be resized to.
    /// </summary>
    public ref readonly TextureDimensions PendingDimensions => ref _pendingDimensions;

    /// <inheritdoc/>
    public override ulong SizeInBytes { get; protected set; }

    /// <summary>
    /// Gets the number of samples used when sampling the texture. Anything greater than 1 is considered as multi-sampled. 
    /// </summary>
    public AntiAliasLevel MultiSampleLevel { get; protected set; }

    /// <summary>
    /// Gets whether or not the texture is multisampled. This is true if <see cref="MultiSampleLevel"/> is at least <see cref="AntiAliasLevel.X2"/>.
    /// </summary>
    public bool IsMultisampled => MultiSampleLevel >= AntiAliasLevel.X2;

    /// <inheritdoc/>
    public MSAAQuality SampleQuality { get; protected set; }

    /// <inheritdoc/>
    public override GpuResourceFormat ResourceFormat
    {
        get => _format;
        protected set
        {
            if (_format != value)
            {
                _format = value;
                IsBlockCompressed = BCHelper.GetBlockCompressed(_format);

                if (IsBlockCompressed)
                    SizeInBytes = BCHelper.GetBCSize(_format, Width, Height, MipMapCount) * ArraySize;
                else
                    SizeInBytes = (ResourceFormat.BytesPerPixel() * (Width * Height)) * ArraySize;
            }
        }
    }

    /// <summary>
    /// Gets the ID of the frame that the current <see cref="GpuTexture"/> was resized. 
    /// If the texture was never resized then the frame ID will be the ID of the frame that the texture was created.
    /// </summary>
    public ulong LastFrameResizedID { get; internal set; }
}
