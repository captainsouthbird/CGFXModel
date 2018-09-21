using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.Shape
{
    public class VertexBufferInterleaved : VertexBuffer
    {
        protected override uint ExpectedTypeId => 0x40000002;

        public uint BufferObject { get; set; }
        public uint LocationFlag { get; set; }

        // NOTE TO SELF -- goes in IMAG chunk!
        public byte[] RawBuffer { get; set; }

        public uint LocationPtr { get; private set; }
        public uint MemoryArea { get; private set; }

        public int VertexStride { get; private set; }

        public List<VertexAttribute> Attributes { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            BufferObject = utility.ReadU32();
            LocationFlag = utility.ReadU32();

            // The RawBuffer contains ???
            CGFXDebug.WriteLog($"NOTE: RawBuffer read in VertexBufferInterleaved, see next address of List");
            RawBuffer = utility.LoadIndirectValueList(() => utility.ReadByte());

            LocationPtr = utility.ReadU32();
            MemoryArea = utility.ReadU32();
            VertexStride = utility.ReadI32();

            Attributes = utility.LoadIndirectObjList(() => Load<VertexAttribute>(utility)).ToList();
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            utility.Write(BufferObject);
            utility.Write(LocationFlag);

            utility.Write(RawBuffer.Length);
            saveContext.IMAGData.EnqueueAndWriteTempRel(RawBuffer);

            utility.Write(LocationPtr);
            utility.Write(MemoryArea);
            utility.Write(VertexStride);

            saveContext.WriteObjectListPointerPlaceholder(Attributes);

            /////////////////////////////
            // Begin saving dependent data

            saveContext.SaveAndMarkReference(Attributes);

            Attributes.SaveList(saveContext);
        }
    }
}
