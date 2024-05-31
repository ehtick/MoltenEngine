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
    List<HeapHandleDX12> _freeAllocations;
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

        _freeAllocations = new List<HeapHandleDX12>();
        _startAllocation = _manager.GetHandleInstance();
        _startAllocation.CpuHandle = _cpuStartHandle;
        _startAllocation.StartIndex = 0;
        _startAllocation.NumSlots = _capacity;
        _startAllocation.Heap = this;
        Free(_startAllocation);
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
                break;
            }
            else // Put all other allocations back in the pool.
            {
                current.IsFree = true;
                HeapHandleDX12 next = current.Next;
                _manager.PoolHandle(current);
                current = next;
            }
        }

        _freeAllocations.Clear();
        _freeAllocations.Add(_startAllocation);
    }

    internal void Defragment()
    {
        if (_freeAllocations.Count > 1)
        {
            // Clear and re-populate the free list to ensure it is up-to-date during defragmentation.
            _freeAllocations.Clear();

            // TODO Implement system that allows allocated heap handles to be moved next to each other and have their reference update in their consumer (e.g. view, texture, etc).

            // Merge adjacent free handles to reduce fragmentation.
            HeapHandleDX12 current = _startAllocation;
            bool newFree = true;

            while (current != null)
            {
                if (current.IsFree)
                {
                    // Update _freeAllocation list, but only if we've hit a new free allocation.
                    if (newFree)
                    {
                        _freeAllocations.Add(current);
                        newFree = false;
                    }

                    // Merge adjacent free allocations.
                    if (current.Next?.IsFree == true)
                    {
                        current.NumSlots += current.Next.NumSlots;
                        HeapHandleDX12 next = current.Next;
                        current.Next = next.Next;
                        _manager.PoolHandle(next);

                        // Don't move to the next handle until there are no more adjacent free to merge.
                        continue;
                    }
                }

                // Move to next handle.
                newFree = true;
                current = current.Next;
            }
        }
    }

    internal void Free(HeapHandleDX12 handle)
    {
        handle.IsFree = true;
        if(handle.Heap == null)
        {

        }
        _freeAllocations.Add(handle);
    }

    internal bool TryAllocate(uint numSlots, out HeapHandleDX12 handle)
    {
        HeapHandleDX12 bestFit = null;
        int bestFitIndex = 0;

        // Find exact or best-fit allocation.
        for (int i = 0; i < _freeAllocations.Count; i++)
        {
            HeapHandleDX12 current = _freeAllocations[i];
            if (current.NumSlots == numSlots)
            {
                current.IsFree = false;
                handle = current;
                _freeAllocations.RemoveAt(i);
                return true;
            }
            else if (bestFit == null || current.NumSlots < bestFit.NumSlots)
            {
                if (current.NumSlots > numSlots)
                {
                    bestFit = current;
                    bestFitIndex = i;
                }
            }
        }

        // Take a slice from the best fit.
        if (bestFit != null)
        {
            // Create new allocation.
            uint startIndex = bestFit.NumSlots - numSlots;
            HeapHandleDX12 newHandle = _manager.GetHandleInstance();
            newHandle.StartIndex = bestFit.StartIndex + startIndex;
            newHandle.Heap = this;
            newHandle.CpuHandle = bestFit.GetOffsetCpuHandle(startIndex);
            newHandle.NumSlots = numSlots;
            newHandle.IsFree = false;
            newHandle.Next = bestFit.Next;

            // Update current allocation.
            bestFit.NumSlots -= numSlots;
            bestFit.Next = newHandle;
            handle = newHandle;

            return true;
        }

        handle = default;
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
