using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGFXModel.Chunks.Model.Shape
{
    public class VertexAttribute : VertexBuffer
    {
        protected override uint ExpectedTypeId => 0x40000001;

        public uint BufferObject { get; set; }
        public uint LocationFlag { get; set; }

        private byte[] RawBuffer { get; set; }

        public uint LocationPtr { get; private set; }
        public uint MemoryArea { get; private set; }
        public GLDataType Format { get; set; }
        public int Elements { get; set; }
        public float Scale { get; set; }
        public int Offset { get; set; }

        protected override void LoadInternal(Utility utility)
        {
            BufferObject = utility.ReadU32();
            LocationFlag = utility.ReadU32();

            // The RawBuffer contains either 16-bit int or floating point values
            // for a Vector4, depending on "Format"; see GetVectors() for more...
            CGFXDebug.WriteLog($"NOTE: RawBuffer read in VertexAttribute, see next address of List");
            RawBuffer = utility.LoadIndirectValueList(() => utility.ReadByte());

            LocationPtr = utility.ReadU32();
            MemoryArea = utility.ReadU32();
            Format = (GLDataType)utility.ReadU32();
            Elements = utility.ReadI32();
            Scale = utility.ReadFloat();
            Offset = utility.ReadI32();
        }

        public Vector4[] GetVectors()
        {
            if (RawBuffer == null) return null;

            int Length = RawBuffer.Length / Elements;

            switch (Format)
            {
                case GLDataType.GL_SHORT: Length >>= 1; break;
                case GLDataType.GL_FLOAT: Length >>= 2; break;
            }

            Vector4[] Output = new Vector4[Length];

            using (var MS = new MemoryStream(RawBuffer))
            {
                var Reader = new BinaryReader(MS);

                for (int i = 0; i < Output.Length; i++)
                {
                    for (int j = 0; j < Elements; j++)
                    {
                        float Value = 0;

                        switch (Format)
                        {
                            case GLDataType.GL_BYTE: Value = Reader.ReadSByte(); break;
                            case GLDataType.GL_UNSIGNED_BYTE: Value = Reader.ReadByte(); break;
                            case GLDataType.GL_SHORT: Value = Reader.ReadInt16(); break;
                            case GLDataType.GL_FLOAT: Value = Reader.ReadSingle(); break;
                        }

                        Value *= Scale;

                        switch (j)
                        {
                            case 0: Output[i].X = Value; break;
                            case 1: Output[i].Y = Value; break;
                            case 2: Output[i].Z = Value; break;
                            case 3: Output[i].W = Value; break;
                        }
                    }
                }
            }

            return Output;
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            utility.Write(BufferObject);
            utility.Write(LocationFlag);

            utility.Write(RawBuffer?.Length ?? 0);
            saveContext.IMAGData.EnqueueAndWriteTempRel(RawBuffer);

            utility.Write(LocationPtr);
            utility.Write(MemoryArea);
            utility.Write((uint)Format);
            utility.Write(Elements);
            utility.Write(Scale);
            utility.Write(Offset);
        }
    }
}
