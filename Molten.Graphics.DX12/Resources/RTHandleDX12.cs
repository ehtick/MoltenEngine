using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;
internal class RTHandleDX12 : ResourceHandleDX12
{
    internal unsafe RTHandleDX12(TextureDX12 texture, ID3D12Resource1* ptr) : 
        base(texture, ptr)
    {
        RTV = new RTViewDX12(this);
    }

    protected override void OnGpuRelease()
    {
        RTV.Dispose();
        base.OnGpuRelease();
    }

    internal RTViewDX12 RTV { get; }
}
