using Molten.Graphics.DX12;
using Silk.NET.Direct3D12;
using System.Runtime.CompilerServices;

namespace Molten.Graphics;
internal struct HeapHandleDX12
{
    public CpuDescriptorHandle Handle;

    public uint StartIndex;

    public uint NumSlots;

    public DescriptorHeapDX12 Heap;

    internal void Free()
    {
        Heap?.Free(ref this);
    }

    internal CpuDescriptorHandle GetCpuHandle(uint index)
    {
        return new CpuDescriptorHandle(Handle.Ptr + (index * Heap.IncrementSize));
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
        Handle.Ptr += Heap.IncrementSize;
    }
}
