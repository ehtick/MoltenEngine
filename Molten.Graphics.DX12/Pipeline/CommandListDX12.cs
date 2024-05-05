using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.Maths;

namespace Molten.Graphics.DX12;

public unsafe class CommandListDX12 : GpuCommandList
{
    bool _isClosed;
    ID3D12GraphicsCommandList7* _handle;
    PipelineInputLayoutDX12 _inputLayout;
    PipelineStateDX12 _pipelineState;

    CpuDescriptorHandle* _rtvs;
    CpuDescriptorHandle* _dsv;
    uint _numRTVs;

    D3DPrimitiveTopology _boundTopology;
    GpuDepthWritePermission _boundDepthMode;

    BarrierStateDX12[] _pendingBarriers;
    Dictionary<ResourceHandleDX12, uint> _pendingBarrierLookup;
    uint _nextBarrierIndex;

    internal CommandListDX12(CommandAllocatorDX12 allocator, ID3D12GraphicsCommandList7* handle) :
        base(allocator.Device)
    {
        _pendingBarrierLookup = new Dictionary<ResourceHandleDX12, uint>();
        _pendingBarriers = new BarrierStateDX12[256];

        Device = allocator.Device;
        Allocator = allocator;
        Fence = new FenceDX12(allocator.Device, FenceFlags.None);

        uint maxRTs = Device.Capabilities.PixelShader.MaxOutputTargets;
        _rtvs = EngineUtil.AllocArray<CpuDescriptorHandle>(maxRTs);
        _dsv = EngineUtil.AllocArray<CpuDescriptorHandle>(1);
        _dsv[0] = default;

        _handle = handle;
        Close();
    }

    protected override GpuResourceMap GetResourcePtr(GpuResource resource, uint subresource, GpuMapType mapType)
    {
        GpuResourceFlags flags = resource.Flags;

        // Validate map type.
        if (mapType == GpuMapType.Read)
        {
            if (!flags.Has(GpuResourceFlags.DownloadMemory))
                throw new InvalidOperationException($"Resource '{resource.Name}' does not allow read access.");
        }
        else if (mapType == GpuMapType.Write)
        {
            if (!flags.Has(GpuResourceFlags.UploadMemory))
                throw new InvalidOperationException($"Resource '{resource.Name}' does not allow write access.");
        }

        resource.Apply(this);
        if (resource.Handle is ResourceHandleDX12 handle)
        {
            ulong rowPitch = 0;
            ulong depthPitch = 0;

            if (resource is GpuTexture tex)
            {
                // TODO Calculate row pitch based on texture size, subresource level, format and dimensions. Also consider block-compression size.
            }
            else if (resource is BufferDX12 buffer)
            {
                if (buffer.RootBuffer != null)
                    rowPitch = buffer.RootBuffer.SizeInBytes;
                else
                    rowPitch = buffer.SizeInBytes;

                depthPitch = rowPitch;
            }

            // TODO Implement support for the read Range parameter. This determines the area of a sub-resouce that the CPU may want to read.
            //      Irrelevant for writing.

            void* ptrMap = null;
            HResult hr = handle.Ptr1->Map(subresource, null, &ptrMap);
            if (!Device.Log.CheckResult(hr, () => $"Failed to map resource {resource.Name} for {mapType} access"))
                return new GpuResourceMap();
            else
                return new GpuResourceMap(ptrMap, rowPitch, depthPitch);
        }
        else
        {
            throw new InvalidOperationException($"Resource '{resource.Name}' is not a valid DX12 resource.");
        }
    }

    protected override void OnUnmapResource(GpuResource resource, uint subresource)
    {
        if (resource.Handle is ResourceHandleDX12 handle)
            handle.Ptr1->Unmap(subresource, null);
    }

    protected override void CopyResource(GpuResource src, GpuResource dest)
    {
        src.Apply(this);
        dest.Apply(this);

        _handle->CopyResource((ResourceHandleDX12)dest.Handle, (ResourceHandleDX12)src.Handle);
        Profiler.ResourceCopyCalls++;
    }

    public override void Begin()
    {
        base.Begin();

        ID3D12PipelineState* pState = null; // initialState != null ? initialState.Handle : null; // TODO Add initial state support
        Handle->Reset(Device.CommandAllocator.Handle, pState);
        _isClosed = false;
    }

    public override void End()
    {
        base.End();
        Close();
    }

