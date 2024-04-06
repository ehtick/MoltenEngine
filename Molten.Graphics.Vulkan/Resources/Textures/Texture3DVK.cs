using Silk.NET.Vulkan;

namespace Molten.Graphics.Vulkan;

public class Texture3DVK : TextureVK, ITexture3D
{
    public Texture3DVK(DeviceVK device,
        TextureDimensions dimensions, GpuResourceFormat format, GpuResourceFlags flags, string name) :
        base(device, dimensions, format, flags, name)
    { }

    protected override void SetCreateInfo(DeviceVK device, ref ImageCreateInfo imgInfo, ref ImageViewCreateInfo viewInfo)
    {
        imgInfo.ImageType = ImageType.Type3D;
        viewInfo.ViewType = ImageViewType.Type3D;
    }
}
