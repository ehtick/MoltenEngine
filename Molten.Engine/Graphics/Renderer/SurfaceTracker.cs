
namespace Molten.Graphics;

public abstract class SurfaceTracker<T> : IDisposable
    where T : ITexture2D
{
    class FrameBufferSurface : GpuObject
    {
        public T Surface;

        public FrameBufferSurface(GpuDevice device) : base(device) { }

        protected override void OnGpuRelease()
        {
            Surface?.Dispose();
        }
    }

    Dictionary<AntiAliasLevel, GpuFrameBuffer<FrameBufferSurface>> _surfaces;

    uint _width;
    uint _height;

    internal SurfaceTracker(GpuDevice device,
        uint width,
        uint height,
        string name,
        SurfaceSizeMode mode = SurfaceSizeMode.Full)
    {
        Device = device;
        _width = width;
        _height = height;
        Name = name;
        Mode = mode;
        _surfaces = new Dictionary<AntiAliasLevel, GpuFrameBuffer<FrameBufferSurface>>();
    }

    internal void Prepare(GpuCommandList cmd, uint targetWidth, uint targetHeight)
    {
        if (_width == targetWidth && _height == targetHeight)
            return;

        _width = targetWidth;
        _height = targetHeight;

        // Reduce target surface dimensions by half if required.
        if (Mode == SurfaceSizeMode.Half)
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
        foreach (GpuFrameBuffer<FrameBufferSurface> fb in _surfaces.Values)
            fb.Dispose(true);
    }

    protected abstract T Create(AntiAliasLevel aaLevel, uint width, uint height);

    internal T this[AntiAliasLevel aaLevel]
    {
        get
        {
            if (!_surfaces.TryGetValue(aaLevel, out GpuFrameBuffer<FrameBufferSurface> fb))
            {
                fb = new GpuFrameBuffer<FrameBufferSurface>(Device, (device) =>
                {
                    return new FrameBufferSurface(device)
                    {
                        Surface = Create(aaLevel, _width, _height)
                    };
                });

                _surfaces[aaLevel] = fb;
            }

            FrameBufferSurface fbs = fb.Prepare();
            T surface = fbs.Surface;

            if (surface.PendingDimensions.Width != _width || surface.PendingDimensions.Height != _height)
                surface.Resize(GpuPriority.StartOfFrame, null, _width, _height);

            return surface;
        }
    }

    internal SurfaceSizeMode Mode { get; }

    internal GpuDevice Device { get; }

    internal string Name { get; }
}

public class SurfaceTracker : SurfaceTracker<IRenderSurface2D>
{
    GpuResourceFormat _format;

    public SurfaceTracker(GpuDevice device, uint width, uint height, GpuResourceFormat format, string name, SurfaceSizeMode mode = SurfaceSizeMode.Full) : 
        base(device, width, height, name, mode)
    {
        _format = format;
    }

    protected override IRenderSurface2D Create(AntiAliasLevel aaLevel, uint width, uint height)
    {
        return Device.Resources.CreateSurface(width, height, _format, aaLevel: aaLevel, name: $"{Name}_{aaLevel}aa");
    }
}