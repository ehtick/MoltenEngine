﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Molten.Collections;

namespace Molten.Graphics
{
    /// <summary>
    /// The base class for an API-specific implementation of a graphics device, which provides command/resource access to a GPU.
    /// </summary>
    public abstract class GraphicsDevice : EngineObject
    {
        long _allocatedVRAM;
        ThreadedQueue<GraphicsObject> _objectsToDispose;

        /// <summary>
        /// Creates a new instance of <see cref="GraphicsDevice"/>.
        /// </summary>
        /// <param name="settings">The <see cref="GraphicsSettings"/> to bind to the device.</param>
        /// <param name="log">The <see cref="Logger"/> to use for outputting information.</param>
        protected GraphicsDevice(GraphicsSettings settings, Logger log)
        {
            Settings = settings;
            Log = log;
            _objectsToDispose = new ThreadedQueue<GraphicsObject>();
        }

        internal void Initialize()
        {
            OnInitialize();

            DepthBank = new DepthStateBank(this);
            BlendBank = new BlendStateBank(this);
        }

        protected abstract void OnInitialize();

        internal void DisposeMarkedObjects()
        {
            while (_objectsToDispose.TryDequeue(out GraphicsObject obj))
                obj.GraphicsRelease();
        }

        public void MarkForRelease(GraphicsObject pObject)
        {
            if (IsDisposed)
                pObject.GraphicsRelease();
            else
                _objectsToDispose.Enqueue(pObject);
        }

        protected override void OnDispose()
        {
            DisposeMarkedObjects();

            Cmd?.Dispose();
            DepthBank?.Dispose();
            BlendBank?.Dispose();
        }

        /// <summary>
        /// Requests a new <see cref="GraphicsDepthState"/> from the current <see cref="GraphicsDevice"/>, with the implementation's default depth-stencil settings.
        /// </summary>
        /// <param name="source">A source depth-stencil state to use as a template configuration.</param>
        /// <returns></returns>
        public abstract GraphicsDepthState CreateDepthState(GraphicsDepthState source = null);

        /// <summary>
        /// Requests a new <see cref="GraphicsBlendState"/> from the current <see cref="GraphicsDevice"/>, with the implementation's default blend settings.
        /// </summary>
        /// <param name="surfaceBlend">The default surface blend configuration.</param>
        /// <returns></returns>
        public abstract GraphicsBlendState CreateBlendState(GraphicsBlendState.RenderSurfaceBlend surfaceBlend);

        /// <summary>
        /// Requests a new <see cref="GraphicsBlendState"/> from the current <see cref="GraphicsDevice"/>, with the implementation's default blend settings.
        /// </summary>
        /// <param name="source">A source blend state to use as a template configuration.</param>
        /// <returns></returns>
        public abstract GraphicsBlendState CreateBlendState(GraphicsBlendState source = null);

        /// <summary>
        /// Requests the default <see cref="GraphicsBlendState.RenderSurfaceBlend"/> configuration from the current <see cref="GraphicsDevice"/>.
        /// </summary>
        /// <returns></returns>
        public abstract GraphicsBlendState.RenderSurfaceBlend GetDefaultSurfaceBlend();

        /// <summary>Track a VRAM allocation.</summary>
        /// <param name="bytes">The number of bytes that were allocated.</param>
        public void AllocateVRAM(long bytes)
        {
            Interlocked.Add(ref _allocatedVRAM, bytes);
        }

        /// <summary>Track a VRAM deallocation.</summary>
        /// <param name="bytes">The number of bytes that were deallocated.</param>
        public void DeallocateVRAM(long bytes)
        {
            Interlocked.Add(ref _allocatedVRAM, -bytes);
        }

        /// <summary>
        /// Gets the amount of VRAM that has been allocated on the current <see cref="GraphicsDevice"/>. 
        /// <para>For a software or integration device, this may be system memory (RAM).</para>
        /// </summary>
        internal long AllocatedVRAM => _allocatedVRAM;

        /// <summary>
        /// Gets the <see cref="Logger"/> that is bound to the current <see cref="GraphicsDevice"/> for outputting information.
        /// </summary>
        public Logger Log { get; }

        /// <summary>
        /// Gets the <see cref="GraphicsSettings"/> bound to the current <see cref="GraphicsDevice"/>.
        /// </summary>
        public GraphicsSettings Settings { get; }

        /// <summary>
        /// Gets the <see cref="IDisplayAdapter"/> that the current <see cref="GraphicsDevice"/> is bound to.
        /// </summary>
        public abstract IDisplayAdapter Adapter { get; }

        /// <summary>
        /// The main <see cref="GraphicsCommandQueue"/> of the current <see cref="GraphicsDevice"/>. This is used for issuing immediate commands to the GPU.
        /// </summary>
        public abstract GraphicsCommandQueue Cmd { get; }

        /// <summary>
        /// Gets the device's depth-stencil state bank.
        /// </summary>
        public DepthStateBank DepthBank { get; private set; }

        /// <summary>
        /// Gets the device's blend state bank.
        /// </summary>
        public BlendStateBank BlendBank { get; private set; }
    }

    /// <summary>
    /// A more advanced version of <see cref="GraphicsDevice"/> which manages the allocation and releasing of an unsafe object pointer, exposed via <see cref="Ptr"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe abstract class GraphicsDevice<T> : GraphicsDevice
        where T : unmanaged
    {
        T* _ptr;

        protected GraphicsDevice(GraphicsSettings settings, Logger log, bool allocate) :
            base(settings, log)
        {
            if (allocate)
                _ptr = EngineUtil.Alloc<T>();
        }

        protected override void OnDispose()
        {
            EngineUtil.Free(ref _ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(GraphicsDevice<T> device)
        {
            return *device.Ptr;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T*(GraphicsDevice<T> device)
        {
            return device._ptr;
        }

        /// <summary>
        /// The underlying, native device pointer.
        /// </summary>
        public T* Ptr => _ptr;

        /// <summary>
        /// Gets a protected reference to the underlying device pointer.
        /// </summary>
        protected ref T* PtrRef => ref _ptr;
    }
}
