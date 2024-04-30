using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;

public unsafe class RenderSurface2DDX12 : Texture2DDX12, IRenderSurface2D
{
    internal RenderSurface2DDX12(
        DeviceDX12 device,
        uint width,
        uint height,
        GpuResourceFlags flags = GpuResourceFlags.None,
        GpuResourceFormat format = GpuResourceFormat.R8G8B8A8_SNorm,
        uint mipCount = 1,
        uint arraySize = 1,
        AntiAliasLevel aaLevel = AntiAliasLevel.None,
        MSAAQuality msaa = MSAAQuality.Default, 
        string name = null,
        ProtectedSessionDX12 protectedSession = null)
        : base(device, width, height, flags, format, mipCount, arraySize, aaLevel, msaa, name, protectedSession)
    {
        Viewport = new ViewportF(0, 0, width, height);
        Name = $"Surface_{name ?? GetType().Name}";
    }

    protected override void OnCreateTexture(ref ID3D12Resource1** ptrResources, ref uint numResources)
    {
        Viewport = new ViewportF(Viewport.X, Viewport.Y, Desc.Width, Desc.Height);
        base.OnCreateTexture(ref ptrResources, ref numResources);        
    }

    protected override unsafe void OnCreateHandle(ref ID3D12Resource1* ptr, ref ResourceHandleDX12 handle)
    {
        RenderTargetViewDesc desc = new RenderTargetViewDesc()
        {
            Format = Desc.Format,
            ViewDimension = RtvDimension.Texture2Darray,
            Texture2DArray = new Tex2DArrayRtv()
            {
                ArraySize = Desc.DepthOrArraySize,
                MipSlice = 0,
                FirstArraySlice = 0,
                PlaneSlice = 0,
            },
        };

        RTHandleDX12 rtHandle = new RTHandleDX12(this, ptr);
        rtHandle.RTV.Initialize(ref desc);
        handle = rtHandle;
    }

    public void Clear(GpuPriority priority, GpuCommandList cmd, Color color)
    {
        SurfaceClearTaskDX12 task = new();
        task.Surface = this;
        task.Color = color;
        Device.PushTask(priority, ref task, cmd);
    }

    /// <summary>Gets the viewport that defines the default renderable area of the render target.</summary>
    public ViewportF Viewport { get; protected set; }
}
