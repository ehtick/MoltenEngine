using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;

public unsafe class ResourceHandleDX12 : GpuResourceHandle
{
    ID3D12Resource1*[] _ptr;
    ResourceStateTrackerDX12[] _stateTracker;

    internal ResourceHandleDX12(GpuResource resource, params ID3D12Resource1*[] ptr) : base(resource)
    {
        if (ptr == null || ptr.Length == 0)
            throw new ArgumentException("Resource handle must contain at least one resource pointer.");

        _ptr = ptr;
        Device = resource.Device as DeviceDX12;
        _stateTracker = new ResourceStateTrackerDX12[ptr.Length];
        for (int i = 0; i < ptr.Length; i++)
        {
            uint numSubResources = resource switch
            {
                TextureDX12 tex => tex.ArraySize * tex.MipMapCount,
                _ => 1,
            };
            _stateTracker[i] = new ResourceStateTrackerDX12(numSubResources);
        }

        if (!resource.Flags.Has(GpuResourceFlags.DenyShaderAccess))
            SRV = new SRViewDX12(this);

        if (resource.Flags.Has(GpuResourceFlags.UnorderedAccess))
            UAV = new UAViewDX12(this);
    }

    internal ResourceHandleDX12(GpuResource resource, ID3D12Resource1** ptr, uint numResources) : base(resource)
    {
        if (numResources == 0)
            throw new ArgumentException("Resource handle must contain at least one resource pointer.");

        _stateTracker = new ResourceStateTrackerDX12[numResources];
        _ptr = new ID3D12Resource1*[numResources];

        for (int i = 0; i < numResources; i++)
        {
            _ptr[i] = ptr[i];
            uint numSubResources = resource switch
            {
                TextureDX12 tex => tex.ArraySize * tex.MipMapCount,
                _ => 1,
            };
            _stateTracker[i] = new ResourceStateTrackerDX12(numSubResources);
        }

        Device = resource.Device as DeviceDX12;

        if (!resource.Flags.Has(GpuResourceFlags.DenyShaderAccess))
            SRV = new SRViewDX12(this);

        if (resource.Flags.Has(GpuResourceFlags.UnorderedAccess))
            UAV = new UAViewDX12(this);
    }

    protected override void OnGpuRelease()
    {
        SRV?.Dispose();
        UAV?.Dispose();
        _stateTracker = null;

        for (int i = 0; i < _ptr.Length; i++)
            NativeUtil.ReleasePtr(ref _ptr[i]);
    }

    public static implicit operator ID3D12Resource1*(ResourceHandleDX12 handle)
    {
        return handle._ptr[handle.Index];
    }

    public static implicit operator ID3D12Resource*(ResourceHandleDX12 handle)
    {
        return (ID3D12Resource*)handle._ptr[handle.Index];
    }

    internal SRViewDX12 SRV { get; }

    internal UAViewDX12 UAV { get; }

    internal new DeviceDX12 Device { get; }

    internal unsafe ID3D12Resource1* Ptr1 => _ptr[Index];

    internal unsafe ID3D12Resource* Ptr => (ID3D12Resource*)_ptr[Index];

    /// <summary>
    /// The current resource pointer index. This is the one that will be used by default when the handle is passed to the D3D12 API.
    /// </summary>
    internal uint Index { get; set; }

    /// <summary>
    /// The number of indexable resources in the handle.
    /// </summary>
    internal uint NumResources => (uint)_ptr.Length;

    /// <summary>
    /// Gets or sets the resource pointer at the specified index.
    /// </summary>
    /// <param name="index">The resource pointer index.</param>
    /// <returns></returns>
    internal ref ID3D12Resource1* this[uint index] => ref _ptr[index];

    /// <summary>
    /// Gets the resoruce barrier state tracker for the current resource handle.
    /// </summary>
    internal ResourceStateTrackerDX12 State => _stateTracker[Index];
}
