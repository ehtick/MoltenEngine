using Android.OS;
using Android.Views;

namespace Molten.Graphics;

// TODO inherit from an OpenGL-based texture
public class AndroidViewSurface : INativeSurface
{
    public event TextureHandler<INativeSurface> OnHandleChanged;

    public event TextureHandler<INativeSurface> OnParentChanged;

    public event TextureHandler<INativeSurface> OnClose;

    public event TextureHandler<INativeSurface> OnMinimize;

    public event TextureHandler<INativeSurface> OnRestore;

    public event TextureHandler<INativeSurface> OnFocusGained;

    public event TextureHandler<INativeSurface> OnFocusLost;

    public event TextureHandler<INativeSurface> OnMaximize;

    public event TextureHandler<INativeSurface> OnResize;

    public View TargetView { get; private set; }

    public IMoltenAndroidActivity TargetActivity { get; private set; }

    ViewportF _vp;

    public AndroidViewSurface(IMoltenAndroidActivity activity)
    {
        TargetView = activity.TargetView;
        TargetActivity = activity;

        CalculateViewport();
        activity.OnTargetViewChanged += Activity_OnTargetViewChanged;
    }

    event TextureHandler<ISwapChainSurface> ISwapChainSurface.OnResize
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

    private void CalculateViewport()
    {
        if(TargetView != null && TargetView.Width > 0 && TargetView.Height > 0)
        {
            // TODO correctly calculate this. The View may not be located at 0,0 within it's parent.
            _vp = new ViewportF()
            {
                X = 0,
                Y = 0,
                Width = TargetView.Width,
                Height = TargetView.Height
            };
        }
        else
        {
            _vp = new ViewportF();

            // GetRealSize() was defined in JellyBeanMr1 / API 17 / Android 4.2
            if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBeanMr1)
            {
                _vp.Width = TargetActivity.UnderlyingActivity.Resources.DisplayMetrics.WidthPixels;
                _vp.Height = TargetActivity.UnderlyingActivity.Resources.DisplayMetrics.HeightPixels;
            }
            else
            {
                Android.Graphics.Point p = new Android.Graphics.Point();
                TargetActivity.UnderlyingActivity.WindowManager.DefaultDisplay.GetRealSize(p);
                _vp.Width = p.X;
                _vp.Height = p.Y;
            }
        }
    }

    private void Activity_OnTargetViewChanged(View o)
    {
        TargetView = o;
        CalculateViewport();
        OnHandleChanged?.Invoke(this);
    }

    public void Dispatch(Action callback)
    {
        throw new NotImplementedException();
    }

    public void Apply(GpuCommandList cmd)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Close()
    {
        throw new NotImplementedException();
    }

    public void Resize(GpuPriority priority, uint newWidth, uint newHeight)
    {
        throw new NotImplementedException();
    }

    public void Resize(GpuPriority priority, uint newWidth, uint newHeight, uint newMipMapCount = 0, uint newArraySize = 0, GpuResourceFormat newFormat = GpuResourceFormat.Unknown)
    {
        throw new NotImplementedException();
    }

    public void Clear(GpuPriority priority, Color color)
    {
        throw new NotImplementedException();
    }

    public void ClearImmediate(GpuCommandList cmd, Color color)
    {
        throw new NotImplementedException();
    }

    public void GetData(GpuPriority priority, GpuCommandList cmd, Action<TextureData> completeCallback)
    {
        throw new NotImplementedException();
    }

    public void SetData(GpuPriority priority, GpuCommandList cmd, TextureData data, uint levelStartIndex = 0, uint arrayStartIndex = 0, uint levelCount = 0, uint arrayCount = 0, uint destLevelIndex = 0, uint destArrayIndex = 0, GpuTaskCallback completeCallback = null)
    {
        throw new NotImplementedException();
    }

    public void SetSubResourceData(GpuPriority priority, GpuCommandList cmd, TextureSlice data, uint mipIndex, uint arraySlice, GpuTaskCallback completeCallback = null)
    {
        throw new NotImplementedException();
    }

    public void SetSubResourceData<T>(GpuPriority priority, GpuCommandList cmd, uint level, T[] data, uint startIndex, uint count, uint pitch, uint arrayIndex = 0, GpuTaskCallback completeCallback = null) where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public void SetSubResourceData<T>(GpuPriority priority, GpuCommandList cmd, ResourceRegion area, T[] data, uint bytesPerPixel, uint level, uint arrayIndex = 0, GpuTaskCallback completeCallback = null) where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public unsafe void SetSubResourceData<T>(GpuPriority priority, GpuCommandList cmd, ResourceRegion region, T* data, uint numElements, uint bytesPerPixel, uint level, uint arrayIndex = 0, GpuTaskCallback completeCallback = null) where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public void CopyTo(GpuPriority priority, GpuResource destination, GpuTaskCallback completeCallback = null)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(GpuPriority priority, uint sourceLevel, uint sourceSlice, GpuResource destination, uint destLevel, uint destSlice, GpuTaskCallback completeCallback = null)
    {
        throw new NotImplementedException();
    }

    public string Title
    {
        get => TargetActivity.UnderlyingActivity.Title;
        set
        {
            TargetActivity.UnderlyingActivity.Title = value;
            TargetActivity.UnderlyingActivity.Window?.SetTitle(value);
        }
    }

    public bool IsFocused
    {
        get => TargetActivity.UnderlyingActivity.HasWindowFocus;
    }

    public WindowMode Mode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IntPtr? ParentHandle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IntPtr? WindowHandle => throw new NotImplementedException();

    public Rectangle RenderBounds => throw new NotImplementedException();

    public bool IsVisible
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public GpuResourceFormat DataFormat => throw new NotImplementedException();

    public bool IsBlockCompressed => throw new NotImplementedException();

    public uint Width => (uint)_vp.Width;

    public uint Height => (uint)_vp.Height;

    public uint Depth => 1;

    public uint MipMapCount => throw new NotImplementedException();

    public uint ArraySize => throw new NotImplementedException();

    public AntiAliasLevel MultiSampleLevel => throw new NotImplementedException();

    public bool IsMultisampled => throw new NotImplementedException();

    public object Tag { get; set; }

    public RenderService Renderer => throw new NotImplementedException();

    public ViewportF Viewport => _vp;

    public GpuDevice Device => throw new NotImplementedException();

    public uint Version { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public uint LastUsedFrameID => throw new NotImplementedException();

    public GpuResourceFlags Flags => throw new NotImplementedException();

    public GpuResourceFormat ResourceFormat => throw new NotImplementedException();

    public MSAAQuality SampleQuality => throw new NotImplementedException();

    public TextureDimensions Dimensions => throw new NotImplementedException();

    public bool IsReleased => throw new NotImplementedException();

    public bool IsEnabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ulong EOID => throw new NotImplementedException();

    public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
