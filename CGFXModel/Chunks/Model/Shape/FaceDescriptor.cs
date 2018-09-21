using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CGFXModel.Chunks.Model.Shape
{
    public class FaceDescriptor : ISaveable
    {
        public enum PICAPrimitiveMode : uint
        {
            Triangles,
            TriangleStrip,
            TriangleFan,
            GeometryPrimitive
        }


        public GLDataType Format { get; private set; }

        private byte _PrimitiveMode { get; set; }

        public PICAPrimitiveMode PrimitiveMode
        {
            get => (PICAPrimitiveMode)_PrimitiveMode;
            set => _PrimitiveMode = (byte)value;
        }

        private byte Visible { get; set; }

        // RawBuffer in this context is a list of indicies in either 8 or 16-bit format (based on "Format")
        private byte[] RawBuffer { get; set; }
        public List<ushort> Indices { get; private set; }

        private uint BufferObj { get; set; }
        private uint LocationFlag { get; set; }

        private uint CommandCachePtr { get; set; }
        private uint CommandCacheLength { get; set; }

        private uint LocationPtr { get; set; }
        private uint MemoryArea { get; set; }

        private uint BoundingVolume { get; set; }

        public static FaceDescriptor Load(Utility utility)
        {
            var fd = new FaceDescriptor();

            CGFXDebug.LoadStart(fd, utility);

            fd.Format = (GLDataType)utility.ReadU32();
            fd._PrimitiveMode = utility.ReadByte();
            fd.Visible = utility.ReadByte();

            utility.AlignRead(4);

            // The RawBuffer contains either 8-bit or 16-bit indices, depending 
            // on "Format"; see GetIndices() for more...
            CGFXDebug.WriteLog($"NOTE: RawBuffer read in FaceDescriptor, see next address of List");
            fd.RawBuffer = utility.LoadIndirectValueList(() => utility.ReadByte());

            // Convert to indices
            fd.GetIndices();

            fd.BufferObj = utility.ReadU32();
            fd.LocationFlag = utility.ReadU32();

            fd.CommandCachePtr = utility.ReadU32();
            fd.CommandCacheLength = utility.ReadU32();

            fd.LocationPtr = utility.ReadU32();
            fd.MemoryArea = utility.ReadU32();

            fd.BoundingVolume = utility.ReadU32();

            return fd;
        }

        private void GetIndices()
        {
            // I'm only expecting GL_UNSIGNED_BYTE or GL_UNSIGNED_SHORT here
            // I suppose the "signed" ones could be supported but no idea if
            // they ever come up or why they would...
            // If this assumption is wrong, fix the promotion code in SetIndices()
            if (Format != GLDataType.GL_UNSIGNED_BYTE && Format != GLDataType.GL_UNSIGNED_SHORT)
            {
                throw new InvalidOperationException($"FaceDescriptor GetIndices: Unexpected Format {Format}");
            }

            bool IsBuffer16Bits = Format == GLDataType.GL_UNSIGNED_SHORT;

            var indices = new ushort[RawBuffer.Length >> (IsBuffer16Bits ? 1 : 0)];

            for (int i = 0; i < RawBuffer.Length; i += (IsBuffer16Bits ? 2 : 1))
            {
                if (IsBuffer16Bits)
                {
                    indices[i >> 1] = (ushort)(
                        RawBuffer[i + 0] << 0 |
                        RawBuffer[i + 1] << 8);
                }
                else
                {
                    indices[i] = RawBuffer[i];
                }
            }

            Indices = indices.ToList();
        }

        private void SetIndices(Utility utility)
        {
            // EXPERIMENTAL: I assume there's no harm in promoting from 8-bit to 16-bit if needed.
            // But to limit possible repercussions, I'm deliberately NOT implementing a coorresponding
            // downgrade path, although it would be theoretically the same idea...
            if((Format == GLDataType.GL_UNSIGNED_BYTE) && Indices.Where(i => i > 255).Any())
            {
                Format = GLDataType.GL_UNSIGNED_SHORT;
            }

            var IsBuffer16Bits = Format == GLDataType.GL_UNSIGNED_SHORT;
            var elementSize = (IsBuffer16Bits ? 2 : 1);
            RawBuffer = new byte[Indices.Count * elementSize];

            for (int i = 0; i < Indices.Count; i++)
            {
                // Whether we're ignoring the high byte, we can use this either way
                var indexBytes = utility.GetU16(Indices[i]);
                var rawBufferIndex = i * elementSize;
                RawBuffer[rawBufferIndex + 0] = indexBytes[0];

                if (IsBuffer16Bits)
                {
                    RawBuffer[rawBufferIndex + 1] = indexBytes[1];
                }
            }
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            utility.Write((uint)Format);
            utility.Write(_PrimitiveMode);
            utility.Write(Visible);

            utility.AlignWrite(4);

            // Convert from Indices
            SetIndices(utility);

            utility.Write(RawBuffer.Length);
            saveContext.IMAGData.EnqueueAndWriteTempRel(RawBuffer);

            utility.Write(BufferObj);
            utility.Write(LocationFlag);

            utility.Write(CommandCachePtr);
            utility.Write(CommandCacheLength);

            utility.Write(LocationPtr);
            utility.Write(MemoryArea);

            utility.Write(BoundingVolume);
        }
    }
}
