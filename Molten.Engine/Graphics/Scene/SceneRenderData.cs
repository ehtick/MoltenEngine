namespace Molten.Graphics;

public delegate void SceneRenderDataHandler(RenderService renderer, SceneRenderData data);

/// <summary>
/// A class for storing renderer-specific information about a scene.
/// </summary>
public class SceneRenderData
{
    /// <summary>
    /// Occurs just before the scene is about to be rendered.
    /// </summary>
    public event SceneRenderDataHandler OnPreRender;

    /// <summary>
    /// Occurs just after the scene has been rendered.
    /// </summary>
    public event SceneRenderDataHandler OnPostRender;

    /// <summary>
    /// If true, the scene will be rendered.
    /// </summary>
    public bool IsVisible = true;

    /// <summary>
    /// The ambient light color.
    /// </summary>
    public Color AmbientLightColor = Color.Black;

    public List<LayerRenderData> Layers = new List<LayerRenderData>();

    GpuDevice _device;

    internal SceneRenderData(GpuDevice device)
    {
        _device = device;
    }

    public void AddLayer(LayerRenderData data)
    {
        RenderLayerAdd task = new();
        task.LayerData = data;
        task.SceneData = this;
        _device.PushTask(GpuPriority.StartOfFrame, ref task, null);
    }

    public void RemoveLayer(LayerRenderData data)
    {
        RenderLayerRemove task = new();
        task.LayerData = data;
        task.SceneData = this;
        _device.PushTask(GpuPriority.StartOfFrame, ref task, null);
    }

    public void ReorderLayer(LayerRenderData data, ReorderMode mode)
    {
        RenderLayerReorder task = new();
        task.LayerData = data;
        task.SceneData = this;
        task.Mode = mode;
        _device.PushTask(GpuPriority.StartOfFrame, ref task, null);
    }

    public void AddObject(RenderCamera obj)
    {
        AddCamera task = new();
        task.Camera = obj;
        task.Data = this;
        _device.PushTask(GpuPriority.StartOfFrame, ref task, null);
    }

    public void RemoveObject(RenderCamera obj)
    {
        RemoveCamera task = new();
        task.Camera = obj;
        task.Data = this;
        _device.PushTask(GpuPriority.StartOfFrame, ref task, null);
    }

    public void AddObject(Renderable obj, ObjectRenderData renderData, LayerRenderData layer)
    {
        RenderableAdd task = new();
        task.Renderable = obj;
        task.Data = renderData;
        task.LayerData = layer;
        _device.PushTask(GpuPriority.StartOfFrame, ref task, null);
    }

    public void RemoveObject(Renderable obj, ObjectRenderData renderData, LayerRenderData layer)
    {
        RenderableRemove task = new();
        task.Renderable = obj;
        task.Data = renderData;
        task.LayerData = layer;
        _device.PushTask(GpuPriority.StartOfFrame, ref task, null);
    }

    /// <summary>
    /// Invokes <see cref="OnPreRender"/> event.
    /// </summary>
    public void PreRenderInvoke(RenderService renderer) => OnPreRender?.Invoke(renderer, this);

    /// <summary>
    /// Invokes <see cref="OnPostRender"/> event.
    /// </summary>
    public void PostRenderInvoke(RenderService renderer) => OnPostRender?.Invoke(renderer, this);

    /* TODO:
    *  - Edit PointLights and CapsuleLights.Data directly in light scene components (e.g. PointLightComponent).
    *  - Renderer will upload the latest data to the GPU 
    */

    /// <summary>
    /// Gets the management list for point lights
    /// </summary>
    public LightList PointLights { get; } = new(100);

    /// <summary>
    /// Gets the management list for capsule lights.
    /// </summary>
    public LightList CapsuleLights { get; } = new(50);

    /// <summary>
    /// Gets a list of cameras that are part of the scene.
    /// </summary>
    public List<RenderCamera> Cameras { get; } = new();

    /// <summary>
    /// Gets or sets the skybox cube-map texture.
    /// </summary>
    public ITextureCube SkyboxTexture { get; set; }
}
