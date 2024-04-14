using Silk.NET.Direct3D12;
using System.Drawing;

namespace Molten.Graphics.DX12;

internal struct SurfaceClearTaskDX12 : IGpuTask<SurfaceClearTaskDX12>
{
    public TextureDX12 Surface;

    public Color Color;

    public GpuTaskCallback OnCompleted;

    public static bool Validate(ref SurfaceClearTaskDX12 t) => true;

    public unsafe static bool Process(GpuCommandList cmd, ref SurfaceClearTaskDX12 t)
    {
        CommandListDX12 dxCmd = (CommandListDX12)cmd;

        t.Surface.Apply(cmd);
        if (t.Surface.Handle is RTHandleDX12 rtHandle)
        {
            dxCmd.Transition(t.Surface, ResourceStates.RenderTarget);
            ref CpuDescriptorHandle cpuHandle = ref rtHandle.RTV.CpuHandle;
            Color4 c4 = t.Color.ToColor4();

            dxCmd.Handle->ClearRenderTargetView(cpuHandle, c4.Values, 0, null);
        }
        else
        {
            throw new GpuResourceException(t.Surface, "Cannot clear a non-render surface texture.");
        }
        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
