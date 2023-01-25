﻿namespace Molten.Graphics
{
    /// <summary>Stores a blend state for use with a <see cref="GraphicsCommandQueue"/>.</summary>
    public abstract class GraphicsBlendState : GraphicsObject, IEquatable<GraphicsBlendState>
    {
        public abstract class RenderSurfaceBlend
        {
            public abstract int BlendEnable { get; set; }

            public abstract bool LogicOpEnable { get; set; }

            public abstract BlendType SrcBlend { get; set; }

            public abstract BlendType DestBlend { get; set; }

            public abstract BlendOperation BlendOp { get; set; }

            public abstract BlendType SrcBlendAlpha { get; set; }

            public abstract BlendType DestBlendAlpha { get; set; }

            public abstract BlendOperation BlendOpAlpha { get; set; }

            public abstract LogicOperation LogicOp { get; set; }

            public abstract ColorWriteFlags RenderTargetWriteMask { get; set; }

            public void Set(RenderSurfaceBlend other)
            {
                BlendEnable = other.BlendEnable;
                LogicOpEnable = other.LogicOpEnable;
                SrcBlend = other.SrcBlend;
                DestBlend = other.DestBlend;
                BlendOp = other.BlendOp;
                SrcBlendAlpha = other.SrcBlendAlpha;
                DestBlendAlpha = other.DestBlendAlpha;
                BlendOpAlpha = other.BlendOpAlpha;
                LogicOp = other.LogicOp;
                RenderTargetWriteMask = other.RenderTargetWriteMask;
            }
        }

        RenderSurfaceBlend[] _surfaceBlends;

        protected GraphicsBlendState(GraphicsDevice device, GraphicsBlendState source) : 
            base(device, GraphicsBindTypeFlags.Input)
        {
            if (source != null)
            {
                BlendFactor = source.BlendFactor;
                BlendSampleMask = source.BlendSampleMask;
                _surfaceBlends = new RenderSurfaceBlend[source._surfaceBlends.Length];
                for (int i = 0; i < _surfaceBlends.Length; i++)
                    _surfaceBlends[i] = InitializeSurfaceBlend(i, source._surfaceBlends[i]);
            }
            else
            {
                BlendFactor = new Color4(1, 1, 1, 1);
                BlendSampleMask = 0xffffffff;
                _surfaceBlends = new RenderSurfaceBlend[device.Adapter.Capabilities.PixelShader.MaxOutResources];
                for (int i = 0; i < _surfaceBlends.Length; i++)
                    _surfaceBlends[i] = InitializeSurfaceBlend(i, null);
            }
        }

        protected abstract RenderSurfaceBlend InitializeSurfaceBlend(int index, RenderSurfaceBlend source);

        internal RenderSurfaceBlend GetSurfaceBlendState(int index)
        {
            return _surfaceBlends[index];
        }

        public override bool Equals(object obj)
        {
            if (obj is GraphicsBlendState other)
                return Equals(other);
            else
                return false;
        }

        public bool Equals(GraphicsBlendState other)
        {
            if (IndependentBlendEnable != other.IndependentBlendEnable)
                return false;

            if (AlphaToCoverageEnable != other.AlphaToCoverageEnable)
                return false;

            // Equality check against all RT blend states
            for(int i = 0; i < Device.Adapter.Capabilities.PixelShader.MaxOutResources; i++)
            {
                RenderSurfaceBlend rt = _surfaceBlends[i];
                RenderSurfaceBlend otherRt = other[i];

                if (rt.BlendOpAlpha != otherRt.BlendOpAlpha ||
                    rt.BlendOp != otherRt.BlendOp ||
                    rt.DestBlendAlpha != otherRt.DestBlendAlpha ||
                    rt.DestBlend != otherRt.DestBlend ||
                    rt.BlendEnable != otherRt.BlendEnable ||
                    rt.RenderTargetWriteMask != otherRt.RenderTargetWriteMask ||
                    rt.SrcBlendAlpha != otherRt.SrcBlendAlpha ||
                    rt.SrcBlend != otherRt.SrcBlend)
                {
                    return false;
                }
            }
            return true;
        }

        public abstract bool AlphaToCoverageEnable { get; set; }

        public abstract bool IndependentBlendEnable { get; set; }

        /// <summary>
        /// Gets or sets a render target blend description at the specified index.
        /// </summary>
        /// <param name="rtIndex">The render target/surface blend index.</param>
        /// <returns></returns>
        public RenderSurfaceBlend this[int rtIndex] => _surfaceBlends[rtIndex];

        /// <summary>
        /// Gets or sets the blend sample mask.
        /// </summary>
        public uint BlendSampleMask { get; set; }

        /// <summary>
        /// Gets or sets the blend factor.
        /// </summary>
        public Color4 BlendFactor { get; set; }
    }
}
