namespace Molten.Graphics.DX12;
internal unsafe class ResourceStateTrackerDX12
{
    BarrierStateDX12[] _ptrStates;
    uint _numSubResources;

    internal ResourceStateTrackerDX12(uint numSubResources)
    {
        _ptrStates = new BarrierStateDX12[numSubResources];
        _numSubResources = numSubResources;
    }

    internal void SetAll(BarrierStateDX12 state)
    {
        for (uint i = 0; i < _numSubResources; i++)
            _ptrStates[i] = state;
    }

    public ref BarrierStateDX12 this[uint subResourceIndex] => ref _ptrStates[subResourceIndex];

    public uint NumSubResources => _numSubResources;
}
