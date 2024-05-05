using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;

struct BarrierStateDX12
{
    public ResourceStates State;

    public ResourceBarrierFlags Flags;

    public BarrierStateDX12(ResourceStates states, ResourceBarrierFlags flags)
    {
        State = states;
        Flags = flags;
    }

    public static bool operator ==(BarrierStateDX12 a, BarrierStateDX12 b)
    {
        return a.State == b.State;
    }

    public static bool operator !=(BarrierStateDX12 a, BarrierStateDX12 b)
    {
        return a.State != b.State;
    }
}