using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;

internal unsafe class RootSignatureDX12 : GpuObject<DeviceDX12>
{
    ID3D12RootSignature* _handle;
    VersionedRootSignatureDesc _desc;

    internal RootSignatureDX12(DeviceDX12 device, ID3D12RootSignature* handle, VersionedRootSignatureDesc desc) :
        base(device)
    {
        _handle = handle;
        _desc = desc;
    }

    /// <summary>
    /// Outputs the layout of the current <see cref="RootSignatureDX12"/> to it's parent <see cref="DeviceDX12"/> log.
    /// </summary>
    internal unsafe void LayoutToLog()
    {
        // Validate GPU heap
        if (_desc.Version == D3DRootSignatureVersion.Version10)
            LogLayout1_0();
        else if (_desc.Version == D3DRootSignatureVersion.Version11)
            LogLayout1_1();
    }

    private void LogLayout1_0()
    {
        throw new NotImplementedException($"{nameof(LayoutToLog)}: Logging of V1.0 root signatures is not yet supported.");
    }

    private void LogLayout1_1()
    {
        ref RootSignatureDesc1 desc = ref _desc.Desc11;
        Device.Log.Debug($"Bound Root-Sigature (V1.1) with {desc.NumParameters} parameter(s):");

        for (uint i = 0; i < desc.NumParameters; i++)
        {
            ref RootParameter1 rootParam = ref desc.PParameters[i];
            Device.Log.Debug($"\tRoot parameter {i} - Type: {rootParam.ParameterType}");

            if (rootParam.ParameterType == RootParameterType.TypeDescriptorTable)
            {
                ref RootDescriptorTable1 table = ref rootParam.DescriptorTable;
                for (uint j = 0; j < table.NumDescriptorRanges; j++)
                {
                    ref DescriptorRange1 range = ref table.PDescriptorRanges[j];
                    Device.Log.Debug($"\t\tRange {j} - Type: {range.RangeType}, Descriptor(s): {range.NumDescriptors}");
                    for (uint k = 0; k < range.NumDescriptors; k++)
                    {
                        uint offsetFromStart = range.OffsetInDescriptorsFromTableStart + k;
                        Device.Log.Debug($"\t\t\tDescriptor {k} - Offset: {offsetFromStart}");
                    }
                }
            }
        }
    }

    protected override void OnGpuRelease()
    {
        NativeUtil.ReleasePtr(ref _handle);

        if (_desc.Version == D3DRootSignatureVersion.Version11)
        {
            ref RootSignatureDesc1 desc = ref _desc.Desc11;

            for (int i = 0; i < desc.NumParameters; i++)
                EngineUtil.Free(ref desc.PParameters[i].DescriptorTable.PDescriptorRanges);

            EngineUtil.Free(ref desc.PParameters);
            EngineUtil.Free(ref desc.PStaticSamplers);
        }
        else if (_desc.Version == D3DRootSignatureVersion.Version10)
        {
            ref RootSignatureDesc desc = ref _desc.Desc10;

            for (int i = 0; i < desc.NumParameters; i++)
                EngineUtil.Free(ref desc.PParameters[i].DescriptorTable.PDescriptorRanges);

            EngineUtil.Free(ref desc.PParameters);
            EngineUtil.Free(ref desc.PStaticSamplers);
        }
    }

    public static implicit operator ID3D12RootSignature*(RootSignatureDX12 sig) => sig._handle;

    public ref readonly ID3D12RootSignature* Handle => ref _handle;

    public ref readonly VersionedRootSignatureDesc Desc => ref _desc;
}
