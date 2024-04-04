namespace Molten.Graphics;

public unsafe abstract class GpuResourceHandle : GpuObject
{
    protected GpuResourceHandle(GpuResource resource) : 
        base(resource.Device)
    {
        Resource = resource;
    }

    /// <summary>
    /// Gets the <see cref="GpuResource"/> that this handle is associated with.
    /// </summary>
    public GpuResource Resource { get; }
}
