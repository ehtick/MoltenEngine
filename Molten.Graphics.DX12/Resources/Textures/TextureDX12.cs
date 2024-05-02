using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Molten.Graphics.DX12;

public abstract class TextureDX12 : GpuTexture, ITexture
{
    unsafe ID3D12Resource1** _ptrs = null;
    uint _numResources = 0;
    ResourceHandleDX12[] _handles;
    uint _handleIndex;

    ResourceBarrier _barrier;
    ResourceStates _barrierState;
    ResourceDesc1 _desc;
    ProtectedSessionDX12 _protectedSession;

    protected TextureDX12(DeviceDX12 device,
        ResourceDimension resourceDimension,
        TextureDimensions dimensions,
        GpuResourceFormat format, GpuResourceFlags flags, string name,
        ProtectedSessionDX12 protectedSession = null) : 
        base(device, ref dimensions, format, flags, name)
    {
        Device = device;
        _protectedSession = protectedSession;

        if (IsBlockCompressed)
            throw new NotSupportedException("1D textures do not supports block-compressed formats.");

        if (dimensions.ArraySize > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(dimensions.ArraySize), "Array size cannot exceed 65535.");

        if (dimensions.MipMapCount > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(dimensions.MipMapCount), "Mip-map level count cannot exceed 65535.");

        if (resourceDimension == ResourceDimension.Buffer)
            throw new InvalidOperationException("Textures cannot use a buffer resource dimension.");

        Desc = new ResourceDesc1()
        {
            Width = dimensions.Width,
            Height = dimensions.Height,
            DepthOrArraySize = (ushort)Math.Max(1, dimensions.ArraySize),
            MipLevels = (ushort)dimensions.MipMapCount,
            Format = format.ToApi(),
            Dimension = resourceDimension,
            Flags = Flags.ToResourceFlags(),
            Alignment = 0,
            Layout = TextureLayout.LayoutUnknown,
            SampleDesc = new SampleDesc()
            {
                Count = (uint)dimensions.MultiSampleLevel,
                Quality = (uint)dimensions.SampleQuality,
            },
            SamplerFeedbackMipRegion = new MipRegion() // Sampler feedback info: https://microsoft.github.io/DirectX-Specs/d3d/SamplerFeedback.html
        };

        if (this is IRenderSurface)
            Desc.Flags |= ResourceFlags.AllowRenderTarget;

        if (this is IDepthStencilSurface)
            Desc.Flags |= ResourceFlags.AllowDepthStencil;
    }

    protected override void OnApply(GpuCommandList cmd)
    {
        if (_handles == null)
            OnCreateResource();
    }

    public void Resize(GpuPriority priority, GpuCommandList cmd,
    uint newWidth,
    uint newMipMapCount = 0,
    uint newArraySize = 0,
    GpuResourceFormat newFormat = GpuResourceFormat.Unknown, 
    GpuTaskCallback completeCallback = null)
    {
        Resize(priority, cmd, newWidth, Dimensions.Height, newMipMapCount, newArraySize, newFormat, completeCallback);
    }

    protected unsafe void OnCreateResource()
    {
        OnCreateTexture(ref _ptrs, ref _numResources);

        // Check if the _handles array needs to be created or resized.
        if (_handles == null)
        {
            _handles = new ResourceHandleDX12[_numResources];
        }
        else if (_handles.Length != _numResources)
        {
            // Resize handles array. If it shrinks, dispose of any leftover handles.
            if (_handles.Length < _numResources)
            {
                for (uint i = _numResources; i < _handles.Length; i++)
                    _handles[i].Dispose();
            }

            Array.Resize(ref _handles, (int)_numResources);
        }

        // Create or update resource handles.
        for (int i = 0; i < _numResources; i++)
        {
            OnCreateHandle(ref _ptrs[i], ref _handles[i]);

            ShaderResourceViewDesc srvDesc = new ShaderResourceViewDesc
            {
                Format = DxgiFormat,
                Shader4ComponentMapping = ResourceInterop.EncodeShader4ComponentMapping(ShaderComponentMapping.FromMemoryComponent0,
                        ShaderComponentMapping.FromMemoryComponent1,
                        ShaderComponentMapping.FromMemoryComponent2,
                        ShaderComponentMapping.FromMemoryComponent3),
            };

            if (!Flags.Has(GpuResourceFlags.DenyShaderAccess))
            {
                SetSRVDescription(ref srvDesc);
                _handles[i].SRV.Initialize(ref srvDesc);
            }

            if (Flags.Has(GpuResourceFlags.UnorderedAccess))
            {
                UnorderedAccessViewDesc uavDesc = default;
                SetUAVDescription(ref srvDesc, ref uavDesc);
                _handles[i].UAV.Initialize(ref uavDesc);
            }
        }
    }

