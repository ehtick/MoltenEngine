﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Molten.Graphics.Textures
{
    internal class BC4Parser : BCBlockParser
    {
        public override GraphicsFormat[] SupportedFormats => new GraphicsFormat[] { GraphicsFormat.BC4_UNorm };

        protected unsafe override void DecodeBlock(BinaryReader reader, BCDimensions dimensions, int width, int height, byte[] output)
        {
            byte red0 = reader.ReadByte();
            byte red1 = reader.ReadByte();
            ulong redMask = Decode8BitSingleChannelMask(reader, out red0, out red1);

            // Decompress pixel data from block
            for (int bpy = 0; bpy < DDSHelper.BLOCK_DIMENSIONS; bpy++)
            {
                int py = (dimensions.Y << 2) + bpy;
                for (int bpx = 0; bpx < DDSHelper.BLOCK_DIMENSIONS; bpx++)
                {
                    int px = (dimensions.X << 2) + bpx;
                    if ((px < width) && (py < height))
                    {
                        int offset = ((py * width) + px) << 2;
                        byte cb = DecodeSingleChannelColor(redMask, bpx, bpy, red0, red1);

                        output[offset] = cb;
                        output[offset + 1] = cb;
                        output[offset + 2] = cb;
                        output[offset + 3] = 255;
                    }
                }
            }
        }

        protected override void EncodeBlock(BinaryWriter writer, BCDimensions dimensions, TextureData.Slice level)
        {
            int bytesPerPixel = 4;
            Encode8BitSingleChannelBlock(writer, level, ref dimensions, bytesPerPixel, 0);
        }
    }
}
