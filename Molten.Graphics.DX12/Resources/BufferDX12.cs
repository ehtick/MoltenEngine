using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Molten.Graphics.DX12;

public sealed class BufferDX12 : GpuBuffer
{
    ResourceHandleDX12 _handle;
    ResourceBarrier _barrier;
    ResourceStates _barrierState;
    internal D3DCBufferType _cbufferType;

    internal BufferDX12(DeviceDX12 device, ConstantBufferInfo info)
    : this(device, 1, info.Size + (256 - (info.Size % 256)), GpuResourceFlags.DenyShaderAccess | GpuResourceFlags.UploadMemory, GpuBufferType.Constant, 1)
    {
        ConstantData = new GpuConstantData(info);
    }

    internal BufferDX12(DeviceDX12 device, uint stride, ulong numElements, GpuResourceFlags flags, GpuBufferType type, uint alignment) :
        base(device, stride, numElements, flags, type, alignment)
    {
        Device = device;
    }

    private BufferDX12(BufferDX12 parentBuffer, ulong offset, uint stride, ulong numElements, GpuResourceFlags flags, GpuBufferType type, uint alignment)
        : base(parentBuffer.Device, stride, numElements, flags, type, alignment)
    {
        if (parentBuffer != null)
        {
            ParentBuffer = parentBuffer;
            RootBuffer = parentBuffer.RootBuffer ?? parentBuffer;
        }

        Offset = parentBuffer.Offset + offset;
    }

    protected unsafe override void OnApply(GpuCommandList cmd)
    {
        if (_handle == null)
        {
            HeapFlags heapFlags = HeapFlags.None;
            ResourceFlags flags = Flags.ToResourceFlags();
            HeapType heapType = Flags.ToHeapType(); // TODO Add UMA support.
            ResourceStates stateFlags = Flags.ToResourceState();
            ID3D12Resource1* resource = null;

            if (ParentBuffer == null)
            {
                HeapProperties heapProp = new HeapProperties()
                {
                    Type = heapType,
                    CPUPageProperty = CpuPageProperty.Unknown,
                    CreationNodeMask = 1,
                    MemoryPoolPreference = MemoryPool.Unknown,
                    VisibleNodeMask = 1,
                };

                // TODO Adjust for GPU memory architecture based on UMA support.
                // See for differences: https://microsoft.github.io/DirectX-Specs/d3d/D3D12GPUUploadHeaps.html
                if (heapType == HeapType.Custom)
                {
                    // TODO Set CPUPageProperty and MemoryPoolPreference based on UMA support.
                }

                if (Flags.Has(GpuResourceFlags.DenyShaderAccess))
                    flags |= ResourceFlags.DenyShaderResource;

                if (Flags.Has(GpuResourceFlags.UnorderedAccess))
                    flags |= ResourceFlags.AllowUnorderedAccess;

                ResourceDesc1 desc = new()
                {
                    Dimension = ResourceDimension.Buffer,
                    Alignment = 0,
                    Width = SizeInBytes,
                    Height = 1,
                    DepthOrArraySize = 1,
                    Layout = TextureLayout.LayoutRowMajor,
                    Format = ResourceFormat.ToApi(),
                    Flags = flags,
                    MipLevels = 1,
                    SampleDesc = new SampleDesc(1, 0),
                };

                Guid guid = ID3D12Resource1.Guid;
                void* ptr = null;
                HResult hr = Device.Handle->CreateCommittedResource2(heapProp, heapFlags, desc, stateFlags, null, null, &guid, &ptr);
                if (!Device.Log.CheckResult(hr, () => $"Failed to create {desc.Dimension} resource"))
                    return;

                resource = (ID3D12Resource1*)ptr;
            }
            else
            {
                RootBuffer.Apply(cmd);
                resource = (ID3D12Resource1*)RootBuffer.Handle.Ptr;
            }

            _handle = OnCreateHandle(resource);
        }
    }

    private unsafe ResourceHandleDX12 OnCreateHandle(ID3D12Resource1* ptr)
    {
        switch (BufferType)
        {
            case GpuBufferType.Vertex:
                _handle = new VBHandleDX12(this, ptr);
                break;

            case GpuBufferType.Index:
                _handle = new IBHandleDX12(this, ptr);
                break;

            case GpuBufferType.Constant:
                _handle = new CBHandleDX12(this, ptr);
                break;

            default:
                _handle = new ResourceHandleDX12(this, ptr);
                break;
        }

        InitializeViews();
        return _handle;
    }

