using Molten.Cache;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Molten.Graphics.DX12;

internal class PipelineStateBuilderDX12
{
    struct CacheKey : IEquatable<CacheKey>
    {
        public ShaderPassDX12 Pass;

        public PipelineInputLayoutDX12 InputLayout;

        public bool IsValid => Pass != null && InputLayout != null;

        public CacheKey(ShaderPassDX12 pass, PipelineInputLayoutDX12 layout)
        {
            Pass = pass;
            InputLayout = layout;
        }

        public override bool Equals(object obj)
        {
            if(obj is CacheKey key)
                return Equals(key);
            else
                return false;
        }

        public bool Equals(CacheKey other)
        {
            return IsValid == other.IsValid 
                && Pass.Equals(other.Pass) 
                && InputLayout.Equals(other.InputLayout);
        }
    }

    KeyedObjectCache<CacheKey, PipelineStateDX12> _cache = new();
    RootSignatureBuilderDX12 _rootBuilder;

    internal PipelineStateBuilderDX12(DeviceDX12 device)
    {
        _rootBuilder = new RootSignatureBuilderDX12(device);
    }

    internal unsafe PipelineStateDX12 Build(
        ShaderPassDX12 pass, 
        PipelineInputLayoutDX12 layout, 
        IndexBufferStripCutValue indexStripCutValue = IndexBufferStripCutValue.ValueDisabled,
        CachedPipelineState? cachedState = null)
    {
        CacheKey cacheKey = new(pass, layout);
        DeviceDX12 device = pass.Device as DeviceDX12;
        PipelineStateDX12 result = null;

        // Return the cached pipeline state.
        if (_cache.Check(ref cacheKey, ref result))
            return result;

        /* TODO Validate topology:
         *  - If the HS and DS members are specified, the PrimitiveTopologyType member for topology type must be set to D3D12_PRIMITIVE_TOPOLOGY_TYPE_PATCH.
         */

        // Proceed to create new pipeline state.
        GraphicsPipelineStateDesc desc = new()
        {
            InputLayout = layout.Desc,
            Flags = PipelineStateFlags.None,
            RasterizerState = pass.RasterizerState.Desc,
            BlendState = pass.BlendState.Description.Desc,
            SampleMask = pass.BlendState.Description.BlendSampleMask,
            DepthStencilState = pass.DepthState.Description.Desc,
            VS = pass.GetBytecode(ShaderStageType.Vertex),
            GS = pass.GetBytecode(ShaderStageType.Geometry),
            DS = pass.GetBytecode(ShaderStageType.Domain),
            HS = pass.GetBytecode(ShaderStageType.Hull),
            PS = pass.GetBytecode(ShaderStageType.Pixel),
            PrimitiveTopologyType = pass.Topology.ToApiPrimitiveType(),
            NodeMask = 0,               // TODO Set this to the node mask of the device.
            IBStripCutValue = indexStripCutValue,
            SampleDesc = new SampleDesc(1, 0),
        };

        // Check if cache data can be set.
        if(cachedState.HasValue)
        {
            if (cachedState.Value.PCachedBlob == null)
                throw new Exception("The provided cached state does not contain a valid blob pointer.");

            if (cachedState.Value.CachedBlobSizeInBytes == 0)
                throw new Exception("The provided cached state cannot have a blob size of 0.");

            desc.CachedPSO = cachedState.Value;
        }

        // Populate render target formats if a pixel shader is present in the pass.
        ShaderPassStage ps = pass[ShaderStageType.Pixel];
        if (ps != null)
        {
            desc.NumRenderTargets = (uint)ps.OutputLayout.Metadata.Length;

            for (int i = 0; i < desc.NumRenderTargets; i++)
                desc.RTVFormats[i] = (Format)pass.FormatLayout.RawFormats[i];
        }
        else // ... If no pixel shader is present, but a geometry shader is, populate the stream output format.
        {
            ShaderPassStage gs = pass[ShaderStageType.Geometry];
            if (gs != null)
            {
                int numEntries = gs.OutputLayout.Metadata.Length;   
                SODeclarationEntry* entries = stackalloc SODeclarationEntry[numEntries];
                for(int i = 0; i < numEntries; i++)
                {
                    ref ShaderIOLayout.ElementMetadata meta = ref gs.OutputLayout.Metadata[i];

                    entries[i] = new SODeclarationEntry()
                    {
                        Stream = meta.StreamOutput,
                        SemanticName = (byte*)SilkMarshal.StringToPtr(meta.Name),
                        SemanticIndex = meta.SemanticIndex,
                        StartComponent = 0, // TODO populate StartComponent
                        ComponentCount = (byte)meta.ComponentCount,
                        OutputSlot = 0,     // TODO populate - 0 to 3 only.
                    };
                }

                // TODO populate this properly.
                desc.StreamOutput = new StreamOutputDesc()
                {
                    RasterizedStream = pass.RasterizedStreamOutput,
                    NumEntries = (byte)numEntries, 
                    NumStrides = 0,         // TODO Populate this.
                    PBufferStrides = null,  // TODO Populate this.
                    PSODeclaration = entries,
                };
            }
        }

        // Populate depth surface format if depth and/or stencil testing is enabled
        if (pass.DepthState.Description.Desc.DepthEnable
            || pass.DepthState.Description.Desc.StencilEnable)
        {
            Format format = (Format)pass.FormatLayout.Depth.ToGraphicsFormat();
            desc.DSVFormat = format;
        }

        // Check multi-sample settings
        if(pass.RasterizerState.Desc.MultisampleEnable)
        {
            desc.SampleDesc = default;       // TODO Implement multisampling
        }

        RootSignatureDX12 rootSig = _rootBuilder.Build(pass, layout, ref desc);
        desc.PRootSignature = rootSig;

        Guid guid = ID3D12PipelineState.Guid;
        void* ptr = null;
        HResult hr = device.Handle->CreateGraphicsPipelineState(ref desc, &guid, &ptr);
        if (!device.Log.CheckResult(hr, () => "Failed to create pipeline state object (PSO)"))
            return null;

        result = new PipelineStateDX12(device, (ID3D12PipelineState*)ptr, rootSig);

        // Add the new pipeline state to the cache.
        _cache.Add(ref cacheKey, ref result);

        // Free all GS stream output semantic name pointers, if any.
        for(int i = 0; i < desc.StreamOutput.NumEntries; i++)
        {
            SODeclarationEntry* entry = &desc.StreamOutput.PSODeclaration[i];
            SilkMarshal.Free((IntPtr)entry->SemanticName);
        }

        return result;
    }
}
