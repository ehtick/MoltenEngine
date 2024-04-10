using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;

internal struct DepthClearTaskDX12 : IGpuTask<DepthClearTaskDX12>
{
    public DepthSurfaceDX12 Surface;

    public float DepthValue;

    public byte StencilValue;

    public DepthClearFlags Flags;

    public GpuTaskCallback OnCompleted;

    public static bool Validate(ref DepthClearTaskDX12 t) => true;

    public unsafe static bool Process(GpuCommandList cmd, ref DepthClearTaskDX12 t)
    {
        CommandListDX12 cmDX12 = (CommandListDX12)cmd;

        t.Surface.Apply(cmd);
        cmDX12.Transition(t.Surface, ResourceStates.DepthWrite);

        DSHandleDX12 dsHandle = (DSHandleDX12)t.Surface.Handle;
        ref CpuDescriptorHandle cpuHandle = ref dsHandle.DSV.CpuHandle;
        ClearFlags flags = 0;

        if (t.Flags.Has(DepthClearFlags.Depth))
            flags = ClearFlags.Depth;

        if (t.Surface.DepthFormat.HasStencil() && t.Flags.HasFlag(DepthClearFlags.Stencil))
            flags |= ClearFlags.Stencil;

        // TODO Add support for clearing areas using Box2D structs.
        if (flags > 0)
            cmDX12.Handle->ClearDepthStencilView(cpuHandle, flags, t.DepthValue, t.StencilValue, 0, null);

        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
