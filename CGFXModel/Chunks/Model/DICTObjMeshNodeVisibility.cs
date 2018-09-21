using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model
{
    public class DICTObjMeshNodeVisibility : ChunkDICTObject
    {
        protected override bool VerifyTypeId(uint typeId)
        {
            return typeId == 0x20000000;    // FIXME -- may not be correct, I have no test data atm
        }
        public override string Magic => "SOBJ";

        public string Name { get; set; }
        public bool IsVisible { get; set; }

        public override void Load(Utility utility)
        {
            base.Load(utility);

            Name = utility.ReadString();
            IsVisible = utility.ReadU32() == 1;
        }

        public override void Save(SaveContext saveContext)
        {
            base.Save(saveContext);

            throw new NotImplementedException();
        }
    }
}
