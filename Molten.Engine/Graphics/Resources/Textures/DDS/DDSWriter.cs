﻿using Molten.Graphics.Textures.DDS;

namespace Molten.Graphics.Textures;

public class DDSWriter : TextureWriter
{
    DDSFormat _format;

    public DDSWriter(DDSFormat format)
    {
        _format = format;
    }

    public unsafe override void WriteData(Stream stream, TextureData data, Logger log, string filename = null)
    {
        if (!uint.IsPow2(data.Width) || !uint.IsPow2(data.Height))
        {
            log.Error("Cannot save DDS file: Width and height must be power-of-two.", filename);
            return;
        }

        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            // Use DXT5 since this covers most types of images (transparent and opaque).
            if (data.IsCompressed == false)
                data.Compress(_format, log);

            // Create new DDS header
            DDSHeader header = new DDSHeader()
            {
                Size = 124,
                Flags = DDSFlags.Capabilities | DDSFlags.Width | DDSFlags.Height | DDSFlags.MipMapCount | DDSFlags.PixelFormat | DDSFlags.LinearSize,
                Height = data.Height,
                Width = data.Width,
                PitchOrLinearSize = data.Levels[0].TotalBytes,
                Depth = 0,
                MipMapCount = data.MipMapLevels,
                Reserved = new uint[11],
                PixelFormat = GetPixelFormat(data),
                Caps = DDSCapabilities.Texture | DDSCapabilities.Complex | DDSCapabilities.MipMap,
                Caps2 = DDSAdditionalCapabilities.None,
                Caps3 = 0,
                Caps4 = 0,
                Reserved2 = 0,
            };

            WriteMagicWord(writer);
            WriteHeader(writer, ref header);

            if (header.PixelFormat.FourCC == "DX10")
            {
                DDSHeaderDXT10 dx10 = new DDSHeaderDXT10()
                {
                    ImageFormat = data.Format,
                    Dimension = GetDx10Dimension(data),
                    MiscFlags = DDSMiscFlags.None,
                    ArraySize = data.ArraySize,
                    MiscFlags2 = DDSMiscFlags2.AlphaUnknown,
                };

                writer.Write((uint)dx10.ImageFormat);
                writer.Write((uint)dx10.Dimension);
                writer.Write((uint)dx10.MiscFlags);
                writer.Write(dx10.ArraySize);
                writer.Write((uint)dx10.MiscFlags2);
            }

            // Write each mip map level
            for (int i = 0; i < data.MipMapLevels; i++)
            {
                Span<byte> pData = new Span<byte>(data.Levels[i].Data, (int)data.Levels[i].TotalBytes);
                writer.BaseStream.Write(pData);
            }
        }
    }

    private DDSResourceDimension GetDx10Dimension(TextureData data)
    {
        if (data.Height > 1)
            return DDSResourceDimension.Texture2D;
        else
            return DDSResourceDimension.Texture1D;
    }

    private DDSPixelFormat GetPixelFormat(TextureData data)
    {
        DDSPixelFormat result = new DDSPixelFormat();
        result.Size = 32;
        result.Flags = DDSPixelFormatFlags.FourCC;

        switch (data.Format)
        {
            case GpuResourceFormat.BC1_UNorm:
                result.FourCC = "DXT1";
                result.RGBBitCount = 32;
                break;

            case GpuResourceFormat.BC2_UNorm:
                result.FourCC = "DXT3";
                result.RGBBitCount = 32;
                break;

            case GpuResourceFormat.BC3_UNorm:
                result.FourCC = "DXT5";
                result.RGBBitCount = 32;
                break;

            case GpuResourceFormat.BC4_UNorm:
                result.FourCC = "BC4U";
                result.RGBBitCount = 8;
                break;

            case GpuResourceFormat.BC4_SNorm:
                result.FourCC = "BC4S";
                result.RGBBitCount = 8;
                break;

            case GpuResourceFormat.BC5_UNorm:
                result.FourCC = "BC5U";
                result.RGBBitCount = 16;
                break;

            case GpuResourceFormat.BC5_SNorm:
                result.FourCC = "BC5S";
                result.RGBBitCount = 16;
                break;

            case GpuResourceFormat.BC7_UNorm:
            case GpuResourceFormat.BC7_UNorm_SRgb:
            case GpuResourceFormat.BC6H_Sf16:
            case GpuResourceFormat.BC6H_Uf16:
                result.FourCC = "DX10";
                result.RGBBitCount = 8;
                break;

            default:
                throw new FormatException("Unsupported DDS block-compression format:" + data.Format);
        }

        return result;
    }

    private uint GetFourCCValue(string cc)
    {
        // Create CC hash.
        uint result = (uint)(cc[3] << 24);
        result |= (uint)(cc[2] << 16);
        result |= (uint)(cc[1] << 8);
        result |= cc[0];

        return result;
    }

    private void WriteMagicWord(BinaryWriter writer)
    {
        char a = 'D';
        char b = 'D';
        char c = 'S';

        uint result = (uint)32 << 24;
        result |= (uint)c << 16;
        result |= (uint)b << 8;
        result |= a;

        writer.Write(result);
    }

    private void WriteHeader(BinaryWriter writer, ref DDSHeader header)
    {
        writer.Write(header.Size);
        writer.Write((uint)header.Flags);
        writer.Write(header.Height);
        writer.Write(header.Width);
        writer.Write(header.PitchOrLinearSize);
        writer.Write(header.Depth);
        writer.Write(header.MipMapCount);

        for (int i = 0; i < header.Reserved.Length; i++)
            writer.Write(header.Reserved[i]);

        writer.Write(header.PixelFormat.Size);
        writer.Write((uint)header.PixelFormat.Flags);

        string f = header.PixelFormat.FourCC;
        uint fourCC = GetFourCCValue(header.PixelFormat.FourCC);
        writer.Write(fourCC);

        writer.Write(header.PixelFormat.RGBBitCount);
        writer.Write(header.PixelFormat.RBitMask);
        writer.Write(header.PixelFormat.GBitMask);
        writer.Write(header.PixelFormat.BBitMask);
        writer.Write(header.PixelFormat.ABitMask);

        writer.Write((uint)header.Caps);
        writer.Write((uint)header.Caps2);
        writer.Write(header.Caps3);
        writer.Write(header.Caps4);
        writer.Write(header.Reserved2);
    }

    protected override void OnDispose(bool immediate) { }
}