    private void Close()
    {
        if (!_isClosed)
        {
            _handle->Close();
            _isClosed = true;
        }
    }

    public override void Execute(params GpuCommandList[] cmds)
    {
        for (int i = 0; i < cmds.Length; i++)
        {
            CommandListDX12 dxCmd = (CommandListDX12)cmds[i];
            if (dxCmd.Type != CommandListType.Bundle)
                throw new GpuCommandListException(this, "Cannot execute a non-bundle command list on another command list");

            _handle->ExecuteBundle((ID3D12GraphicsCommandList*)dxCmd.Handle);
        }
    }

    public override void BeginEvent(string label)
    {
        // TODO Requires mappings for PIX on Windows: https://devblogs.microsoft.com/pix/winpixeventruntime/
        // See Also: https://learn.microsoft.com/en-us/gaming/gdk/_content/gc/reference/tools/pix3/functions/pixscopedevent-overloads
    }

    public override void EndEvent()
    {
        // TODO Requires mappings for PIX on Windows: https://devblogs.microsoft.com/pix/winpixeventruntime/
        // See Also: https://learn.microsoft.com/en-us/gaming/gdk/_content/gc/reference/tools/pix3/functions/pixscopedevent-overloads
    }

    public override void SetMarker(string label)
    {
        // TODO Requires mappings for PIX on Windows: https://devblogs.microsoft.com/pix/winpixeventruntime/
        // See Also: https://learn.microsoft.com/en-us/gaming/gdk/_content/gc/reference/tools/pix3/functions/pixscopedevent-overloads
    }

    protected override void OnGenerateMipmaps(GpuTexture texture)
    {
        // TODO: Implement compute-based mip-map generation - This can then be commonized for DX11/Vulkan too.
        //       See: https://www.3dgep.com/learning-directx-12-4/#Generate_Mipmaps_Compute_Shader

        Device.Log.Error("DX12 does not currently support compute-based mip-map generation.");
        //throw new NotImplementedException();
    }

    internal void Transition(GpuResource resource, ResourceStates newState, 
        ResourceBarrierFlags newFlags = ResourceBarrierFlags.None,
        uint startSubIndex = 0,
        uint endSubIndex = D3D12.ResourceBarrierAllSubresources)
    {
        ResourceHandleDX12 handle = resource.Handle as ResourceHandleDX12;
        Transition(handle, newState, newFlags, startSubIndex, endSubIndex);
    }

