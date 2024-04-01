﻿namespace Molten.Graphics;

public interface ISwapChainSurface : IRenderSurface2D
{
    /// <summary>
    /// Occurs after the <see cref="ISwapChainSurface"/> is done resizing. Executed by the renderer thread it is bound to.
    /// </summary>
    event TextureHandler<ISwapChainSurface> OnResize;

    /// <summary>
    /// Dispatches a callback to be invoked next time the <see cref="ISwapChainSurface"/> is presented on its parent render thread.
    /// </summary>
    /// <param name="callback"></param>
    void Dispatch(Action callback);

    /// <summary>
    /// Gets or sets whether or not the current <see cref="ISwapChainSurface"/> is enabled. 
    /// If false, the current <see cref="ISwapChainSurface"/> will not be presented to its output.
    /// </summary>
    bool IsEnabled { get; set; }
}