    private unsafe void InitializeViews()
    {
        switch (_handle)
        {
            case VBHandleDX12 vbHandle:
                vbHandle.View = new VertexBufferView()
                {
                    BufferLocation = _handle.Ptr1->GetGPUVirtualAddress() + Offset,
                    SizeInBytes = (uint)SizeInBytes,
                    StrideInBytes = Stride,
                };
                break;

            case IBHandleDX12 ibHandle:
                ibHandle.View = new IndexBufferView()
                {
                    BufferLocation = _handle.Ptr1->GetGPUVirtualAddress() + Offset,
                    Format = ResourceFormat.ToApi(),
                    SizeInBytes = (uint)SizeInBytes,
                };
                break;

            case CBHandleDX12 cbHandle:
                // TODO Constant buffers must be 256-bit aligned, which means the data sent to SetData() must be too.
                //      We can validate this by checking if the stride is a multiple of 256: sizeof(T) % 256 == 0
                //      This value is also provided via D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT.
                //      
                //      If not, we throw an exception stating this.

                ConstantBufferViewDesc cbDesc = new()
                {
                    BufferLocation = _handle.Ptr1->GetGPUVirtualAddress() + Offset,
                    SizeInBytes = (uint)SizeInBytes,
                };
                cbHandle.CBV.Initialize(ref cbDesc);
                break;
        }

        if (!Flags.Has(GpuResourceFlags.DenyShaderAccess))
        {
            ShaderResourceViewDesc desc = new()
            {
                Format = ResourceFormat.ToApi(),
                ViewDimension = SrvDimension.Buffer,
                Shader4ComponentMapping = ResourceInterop.EncodeDefault4ComponentMapping(),
                Buffer = new BufferSrv()
                {
                    FirstElement = Stride > 0 ? (Offset / Stride) : 0,
                    NumElements = (uint)ElementCount,
                    Flags = BufferSrvFlags.None,
                    StructureByteStride = Stride, // TODO If stride is 0, then it is a typed buffer, where the ResourceFormat must be set to a valid format.
                },
            };

            _handle.SRV.Initialize(ref desc);
        }

        if (Flags.Has(GpuResourceFlags.UnorderedAccess))
        {
            UnorderedAccessViewDesc desc = new()
            {
                Format = ResourceFormat.ToApi(),
                ViewDimension = UavDimension.Buffer,
                Buffer = new BufferUav()
                {
                    FirstElement = Stride > 0 ? (Offset / Stride) : 0,
                    NumElements = (uint)ElementCount,
                    Flags = BufferUavFlags.None,
                    CounterOffsetInBytes = 0,
                    StructureByteStride = Stride, // TODO If stride is 0, then it is a typed buffer, where the ResourceFormat must be set to a valid format.
                },
            };

            _handle.UAV.Initialize(ref desc);
        }
    }

    protected override GpuBuffer OnAllocateSubBuffer(
        ulong offset, 
        uint stride, 
        ulong numElements, 
        GpuResourceFlags flags,
        GpuBufferType type,
        uint alignment)
    {
        // TODO check through existing allocations to see if we can re-use one.
        return new BufferDX12(this, offset, stride, numElements, Flags, BufferType, alignment);
    }

    public override bool SetLocation(ulong offset, uint stride, ulong numBytes, Logger log = null)
    {
        if (ParentBuffer == null)
            throw new InvalidOperationException("Cannot set the location of a root GPU buffer. Must be a sub-allocated buffer");

        ulong parentMaxOffset = ParentBuffer.Offset + ParentBuffer.SizeInBytes;

        if (offset >= parentMaxOffset)
        {
            log?.Error("The offset of the sub-allocated buffer exceeds the size of the parent buffer.");
            return false;
        }

        if (offset + numBytes >= parentMaxOffset)
        {
            log?.Error("The offset + size of the sub-allocated buffer exceeds the size of the parent buffer.");
            return false;
        }

        Offset = offset;
        SizeInBytes = numBytes;
        Stride = stride;

        if(_handle != null)
            InitializeViews();

        return true;
    }

    protected override void OnGpuRelease()
    {
        _handle?.Dispose();
        base.OnGpuRelease();
    }

    /// <inheritdoc/>
    public override ResourceHandleDX12 Handle => _handle;

    /// <inheritdoc/>
    public override GpuResourceFormat ResourceFormat { get; protected set; }

    public new DeviceDX12 Device { get; }

    /// <summary>
    /// Gets the root <see cref="BufferDX12"/> instance. This is the top-most buffer, regardless of how many nested sub-buffers we allocated.
    /// </summary>
    internal BufferDX12 RootBuffer { get; private set; }

    /// <summary>
    /// Gets the internal resource barrier state of the current <see cref="BufferDX12"/>.
    /// </summary>
    internal ResourceStates BarrierState { get; set; }
}