    internal void Transition(ResourceHandleDX12 handle, ResourceStates newState, 
        ResourceBarrierFlags newFlags = ResourceBarrierFlags.None,
        uint startSubIndex = 0,
        uint endSubIndex = D3D12.ResourceBarrierAllSubresources)
    {
#if DEBUG
        if (_isClosed)
            throw new InvalidOperationException("Cannot transition a resource while the command list is closed.");

        if (startSubIndex >= endSubIndex)
            throw new IndexOutOfRangeException("startSubIndex must be less than endSubIndex");
        else if (endSubIndex < D3D12.ResourceBarrierAllSubresources && endSubIndex > handle.State.NumSubResources)
            throw new IndexOutOfRangeException("endSubIndex must be less than the number of sub-resources in the resource.");
#endif

        // Local method for setting a barrier.
        bool SetBarrier(ref BarrierStateDX12 prevBarrier, ref BarrierStateDX12 newBarrier, ref ResourceBarrier barrier, uint subIndex)
        {
            // Validate flag pairing.
            if (prevBarrier.Flags == newBarrier.Flags)
            {
                if (prevBarrier.Flags == ResourceBarrierFlags.BeginOnly)
                    throw new InvalidOperationException("Cannot use BEGIN_ONLY flags again before END_ONLY flags were used.");
                else if (prevBarrier.Flags == ResourceBarrierFlags.EndOnly)
                    throw new InvalidOperationException("Cannot use END_ONLY flags again before BEGIN_ONLY flags were used.");

                // Skip setting the subresource barrier if it's already in the desired state.
                if (prevBarrier == newBarrier)
                    return false;
            }
            else if (prevBarrier.Flags != ResourceBarrierFlags.None && newBarrier.Flags == ResourceBarrierFlags.None) // Transitioning too none.
            {
                if (prevBarrier == newBarrier)
                    return false;
            }
            else if (prevBarrier.Flags == ResourceBarrierFlags.None && newBarrier.Flags != ResourceBarrierFlags.None) // Transitioning from none.
            {
                if (prevBarrier == newBarrier)
                    return false;
            }

            //Device.Log.Debug($"[Frame {Device.Renderer.FrameID}] Transitioning {handle.Resource.Name}[{subIndex}] from {prevBarrier.State} to {newState} - Flags: {newFlags}.");

            barrier = new ResourceBarrier()
            {
                Flags = newFlags,
                Type = ResourceBarrierType.Transition,
                Transition = new ResourceTransitionBarrier()
                {
                    PResource = handle,
                    StateAfter = newState,
                    StateBefore = prevBarrier.State,
                    Subresource = subIndex,
                },
            };

            prevBarrier.State = newState;
            if(newFlags != ResourceBarrierFlags.None)
                prevBarrier.Flags = newFlags; 

            return true;
        }

        if(!_pendingBarrierLookup.TryGetValue(handle, out uint barrierStartIndex))
        {
            barrierStartIndex = _nextBarrierIndex;
            _nextBarrierIndex += handle.State.NumSubResources;
            _pendingBarrierLookup.Add(handle, barrierStartIndex);

            // Set the barrier to the current global state of the resource.
            for (uint i = 0; i < handle.State.NumSubResources; i++)
                _pendingBarriers[barrierStartIndex + i] = handle.State[i];
        }

        BarrierStateDX12 newBarrier = new BarrierStateDX12(newState, newFlags);

        // If all sub resources should be set to the same state,
        // we use the special D3D12.ResourceBarrierAllSubresources value to do it in a single API call.
        if (endSubIndex == D3D12.ResourceBarrierAllSubresources)
        {
            ResourceBarrier barrier = new();
            ref BarrierStateDX12 prevBarrier = ref _pendingBarriers[barrierStartIndex];
            if (SetBarrier(ref prevBarrier, ref newBarrier, ref barrier, endSubIndex))
                _handle->ResourceBarrier(1, &barrier);
        }
        else
        {
            uint maxChangeCount = endSubIndex - startSubIndex;
            ResourceBarrier* barriers = stackalloc ResourceBarrier[(int)maxChangeCount];
            uint changeCount = 0;

            for (uint i = startSubIndex; i < endSubIndex; i++)
            {
                ref BarrierStateDX12 prevBarrier = ref _pendingBarriers[barrierStartIndex + i];
                if (SetBarrier(ref prevBarrier, ref newBarrier, ref barriers[changeCount], i))
                    changeCount++;
            }

            if (changeCount > 0)
                _handle->ResourceBarrier(changeCount, barriers);
        }
    }

    internal void ApplyBarrierStates()
    {
        foreach (KeyValuePair<ResourceHandleDX12, uint> kvp in _pendingBarrierLookup)
        {
            ResourceHandleDX12 handle = kvp.Key;
            uint startIndex = kvp.Value;

            for (uint i = 0; i < handle.State.NumSubResources; i++)
                handle.State[i] = _pendingBarriers[startIndex + i];
        }

        _nextBarrierIndex = 0;
        _pendingBarrierLookup.Clear();
    }

    protected override void OnResetState()
    {
        // Unbind all output surfaces
        _handle->OMSetRenderTargets(0, null, false, null);
        _boundTopology = D3DPrimitiveTopology.D3DPrimitiveTopologyUndefined;
        _boundDepthMode = GpuDepthWritePermission.Enabled;
    }


