using System;
using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model
{
    public class DICTObjMesh : ChunkDICTObject
    {
        public int ShapeIndex { get; set; }
        public int MaterialId { get; set; }
        public DICTObjModel Model { get; set; } // REQUIRED that this be assigned after Mesh is loaded, and will require special writing later
        public bool IsVisible { get; set; }     // Actually byte-sized, stores 0 or 1
        public byte RenderPriority { get; set; }
        public ushort MeshNodeIndex { get; set; }
        public int PrimitiveIndex { get; set; }

        // According to SPICA, all of the following is only used "by the game engine"
        // and will always be zero from the original file ...
        public uint Flags { get; private set; }

        public uint[] AttrScaleCommands { get; private set; }   // 12

        public uint EnableCommandsPtr { get; private set; }
        public uint EnableCommandsLength { get; private set; }

        public uint DisableCommandsPtr { get; private set; }
        public uint DisableCommandsLength { get; private set; }

        public string MeshNodeName { get; private set; }

        public ulong RenderKeyCache { get; private set; }
        public uint CommandAlloc { get; private set; }

        // NOTE -- through testing there is absolutely always 8 bytes (all zeroed) following a Mesh.
        // However, for all but the LAST ONE there is an additional 4 bytes (of zeroes.)
        // It seems in the case of the "last" Mesh, we've always "run into" the TypeId for the 
        // following MTOB (Material DICT.) I don't know what these values might be... they don't align
        // particularly, so they don't seem to be alignment bytes. The best I can do with them is read 
        // the first two and then if the third one contains information (because it's the "last" Mesh,
        // presumably) then I discard it (set it null, in this case.) Probably all of this is suspect,
        // but I don't know if anything else clearly indicates what this is.
        public uint Unknown1 { get; private set; }   // SPICA doesn't mark this, found through testing
        public uint Unknown2 { get; private set; }   // SPICA doesn't mark this, found through testing
        public uint? Unknown3 { get; private set; }   // SPICA doesn't mark this, found through testing

        protected override bool VerifyTypeId(uint typeId)
        {
            return typeId == 0x01000000;
        }
        public override string Magic => "SOBJ";

        public override void Load(Utility utility)
        {
            base.Load(utility);

            // TODO -- intelligently link these somehow??
            ShapeIndex = utility.ReadI32();
            MaterialId = utility.ReadI32();

            // POINTS back to parent CMDL; useless to store since we can't know this when re-writing until later
            var ownerModelOffset = utility.ReadOffset();

            IsVisible = (utility.ReadByte() & 1) > 0;
            RenderPriority = utility.ReadByte();
            MeshNodeIndex = utility.ReadU16();  // ObjectNodeVisibilityIndex in Ohana3DS, MeshNodeIndex in SPICA
            PrimitiveIndex = utility.ReadI32();  // CurrentPrimitiveIndex in Ohana3DS

            Flags = utility.ReadU32();
            AttrScaleCommands = utility.ReadUInts(12);
            EnableCommandsPtr = utility.ReadU32();
            EnableCommandsLength = utility.ReadU32();
            DisableCommandsPtr = utility.ReadU32();
            DisableCommandsLength = utility.ReadU32();
            MeshNodeName = utility.ReadString();
            RenderKeyCache = utility.ReadU32();
            CommandAlloc = utility.ReadU32();

            // UNKNOWN, not in SPICA or anywhere I can tell, see notes at declaration
            Unknown1 = utility.ReadU32();
            Unknown2 = utility.ReadU32();
            Unknown3 = utility.ReadU32();   // May be the start of MTOB

            if(Unknown1 != 0 || Unknown2 != 0)
            {
                throw new InvalidOperationException("DICTObjMesh Load: Unexpected non-zero values in Unknown1 / Unknown2");
            }

            if(Unknown3 == 0x08000000)  // The MTOB this "runs into"
            {
                Unknown3 = null;    // Do not persist, it's wrong
            }
            else if(Unknown3 != 0)
            {
                throw new InvalidOperationException("DICTObjMesh Load: Unrecognized non-zero value in Unknown3");
            }
        }

        public override void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            base.Save(saveContext);

            utility.Write(ShapeIndex);
            utility.Write(MaterialId);

            saveContext.WritePointerPlaceholder(Model);

            utility.Write((byte)(IsVisible ? 1 : 0));
            utility.Write(RenderPriority);
            utility.Write(MeshNodeIndex);  // ObjectNodeVisibilityIndex in Ohana3DS, MeshNodeIndex in SPICA
            utility.Write(PrimitiveIndex);  // CurrentPrimitiveIndex in Ohana3DS

            utility.Write(Flags);
            utility.Write(AttrScaleCommands);
            utility.Write(EnableCommandsPtr);
            utility.Write(EnableCommandsLength);
            utility.Write(DisableCommandsPtr);
            utility.Write(DisableCommandsLength);
            saveContext.StringTable.EnqueueAndWriteTempRel(MeshNodeName);
            utility.Write(RenderKeyCache);
            utility.Write(CommandAlloc);

            // UNKNOWN, not in SPICA or anywhere I can tell, see notes at declaration
            utility.Write(Unknown1);
            utility.Write(Unknown2);

            if (Unknown3 != null)
            {
                utility.Write(Unknown3.Value);
            }
        }
    }
}
