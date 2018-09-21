using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.Material
{
    public class LUT : ChunkDICTObject
    {
        protected override bool VerifyTypeId(uint typeId)
        {
            return typeId == 0x04000000;
        }

        public override string Magic => "????";

        public string LUTName { get; private set; }
        public bool IsAbsolute { get; set; }
        public byte[] RawCommands { get; private set; }

        public override void Load(Utility utility)
        {
            base.Load(utility);

            LUTName = utility.ReadString();
            IsAbsolute = utility.ReadU32() != 0;

            // See GfxLUTSampler.cs though I'm not sure it's much help :(
            // MIGHT be 256 bytes (or 256 4Bs ???) to read for RawCommands???
            // Maybe a length pointer???
            // Dunno...


            // THIS IS A GUESS:
            var length = utility.ReadU32();
            RawCommands = utility.ReadBytes(length); // <----- ????

            throw new NotImplementedException();
        }

        public override void Save(SaveContext saveContext)
        {
            base.Save(saveContext);

            throw new NotImplementedException();
        }
    }

    public class ChunkDICTLUT : ChunkDICT<LUT>
    {
    }
}
