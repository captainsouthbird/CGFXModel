using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.Material
{
    public class TextureReference : ChunkDICTObject
    {
        protected override bool VerifyTypeId(uint typeId)
        {
            return typeId == 0x20000004;
        }
        public override string Magic => "TXOB"; // NOT the same as the Texture chunk's TXOB!

        public string ReferenceName { get; set; }    // NOT the usual "Name" field (which just points to a NULL string)
        public uint TexturePtr { get; private set; }    // ??? Name from SPICA

        public override void Load(Utility utility)
        {
            base.Load(utility);

            ReferenceName = utility.ReadString();
            TexturePtr = utility.ReadU32();
        }

        public override void Save(SaveContext saveContext)
        {
            base.Save(saveContext);

            saveContext.StringTable.EnqueueAndWriteTempRel(ReferenceName);
            saveContext.Utility.Write(TexturePtr);
        }
    }
}
