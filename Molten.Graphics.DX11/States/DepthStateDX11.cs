﻿using Silk.NET.Direct3D11;

namespace Molten.Graphics
{
    /// <summary>Stores a depth-stencil state for use with a <see cref="CommandQueueDX11"/>.</summary>
    internal unsafe class DepthStateDX11 : GraphicsDepthState
    {
        public class FaceDX11 : Face
        {
            internal DepthStencilopDesc _desc;
            DepthStateDX11 _parent;

            internal FaceDX11(DepthStateDX11 parent, ref DepthStencilopDesc defaultDesc)
            {
                _parent = parent;
                _desc = defaultDesc;
            }

            public override ComparisonFunction Comparison
            {
                get => (ComparisonFunction)_desc.StencilFunc;
                set
                {
                    if (_desc.StencilFunc != (ComparisonFunc)value)
                    {
                        _desc.StencilFunc = (ComparisonFunc)value;
                        _parent._dirty = true;
                    }
                }
            }

            public override DepthStencilOperation StencilPass
            {
                get => (DepthStencilOperation)_desc.StencilPassOp;
                set
                {
                    if (_desc.StencilPassOp != (StencilOp)value)
                    {
                        _desc.StencilPassOp = (StencilOp)value;
                        _parent._dirty = true;
                    }
                }
            }

            public override DepthStencilOperation StencilFail
            {
                get => (DepthStencilOperation)_desc.StencilFailOp;
                set
                {
                    if (_desc.StencilFailOp != (StencilOp)value)
                    {
                        _desc.StencilFailOp = (StencilOp)value;
                        _parent._dirty = true;
                    }
                }
            }

            public override DepthStencilOperation DepthFail
            {
                get => (DepthStencilOperation)_desc.StencilDepthFailOp;
                set
                {
                    if (_desc.StencilDepthFailOp != (StencilOp)value)
                    {
                        _desc.StencilDepthFailOp = (StencilOp)value;
                        _parent._dirty = true;
                    }
                }
            }
        }
        
        public unsafe ID3D11DepthStencilState* NativePtr => _native;
        ID3D11DepthStencilState* _native;
        DepthStencilDesc _desc;
        internal bool _dirty;

        internal DepthStateDX11(DeviceDX11 device, DepthStateDX11 source = null) :
            base(device, source)
        {
            _dirty = true;
        }

        protected override Face CreateFace(bool isFrontFace)
        {
            if(isFrontFace)
                return new FaceDX11(this, ref _desc.FrontFace);
            else
                return new FaceDX11(this, ref _desc.BackFace);
        }

        protected override void OnApply(GraphicsCommandQueue cmd)
        {
            if (_native == null || _dirty)
            {
                _dirty = false;
                SilkUtil.ReleasePtr(ref _native);

                // Copy the front and back-face settings into the main description
                _desc.FrontFace = (FrontFace as FaceDX11)._desc;
                _desc.BackFace = (BackFace as FaceDX11)._desc;

                // Create new state
                (cmd as CommandQueueDX11).DXDevice.Ptr->CreateDepthStencilState(ref _desc, ref _native);
                Version++;
            }
        }

        public override void GraphicsRelease()
        {
            SilkUtil.ReleasePtr(ref _native);
        }

        public override bool IsDepthEnabled
        {
            get => _desc.DepthEnable > 0;
            set
            {
                int val = value ? 1 : 0;
                if (_desc.DepthEnable != val)
                {
                    _desc.DepthEnable = val;
                    _dirty = true;
                }
            }
        }

        public override bool IsStencilEnabled
        {
            get => _desc.StencilEnable > 0;
            set
            {
                int val = value ? 1 : 0;
                if (_desc.StencilEnable != val)
                {
                    _desc.StencilEnable = val;
                    _dirty = true;
                }
            }
        }

        public override bool DepthWriteEnabled
        {
            get => _desc.DepthWriteMask == DepthWriteMask.All;
            set
            {
                DepthWriteMask mask = value ? DepthWriteMask.All : DepthWriteMask.None;
                if (_desc.DepthWriteMask != mask)
                {
                    _desc.DepthWriteMask = mask;
                    _dirty = true;
                }
            }
        }

        public override ComparisonFunction DepthComparison
        {
            get => (ComparisonFunction)_desc.DepthFunc;
            set
            {
                if (_desc.DepthFunc != (ComparisonFunc)value)
                {
                    _desc.DepthFunc = (ComparisonFunc)value;
                    _dirty = true;
                }
            }
        }

        public override byte StencilReadMask
        {
            get => _desc.StencilReadMask;
            set
            {
                if (_desc.StencilReadMask != value)
                {
                    _desc.StencilReadMask = value;
                    _dirty = true;
                }
            }
        }

        public override byte StencilWriteMask
        {
            get => _desc.StencilWriteMask;
            set
            {
                if (_desc.StencilWriteMask != value)
                {
                    _desc.StencilWriteMask = value;
                    _dirty = true;
                }
            }
        }

        public override bool DepthBoundsTestEnabled
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override float MaxDepthBounds { get; set; }

        public override float MinDepthBounds { get; set; }

        public static implicit operator ID3D11DepthStencilState*(DepthStateDX11 bindable)
        {
            return bindable.NativePtr;
        }
    }
}
