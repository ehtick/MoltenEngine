using Silk.NET.Direct3D12;
using System.Runtime.InteropServices;

namespace Molten.Graphics.DX12;

[StructLayout(LayoutKind.Explicit)]
struct BarrierStateDX12
{
    [FieldOffset(0)]
    public ResourceStates State;

    [FieldOffset(4)]
    public ResourceBarrierFlags Flags;

    [FieldOffset(0)]
    public ulong Mask;

    public BarrierStateDX12(ResourceStates states, ResourceBarrierFlags flags)
    {
        State = states;
        Flags = flags;
    }

    public static bool operator ==(BarrierStateDX12 a, BarrierStateDX12 b)
    {
        return a.State == b.State && a.Flags == b.Flags;
    }

    public static bool operator !=(BarrierStateDX12 a, BarrierStateDX12 b)
    {
        return a.State != b.State || a.Flags != b.Flags;
    }
}