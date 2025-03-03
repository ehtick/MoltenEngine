﻿// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Molten;

/// <summary>
/// Defines viewport dimensions using insigned integer values.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
[Serializable]
public struct ViewportUI : IEquatable<ViewportUI>
{
    /// <summary>
    /// Position of the pixel coordinate of the upper-left corner of the viewport.
    /// </summary>
    [DataMember]
    [FieldOffset(0)]
    public uint X;

    /// <summary>
    /// Position of the pixel coordinate of the upper-left corner of the viewport.
    /// </summary>
    [DataMember]
    [FieldOffset(4)]
    public uint Y;

    /// <summary>
    /// The size/dimensions of the viewport. Maps directly to <see cref="Width"/> and <see cref="Height"/> in memory.
    /// </summary>
    [FieldOffset(8)]
    public Vector2UI Size;

    /// <summary>
    /// Width dimension of the viewport. Maps directly to <see cref="Size"/>.X in memory.
    /// </summary>
    [DataMember]
    [FieldOffset(8)]
    public uint Width;

    /// <summary>
    /// Height dimension of the viewport. Maps directly to <see cref="Size"/>.Y in memory.
    /// </summary>
    [DataMember]
    [FieldOffset(12)]
    public uint Height;

    /// <summary>
    /// Gets or sets the minimum depth of the clip volume.
    /// </summary>
    [DataMember]
    [FieldOffset(16)]
    public float MinDepth;

