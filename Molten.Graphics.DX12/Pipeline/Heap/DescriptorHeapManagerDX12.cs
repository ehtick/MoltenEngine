using Silk.NET.Direct3D12;

namespace Molten.Graphics.DX12;
internal class DescriptorHeapManagerDX12 : GpuObject<DeviceDX12>
{
    const int RESOURCE_HEAP_SIZE = 512;
    const int SAMPLER_HEAP_SIZE = 256;

    DescriptorHeapAllocatorDX12 _resourceHeap;
    DescriptorHeapAllocatorDX12 _samplerHeap;
    DescriptorHeapAllocatorDX12 _dsvHeap;
    DescriptorHeapAllocatorDX12 _rtvHeap;

    GpuFrameBuffer<DescriptorHeapAllocatorDX12> _gpuResourceHeapBuffer;
    GpuFrameBuffer<DescriptorHeapAllocatorDX12> _gpuSamplerHeapBuffer;

    DescriptorHeapAllocatorDX12 _gpuResourceHeap;
    DescriptorHeapAllocatorDX12 _gpuSamplerHeap;
    Stack<HeapHandleDX12> _heapHandlePool;

    /// <summary>
    /// Creates a new instance of <see cref="DescriptorHeapAllocatorDX12"/>.
    /// </summary>
    /// <param name="device">The device that the heap manager belongs to.</param>
    /// <param name="heapCapacity">The number of slots to provision within each individual heap.</param>
    internal unsafe DescriptorHeapManagerDX12(DeviceDX12 device) :
        base(device)
    {
        _heapHandlePool = new Stack<HeapHandleDX12>();
        _resourceHeap = new DescriptorHeapAllocatorDX12(this, DescriptorHeapType.CbvSrvUav, DescriptorHeapFlags.None, 512);
        _samplerHeap = new DescriptorHeapAllocatorDX12(this, DescriptorHeapType.Sampler, DescriptorHeapFlags.None, 512);
        _dsvHeap = new DescriptorHeapAllocatorDX12(this, DescriptorHeapType.Dsv, DescriptorHeapFlags.None, 512);
        _rtvHeap = new DescriptorHeapAllocatorDX12(this, DescriptorHeapType.Rtv, DescriptorHeapFlags.None, 512);

        _gpuResourceHeapBuffer = new GpuFrameBuffer<DescriptorHeapAllocatorDX12>(device, (creationDevice) =>
        {
            return new DescriptorHeapAllocatorDX12(this, 
                DescriptorHeapType.CbvSrvUav, 
                DescriptorHeapFlags.ShaderVisible, 
                RESOURCE_HEAP_SIZE);
        });

        _gpuSamplerHeapBuffer = new GpuFrameBuffer<DescriptorHeapAllocatorDX12>(device, (creationDevice) =>
        {
            return new DescriptorHeapAllocatorDX12(this, 
                DescriptorHeapType.Sampler, 
                DescriptorHeapFlags.ShaderVisible, 
                SAMPLER_HEAP_SIZE);
        });
    }

    internal HeapHandleDX12 GetHandleInstance()
    {
        if (_heapHandlePool.Count > 0)
            return _heapHandlePool.Pop();
        else 
            return new HeapHandleDX12();
    }

    internal void PoolHandle(HeapHandleDX12 handle)
    {
        handle.Heap = null;
        handle.Next = null;
        _heapHandlePool.Push(handle);
    }

    internal HeapHandleDX12 GetResourceHandle(uint numDescriptors)
    {
        return _resourceHeap.Allocate(numDescriptors);
    }

    internal HeapHandleDX12 GetRTHandle(uint numDescriptors)
    {
        return _rtvHeap.Allocate(numDescriptors);
    }

    internal HeapHandleDX12 GetDepthHandle(uint numDescriptors)
    {
        return _dsvHeap.Allocate(numDescriptors);
    }

    internal HeapHandleDX12 GetSamplerHandle(uint numDescriptors)
    {
        return _samplerHeap.Allocate(numDescriptors);
    }

    /// <summary>
    /// Consolidates all of the CPU-side descriptors into a single GPU descriptor heap ready for use.
    /// </summary>
    internal unsafe void PrepareGpuHeap(ShaderPassDX12 pass, PipelineStateDX12 state, CommandListDX12 cmd)
    {
        HeapHandleDX12 resTable = BuildGpuResourceTable(pass, state, cmd);
        HeapHandleDX12 samplerTable = BuildGpuSamplerTable(pass, state, cmd);
        //state.RootSignature.Meta.ToLog(Device.Log);

        if (resTable.Heap != null && samplerTable.Heap != null)
        {
            ID3D12DescriptorHeap** pHeaps = stackalloc ID3D12DescriptorHeap*[2] { resTable.Heap.Handle, samplerTable.Heap.Handle };

            cmd.Handle->SetDescriptorHeaps(2, pHeaps);
            cmd.Handle->SetGraphicsRootDescriptorTable(0, resTable.GetGpuHandle());
            cmd.Handle->SetGraphicsRootDescriptorTable(1, samplerTable.GetGpuHandle());
        }
        else if (resTable.Heap != null)
        {
            ID3D12DescriptorHeap** pHeaps = stackalloc ID3D12DescriptorHeap*[1] { resTable.Heap.Handle };
            cmd.Handle->SetDescriptorHeaps(1, pHeaps);
            cmd.Handle->SetGraphicsRootDescriptorTable(0, resTable.GetGpuHandle());
        }
        else if (samplerTable.Heap != null)
        {
            ID3D12DescriptorHeap** pHeaps = stackalloc ID3D12DescriptorHeap*[1] { samplerTable.Heap.Handle };
            cmd.Handle->SetDescriptorHeaps(1, pHeaps);
            cmd.Handle->SetGraphicsRootDescriptorTable(0, samplerTable.GetGpuHandle());
        }
    }

