﻿namespace Molten.Graphics;

public abstract class ShaderResourceVariable : ShaderVariable
{
    GpuResource _resource;
    GpuResource _default;

    protected abstract bool ValidateResource(GpuResource value);

    private void SetResource(ref GpuResource dest, object value, string logKey)
    {
        if (dest != value)
        {
            if (value != null)
            {
                if (value is GpuResource res && ValidateResource(res))
                {
                    if (ExpectedFormat != GpuResourceFormat.Unknown && res.ResourceFormat != ExpectedFormat)
                    {
                        Parent.Device.Log.Error($"Resource ({logKey}) format mismatch on '{Name}' of '{Parent.Name}':");
                        Parent.Device.Log.Error($"\tResource: {res.Name}");
                        Parent.Device.Log.Error($"\tExpected: {ExpectedFormat}");
                        Parent.Device.Log.Error($"\tReceived: {res.ResourceFormat}");
                    }
                    else
                    {
                        dest = res;
                    }
                }
                else
                {
                    Parent.Device.Log.Error($"Cannot set non-resource '{value.GetType().Name}' object on resource ({logKey}) variable '{Name}' of '{Parent.Name}'");
                }
            }
            else
            {
                dest = _default;
            }
        }
    }

    /// <summary>Gets the resource bound to the variable.</summary>
    public GpuResource Resource => _resource ?? _default;

    /// <summary>
    /// Gets or sets the value of the resource variable.
    /// </summary>
    public override object Value
    {
        get => _resource ?? _default;
        set => SetResource(ref _resource, value, "value");
    }

    /// <summary>
    /// Gets or sets the internal default value of the resource variable.
    /// </summary>
    internal object DefaultValue
    {
        get => _default;
        set => SetResource(ref _default, value, "default");
    }

    /// <summary>
    /// Gets the expected format of the <see cref="Value"/> resource.
    /// </summary>
    public GpuResourceFormat ExpectedFormat { get; internal set; }
}

/// <summary>
/// A <see cref="ShaderResourceVariable"/> that expects a specific type of resource.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ShaderResourceVariable<T> : ShaderResourceVariable
{
    protected override sealed bool ValidateResource(GpuResource value)
    {
        return value is T;
    }
}
