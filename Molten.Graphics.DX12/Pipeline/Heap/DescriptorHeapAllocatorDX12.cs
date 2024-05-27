using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;
internal class DescriptorHeapAllocatorDX12 : GpuObject<DeviceDX12>
{
    DescriptorHeapManagerDX12 _manager;
    List<DescriptorHeapDX12> _heaps;
    DescriptorHeapDesc _desc;

    internal DescriptorHeapAllocatorDX12(DescriptorHeapManagerDX12 manager, DescriptorHeapType type, DescriptorHeapFlags flags, uint numDescriptors) : 
        base(manager.Device)
    {
        _manager = manager;
        _heaps = new List<DescriptorHeapDX12>();
        _desc = new DescriptorHeapDesc()
        {
            NodeMask = 0,
            Type = type,
            Flags = flags,
            NumDescriptors = numDescriptors,
        };
    }

    internal void Reset()
    {
        for (int i = 0; i < _heaps.Count; i++)
            _heaps[i].Reset();
    }

    internal void Defragment()
    {
        for (int i = 0; i < _heaps.Count; i++)
            _heaps[i].Defragment();
    }

    internal HeapHandleDX12 Allocate(uint numDescriptors)
    {
        if(numDescriptors > _desc.NumDescriptors)
            throw new InvalidOperationException($"The number of requested descriptors exceeds the capacity of a heap ({_desc.NumDescriptors}).");

        HeapHandleDX12 handle;
        DescriptorHeapDX12 heap;

        // Attempt to allocate from existing heaps.
        for (int i = 0; i < _heaps.Count; i++)
        {
            heap = _heaps[i];
            if(heap.TryAllocate(numDescriptors, out handle))
                return handle;
        }

        // Allocate a new heap.
        heap = new DescriptorHeapDX12(_manager, _desc);
        _heaps.Add(heap);
        if(!heap.TryAllocate(numDescriptors, out handle))
            throw new InvalidOperationException("Failed to allocate a new descriptor heap.");

        return handle;
    }

    protected override void OnGpuRelease()
    {
        for(int i = 0; i < _heaps.Count; i++)
            _heaps[i].Dispose(true);
    }
}
