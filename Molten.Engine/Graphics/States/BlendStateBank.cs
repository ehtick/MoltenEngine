﻿namespace Molten.Graphics
{
    public class BlendStateBank : GraphicsStateBank<GraphicsBlendState, BlendPreset>
    {
        GraphicsDevice _device;

        internal BlendStateBank(GraphicsDevice device)
        {
            _device = device;

            GraphicsBlendState state = device.CreateBlendState();
            AddPreset(BlendPreset.Default, state);

            // Additive blending preset.
            state = device.CreateBlendState();
            GraphicsBlendState.RenderSurfaceBlend sBlend = state[0];
            sBlend.SrcBlend = BlendType.One;
            sBlend.DestBlend = BlendType.One;
            sBlend.BlendOp = BlendOperation.Add;
            sBlend.SrcBlendAlpha = BlendType.One;
            sBlend.DestBlendAlpha = BlendType.One;
            sBlend.BlendOpAlpha = BlendOperation.Add;
            sBlend.RenderTargetWriteMask = ColorWriteFlags.All;
            sBlend.BlendEnable = true;
            sBlend.LogicOp = LogicOperation.Noop;
            sBlend.LogicOpEnable = false;
            state.AlphaToCoverageEnable = false;
            state.IndependentBlendEnable = false;
            AddPreset(BlendPreset.Additive, state);

            // Pre-multiplied alpha
            state = device.CreateBlendState();
            sBlend = state[0];
            sBlend.SrcBlend = BlendType.SrcAlpha;
            sBlend.DestBlend = BlendType.InvSrcAlpha;
            sBlend.BlendOp = BlendOperation.Add;
            sBlend.SrcBlendAlpha = BlendType.InvDestAlpha;
            sBlend.DestBlendAlpha = BlendType.One;
            sBlend.BlendOpAlpha = BlendOperation.Add;
            sBlend.RenderTargetWriteMask = ColorWriteFlags.All;
            sBlend.BlendEnable = true;
            sBlend.LogicOp = LogicOperation.Noop;
            sBlend.LogicOpEnable = false;
            state.AlphaToCoverageEnable = false;
            state.IndependentBlendEnable = false;
            AddPreset(BlendPreset.PreMultipliedAlpha, state);
        }

        public override GraphicsBlendState GetPreset(BlendPreset value)
        {
            return _presets[(int)value];
        }

        public GraphicsBlendState NewFromPreset(BlendPreset value)
        {
            return _device.CreateBlendState(_presets[(int)value]);
        }
    }

    public enum BlendPreset
    {
        /// <summary>The default blend mode.</summary>
        Default = 0,

        /// <summary>Additive blending mode.</summary>
        Additive = 1,

        /// <summary>Pre-multiplied alpha blending mode.</summary>
        PreMultipliedAlpha = 2,
    }
}