    protected override void ProcessResize(GpuCommandList cmd, ref ResizeTextureTask t)
    {
        if (_handles != null)
        {
            for (int i = 0; i < _handles.Length; i++)
                _handles[i]?.Dispose();

            _handles = null;
        }

        Apply(cmd);
    }

    protected virtual ClearValue GetClearValue() => default;

    protected unsafe override void OnGpuRelease()
    {
        if (_handles != null)
        {
            for (int i = 0; i < _handles.Length; i++)
                _handles[i]?.Dispose();

            _handles = null;
        }

        EngineUtil.FreePtrArray(ref _ptrs);
    }

    protected unsafe virtual void OnCreateTexture(ref ID3D12Resource1** ptrResources, ref uint numResources)
    {
        HeapFlags heapFlags = HeapFlags.None;
        ResourceFlags flags = Flags.ToResourceFlags();
        HeapType heapType = Flags.ToHeapType(); // TODO Add UMA support.
        ResourceStates initialState = Flags.ToResourceState();

        switch (this)
        {
            case IRenderSurface:
                initialState |= ResourceStates.RenderTarget;
                break;

            case IDepthStencilSurface:
                initialState |= ResourceStates.DepthWrite;
                break;
        }

        HeapProperties heapProp = new HeapProperties()
        {
            Type = heapType,
            CPUPageProperty = CpuPageProperty.Unknown,
            //CreationNodeMask = 1,
            MemoryPoolPreference = MemoryPool.Unknown,
            //VisibleNodeMask = 1,
        };

        // TODO Adjust for GPU memory architecture based on UMA support.
        // See for differences: https://microsoft.github.io/DirectX-Specs/d3d/D3D12GPUUploadHeaps.html
        if (heapType == HeapType.Custom)
        {
            // TODO Set CPUPageProperty and MemoryPoolPreference based on UMA support.
        }

        if (Flags.Has(GpuResourceFlags.DenyShaderAccess))
            flags |= ResourceFlags.DenyShaderResource;

        if (Flags.Has(GpuResourceFlags.UnorderedAccess))
            flags |= ResourceFlags.AllowUnorderedAccess;

        ClearValue clearValue = GetClearValue();
        ClearValue* ptrClearValue = clearValue.Format != Format.FormatUnknown ? &clearValue : null;
        Guid guid = ID3D12Resource1.Guid;
        void* ptr = null;

        fixed (ResourceDesc1* ptrDesc = &_desc)
        {
            HResult hr = Device.Handle->CreateCommittedResource2(&heapProp, heapFlags, ptrDesc, initialState, ptrClearValue, _protectedSession, &guid, &ptr);
            if (!Device.Log.CheckResult(hr, () => $"Failed to create {_desc.Dimension} resource"))
            {
                _ptrs = null;
                return;
            }
        }

        _numResources = 1;

        if(ptrResources == null)
            ptrResources = EngineUtil.AllocPtrArray<ID3D12Resource1>(_numResources);

        ptrResources[0] = (ID3D12Resource1*)ptr;
    }

    protected unsafe virtual void OnCreateHandle(ref ID3D12Resource1* ptr, ref ResourceHandleDX12 handle)
    {
        handle = new ResourceHandleDX12(this, ptr);
    }

    protected abstract void SetUAVDescription(ref ShaderResourceViewDesc srvDesc, ref UnorderedAccessViewDesc desc);

    protected abstract void SetSRVDescription(ref ShaderResourceViewDesc desc);

    //protected override void OnResizeTextureImmediate(GpuCommandList cmd, ref readonly TextureDimensions dimensions, GpuResourceFormat format)
    //{
    //    _desc.Width = dimensions.Width;
    //    _desc.MipLevels = (ushort)dimensions.MipMapCount;
    //    _desc.Format = format.ToApi();

    //    if (Desc.Dimension != ResourceDimension.Texture1D)
    //        _desc.Height = dimensions.Height;
    //    else
    //        _desc.Height = 1;

    //    if (Desc.Dimension == ResourceDimension.Texture3D)
    //        _desc.DepthOrArraySize = (ushort)dimensions.Depth;
    //    else
    //        _desc.DepthOrArraySize = (ushort)Math.Max(1, dimensions.ArraySize);

    //    Dimensions = dimensions;

    //    OnCreateResource();
    //}

    /// <summary>Gets the DXGI format of the texture.</summary>
    public Format DxgiFormat => ResourceFormat.ToApi();

    public new DeviceDX12 Device { get; }

    public override ResourceHandleDX12 Handle => _handles[_handleIndex];

    internal ref ResourceDesc1 Desc => ref _desc;

    protected ref uint HandleIndex => ref _handleIndex;
}
