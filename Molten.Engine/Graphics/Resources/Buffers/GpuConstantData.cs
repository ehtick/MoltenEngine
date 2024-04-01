namespace Molten.Graphics;

public unsafe class GpuConstantData : IDisposable
{
    internal Dictionary<string, GraphicsConstantVariable> _varLookup;
    byte* _constData;

    public GpuConstantData(ConstantBufferInfo info)
    {
        Type = info.Type;
        BufferName = info.Name;
        SizeInBytes = info.Size;
        _varLookup = new Dictionary<string, GraphicsConstantVariable>();
        _constData = (byte*)EngineUtil.Alloc(SizeInBytes);

        Variables = ConstantBufferInfo.BuildBufferVariables(this, info);
        foreach (GraphicsConstantVariable v in Variables)
            _varLookup.Add(v.Name, v);
    }

    internal void Apply(GpuCommandList cmd, GpuBuffer cBuffer)
    {
        // Setting data via shader variabls takes precedent. All standard buffer changes (set/append) will be ignored and wiped.
        if (IsDirty)
        {
            IsDirty = false;

            // Re-write all data to the variable buffer to maintain byte-ordering.
            foreach (GraphicsConstantVariable v in Variables)
                v.Write(_constData + v.ByteOffset);

            using (GpuStream stream = cmd.MapResource(cBuffer, 0, 0, GpuMapType.Write))
                stream.WriteRange(_constData, SizeInBytes);
        }
    }

    public bool Equals(GpuConstantData other)
    {
        return GraphicsConstantVariable.AreEqual(Variables, other.Variables);
    }

    public void Dispose()
    {
        EngineUtil.Free(ref _constData);
    }

    /// <summary>
    /// Gets the name of the constant buffer.
    /// </summary>
    internal string BufferName { get; }

    /// <summary>
    /// Gets or sets whether or not the constant buffer is dirty.
    /// </summary>
    internal bool IsDirty { get; set; }

    internal ConstantBufferType Type { get; set; }

    /// <summary>
    /// Gets a list of variables that represent elements within the constant buffer.
    /// </summary>
    public GraphicsConstantVariable[] Variables { get; }

    /// <summary>
    /// Gets the expected size of the constant data, in bytes.
    /// </summary>
    internal uint SizeInBytes { get; }

    internal byte* DataPtr => _constData;
}
