
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

    internal void Prepare(GpuCommandList cmd, uint targetWidth, uint targetHeight)
    {
        if (_width == targetWidth && _height == targetHeight)
            return;

        _width = targetWidth;
        _height = targetHeight;

        // Reduce target surface dimensions by half if required.
        if (_mode == SurfaceSizeMode.Half)
        {
            targetWidth = (targetWidth / 2) + 1;
            targetHeight = (targetHeight / 2) + 1;
        }

        // Resize each surface for the current frame.
        foreach (GpuFrameBuffer<FrameBufferSurface> frameBuffer in _surfaces.Values)
        {
            FrameBufferSurface fbs = frameBuffer.Prepare();
            fbs.Surface?.Resize(GpuPriority.Immediate, cmd, targetWidth, targetHeight);
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