using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;

internal unsafe class DescriptorHeapDX12 : GpuObject<DeviceDX12>
{
    ID3D12DescriptorHeap* _handle;
    uint _incrementSize;
    bool _isGpuVisible;
    uint _capacity;
    CpuDescriptorHandle _cpuStartHandle;

    HeapHandleDX12 _startAllocation;
    DescriptorHeapManagerDX12 _manager;

    internal DescriptorHeapDX12(DescriptorHeapManagerDX12 manager, DescriptorHeapDesc desc) : 
        base(manager.Device)
    {
        _manager = manager;
        Guid guid = ID3D12DescriptorHeap.Guid;
        void* ptr = null;
        HResult hr = Device.Handle->CreateDescriptorHeap(ref desc, &guid, &ptr);

        if(!Device.Log.CheckResult(hr, () => $"Failed to create descriptor heap with capacity '{desc.NumDescriptors}'"))
            return;

        _capacity = desc.NumDescriptors;
        _handle = (ID3D12DescriptorHeap*)ptr;
        _incrementSize = Device.Handle->GetDescriptorHandleIncrementSize(desc.Type);
        _isGpuVisible = (desc.Flags & DescriptorHeapFlags.ShaderVisible) == DescriptorHeapFlags.ShaderVisible;
        _cpuStartHandle = _handle->GetCPUDescriptorHandleForHeapStart();

        _startAllocation = _manager.GetHandleInstance();
        _startAllocation.CpuHandle = _cpuStartHandle;
        _startAllocation.StartIndex = 0;
        _startAllocation.NumSlots = _capacity;
        _startAllocation.IsFree = true;
        _startAllocation.Heap = this;
    }

    internal void Reset()
    {
        // TODO Clear all allocations and put them back in the pool for re-use.
        HeapHandleDX12 current = _startAllocation;
        while(current != null)
        {
            // Use the last allocation as the first.
            if(current.Next == null)
            {
                _startAllocation = current;
                _startAllocation.IsFree = true;
                _startAllocation.CpuHandle = _cpuStartHandle;
                _startAllocation.StartIndex = 0;
                _startAllocation.NumSlots = _capacity;
                _startAllocation.Next = null;
            }
            else // Put all other allocations back in the pool.
            {
                current.IsFree = true;
                _manager.PoolHandle(current);
            }

            current = current.Next;
        }
    }

    internal void Prepare()
    {
        // TODO Defragment the heap (merge adjacent free slots).
        // TODO Implement system that allows allocated heap handles to be moved next to each other and have their reference update in their consumer (e.g. view, texture, etc).
    }

    internal bool TryAllocate(uint numSlots, out HeapHandleDX12 handle)
    {
        handle = default;

        HeapHandleDX12 current = _startAllocation;
        while (current != null)
        {
            // If the allocation is an exact fit, return it as-is.
            if (current.NumSlots == numSlots)
            {
                current.IsFree = false;
                current.Heap = this;
                handle = current;
                return true;
            }
            else if (current.NumSlots > numSlots)
            {

            }

            current = current.Next;
        }

        // If the heap is full, return false.
        //if (_availabilityMask != ulong.MaxValue)
        //{
        //    uint startIndex = 0; // The first slot of the requested range.
        //    ulong mask;
        //    ulong slotMask = 0;

        //    for (int i = 0; i < _capacity; i++)
        //    {
        //        mask = (1UL << i);

        //        // If the slot is already taken, reset the free slot count.
        //        if ((_availabilityMask & mask) == mask)
        //        {
        //            startIndex = (uint)i;
        //        }
        //        else
        //        {
        //            slotMask |= mask;
        //            if ((i + 1) - startIndex == numSlots)
        //            {
        //                _availabilityMask |= slotMask;
        //                handle.Handle = new CpuDescriptorHandle(_cpuStartHandle.Ptr + (startIndex * _incrementSize));
        //                handle.Heap = this;
        //                handle.StartIndex = startIndex;
        //                handle.NumSlots = numSlots;
        //                return true;
        //            }
        //        }
        //    }
        //}

        return false;
    }

    internal GpuDescriptorHandle GetGpuHandle()
    {
        if (!_isGpuVisible)
            throw new InvalidOperationException("Cannot get a GPU handle from a non-GPU visible heap.");

        return _handle->GetGPUDescriptorHandleForHeapStart();
    }

    protected override void OnGpuRelease()
    {
        NativeUtil.ReleasePtr(ref _handle);
    }

    public static implicit operator ID3D12DescriptorHeap*(DescriptorHeapDX12 heap)
    {
        return heap._handle;
    }

    /// <summary>
    /// Gets the increment size of the heap, in bytes.
    /// </summary>
    public uint IncrementSize => _incrementSize;

    internal CpuDescriptorHandle CpuStartHandle => _cpuStartHandle;

    internal ref readonly ID3D12DescriptorHeap* Handle => ref _handle;
}
