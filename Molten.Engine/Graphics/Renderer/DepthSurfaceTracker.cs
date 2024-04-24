namespace Molten.Graphics;

internal class DepthSurfaceTracker : SurfaceTracker<IDepthStencilSurface>
{
    DepthFormat _format;

    internal DepthSurfaceTracker(
        GpuDevice device,
        uint width,
        uint height,
        DepthFormat format,
        SurfaceSizeMode mode = SurfaceSizeMode.Full) : base(device, width, height, "depth", mode)
    {
        _format = format;
    }

    protected override IDepthStencilSurface Create(AntiAliasLevel aaLevel, uint width, uint height)
    {
        IDepthStencilSurface ds = Device.Resources.CreateDepthSurface(width, height, _format, aaLevel:aaLevel, name:$"{Name}_{aaLevel}aa");
        return ds;
    }
}
