
namespace Molten.Graphics;

public class SurfaceTracker : IDisposable
{
    class FrameBufferSurface : GpuObject
    {
        public IRenderSurface2D Surface;

        public FrameBufferSurface(GpuDevice device) : base(device) { }

        protected override void OnGpuRelease()
        {
            Surface?.Dispose();
        }
    }

    SurfaceSizeMode _mode;
    Dictionary<AntiAliasLevel, GpuFrameBuffer<FrameBufferSurface>> _surfaces;

    GpuDevice _device;
    uint _width;
    uint _height;
    GpuResourceFormat _format;
    string _name;

    internal SurfaceTracker(GpuDevice device,
        AntiAliasLevel[] aaLevels,
        uint width,
        uint height,
        GpuResourceFormat format,
        string name,
        SurfaceSizeMode mode = SurfaceSizeMode.Full)
    {
        _device = device;
        _width = width;
        _height = height;
        _format = format;
        _name = name;
        _mode = mode;
        _surfaces = new Dictionary<AntiAliasLevel, GpuFrameBuffer<FrameBufferSurface>>();
    }

    internal void RefreshSize(uint minWidth, uint minHeight)
    {
        _width = minWidth;
        _height = minHeight;

        switch (_mode)
        {
            case SurfaceSizeMode.Full:
                foreach (IRenderSurface2D rs in _surfaces.Values)
                    rs?.Resize(GpuPriority.StartOfFrame, null, minWidth, minHeight);
                break;

            case SurfaceSizeMode.Half:
                foreach (IRenderSurface2D rs in _surfaces.Values)
                    rs?.Resize(GpuPriority.StartOfFrame, null, (minWidth / 2) + 1, (minHeight / 2) + 1);
                break;
        }
    }

    public void Dispose()
    {
        foreach (IRenderSurface2D rs in _surfaces.Values)
            rs.Dispose();
    }

    internal IRenderSurface2D this[AntiAliasLevel aaLevel]
    {
        get
        {
            if (!_surfaces.TryGetValue(aaLevel, out GpuFrameBuffer<FrameBufferSurface> fb))
            {
                fb = new GpuFrameBuffer<FrameBufferSurface>(_device, (device) =>
                {
                    return new FrameBufferSurface(device)
                    {
                        Surface = _device.Resources.CreateSurface(_width, _height, _format, aaLevel: aaLevel, name: $"{_name}_{aaLevel}aa"),
                    };
                });

                _surfaces[aaLevel] = fb;
            }

            FrameBufferSurface fbs = fb.Prepare();
            IRenderSurface2D rs = fbs.Surface;

            if (rs.Width != _width || rs.Height != _height)
                rs.Resize(GpuPriority.StartOfFrame, null, _width, _height);

            return rs;
        }
    }
}