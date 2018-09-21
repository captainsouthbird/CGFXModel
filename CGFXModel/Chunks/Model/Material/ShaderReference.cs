using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.Material
{
    public class ShaderReference : ChunkDICTObject
    {
        protected override bool VerifyTypeId(uint typeId)
        {
            return typeId == 0x80000001;
        }
        public override string Magic => "SHDR"; 

        public string ReferenceName { get; set; }    // NOT the usual "Name" field (which just points to a NULL string)
        public uint ShaderPtr { get; private set; }    // ??? Name from SPICA

        public override void Load(Utility utility)
        {
            base.Load(utility);

            ReferenceName = utility.ReadString();
            ShaderPtr = utility.ReadU32();
        }

        public override void Save(SaveContext saveContext)
        {
            base.Save(saveContext);

            saveContext.StringTable.EnqueueAndWriteTempRel(ReferenceName);
            saveContext.Utility.Write(ShaderPtr);
        }
    }
}
