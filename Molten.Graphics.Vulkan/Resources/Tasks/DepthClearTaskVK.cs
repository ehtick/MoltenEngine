using Silk.NET.Vulkan;

namespace Molten.Graphics.Vulkan;

internal struct DepthClearTaskVK : IGpuTask<DepthClearTaskVK>
{
    public DepthSurfaceVK Surface;

    public float DepthValue;

    public uint StencilValue;

    internal GpuTaskCallback OnCompleted;

    public unsafe static bool Process(GpuCommandList cmd, ref DepthClearTaskVK t)
    {
        // TODO Implement proper handling of barrier transitions.
        //  -- Transition from the current layout to the one we need.
        //  -- Transition back to the original layout once we're done.

        /*if (t.Surface.ApplyQueue.Count > 0)
        {
            t.Surface.ClearValue = null;

            CommandListVK vkCmd = cmd as CommandListVK;
            t.Surface.Apply(cmd);

            //vkCmd.Sync(GpuCommandListFlags.SingleSubmit);
            t.Surface.Transition(vkCmd, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);

            ImageSubresourceRange range = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseArrayLayer = 0,
                LayerCount = t.Surface.ArraySize,
                BaseMipLevel = 0,
                LevelCount = t.Surface.MipMapCount,
            };

            vkCmd.ClearDepthImage(*t.Surface.Handle.NativePtr, ImageLayout.TransferDstOptimal, t.DepthValue, t.StencilValue, &range, 1);
            t.Surface.Transition(vkCmd, ImageLayout.TransferDstOptimal, ImageLayout.DepthAttachmentOptimal);
            vkCmd.Sync();
        }
        else
        {*/
            t.Surface.ClearValue = new ClearDepthStencilValue(t.DepthValue, t.StencilValue);
        //}

        return true;
    }

    public void Complete(bool success)
    {
        OnCompleted?.Invoke(success);
    }
}