    internal void Prepare()
    {
        _resourceHeap.Defragment();
        _samplerHeap.Defragment();
        _dsvHeap.Defragment();
        _rtvHeap.Defragment();

        _gpuResourceHeap = _gpuResourceHeapBuffer.Prepare();
        _gpuResourceHeap.Reset();

        _gpuSamplerHeap = _gpuSamplerHeapBuffer.Prepare();
        _gpuSamplerHeap.Reset();

        _gpuResourceHeap.Defragment();
        _gpuSamplerHeap.Defragment();
    }

    private unsafe HeapHandleDX12 BuildGpuResourceTable(ShaderPassDX12 pass, PipelineStateDX12 state, CommandListDX12 cmd)
    {
        if(pass.Bindings.TotalResourceBindings == 0)
            return new HeapHandleDX12();

        HeapHandleDX12 gpuResHandle = _gpuResourceHeap.Allocate(pass.Bindings.TotalResourceBindings);
        CpuDescriptorHandle destHandle = gpuResHandle.CpuHandle;

        // TODO Replace this once DX11 is removed and resources can be created during instantiation instead of during Apply().
        // Apply resources.
        for (int i = 0; i < pass.Bindings.Resources.Length; i++)
        {
            ShaderBindType bindType = (ShaderBindType)i;
            ref ShaderBind<ShaderResourceVariable>[] resources = ref pass.Bindings.Resources[i];

            // Iterate over resources
            for (int r = 0; r < resources.Length; r++)
            {
                ref ShaderBind<ShaderResourceVariable> bind = ref resources[r];
                bind.Object.Resource?.Apply(cmd);

                // TODO Improve this - Batch copy instead of copying 1 at a time. This requires batched resource handles in shaders.
                ResourceHandleDX12 resHandle = bind.Object.Resource.Handle as ResourceHandleDX12;
                CpuDescriptorHandle srcHandle = default;

                switch (bindType)
                {
                    case ShaderBindType.ConstantBuffer:
                        CBHandleDX12 cbHandle = resHandle as CBHandleDX12;
                        srcHandle = cbHandle.CBV.CpuHandle;
                        cmd.Transition(resHandle, ResourceStates.VertexAndConstantBuffer);
                        break;

                    case ShaderBindType.Resource:
                        srcHandle = resHandle.SRV.CpuHandle;
                        cmd.Transition(resHandle, ResourceStates.AllShaderResource);
                        break;

                    case ShaderBindType.UnorderedAccess:
                        srcHandle = resHandle.UAV.CpuHandle;
                        // TODO Handle UAV barriers?
                        break;
                }

                if (srcHandle.Ptr != 0)
                    Device.Handle->CopyDescriptorsSimple(1, destHandle, srcHandle, DescriptorHeapType.CbvSrvUav);

                destHandle.Ptr += gpuResHandle.Heap.IncrementSize;
            }
        }

        return gpuResHandle;
    }

    private unsafe HeapHandleDX12 BuildGpuSamplerTable(ShaderPassDX12 pass, PipelineStateDX12 state, CommandListDX12 cmd)
    {
        uint numSamplers = (uint)pass.Bindings.Samplers.Length;
        uint numHeapSamplers = 0;

        // Perform a quick check to find out how many heap-based samplers we have, if any.
        for (uint i = 0; i < numSamplers; i++)
        {
            ref ShaderBind<ShaderSamplerVariable> bind = ref pass.Bindings.Samplers[i];
            if (!bind.Object.IsImmutable)
                numHeapSamplers++;
        }


        if (numHeapSamplers == 0)
            return new HeapHandleDX12();

        HeapHandleDX12 gpuSamplerHandle = _gpuSamplerHeap.Allocate(numHeapSamplers);
        CpuDescriptorHandle destHandle = gpuSamplerHandle.CpuHandle;

        // Iterate over samplers and bind heap-based ones.
        for (int i = 0; i < pass.Bindings.Samplers.Length; i++)
        {
            ref ShaderBind<ShaderSamplerVariable> bind = ref pass.Bindings.Samplers[i];
            if (!bind.Object.IsImmutable && bind.Object?.Value != null)
            {
                ShaderSampler heapSampler = bind.Object.Sampler;
                CpuDescriptorHandle srcHandle = new();  // TODO Retrieve heap handle for HeapSamplerDX12 and copy it to the sampler heap.

                if (srcHandle.Ptr != 0)
                    Device.Handle->CopyDescriptorsSimple(1, destHandle, srcHandle, DescriptorHeapType.Sampler);

                destHandle.Ptr += gpuSamplerHandle.Heap.IncrementSize;
            }
        }

        return gpuSamplerHandle;
    }

    protected unsafe override void OnGpuRelease()
    {
        _resourceHeap.Dispose(true);
        _samplerHeap.Dispose(true);
        _dsvHeap.Dispose(true);
        _rtvHeap.Dispose(true);

        _gpuResourceHeapBuffer.Dispose(true);
        _gpuSamplerHeapBuffer.Dispose(true);

        _gpuResourceHeap = null;
        _gpuSamplerHeap = null;
    }
}