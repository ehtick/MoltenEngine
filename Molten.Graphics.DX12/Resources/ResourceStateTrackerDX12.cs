using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;
internal unsafe class ResourceStateTrackerDX12 : IDisposable
{
    ResourceStates* _ptrStates;
    uint _numSubResources;

    internal ResourceStateTrackerDX12(uint numSubResources)
    {
        _ptrStates = EngineUtil.AllocArray<ResourceStates>(numSubResources);
        _numSubResources = numSubResources;
    }

    ~ResourceStateTrackerDX12()
    {
        Dispose();
    }

    internal void SetAll(ResourceStates state)
    {
        for (uint i = 0; i < _numSubResources; i++)
            _ptrStates[i] = state;
    }

    public void Dispose()
    {
        EngineUtil.Free(ref _ptrStates);
    }

    public ref ResourceStates this[uint subResourceIndex] => ref _ptrStates[subResourceIndex];

    public ref readonly ResourceStates* PtrStates => ref _ptrStates;

    public uint NumSubResources => _numSubResources;
}
