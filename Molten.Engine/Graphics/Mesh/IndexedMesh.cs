﻿namespace Molten.Graphics
{
    /// <summary>Represents an indexed mesh. These store mesh data by referring to vertices using index values stored in an index buffer. 
    /// In most cases this reduces the vertex data size drastically.</summary>
    /// <typeparam name="T">The vertex type in the form of a <see cref="IVertexType"/> type.</typeparam>
    /// <seealso cref="Mesh{T}" />
    public class IndexedMesh<T> : Mesh<T> where T : unmanaged, IVertexType
    {
        private protected IGraphicsBufferSegment _ib;
        private protected IndexBufferFormat _iFormat;
        private protected uint _indexCount;

        internal IndexedMesh(RenderService renderer, uint maxVertices, uint maxIndices, VertexTopology topology, IndexBufferFormat indexFormat, bool dynamic) : 
            base(renderer, maxVertices, topology, dynamic)
        {
            MaxIndices = maxIndices;
            _iFormat = indexFormat;

            IGraphicsBuffer iBuffer = dynamic ? renderer.DynamicVertexBuffer : renderer.StaticVertexBuffer;

            /* MSDN: The only formats allowed for index buffer data are 16-bit (DXGI_FORMAT_R16_UINT) and 32-bit (DXGI_FORMAT_R32_UINT) integers.
             */
            switch (_iFormat)
            {
                case IndexBufferFormat.Unsigned16Bit:
                    _ib = iBuffer.Allocate<ushort>(maxIndices);
                    break;

                case IndexBufferFormat.Unsigned32Bit:
                    _ib = iBuffer.Allocate<uint>(maxIndices);
                    break;
            }

            _ib.SetIndexFormat(indexFormat);
        }

        public void SetIndices<I>(I[] data) where I : unmanaged
        {
            SetIndices<I>(data, 0, (uint)data.Length);
        }

        public void SetIndices<I>(I[] data, uint count) where I : unmanaged
        {
            SetIndices<I>(data, 0, count);
        }

        public void SetIndices<I>(I[] data, uint startIndex, uint count) where I : unmanaged
        {
            _indexCount = count;
            _ib.SetData(data, startIndex, count, 0, Renderer.StagingBuffer); // Staging buffer will be ignored if the mesh is dynamic.
        }

        protected override void OnApply(GraphicsCommandQueue cmd)
        {
            base.OnApply(cmd);
            cmd.IndexBuffer.Value = _ib;
        }

        protected override void OnPostDraw(GraphicsCommandQueue cmd)
        {
            base.OnPostDraw(cmd);
            cmd.IndexBuffer.Value = null;
        }

        protected override void OnDraw(GraphicsCommandQueue cmd)
        {
            cmd.DrawIndexed(Material, _indexCount, Topology);
        }

        public override void Dispose()
        {
            base.Dispose();
            _ib.Release();
        }

        public uint MaxIndices { get; }

        public uint IndexCount => _indexCount;
    }
}
