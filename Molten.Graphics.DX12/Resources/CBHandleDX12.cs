using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;
internal class CBHandleDX12 : ResourceHandleDX12
{
    internal unsafe CBHandleDX12(BufferDX12 cBuffer, ID3D12Resource1* ptr) : 
        base(cBuffer, ptr)
    {
        CBV = new CBViewDX12(this);
    }

    protected override void OnGpuRelease()
    {
        CBV.Dispose();
        base.OnGpuRelease();
    }

    internal CBViewDX12 CBV { get; }
}