    protected override GpuBindResult DoRenderPass(ShaderPass hlslPass, QueueValidationMode mode, Action callback)
    {
        ShaderPassDX12 pass = hlslPass as ShaderPassDX12;
        D3DPrimitiveTopology passTopology = pass.Topology.ToApi();

        if (passTopology == D3DPrimitiveTopology.D3D11PrimitiveTopologyUndefined)
            return GpuBindResult.UndefinedTopology;

        // Clear output merger and rebind targets later.
        _handle->OMSetRenderTargets(0, null, false, null);

        // Check topology
        if (_boundTopology != passTopology)
        {
            _boundTopology = passTopology;
            _handle->IASetPrimitiveTopology(_boundTopology);
        }

        if (State.VertexBuffers.Bind(this))
            BindVertexBuffers();

        if (State.IndexBuffer.Bind(this))
        {
            GpuBuffer iBuffer = State.IndexBuffer.BoundValue;
            if (iBuffer != null)
            {
                IBHandleDX12 ibHandle = (IBHandleDX12)iBuffer.Handle;
                _handle->IASetIndexBuffer(ref ibHandle.View);
            }
            else
            {
                _handle->IASetIndexBuffer(null);
            }
        }

        // Check if viewports need updating.
        // TODO Consolidate - Molten viewports are identical in memory layout to DX11 viewports.
        if (State.Viewports.IsDirty)
        {
            fixed (ViewportF* ptrViewports = State.Viewports.Items)
                _handle->RSSetViewports((uint)State.Viewports.Length, (Silk.NET.Direct3D12.Viewport*)ptrViewports);

            State.Viewports.IsDirty = false;
        }

        // Check if scissor rects need updating
        if (State.ScissorRects.IsDirty)
        {
            fixed (Rectangle* ptrRect = State.ScissorRects.Items)
                _handle->RSSetScissorRects((uint)State.ScissorRects.Length, (Box2D<int>*)ptrRect);

            State.ScissorRects.IsDirty = false;
        }

        // Bind and transition render surfaces (BEGIN_ONLY).
        if (State.Surfaces.Bind(this))
        {
            _numRTVs = 0;

            for (int i = 0; i < State.Surfaces.Length; i++)
            {
                if (State.Surfaces.BoundValues[i] != null)
                {
                    RTHandleDX12 rsHandle = State.Surfaces.BoundValues[i].Handle as RTHandleDX12;
                    _rtvs[_numRTVs] = rsHandle.RTV.CpuHandle;
                    _numRTVs++;
                }
            }
        }

        // Bind depth surface.
        GpuDepthWritePermission depthWriteMode = pass.WritePermission;
        if (State.DepthSurface.Bind(this) || (_boundDepthMode != depthWriteMode))
        {
            if (State.DepthSurface.BoundValue != null && depthWriteMode != GpuDepthWritePermission.Disabled)
            {
                DSHandleDX12 dsHandle = State.DepthSurface.BoundValue.Handle as DSHandleDX12;
                if (depthWriteMode == GpuDepthWritePermission.ReadOnly)
                    _dsv[0] = dsHandle.ReadOnlyDSV.CpuHandle;
                else
                    _dsv[0] = dsHandle.DSV.CpuHandle;
            }
            else
            {
                _dsv[0] = default;
            }

            _boundDepthMode = depthWriteMode;
        }

        PipelineInputLayoutDX12 _inputLayout = GetInputLayout(pass);
        PipelineStateDX12 state = Device.StateBuilder.Build(pass, _inputLayout);

        _handle->SetPipelineState(state.Handle);
        _handle->SetGraphicsRootSignature(state.RootSignature.Handle);

        Device.Heap.PrepareGpuHeap(pass, this);

        CpuDescriptorHandle* dsvHandle = _dsv->Ptr != 0 ? _dsv : null;
        _handle->OMSetRenderTargets(_numRTVs, _rtvs, false, dsvHandle);
        Profiler.BindSurfaceCalls++;

        GpuBindResult vResult = Validate(mode);

        if (vResult == GpuBindResult.Successful)
        {
            // Re-render the same pass for K iterations.
            for (int k = 0; k < pass.Iterations; k++)
            {
                BeginEvent($"Iteration {k}");
                callback();
                Profiler.DrawCalls++;
                EndEvent();
            }
        }

        // Validate pipeline state.
        return vResult;
    }

    protected override GpuBindResult DoComputePass(ShaderPass pass)
    {
        throw new NotImplementedException();
    }

    protected override GpuBindResult CheckInstancing()
    {
        if (_inputLayout != null && _inputLayout.IsInstanced)
            return GpuBindResult.Successful;
        else
            return GpuBindResult.NonInstancedVertexLayout;
    }

    private void BindVertexBuffers()
    {
        int count = State.VertexBuffers.Length;
        VertexBufferView* pBuffers = stackalloc VertexBufferView[count];
        GpuBuffer buffer = null;

        for (int i = 0; i < count; i++)
        {
            buffer = State.VertexBuffers.BoundValues[i];

            if (buffer != null)
                pBuffers[i] = ((VBHandleDX12)buffer.Handle).View;
            else
                pBuffers[i] = default;
        }

        _handle->IASetVertexBuffers(0, (uint)count, pBuffers);
    }

