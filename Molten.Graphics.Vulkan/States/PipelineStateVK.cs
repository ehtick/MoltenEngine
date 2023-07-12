﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Molten.Graphics.Vulkan
{
    internal unsafe class PipelineStateVK : GraphicsObject
    {
        /// <summary>
        /// A list of all shader stages in the order they are expected by Vulkan and Molten.
        /// </summary>
        internal static readonly ShaderType[] ShaderTypes = new ShaderType[] {
            ShaderType.Vertex,
            ShaderType.Hull,
            ShaderType.Domain,
            ShaderType.Geometry,
            ShaderType.Pixel,
            ShaderType.Compute
        };

        internal static readonly Dictionary<ShaderType, ShaderStageFlags> ShaderStageLookup = new Dictionary<ShaderType, ShaderStageFlags>()
        {
            [ShaderType.Vertex] = ShaderStageFlags.VertexBit,
            [ShaderType.Hull] = ShaderStageFlags.TessellationControlBit,
            [ShaderType.Domain] = ShaderStageFlags.TessellationEvaluationBit,
            [ShaderType.Geometry] = ShaderStageFlags.GeometryBit,
            [ShaderType.Pixel] = ShaderStageFlags.FragmentBit
        };

        GraphicsPipelineCreateInfo _info;
        Pipeline _pipeline;
        List<PipelineStateVK> _derivatives = new List<PipelineStateVK>();

        BlendStateVK _blendState;
        DepthStateVK _depthState;
        RasterizerStateVK _rasterizerState;
        DynamicStateVK _dynamicState;
        InputAssemblyStateVK _inputState;
        DescriptorSetLayoutVK _descriptorLayout;
        PipelineLayoutVK _pipelineLayout;

        AttachmentVK[] _attachments;

        public PipelineStateVK(DeviceVK device, ShaderPassVK pass, ref ShaderPassParameters parameters) : 
            base(device)
        {
            _attachments = new AttachmentVK[0];

            _info = new GraphicsPipelineCreateInfo();
            _info.SType = StructureType.GraphicsPipelineCreateInfo;
            _info.Flags = PipelineCreateFlags.None;
            _info.PNext = null;

            // Populate dynamic state
            _blendState = new BlendStateVK(device, ref parameters);
            _blendState = device.CacheObject(_blendState.Desc, _blendState);

            _depthState = new DepthStateVK(device, ref parameters);
            _depthState = device.CacheObject(_depthState.Desc, _depthState);

            _rasterizerState = new RasterizerStateVK(device, ref parameters);
            _rasterizerState = device.CacheObject(_rasterizerState.Desc, _rasterizerState);

            _inputState = new InputAssemblyStateVK(device, ref parameters);
            _inputState = device.CacheObject(_inputState.Desc, _inputState);

            DynamicState[] dynamics = new DynamicState[]
            {
                DynamicState.ViewportWithCount,
                DynamicState.ScissorWithCount,
            };

            _dynamicState = new DynamicStateVK(device, ref parameters, dynamics);
            _dynamicState = device.CacheObject(_dynamicState.Desc, _dynamicState);

            // Setup shader stage info
            _info.PStages = EngineUtil.AllocArray<PipelineShaderStageCreateInfo>((uint)pass.CompositionCount);
            _info.StageCount = 0;

            // Iterate over and add pass compositions in the order Vulkan expects.
            foreach (ShaderType type in ShaderTypes)
            {
                ShaderComposition c = pass[type];
                if (c == null)
                    continue;

                ref PipelineShaderStageCreateInfo stageDesc = ref _info.PStages[_info.StageCount++];
                stageDesc.SType = StructureType.PipelineShaderStageCreateInfo;
                stageDesc.PName = (byte*)SilkMarshal.StringToPtr(c.EntryPoint, NativeStringEncoding.UTF8);
                stageDesc.Stage = ShaderStageLookup[type];
                stageDesc.Module = *(ShaderModule*)c.PtrShader;
                stageDesc.Flags = PipelineShaderStageCreateFlags.None;
                stageDesc.PNext = null;
            }

            _descriptorLayout = new DescriptorSetLayoutVK(device, pass);
            _pipelineLayout = new PipelineLayoutVK(device, _descriptorLayout);

            _info.PMultisampleState = null;                         // TODO initialize
            _info.BasePipelineIndex = 0;                            // TODO initialize
            _info.BasePipelineHandle = new Pipeline();              // TODO initialize 
            _info.PTessellationState = null;                        // TODO initialize
            _info.PVertexInputState = null;                         // TODO initialize
            _info.PViewportState = null;                            // Ignored. Set in dynamic state.
            _info.RenderPass = new RenderPass();                    // TODO initialize - This should be stored in RenderStep based on inputput and output surfaces.
            _info.Subpass = 0;                                      // TODO initialize

            _info.PColorBlendState = _blendState.Desc;
            _info.PRasterizationState = _rasterizerState.Desc;
            _info.PDepthStencilState = _depthState.Desc;
            _info.PDynamicState = _dynamicState.Desc;
            _info.PInputAssemblyState = _inputState.Desc;
            _info.Layout = _pipelineLayout.Handle;

            /* TODO Implement derivative pipeline support:
             *   - ShaderPass will provide a base PassPipelineVK instance
             *   - GraphicsDevice will store PassPipelineVK instances by ShaderPassVK and render surface count + type
             *   - GraphicsQueue.ApplyRenderState() or ApplyComputeState() must check the pipeline cache for a matching pipelines based on: 
             *      - Bound render surfaces
             *   - See: https://registry.khronos.org/vulkan/specs/1.3-extensions/html/vkspec.html#pipelines-pipeline-derivatives
             *      - Ensure VK_PIPELINE_CREATE_DERIVATIVE_BIT is set on derivative pipelines.
             *      - Base pipeline must set VK_PIPELINE_CREATE_ALLOW_DERIVATIVES_BIT 
             *      
             *   - Implement PipelineCacheVK:
             *      - Implement PipelineVK and move all properties out of ShaderPassVK and into PipelineVK
             */

            // Create pipeline.
            fixed (GraphicsPipelineCreateInfo* pResult = &_info)
            {
                fixed (Pipeline* ptrPipeline = &_pipeline)
                    device.VK.CreateGraphicsPipelines(device, new PipelineCache(), 1, _info, null, ptrPipeline);
            }

            // TODO after render/compute pass, reset the load-op of surfaces.
        }

        protected PipelineStateVK(DeviceVK device, PipelineStateVK baseState, IRenderSurfaceVK[] surfaces) : 
            base(device)
        {
            if (baseState == null)
                throw new ArgumentNullException(nameof(baseState), "Base state cannot be null");

            _attachments = new AttachmentVK[surfaces.Length];
            _info = new GraphicsPipelineCreateInfo();
            _info.SType = StructureType.GraphicsPipelineCreateInfo;
            _info.Flags = PipelineCreateFlags.None;
            _info.PNext = null;
            _info.BasePipelineHandle = baseState;
            BaseState = baseState;

            for (int i = 0; i < surfaces.Length; i++)
                _attachments[i] = new AttachmentVK(surfaces[i]);


            /* TODO:
             *  - Remove IRenderSurfaceVK
             *  - Move IRenderSurfaceVK.ClearColor to IRenderSurface
             *  - Surface clearing should be API-specific
             *  - If color is null, clear should not be performed
             *  - Doing clears in this way allows Vulkan to optimize clear commands by doing it directly in the render pass via attachment load ops (LOAD_OP_CLEAR).
             *      -- Also uses VkClearValue via the RenderPassBeginInfo struct.
             *      -- This also allows us to clear multiple targets at once, including depth/stencil.
             */
        }

        internal PipelineStateVK GetState(params IRenderSurfaceVK[] surfaces)
        {
            DeviceVK device = Device as DeviceVK;

            if (DoSurfacesMatch(device, surfaces))
                return this;
            else
                return GetDerivation(device, surfaces);
        }

        private PipelineStateVK GetDerivation(DeviceVK device, IRenderSurfaceVK[] surfaces)
        {
            // Check if we have an existing derivative that matches our surface attachments.
            foreach (PipelineStateVK derivative in _derivatives)
            {
                if(derivative.DoSurfacesMatch(device, surfaces))
                    return derivative;  
            }

            PipelineStateVK derivation = new PipelineStateVK(device, this, surfaces);
            _derivatives.Add(derivation);
            return derivation;
        }

        private bool DoSurfacesMatch(DeviceVK device, IRenderSurface[] surfaces)
        {
            if (surfaces.Length == _attachments.Length)
            {
                for (int i = 0; i < surfaces.Length; i++)
                {
                    if (!surfaces[i].Equals(_attachments[i]))
                        return false;
                }
            }

            return true;
        }

        protected override void OnGraphicsRelease()
        {
            DeviceVK device = Device as DeviceVK;

            // Release indirect memory allocations for pipleine shader stages
            for (uint i = 0; i < _info.StageCount; i++)
            {
                ref PipelineShaderStageCreateInfo stageDesc = ref _info.PStages[i];
                SilkMarshal.FreeString((nint)stageDesc.PName);
            }

            EngineUtil.Free(ref _info.PStages);

            _pipelineLayout.Dispose();
            _descriptorLayout.Dispose();

            if (_pipeline.Handle != 0)
            {
                device.VK.DestroyPipeline(device, _pipeline, null);
                _pipeline = new Pipeline();
            }
        }

        public static implicit operator Pipeline(PipelineStateVK state)
        {
            return state._pipeline;
        }

        internal PipelineStateVK BaseState { get; }
    }
}
