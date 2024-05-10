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

    GpuFrameBuffer<DescriptorHeapAllocatorDX12> _gpuResourceHeap;
    GpuFrameBuffer<DescriptorHeapAllocatorDX12> _gpuSamplerHeap;

    DescriptorHeapAllocatorDX12 _prevGpuResourceHeap;
    DescriptorHeapAllocatorDX12 _prevGpuSamplerHeap;

    /// <summary>
    /// Creates a new instance of <see cref="DescriptorHeapAllocatorDX12"/>.
    /// </summary>
    /// <param name="device">The device that the heap manager belongs to.</param>
    /// <param name="heapCapacity">The number of slots to provision within each individual heap.</param>
    internal unsafe DescriptorHeapManagerDX12(DeviceDX12 device) :
        base(device)
    {
        _resourceHeap = new DescriptorHeapAllocatorDX12(device, DescriptorHeapType.CbvSrvUav, DescriptorHeapFlags.None, 64);
        _samplerHeap = new DescriptorHeapAllocatorDX12(device, DescriptorHeapType.Sampler, DescriptorHeapFlags.None, 64);
        _dsvHeap = new DescriptorHeapAllocatorDX12(device, DescriptorHeapType.Dsv, DescriptorHeapFlags.None, 64);
        _rtvHeap = new DescriptorHeapAllocatorDX12(device, DescriptorHeapType.Rtv, DescriptorHeapFlags.None, 64);

        _gpuResourceHeap = new GpuFrameBuffer<DescriptorHeapAllocatorDX12>(device, (creationDevice) =>
        {
            return new DescriptorHeapAllocatorDX12(creationDevice as DeviceDX12, 
                DescriptorHeapType.CbvSrvUav, 
                DescriptorHeapFlags.ShaderVisible, 
                RESOURCE_HEAP_SIZE);
        });

        _gpuSamplerHeap = new GpuFrameBuffer<DescriptorHeapAllocatorDX12>(device, (creationDevice) =>
        {
            return new DescriptorHeapAllocatorDX12(creationDevice as DeviceDX12, 
                DescriptorHeapType.Sampler, 
                DescriptorHeapFlags.ShaderVisible, 
                SAMPLER_HEAP_SIZE);
        });
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
        HeapHandleDX12 resTable = PrepareResourceTable(pass, state, cmd);
        HeapHandleDX12 samplerTable = PrepareSamplerTable(pass, state, cmd);
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

    private unsafe HeapHandleDX12 PrepareResourceTable(ShaderPassDX12 pass, PipelineStateDX12 state, CommandListDX12 cmd)
    {
        if(pass.Bindings.TotalResourceBindings == 0)
            return new HeapHandleDX12();

        DeviceDX12 device = pass.Device as DeviceDX12;
        DescriptorHeapAllocatorDX12 resAllocator = _gpuResourceHeap.Prepare();
        if (resAllocator != _prevGpuResourceHeap)
            resAllocator.Reset();

        HeapHandleDX12 gpuResHandle = resAllocator.Allocate(pass.Bindings.TotalResourceBindings);
        HeapHandleDX12 resTableHandle = gpuResHandle;

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

                // TODO Improve this
                ResourceHandleDX12 resHandle = bind.Object.Resource.Handle as ResourceHandleDX12;
                CpuDescriptorHandle cpuHandle = default;
                switch (bindType)
                {
                    case ShaderBindType.ConstantBuffer:
                        CBHandleDX12 cbHandle = resHandle as CBHandleDX12;
                        cpuHandle = cbHandle.CBV.CpuHandle;
                        cmd.Transition(resHandle, ResourceStates.VertexAndConstantBuffer);
                        break;

                    case ShaderBindType.Resource:
                        cpuHandle = resHandle.SRV.CpuHandle;
                        cmd.Transition(resHandle, ResourceStates.AllShaderResource);
                        break;

                    case ShaderBindType.UnorderedAccess:
                        cpuHandle = resHandle.UAV.CpuHandle;
                        // TODO Handle UAV barriers?
                        break;
                }

                if (cpuHandle.Ptr != 0)
                {
                    Device.Handle->CopyDescriptorsSimple(1, gpuResHandle.Handle, cpuHandle, DescriptorHeapType.CbvSrvUav);
                }

                gpuResHandle.Increment();
            }
        }

        _prevGpuResourceHeap = resAllocator;
        return resTableHandle;
    }

    private unsafe HeapHandleDX12 PrepareSamplerTable(ShaderPassDX12 pass, PipelineStateDX12 state, CommandListDX12 cmd)
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

        DescriptorHeapAllocatorDX12 samplerAllocator = _gpuSamplerHeap.Prepare();
        if (samplerAllocator != _prevGpuSamplerHeap)
            samplerAllocator.Reset();

        HeapHandleDX12 gpuSamplerHandle = samplerAllocator.Allocate(numHeapSamplers);
        HeapHandleDX12 samplerTableHandle = gpuSamplerHandle;

        // Iterate over samplers and bind heap-based ones.
        for (int i = 0; i < pass.Bindings.Samplers.Length; i++)
        {
            ref ShaderBind<ShaderSamplerVariable> bind = ref pass.Bindings.Samplers[i];
            if (!bind.Object.IsImmutable && bind.Object?.Value != null)
            {
                ShaderSampler heapSampler = bind.Object.Sampler;
                CpuDescriptorHandle cpuHandle = new();  // TODO Retrieve heap handle for HeapSamplerDX12 and copy it to the sampler heap.

                if (cpuHandle.Ptr != 0)
                    Device.Handle->CopyDescriptorsSimple(1, gpuSamplerHandle.Handle, cpuHandle, DescriptorHeapType.Sampler);

                gpuSamplerHandle.Increment();
            }
        }

        _prevGpuSamplerHeap = samplerAllocator;
        return samplerTableHandle;
    }

    protected unsafe override void OnGpuRelease()
    {
        _resourceHeap.Dispose(true);
        _samplerHeap.Dispose(true);
        _dsvHeap.Dispose(true);
        _rtvHeap.Dispose(true);

        _gpuResourceHeap.Dispose(true);
        _gpuSamplerHeap.Dispose(true);

        _prevGpuResourceHeap = null;
        _prevGpuSamplerHeap = null;
    }
}