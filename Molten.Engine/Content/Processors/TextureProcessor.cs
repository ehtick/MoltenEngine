﻿using Molten.Graphics;
using Molten.Graphics.Textures;
using Molten.Graphics.Textures.DDS;
using System.Text;

namespace Molten.Content;

public class TextureProcessor : ContentProcessor<TextureParameters>
{
    public override Type[] AcceptedTypes { get; } = [ typeof(ITexture), typeof(TextureData) ];

    public override Type[] RequiredServices { get; } = [typeof(RenderService) ];

    public override Type PartType { get; } = typeof(TextureData);

    protected override bool OnReadPart(ContentLoadHandle handle, Stream stream, TextureParameters parameters, object existingPart, out object partAsset)
    {
        TextureData data = null;
        TextureReader texReader;
        string extension = handle.Info.Extension.ToLower();

        using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
        {
            switch (extension)
            {
                case ".dds":
                    texReader = new DDSReader();
                    break;

                // Although the default texture reader can load all of the formats that Magick supports, we'll stick to ones we fully support for now.
                // Formats such as .gif can be handled as texture arrays later down the line.
                case ".png":
                case ".jpeg":
                case ".bmp":
                    texReader = new DefaultTextureReader();
                    break;

                default:
                    texReader = null;
                    break;
            }

            data = texReader.Read(reader, handle.Manager.Log, handle.RelativePath);
            texReader.Dispose();
        }

        partAsset = data;

        // Load failed?
        if (data == null)
            return false;

        if (data.MipMapLevels == 1)
        {
            if (parameters.GenerateMipmaps)
            {
                //if (!data.GenerateMipMaps())
                //   log.WriteError("[CONTENT] Unable to generate mip-maps for non-power-of-two texture.", file.ToString());
            }
        }

        return true;
    }

    protected override bool OnBuildAsset(ContentLoadHandle handle, ContentLoadHandle[] parts, TextureParameters parameters, object existingAsset, out object asset)
    {
        TextureData finalData = null;

        uint arraySize = (uint)(typeof(ITextureCube).IsAssignableFrom(handle.ContentType) ? Math.Min(6, parts.Length) : parts.Length);

        for (uint i = 0; i < parts.Length; i++)
        {
            ContentLoadHandle partHandle = parts[i];
            if (partHandle.Asset == null)
                continue;

            TextureData data = partHandle.Get<TextureData>();

            finalData ??= new TextureData(data.Width, data.Height, data.Depth, data.MipMapLevels, arraySize)
            {
                Format = data.Format,
                IsCompressed = data.IsCompressed,
            };


            finalData.Set(data, i);
        }

        // Compress or decompress
        if (parameters.BlockCompressionFormat.HasValue)
        {
            // Don't block-compress 1D textures.
            if (finalData.Height > 1)
            {
                if (!finalData.IsCompressed)
                    finalData.Compress(parameters.BlockCompressionFormat.Value, handle.Manager.Log);
            }
        }

        // TODO improve for texture arrays - Only update the array slice(s) that have changed.
        // Check if an existing texture was passed in.
        if (handle.ContentType == typeof(TextureData))
        {
            asset = finalData;
            return true;
        }
        else
        {
            ITexture tex = existingAsset as ITexture;
            asset = tex;

            if (tex != null)
                ReloadTexture(handle.Manager, tex, finalData);
            else
                asset = CreateTexture(handle, finalData);
        }

        return true;
    }

    private ITexture CreateTexture(ContentHandle handle, TextureData data)
    {
        ITexture tex = null;
        ContentManager manager = handle.Manager;
        GpuDevice device = manager.Engine.Renderer.Device;

        if (handle.ContentType == typeof(ITexture2D))
            tex = device.Resources.CreateTexture2D(data);
        else if (handle.ContentType == typeof(ITextureCube))
            tex = device.Resources.CreateTextureCube(data);
        else if (handle.ContentType == typeof(ITexture1D))
            tex = device.Resources.CreateTexture1D(data);
        else
            manager.Log.Error($"Unsupported texture type {handle.ContentType}", handle.RelativePath);

        tex.Name += "_" + handle.RelativePath;
        return tex;
    }