    /// <summary>
    /// Gets or sets the maximum depth of the clip volume.
    /// </summary>
    [DataMember]
    [FieldOffset(20)]
    public float MaxDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="Viewport"/> struct.
    /// </summary>
    /// <param name="x">The x coordinate of the upper-left corner of the viewport in pixels.</param>
    /// <param name="y">The y coordinate of the upper-left corner of the viewport in pixels.</param>
    /// <param name="width">The width of the viewport in pixels.</param>
    /// <param name="height">The height of the viewport in pixels.</param>
    public ViewportUI(uint x, uint y, uint width, uint height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        MinDepth = 0f;
        MaxDepth = 1f;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Viewport"/> struct.
    /// </summary>
    /// <param name="x">The x coordinate of the upper-left corner of the viewport in pixels.</param>
    /// <param name="y">The y coordinate of the upper-left corner of the viewport in pixels.</param>
    /// <param name="width">The width of the viewport in pixels.</param>
    /// <param name="height">The height of the viewport in pixels.</param>
    /// <param name="minDepth">The minimum depth of the clip volume.</param>
    /// <param name="maxDepth">The maximum depth of the clip volume.</param>
    public ViewportUI(uint x, uint y, uint width, uint height, float minDepth, float maxDepth)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        MinDepth = minDepth;
        MaxDepth = maxDepth;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Viewport"/> struct.
    /// </summary>
    /// <param name="bounds">A bounding box that defines the location and size of the viewport in a render target.</param>
    public ViewportUI(RectangleUI bounds)
    {
        X = bounds.X;
        Y = bounds.Y;
        Width = bounds.Width;
        Height = bounds.Height;
        MinDepth = 0f;
        MaxDepth = 1f;
    }

    /// <summary>
    /// Gets the size of this resource.
    /// </summary>
    /// <value>The bounds.</value>
    public RectangleUI Bounds
    {
        get => new RectangleUI(X, Y, Width, Height);

        set
        {
            X = value.X;
            Y = value.Y;
            Width = value.Width;
            Height = value.Height;
        }
    }

    /// <summary>
    /// Determines whether the specified <see cref="Viewport"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="Viewport"/> to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="Viewport"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ref ViewportUI other)
    {
        return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height && MathHelper.NearEqual(MinDepth, other.MinDepth) && MathHelper.NearEqual(MaxDepth, other.MaxDepth);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Viewport"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="Viewport"/> to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="Viewport"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ViewportUI other)
    {
        return Equals(ref other);
    }

    /// <summary>
    /// Determines whether the specified object is equal to this instance.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object obj)
    {
        if(!(obj is Viewport))
            return false;

        var strongValue = (ViewportUI)obj;
        return Equals(ref strongValue);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            uint hashCode = X;
            hashCode = (hashCode * 397) ^ Y;
            hashCode = (hashCode * 397) ^ Width;
            hashCode = (hashCode * 397) ^ Height;
            hashCode = (hashCode * 397) ^ (uint)MinDepth.GetHashCode();
            hashCode = (hashCode * 397) ^ (uint)MaxDepth.GetHashCode();
            return (int)hashCode;
        }
    }
    
    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ViewportUI left, ViewportUI right)
    {
        return left.Equals(ref right);
    }

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ViewportUI left, ViewportUI right)
    {
        return !left.Equals(ref right);
    }

    /// <summary>
    /// Retrieves a string representation of this object.
    /// </summary>
    /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.CurrentCulture, "{{X:{0} Y:{1} Width:{2} Height:{3} MinDepth:{4} MaxDepth:{5}}}", X, Y, Width, Height, MinDepth, MaxDepth);
    }

    /// <summary>
    /// Projects a 3D vector from object space into screen space.
    /// </summary>
    /// <param name="source">The vector to project.</param>
    /// <param name="projection">The projection matrix.</param>
    /// <param name="view">The view matrix.</param>
    /// <param name="world">The world matrix.</param>
    /// <returns>The projected vector.</returns>
    public Vector3F Project(Vector3F source, Matrix4F projection, Matrix4F view, Matrix4F world)
    {
        Matrix4F matrix;
        Matrix4F.Multiply(ref world, ref view, out matrix);
        Matrix4F.Multiply(ref matrix, ref projection, out matrix);

        Vector3F vector;
        Project(ref source, ref matrix, out vector);
        return vector;
    }

    /// <summary>
    /// Projects a 3D vector from object space into screen space.
    /// </summary>
    /// <param name="source">The vector to project.</param>
    /// <param name="matrix">A combined WorldViewProjection matrix.</param>
    /// <param name="vector">The projected vector.</param>
    public void Project(ref Vector3F source, ref Matrix4F matrix, out Vector3F vector)
    {
        Vector3F.Transform(ref source, ref matrix, out vector);
        float a = (((source.X * matrix.M14) + (source.Y * matrix.M24)) + (source.Z * matrix.M34)) + matrix.M44;

        if (!MathHelper.IsOne(a))
        {
            vector = (vector / a);
        }

        vector.X = (((vector.X + 1f) * 0.5f) * Width) + X;
        vector.Y = (((-vector.Y + 1f) * 0.5f) * Height) + Y;
        vector.Z = (vector.Z * (MaxDepth - MinDepth)) + MinDepth;
    }

    /// <summary>
    /// Converts a screen space pouint into a corresponding pouint in world space.
    /// </summary>
    /// <param name="source">The vector to project.</param>
    /// <param name="projection">The projection matrix.</param>
    /// <param name="view">The view matrix.</param>
    /// <param name="world">The world matrix.</param>
    /// <returns>The unprojected Vector.</returns>
    public Vector3F Unproject(Vector3F source, Matrix4F projection, Matrix4F view, Matrix4F world)
    {
        Matrix4F matrix;
        Matrix4F.Multiply(ref world, ref view, out matrix);
        Matrix4F.Multiply(ref matrix, ref projection, out matrix);
        Matrix4F.Invert(ref matrix, out matrix);

        Vector3F vector;
        Unproject(ref source, ref matrix, out vector);
        return vector;
    }

    /// <summary>
    /// Converts a screen space pouint into a corresponding pouint in world space.
    /// </summary>
    /// <param name="source">The vector to project.</param>
    /// <param name="matrix">An inverted combined WorldViewProjection matrix.</param>
    /// <param name="vector">The unprojected vector.</param>
    public void Unproject(ref Vector3F source, ref Matrix4F matrix, out Vector3F vector)
    {
        vector.X = (((source.X - X) / (Width)) * 2f) - 1f;
        vector.Y = -((((source.Y - Y) / (Height)) * 2f) - 1f);
        vector.Z = (source.Z - MinDepth) / (MaxDepth - MinDepth);

        float a = (((vector.X * matrix.M14) + (vector.Y * matrix.M24)) + (vector.Z * matrix.M34)) + matrix.M44;
        Vector3F.Transform(ref vector, ref matrix, out vector);

        if (!MathHelper.IsOne(a))
        {
            vector = (vector / a);
        }
    }

    /// <summary>
    /// Gets the aspect ratio used by the viewport.
    /// </summary>
    /// <value>The aspect ratio.</value>
    public float AspectRatio
    {
        get
        {
            if (Height != 0)
                return Width / (float)Height;
            else
                return 0f;
        }
    }
}
