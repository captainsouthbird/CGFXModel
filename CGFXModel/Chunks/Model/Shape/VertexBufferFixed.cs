using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.Shape
{
    public class VertexBufferFixed : VertexBuffer
    {
        protected override uint ExpectedTypeId => 0x80000000;

        public GLDataType Format { get; set; }
        public int Elements { get; set; }
        public float Scale { get; set; }
        public float[] Vector { get; set; }

        protected override void LoadInternal(Utility utility)
        {
            Format = (GLDataType)utility.ReadU32();
            Elements = utility.ReadI32();
            Scale = utility.ReadFloat();

            // Vectors
            Vector = utility.LoadIndirectValueList(() => utility.ReadFloat());
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            utility.Write((uint)Format);
            utility.Write(Elements);
            utility.Write(Scale);

            // Vectors
            saveContext.WriteValueListPointerPlaceholder(Vector);

            /////////////////////////////
            // Begin saving dependent data

            // UNTESTED (no test data available at the moment)
            saveContext.SaveAndMarkReference(Vector);
        }
    }
}