    protected override unsafe void UpdateResource(GpuResource resource, uint subresource, ResourceRegion? region, void* ptrData, ulong rowPitch, ulong slicePitch)
    {
        //Box* destBox = null;

        //if (region != null)
        //{
        //    ResourceRegion value = region.Value;
        //    destBox = (Box*)&value;
        //}

        // TODO Calculate byte offset and number of bytes from resource region.

        using (GpuStream stream = MapResource(resource, subresource, 0, GpuMapType.Write))
        {
            stream.Write(ptrData, (long)slicePitch);
            Profiler.SubResourceUpdateCalls++;
        }
    }

    public override unsafe void CopyResourceRegion(GpuResource source, uint srcSubresource, ResourceRegion? sourceRegion, GpuResource dest, uint destSubresource, Vector3UI destStart)
    {
        throw new NotImplementedException();
    }

    public override GpuBindResult Draw(Shader shader, uint vertexCount, uint vertexStartIndex = 0)
    {
        return ApplyState(shader, QueueValidationMode.Unindexed, () =>
            _handle->DrawInstanced(vertexCount, 1, vertexStartIndex, 0));
    }

    public override GpuBindResult DrawInstanced(Shader shader, uint vertexCountPerInstance,
        uint instanceCount,
        uint vertexStartIndex = 0,
        uint instanceStartIndex = 0)
    {
        return ApplyState(shader, QueueValidationMode.Instanced, () =>
            _handle->DrawInstanced(vertexCountPerInstance, instanceCount, vertexStartIndex, instanceStartIndex));
    }

    public override GpuBindResult DrawIndexed(Shader shader, uint indexCount, int vertexIndexOffset = 0, uint startIndex = 0)
    {
        return ApplyState(shader, QueueValidationMode.Indexed, () =>
            _handle->DrawIndexedInstanced(indexCount, 1, startIndex, vertexIndexOffset, 0));
    }

    public override GpuBindResult DrawIndexedInstanced(Shader shader, uint indexCountPerInstance, uint instanceCount, uint startIndex = 0, int vertexIndexOffset = 0, uint instanceStartIndex = 0)
    {
        return ApplyState(shader, QueueValidationMode.InstancedIndexed, () =>
            _handle->DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndex, vertexIndexOffset, instanceStartIndex));
    }

    public override GpuBindResult Dispatch(Shader shader, Vector3UI groups)
    {
        DrawInfo.Custom.ComputeGroups = groups;
        return ApplyState(shader, QueueValidationMode.Compute, null);
    }

    /// <summary>Retrieves or creates a usable input layout for the provided vertex buffers and sub-effect.</summary>
    /// <returns>An instance of InputLayout.</returns>
    private PipelineInputLayoutDX12 GetInputLayout(ShaderPassDX12 pass)
    {
        // Retrieve layout list or create new one if needed.
        PipelineInputLayoutDX12 match = null;

        Device.PipelineLayoutCache.For(0, (index, layout) =>
        {
            if (layout.IsMatch(State.VertexBuffers))
            {
                match = layout;
                return true;
            }

            return false;
        });

        if (match != null)
            return match;

        PipelineInputLayoutDX12 input = new PipelineInputLayoutDX12(Device, State.VertexBuffers, pass);
        Device.PipelineLayoutCache.Add(input);

        return input;
    }

    public override void Free()
    {
        throw new NotImplementedException();
    }

    protected override void OnGpuRelease()
    {
        EngineUtil.Free(ref _rtvs);
        EngineUtil.Free(ref _dsv);
        NativeUtil.ReleasePtr(ref _handle);
        Fence?.Dispose();

        Allocator.Unallocate(this);
    }

    /// <summary>
    /// Gets the parent <see cref="CommandAllocatorDX12"/> from which the current <see cref="CommandListDX12"/> was allocated.
    /// </summary>
    internal CommandAllocatorDX12 Allocator { get; }

    public CommandListType Type => Allocator.Type;

    public override FenceDX12 Fence { get; }

    public static implicit operator ID3D12CommandList*(CommandListDX12 cmd) => (ID3D12CommandList*)cmd._handle;

    public unsafe ID3D12CommandList* BaseHandle => (ID3D12CommandList*)_handle;

    internal ref readonly ID3D12GraphicsCommandList7* Handle => ref _handle;

    public new DeviceDX12 Device { get; }
}
