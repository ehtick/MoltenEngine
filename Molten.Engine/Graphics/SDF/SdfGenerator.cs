﻿using Molten.DoublePrecision;
using Molten.Shapes;

namespace Molten.Graphics.SDF;

/// <summary>
/// A utility class for generating signed-distance-field (SDF) textures. Also capable of rasterizing them to an output texture.
/// <para>See readme.md here for shader details: https://github.com/Chlumsky/msdfgen</para>
/// </summary>
public class SdfGenerator
{
    public const double DISTANCE_DELTA_FACTOR = 1.001;
    internal const double DEFAULT_ANGLE_THRESHOLD = 3;
    internal const double MSDFGEN_CORNER_DOT_EPSILON = 0.000001;
    internal const double MSDFGEN_DECONVERGENCE_FACTOR = 0.000001;

    public unsafe TextureSliceRef<Color3> Generate(uint pWidth, uint pHeight, Shape shape, SdfProjection projection, double pxRange, FillRule fl)
    {
        if (pWidth == 0 || pHeight == 0)
            throw new Exception("Texture slice width and height must be at least 1 pixel");

        SdfConfig config = new SdfConfig()
        {
            DistanceCheckMode = SdfConfig.DistanceErrorCheckMode.CHECK_DISTANCE_AT_EDGE,
            Mode = SdfConfig.ErrorCorrectMode.EDGE_PRIORITY
        };

        double range = pxRange / Math.Min(projection.Scale.X, projection.Scale.Y);
        const string edgeAssignment = null; // "cmywCMYW";
        SdfConfig postGenConfig = new SdfConfig(config);

        config.Mode = SdfConfig.ErrorCorrectMode.DISABLED;
        postGenConfig.DistanceCheckMode = SdfConfig.DistanceErrorCheckMode.DO_NOT_CHECK_DISTANCE;

        uint numBytes = pWidth * pHeight * (uint)sizeof(Color3);
        TextureSlice sdf = new TextureSlice(pWidth, pHeight, 1, numBytes);

        TextureSliceRef<Color3> sdfRef = sdf.GetReference<Color3>();

        EdgeColouring.EdgeColoringSimple(shape, DEFAULT_ANGLE_THRESHOLD, 0);
        EdgeColouring.ParseColoring(shape, edgeAssignment);
        GenerateMSDF(sdfRef, shape, projection, range, config);

        ErrorCorrection.MultiDistanceSignCorrection(sdfRef, shape, projection, fl);
        ErrorCorrection.MsdfErrorCorrection(sdfRef, shape, projection, range, postGenConfig);

        return sdfRef;
    }

    public unsafe ITexture2D ConvertToTexture(RenderService renderer, TextureSliceRef<Color3> src)
    {
        const int NUM_SRC_CHANNELS = 3;
        const int NUM_DEST_CHANNELS = 4;

        uint rowPitch = (src.Width * (uint)sizeof(Color));
        Color[] finalData = new Color[src.Width * src.Height];
        ITexture2D tex = renderer.Device.Resources.CreateTexture2D(src.Width, src.Height, 1, 1, 
            GpuResourceFormat.R8G8B8A8_UNorm, 
            GpuResourceFlags.DefaultMemory, name: $"SDF_{src.Width}x{src.Height}");

        fixed (Color* ptr = finalData)
        {
            byte* pData = (byte*)ptr;
            float* srcData = (float*)src.Data;

            for (uint i = 0; i < finalData.Length; i++)
            {
                pData[0] = (byte)srcData[0];
                pData[1] = (byte)srcData[1];
                pData[2] = (byte)srcData[2];
                pData[3] = 255;

                pData += NUM_DEST_CHANNELS;
                srcData += NUM_SRC_CHANNELS;
            }
        }

        tex.SetSubResourceData(GpuPriority.StartOfFrame, null, 0, finalData, 0, (uint)finalData.Length, rowPitch);
        return tex;
    }

    private unsafe void GenerateMSDF(TextureSliceRef<Color3> output, Shape shape, SdfProjection projection, double range, SdfConfig config)
    {
        var dpc = new MultiDistancePixelConversion(range);
        ShapeDistanceFinder distanceFinder = new ShapeDistanceFinder(shape);

        bool rightToLeft = false;
        for (int y = 0; y < output.Height; ++y)
        {
            for (int col = 0; col < output.Width; ++col)
            {
                int x = (int)(rightToLeft ? output.Width - col - 1 : col);
                Vector2D p = projection.Unproject(new Vector2D(x + .5, y + .5));
                Color3D distance = distanceFinder.distance(ref p);
                dpc.Convert(output[x, y], distance);
            }
            rightToLeft = !rightToLeft;
        }

        ErrorCorrection.MsdfErrorCorrection(output, shape, projection, range, config);
    }

    internal static void DeconvergeCubicEdge(CubicEdge edge, int param, double amount)
    {
        Vector2D dir = edge.GetDirection(param);
        Vector2D normal = dir.GetOrthonormal();
        double h = Vector2D.Dot(edge.GetDirectionChange(param) - dir, normal);
        switch (param)
        {
            case 0:
                edge.P[1] += amount * (dir + Math.Sign(h) * Math.Sqrt(Math.Abs(h)) * normal);
                break;
            case 1:
                edge.P[2] -= amount * (dir - Math.Sign(h) * Math.Sqrt(Math.Abs(h)) * normal);
                break;
        }
    }

    internal static void DeconvergeEdge(Edge edge, int param)
    {
        if (edge is QuadraticEdge quadratic)
            edge = quadratic.ConvertToCubic();

        if (edge is CubicEdge cubic)
            DeconvergeCubicEdge(cubic, param, MSDFGEN_DECONVERGENCE_FACTOR);
    }

    /// <summary>
    /// Normalizes the shape geometry for distance field generation.
    /// </summary>
    public void Normalize(Shape shape)
    {
        foreach (Contour contour in shape.Contours)
        {
            if (contour.Edges.Count == 1)
            {
                Edge[] parts = new Edge[3];

                contour.Edges[0].SplitInThirds(ref parts[0], ref parts[1], ref parts[2]);
                contour.Clear();
                contour.Add(parts[0]);
                contour.Add(parts[1]);
                contour.Add(parts[2]);
            }
            else
            {
                Edge prevEdge = contour.Edges.Last();

                foreach (Edge edge in contour.Edges)
                {
                    Vector2D prevDir = prevEdge.GetDirection(1).GetNormalized();
                    Vector2D curDir = edge.GetDirection(0).GetNormalized();

                    if (Vector2D.Dot(prevDir, curDir) < MSDFGEN_CORNER_DOT_EPSILON - 1)
                    {
                        DeconvergeEdge(prevEdge, 1);
                        DeconvergeEdge(edge, 0);
                    }

                    prevEdge = edge;
                }
            }
        }
    }

    public unsafe void To8Bit(TextureSliceRef<Color3> bitmap)
    {
        const int CHANNELS_PER_PIXEL = 3;
        float* data = (float*)bitmap.Data;
        float* end = data + ((bitmap.Width * bitmap.Height) * CHANNELS_PER_PIXEL);

        for (float* p = data; p < end; ++p)
            *p = float.Clamp(256f * *p, 0, 255f);
    }
}
