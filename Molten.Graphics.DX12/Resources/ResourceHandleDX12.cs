using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;

public unsafe class ResourceHandleDX12 : GpuResourceHandle
{
    ID3D12Resource1* _ptr;

    internal ResourceHandleDX12(GpuResource resource, ID3D12Resource1* ptr) : base(resource)
    {
        if (ptr == null)
            throw new ArgumentNullException("ptr", "Resource pointer cannot be null.");

        _ptr = ptr;
        Device = resource.Device as DeviceDX12;

        uint numSubResources = 1;
        if (resource is TextureDX12 tex)
            numSubResources = tex.ArraySize * tex.MipMapCount;

        State = new ResourceStateTrackerDX12(numSubResources);

        if (!resource.Flags.Has(GpuResourceFlags.DenyShaderAccess))
            SRV = new SRViewDX12(this);

        if (resource.Flags.Has(GpuResourceFlags.UnorderedAccess))
            UAV = new UAViewDX12(this);
    }

    protected override void OnGpuRelease()
    {
        SRV?.Dispose();
        UAV?.Dispose();
        NativeUtil.ReleasePtr(ref _ptr);
    }

    public static implicit operator ID3D12Resource1*(ResourceHandleDX12 handle)
    {
        return handle._ptr;
    }

    public static implicit operator ID3D12Resource*(ResourceHandleDX12 handle)
    {
        return (ID3D12Resource*)handle._ptr;
    }

    internal SRViewDX12 SRV { get; }

    internal UAViewDX12 UAV { get; }

    internal new DeviceDX12 Device { get; }

    internal unsafe ref ID3D12Resource1* Ptr1 => ref _ptr;

    internal unsafe ID3D12Resource* Ptr => (ID3D12Resource*)_ptr;

    /// <summary>
    /// The current resource pointer index. This is the one that will be used by default when the handle is passed to the D3D12 API.
    /// </summary>
    internal uint Index { get; set; }

    /// <summary>
    /// Gets the resoruce barrier state tracker for the current resource handle.
    /// </summary>
    internal ResourceStateTrackerDX12 State { get; }
}
