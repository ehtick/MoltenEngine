using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;

internal unsafe class RootSignatureDX12 : GpuObject<DeviceDX12>
{
    ID3D12RootSignature* _handle;
    RootSigMetaDX12 _meta;

    internal RootSignatureDX12(DeviceDX12 device, ID3D12RootSignature* handle, RootSigMetaDX12 meta) :
        base(device)
    {
        _handle = handle;
        Meta = meta;
    }

    protected override void OnGpuRelease()
    {
        NativeUtil.ReleasePtr(ref _handle);
        _meta.Dispose();
    }

    public static implicit operator ID3D12RootSignature*(RootSignatureDX12 sig) => sig._handle;

    public ref readonly ID3D12RootSignature* Handle => ref _handle;

    public RootSigMetaDX12 Meta { get; }
}
