using Silk.NET.Direct3D12;
using System.Runtime.InteropServices;

namespace Molten.Graphics.DX12;
internal unsafe class RootSigMetaDX12 : IDisposable
{
    internal struct BindDescriptorTable
    {
        public uint NumRanges;

        public BindDescriptorRange* Ranges;
    }

    internal struct BindDescriptorRange
    {
        public uint NumDescriptors;

        public DescriptorRangeType Type;

        public uint BaseShaderRegister;

        public uint RegisterSpace;

        /// <summary>
        /// Offset by number of descriptors from start of the descriptor table.
        /// </summary>
        public uint OffsetInDescriptorsFromTableStart;

        public DescriptorRangeFlags Flags;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct BindParameter
    {
        [FieldOffset(0)]
        public RootParameterType Type;

        [FieldOffset(8)]
        public BindDescriptorTable DescriptorTable;
    }

    internal BindParameter[] Parameters;

    internal BindDescriptorRange* Ranges;

    internal RootSigMetaDX12(uint numParameters, uint numRanges, D3DRootSignatureVersion version)
    {
        Parameters = new BindParameter[numParameters];
        NumRanges = numRanges;
        Ranges = EngineUtil.AllocArray<BindDescriptorRange>(numRanges);
        Version = version;
    }

    ~RootSigMetaDX12()
    {
        Dispose();
    }

    public void Dispose()
    {
        EngineUtil.Free(ref Ranges);
    }

    /// <summary>
    /// Outputs a root signature layout to the log, based on data held in the current <see cref="RootSigMetaDX12"/>.
    /// </summary>
    /// <param name="log"></param>
    internal void ToLog(Logger log)
    {
        log.Debug($"Bound Root-Signature ({Version}) with {Parameters.Length} parameter(s):");

        for (uint i = 0; i < Parameters.Length; i++)
        {
            ref BindParameter rootParam = ref Parameters[i];
            log.Debug($"\tRoot parameter {i} - Type: {rootParam.Type}");

            if (rootParam.Type == RootParameterType.TypeDescriptorTable)
            {
                ref BindDescriptorTable table = ref rootParam.DescriptorTable;
                for (int j = 0; j < table.NumRanges; j++)
                {
                    ref BindDescriptorRange range = ref table.Ranges[j];
                    log.Debug($"\t\tRange {j} - Type: {range.Type}, Descriptor(s): {range.NumDescriptors}, Flags: {range.Flags}");
                    for (uint k = 0; k < range.NumDescriptors; k++)
                    {
                        uint offsetFromStart = range.OffsetInDescriptorsFromTableStart + k;
                        uint register = range.BaseShaderRegister + k;
                        log.Debug($"\t\t\tDescriptor {k} - Offset: {offsetFromStart} - Register: {register}");
                    }
                }
            }
        }
    }

    internal D3DRootSignatureVersion Version { get; }

    internal uint NumRanges { get; }
}
