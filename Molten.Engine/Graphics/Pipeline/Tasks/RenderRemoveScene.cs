﻿namespace Molten.Graphics;

/// <summary>A <see cref="RenderSceneChange"/> for adding a <see cref="SceneObject"/> to the root of a scene.</summary>
internal class RenderRemoveScene : GraphicsTask
{
    public SceneRenderData Data;

    public override void ClearForPool()
    {
        Data = null;
    }

    protected override bool OnProcess(RenderService renderer, GraphicsQueue queue)
    {
        queue.Device.Renderer.Scenes.Remove(Data);
        return true;
    }
}
