namespace Molten.Graphics;

/// <summary>
/// Represents a custom implementation of a native GUI control-based render surface.
/// </summary>
public interface INativeSurface : ISwapChainSurface, IWindow
{
    /// <summary>
    /// Occurs when the underlying native <see cref="WindowHandle"/> has changed. Invoked by the renderer it is bound to.
    /// </summary>
    event TextureHandler<INativeSurface> OnHandleChanged;

    /// <summary>
    /// Occurs when the underlying, native control parent has changed. This affects the <see cref="WindowHandle"/> in some situations.
    /// </summary>
    event TextureHandler<INativeSurface> OnParentChanged;

    /// <summary>Invoked when the current <see cref="INativeSurface"/> has began it's closing process. Invoked by the renderer it is bound to.</summary>
    event TextureHandler<INativeSurface> OnClose;

    /// <summary>Invoked when the current <see cref="INativeSurface"/> is minimized. Invoked by the renderer it is bound to.</summary>
    event TextureHandler<INativeSurface> OnMinimize;

    /// <summary>Invoked when the current <see cref="INativeSurface"/> is maximized. Invoked by the renderer it is bound to.</summary>
    event TextureHandler<INativeSurface> OnMaximize;

    /// <summary>Invoked when the current <see cref="INativeSurface"/> is restored. Invoked by the renderer it is bound to.</summary>
    event TextureHandler<INativeSurface> OnRestore;

    /// <summary>Invoked when the current <see cref="INativeSurface"/> gains focus.</summary>
    event TextureHandler<INativeSurface> OnFocusGained;

    /// <summary>Invoked when the current <see cref="INativeSurface"/> loses focus.</summary>
    event TextureHandler<INativeSurface> OnFocusLost;

    /// <summary>
    /// Gets whether or not the current <see cref="INativeSurface"/> is focused.
    /// </summary>
    bool IsFocused { get; }

    /// <summary>Gets or sets the window mode of the underlying control.</summary>
    WindowMode Mode { get; set; }

    /// <summary>
    /// Gets the current <see cref="INativeSurface"/> parent's control handle. Null if no parent is assigned.
    /// </summary>
    nint? ParentHandle { get; set; }

    /// <summary>
    /// Gets the handle of the window or form containing the current <see cref="INativeSurface"/>. This is not neccessarily it's direct parent of ancestor.
    /// </summary>
    nint? WindowHandle { get; }
}
