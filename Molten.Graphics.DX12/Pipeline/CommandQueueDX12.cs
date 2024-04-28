using Molten.Collections;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;

public unsafe class CommandQueueDX12 : GpuObject<DeviceDX12>
{
    CommandQueueDesc _desc;
    ID3D12CommandQueue* _handle;
    CommandAllocatorDX12 _currentCmdAllocator;
    CommandListDX12 _prevCmdList;
    Interlocker _lockerExecute;

    internal CommandQueueDX12(Logger log, DeviceDX12 device, DeviceBuilderDX12 builder, ref CommandQueueDesc desc) : 
        base(device)
    {
        _desc = desc;
        _lockerExecute = new Interlocker();
        Log = log;
        Device = device;

        Initialize(builder);
    }

    private void Initialize(DeviceBuilderDX12 builder)
    {
        Guid cmdGuid = ID3D12CommandQueue.Guid;
        void* cmdQueue = null;

        DeviceDX12 device = Device; 
        HResult r = device.Handle->CreateCommandQueue(ref _desc, &cmdGuid, &cmdQueue);
        if (!device.Log.CheckResult(r))
        {
            Log.Error($"Failed to initialize '{_desc.Type}' command queue");
            return;
        }

        Log.WriteLine($"Initialized '{_desc.Type}' command queue");

        _handle = (ID3D12CommandQueue*)cmdQueue;
    }

    internal void Execute(GpuCommandList cmd)
    {
        if (cmd.HasBegan)
            throw new GpuCommandListException(cmd, "Cannot execute a command list that has not been closed.");

        CommandListDX12 dxCmd = (CommandListDX12)cmd;
        ID3D12CommandList** lists = stackalloc ID3D12CommandList*[] { dxCmd.BaseHandle };

        _lockerExecute.Lock();

        // Tell the GPU to wait for the previous command list to execute before executing the new one.
        if (_prevCmdList != null)
        {
            ulong fenceValue = _prevCmdList.Fence.Signal(this);
            _handle->Wait(_prevCmdList.Fence.Handle, fenceValue);
        }

        _handle->ExecuteCommandLists(1, lists);
        dxCmd.ApplyBarrierStates();
        _prevCmdList = dxCmd;
        _lockerExecute.Unlock();
    }

    protected override void OnGpuRelease()
    {
        NativeUtil.ReleasePtr(ref _handle);
    }

    internal ID3D12CommandQueue* Handle => _handle;

    internal Logger Log { get; }

    internal new DeviceDX12 Device { get; }
}