    private void ReloadTexture(ContentManager manager, ITexture tex, TextureData data)
    {
        RenderService renderer = manager.Engine.Renderer;

        switch (tex)
        {
            case ITextureCube texCube:
                // TODO include mip-map count in resize
                if (texCube.Width != data.Width ||
                    texCube.Height != data.Height ||
                    tex.MipMapCount != data.MipMapLevels)
                    texCube.Resize(GpuPriority.StartOfFrame, null, data.Width, data.Height, data.MipMapLevels);

                texCube.SetData(GpuPriority.StartOfFrame, null, data, 0, 0, data.MipMapLevels, Math.Min(data.ArraySize, 6), 0, 0);
                break;

            case ITexture2D tex2d:
                // TODO include mip-map count in resize
                if (tex2d.Width != data.Width ||
                    tex2d.Height != data.Height ||
                    tex2d.ArraySize != data.ArraySize ||
                    tex.MipMapCount != data.MipMapLevels)
                {
                    tex2d.Resize(GpuPriority.StartOfFrame, null, data.Width, data.Height, data.MipMapLevels, data.ArraySize, data.Format);
                }

                tex2d.SetData(GpuPriority.StartOfFrame, null, data, 0, 0, data.MipMapLevels, data.ArraySize, 0, 0);
                break;

            case ITexture1D tex1d:
                // TODO include mip-map count in resize
                if (tex1d.Width != data.Width || tex.MipMapCount != data.MipMapLevels)
                    tex1d.Resize(GpuPriority.StartOfFrame, null, data.Width, data.MipMapLevels, data.ArraySize, data.Format);

                tex.SetData(GpuPriority.StartOfFrame, null, data, 0, 0, data.MipMapLevels, data.ArraySize, 0, 0);
                break;
        }
    }

    protected override bool OnWrite(ContentHandle handle, Stream stream, TextureParameters parameters, object asset)
    {
        string extension = handle.Info.Extension.ToLower();
        TextureWriter texWriter = null;

        switch (extension)
        {
            case ".dds":
                DDSFormat? pFormat = parameters.BlockCompressionFormat;
                DDSFormat ddsFormat = pFormat.HasValue ? pFormat.Value : DDSFormat.DXT5;
                texWriter = new DDSWriter(ddsFormat);
                break;

            case ".png":
                texWriter = new PNGWriter();
                break;

            case ".jpeg":
            case ".jpg":
                texWriter = new JPEGWriter();
                break;

            case ".bmp":
                texWriter = new BMPWriter();
                break;
        }

        if (texWriter == null)
        {
            handle.Manager.Log.Error($"Unable to write texture to file. Unsupported format: {extension}", handle.RelativePath);
            return false;
        }

        // TODO improve for texture arrays - Only update the array slice(s) that have changed.
        // Check if an existing texture was passed in.
        if (handle.ContentType == typeof(TextureData))
        {
            TextureData dataToSave = asset as TextureData;
            texWriter.WriteData(stream, dataToSave, handle.Manager.Log, handle.RelativePath);
        }
        else
        {
            // TODO finish support for writing textures directly

            ITexture tex = asset as ITexture;
            ITexture staging = handle.Manager.Engine.Renderer.Device.Resources.CreateUploadTexture(tex);

            if (staging != null)
            {
                TextureData tData = null;
                tex.GetData(GpuPriority.EndOfFrame, null, (data) =>
                {
                    tData = data;
                });

                // TODO Remove the need for this horrible Thread.Sleep() wait loop.
                while (tData == null)
                    Thread.Sleep(10);

                texWriter.WriteData(stream, tData, handle.Manager.Log, handle.RelativePath);
            }
            else
            {
                handle.Manager.Log.Error($"Unable to write texture to file. Unsupported texture type: {tex.GetType().Name}");
                return false;
            }
        }

        return true;
    }
}
