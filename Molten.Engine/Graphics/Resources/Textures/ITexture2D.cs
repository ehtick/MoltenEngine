namespace Molten.Graphics;

/// <summary>
/// Represents the implementation of a 2D texture.
/// </summary>
public interface ITexture2D : ITexture
{
    /// <summary>
    /// Changes the current texture's dimensions and format.
    /// </summary>
    /// <param name="priority">The priority of the resize operation.</param>
    /// <param name="cmd">The command list that should execute the operation.</param>
    /// <param name="newWidth">The new width.</param>
    /// <param name="newHeight">The new height.</param>
    /// <param name="newMipMapCount">The new mip-map level count.</param>
    /// <param name="newArraySize">The new array size.</param>
    /// <param name="newFormat">The new graphics format.</param>
    /// <param name="completeCallback"></param>
    void Resize(GpuPriority priority, GpuCommandList cmd,
        uint newWidth, 
        uint newHeight,
        uint newMipMapCount = 0, 
        uint newArraySize = 0, 
        GpuResourceFormat newFormat = GpuResourceFormat.Unknown,
        GpuTaskCallback completeCallback = null);
}
