namespace Molten.Graphics;

/// <summary>Represents a 1D texture, while also acting as the base for all other texture implementations.</summary>
/// <seealso cref="IDisposable" />
public interface ITexture : IGpuResource
{
    /// <summary>
    /// Invoked when the current <see cref="ITexture"/> is resized.
    /// </summary>
    event TextureHandler<ITexture> OnResize;

    /// <summary>
    /// Retrieves the data which makes up the entire texture across all mip-map levels and array slices. The data is returned in a single <see cref="TextureData"/> object.
    /// </summary>
    /// <param name="priority">The priority of the operation.</param>
    /// <param name="completeCallback">A callback to invoke once the GPU has completed the data transfer.</param>
    void GetData(GpuPriority priority, GpuCommandList cmd, Action<TextureData> completeCallback);

    /// <summary>
    /// Transfers thte provided <see cref="TextureData"/> to the current <see cref="ITexture"/> and invokes the provided completion callback once the operation is complete.
    /// </summary>
    /// <param name="priority">The priority of the operation.</param>
    /// <param name="cmd">The command list that sohuld perform the operation.</param>
    /// <param name="data">The <see cref="TextureData"/> to be copied.</param>
    /// <param name="levelStartIndex">The first mip-map level to start copying from.</param>
    /// <param name="arrayStartIndex">The first array slice index to start copying from.</param>
    /// <param name="levelCount">The number of mip-map levels to be copied.</param>
    /// <param name="arrayCount">The number of array slices to be copied.</param>
    /// <param name="destLevelIndex">The destination mip-map level to start copying to.</param>
    /// <param name="destArrayIndex">The destination array slice index to start copying to.</param>
    /// <param name="completeCallback">A callback to invoke once the GPU has completed the data transfer.</param>
    void SetData(GpuPriority priority, GpuCommandList cmd, TextureData data, uint levelStartIndex = 0, uint arrayStartIndex = 0,
        uint levelCount = 0, uint arrayCount = 0,
        uint destLevelIndex = 0, uint destArrayIndex = 0,
        GpuTaskCallback completeCallback = null);

    void SetSubResourceData(GpuPriority priority, GpuCommandList cmd, TextureSlice data, uint mipIndex, uint arraySlice, GpuTaskCallback completeCallback = null);

    void SetSubResourceData<T>(GpuPriority priority, GpuCommandList cmd, uint level, T[] data, uint startIndex, uint count, uint pitch, uint arrayIndex = 0,
        GpuTaskCallback completeCallback = null) where T : unmanaged;

    void SetSubResourceData<T>(GpuPriority priority, GpuCommandList cmd, ResourceRegion area, T[] data, uint bytesPerPixel, uint level, uint arrayIndex = 0,
        GpuTaskCallback completeCallback = null) where T : unmanaged;

    unsafe void SetSubResourceData<T>(GpuPriority priority, GpuCommandList cmd, ResourceRegion region, T* data,
        uint numElements, uint bytesPerPixel, uint level, uint arrayIndex = 0,
        GpuTaskCallback completeCallback = null) where T : unmanaged;

    /// <summary>Generates mip maps for the texture via the current <see cref="GpuTexture"/>, if allowed.</summary>
    /// <param name="priority">The priority of the copy operation.</param>
    /// <param name="cmd">The command list that will execute the operation.</param>
    /// <param name="callback">A callback to run once the operation has completed.</param>
    void GenerateMipMaps(GpuPriority priority, GpuCommandList cmd, GpuTaskCallback callback = null);

    /// <summary>Gets the width of the texture.</summary>
    uint Width { get; }

    /// <summary>Gets the height of the texture.</summary>
    uint Height { get; }

    /// <summary>Gets the depth of the texture.</summary>
    uint Depth { get; }

    /// <summary>Gets whether or not the texture is using a supported block-compressed format.</summary>
    bool IsBlockCompressed { get; }

    /// <summary>Gets the number of mip map levels in the texture.</summary>
    uint MipMapCount { get; }

    /// <summary>Gets the number of array slices in the texture.</summary>
    uint ArraySize { get; }

    /// <summary>
    /// Gets the number of samples used when sampling the texture. Anything greater than 1 is considered as multi-sampled. 
    /// </summary>
    AntiAliasLevel MultiSampleLevel { get; }

    /// <summary>
    /// Gets the MSAA sample quality level. This is only valid if <see cref="MultiSampleLevel"/> is higher than <see cref="AntiAliasLevel.None"/>.
    /// </summary>
    MSAAQuality SampleQuality { get; }

    /// <summary>
    /// Gets whether or not the texture is multisampled. This is true if <see cref="MultiSampleLevel"/> is higher than <see cref="AntiAliasLevel.None"/>.
    /// </summary>
    bool IsMultisampled { get; }

    /// <summary>
    /// Gets the dimensions of the texture.
    /// </summary>
    TextureDimensions Dimensions { get; }
}
