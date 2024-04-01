using Molten.Graphics.DX12;
using Silk.NET.Direct3D12;

namespace Molten.Graphics;
internal struct HeapHandleDX12
{
    public CpuDescriptorHandle CpuHandle;

    public uint StartIndex;

    public uint NumSlots;

    public DescriptorHeapDX12 Heap;

    internal void Free()
    {
        Heap?.Free(ref this);
    }

    internal CpuDescriptorHandle GetCpuHandle(uint index)
    {
        return new CpuDescriptorHandle(CpuHandle.Ptr + (index * Heap.IncrementSize));
    }
}
