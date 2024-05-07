using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;
internal unsafe class RootSignatureBuilderDX12
{
    static readonly char[] _errorDelimiters = ['\r', '\n'];

    static readonly Dictionary<D3DRootSignatureVersion, RootSignaturePopulatorDX12> _rootPopulators = new()
    {
        [D3DRootSignatureVersion.Version10] = new RootSigPopulator1_0(),
        [D3DRootSignatureVersion.Version11] = new RootSigPopulator1_1(),
    };

    D3DRootSignatureVersion _rootSignatureVersion;
    RootSignaturePopulatorDX12 _rootParser;

    internal RootSignatureBuilderDX12(DeviceDX12 device)
    {
        _rootSignatureVersion = device.CapabilitiesDX12.RootSignatureVersion;

        // Decrease root signature version until we find one the engine supports.
        while (!_rootPopulators.TryGetValue(_rootSignatureVersion, out _rootParser))
        {
            _rootSignatureVersion--;
            if (_rootSignatureVersion <= 0)
                throw new NotSupportedException("The current device does not support any known root signature versions.");
        }
    }

    internal RootSignatureDX12 Build(ShaderPassDX12 pass, PipelineInputLayoutDX12 layout, ref readonly GraphicsPipelineStateDesc psoDesc)
    {
        DeviceDX12 device = pass.Device as DeviceDX12;

        // TODO Check root signature cache for existing root signature.

        VersionedRootSignatureDesc sigDesc = new(_rootSignatureVersion);
        RootSigMetaDX12 rootMeta = _rootParser.Populate(ref sigDesc, in psoDesc, pass, layout);

        // Serialize the root signature.
        ID3D10Blob* signature = null;
        ID3D10Blob* errors = null;

        HResult hr = device.Renderer.Api.SerializeVersionedRootSignature(&sigDesc, &signature, &errors);
        if (!device.Log.CheckResult(hr, () => "Failed to serialize root signature"))
        {
            ParseErrors(pass, hr, errors);
            hr.Throw();
        }

        // TODO Implement root signature caching - Store the serialized signature blob in cache file.

        // Create the root signature.
        Guid guid = ID3D12RootSignature.Guid;
        void* ptr = null;
        hr = device.Handle->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), &guid, &ptr);
        if (!device.Log.CheckResult(hr, () => "Failed to create root signature"))
            hr.Throw();

        return new RootSignatureDX12(device, (ID3D12RootSignature*)ptr, rootMeta);
    }

    private unsafe void ParseErrors(ShaderPassDX12 pass, HResult hr, ID3D10Blob* errors)
    {
        if (errors == null)
            return;

        void* ptrErrors = errors->GetBufferPointer();
        nuint numBytes = errors->GetBufferSize();
        string strErrors = SilkMarshal.PtrToString((nint)ptrErrors, NativeStringEncoding.UTF8);
        string[] errorList = strErrors.Split(_errorDelimiters, StringSplitOptions.RemoveEmptyEntries);

        if (hr.IsSuccess)
        {
            for (int i = 0; i < errorList.Length; i++)
                pass.Device.Log.Warning($"[{nameof(PipelineStateBuilderDX12)}] {errorList[i]}");
        }
        else
        {
            for (int i = 0; i < errorList.Length; i++)
                pass.Device.Log.Error($"[{nameof(PipelineStateBuilderDX12)}] {errorList[i]}");
        }
    }
}
