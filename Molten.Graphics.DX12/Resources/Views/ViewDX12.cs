using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;

/// <summary>
/// Represents a view for a DirectX 12 resource, such as a shader resource (SRV), unordered-access (UAV) resource or render-target (RTV).
/// </summary>
/// <typeparam name="DESC">Underlying view description type.</typeparam>
public abstract class ViewDX12<DESC> : IDisposable
    where DESC : unmanaged
{
    HeapHandleDX12 _heapHandle;
    CpuDescriptorHandle _cpuHandle;

    protected ViewDX12(ResourceHandleDX12 handle) {
        Handle = handle;
    }

    /// <summary>
    /// Allocates a new descriptor heap handle and (re)creates the view. 
    /// If a heap handle already exists, it will be reused if possible.
    /// </summary>
    /// <param name="desc"></param>
    /// <param name="numDescriptors"></param>
    internal unsafe void Initialize(ref DESC desc)
    {
        if (_heapHandle == null)
            OnAllocateHandle(out _heapHandle);

        for (uint i = 0; i < _heapHandle.NumSlots; i++)
        {
            _cpuHandle = _heapHandle.GetCpuHandle(i);
            OnCreate(ref desc, Handle, ref _cpuHandle, i);
        }
    }

    private protected abstract void OnAllocateHandle(out HeapHandleDX12 handle);

    protected unsafe abstract void OnCreate(ref DESC desc, ID3D12Resource1* resource, ref CpuDescriptorHandle heapHandle, uint resourceIndex);

    public void Dispose()
    {
        _heapHandle.Free();
    }

    /// <summary>
    /// Gets the parent <see cref="ResourceHandleDX12"/>.
    /// </summary>
    internal ResourceHandleDX12 Handle { get; private set; }

    /// <summary>
    /// Gets the CPU-based descriptor handle for the view.
    /// </summary>
    internal ref HeapHandleDX12 DescriptorHeapHandle => ref _heapHandle;

    internal ref CpuDescriptorHandle CpuHandle => ref _cpuHandle;

    /// <summary>
    /// Gets the allocated slot within the <see cref="ParentHeap"/>.
    /// </summary>
    internal uint HeapSlotIndex { get; set; }

    /// <summary>
    /// Gets the parent <see cref="DescriptorHeapDX12"/>.
    /// </summary>
    internal DescriptorHeapDX12 ParentHeap { get; set; }
}
