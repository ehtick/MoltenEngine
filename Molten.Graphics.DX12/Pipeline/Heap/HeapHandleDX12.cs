using Molten.Graphics.DX12;
using Silk.NET.Direct3D12;
using System.Runtime.CompilerServices;

namespace Molten.Graphics;

internal class HeapHandleDX12
{
    public CpuDescriptorHandle CpuHandle;

    public uint StartIndex;

    public uint NumSlots;

    public DescriptorHeapDX12 Heap;

    internal HeapHandleDX12 Next;

    internal bool IsFree;

    internal void Free()
    {
        IsFree = true;
    }

    internal CpuDescriptorHandle GetCpuHandle(uint index)
    {
        if (index >= NumSlots)
            throw new IndexOutOfRangeException("Index cannot be greater than the number of slots allocated to the heap handle.");

        return new CpuDescriptorHandle(CpuHandle.Ptr + (index * Heap.IncrementSize));
    }

    internal GpuDescriptorHandle GetGpuHandle()
    {
        GpuDescriptorHandle handle = Heap.GetGpuHandle();
        handle.Ptr += (StartIndex * Heap.IncrementSize);
        return handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Increment()
    {
        StartIndex++;
        CpuHandle.Ptr += Heap.IncrementSize;
    }
}
