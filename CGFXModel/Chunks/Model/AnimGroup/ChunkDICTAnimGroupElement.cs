using CGFXModel.Utilities;
using System;

namespace CGFXModel.Chunks.Model.AnimGroup
{
    public enum AnimGroupObjType
    {
        Bone,
        MaterialColor,
        TexSampler,
        TexMapper,
        BlendOperation,
        TexCoord,
        Model,
        Mesh,
        MeshNodeVisibility
    }

    public class AnimGroupMeshNodeVis : AnimGroupElementBase
    {
        public string NodeName { get; set; }
        public AnimGroupObjType ObjType2 { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            NodeName = utility.ReadString();
            ObjType2 = (AnimGroupObjType)utility.ReadU32();     // Always the same as ObjType??
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.StringTable.EnqueueAndWriteTempRel(NodeName);
            saveContext.Utility.Write((uint)ObjType2);
        }
    }

    public class AnimGroupMesh : AnimGroupElementBase
    {
        public int MeshIndex { get; set; }
        public AnimGroupObjType ObjType2 { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            MeshIndex = utility.ReadI32();
            ObjType2 = (AnimGroupObjType)utility.ReadU32();     // Always the same as ObjType??
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.Utility.Write(MeshIndex);
            saveContext.Utility.Write((uint)ObjType2);
        }
    }

    public class AnimGroupTexSampler : AnimGroupElementBase
    {
        public string MaterialName { get; set; }
        public int TexSamplerIndex { get; set; }
        public AnimGroupObjType ObjType2 { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            MaterialName = utility.ReadString();
            TexSamplerIndex = utility.ReadI32();
            ObjType2 = (AnimGroupObjType)utility.ReadU32();     // Always the same as ObjType??
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.StringTable.EnqueueAndWriteTempRel(MaterialName);
            saveContext.Utility.Write(TexSamplerIndex);
            saveContext.Utility.Write((uint)ObjType2);
        }
    }

    public class AnimGroupBlendOp : AnimGroupElementBase
    {
        public string MaterialName { get; set; }
        public AnimGroupObjType ObjType2 { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            MaterialName = utility.ReadString();
            ObjType2 = (AnimGroupObjType)utility.ReadU32();     // Always the same as ObjType??
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.StringTable.EnqueueAndWriteTempRel(MaterialName);
            saveContext.Utility.Write((uint)ObjType2);
        }
    }

    public class AnimGroupMaterialColor : AnimGroupElementBase
    {
        public string MaterialName { get; set; }
        public AnimGroupObjType ObjType2 { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            MaterialName = utility.ReadString();
            ObjType2 = (AnimGroupObjType)utility.ReadU32();     // Always the same as ObjType??
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.StringTable.EnqueueAndWriteTempRel(MaterialName);
            saveContext.Utility.Write((uint)ObjType2);
        }
    }

    public class AnimGroupModel : AnimGroupElementBase
    {
        public AnimGroupObjType ObjType2 { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            ObjType2 = (AnimGroupObjType)utility.ReadU32();     // Always the same as ObjType??
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.Utility.Write((uint)ObjType2);
        }
    }

    public class AnimGroupTexMapper : AnimGroupElementBase
    {
        public string MaterialName { get; set; }
        public int TexMapperIndex { get; set; }
        public AnimGroupObjType ObjType2 { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            MaterialName = utility.ReadString();
            TexMapperIndex = utility.ReadI32();
            ObjType2 = (AnimGroupObjType)utility.ReadU32();     // Always the same as ObjType??
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.StringTable.EnqueueAndWriteTempRel(MaterialName);
            saveContext.Utility.Write(TexMapperIndex);
            saveContext.Utility.Write((uint)ObjType2);
        }
    }

    public class AnimGroupBone : AnimGroupElementBase
    {
        public string BoneName { get; set; }
        public AnimGroupObjType ObjType2 { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            BoneName = utility.ReadString();
            ObjType2 = (AnimGroupObjType)utility.ReadU32();     // Always the same as ObjType??
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.StringTable.EnqueueAndWriteTempRel(BoneName);
            saveContext.Utility.Write((uint)ObjType2);
        }
    }

    public class AnimGroupTexCoord : AnimGroupElementBase
    {
        public string MaterialName { get; set; }
        public int TexCoordIndex { get; set; }
        public AnimGroupObjType ObjType2 { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            MaterialName = utility.ReadString();
            TexCoordIndex = utility.ReadI32();
            ObjType2 = (AnimGroupObjType)utility.ReadU32();     // Always the same as ObjType??
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.StringTable.EnqueueAndWriteTempRel(MaterialName);
            saveContext.Utility.Write(TexCoordIndex);
            saveContext.Utility.Write((uint)ObjType2);
        }
    }

    public abstract class AnimGroupElementBase : ISaveable
    {
        public uint TypeId { get; private set; }
        public string Name { get; set; }
        public int MemberOffset { get; set; }
        public int BlendOpIndex { get; set; }
        public AnimGroupObjType ObjType { get; protected set; }
        public uint MemberType { get; private set; }
        public uint MaterialPtr { get; private set; }

        public static AnimGroupElementBase Load(Utility utility)
        {
            AnimGroupElementBase result = null;

            // We need to load the TypeId because it tells us what kind of AnimGroupElement to expect...
            var typeId = utility.ReadU32();
            if (typeId == 0x00080000)
            {
                result = new AnimGroupMeshNodeVis();
            }
            else if(typeId == 0x01000000)
            {
                result = new AnimGroupMesh();
            }
            else if(typeId == 0x02000000)
            {
                result = new AnimGroupTexSampler();
            }
            else if(typeId == 0x04000000)
            {
                result = new AnimGroupBlendOp();
            }
            else if(typeId == 0x08000000)
            {
                result = new AnimGroupMaterialColor();
            }
            else if(typeId == 0x10000000)
            {
                result = new AnimGroupModel();
            }
            else if(typeId == 0x20000000)
            {
                result = new AnimGroupTexMapper();
            }
            else if(typeId == 0x40000000)
            {
                result = new AnimGroupBone();
            }
            else if(typeId == 0x80000000)
            {
                result = new AnimGroupTexCoord();
            }

            if (result == null)
            {
                throw new NotImplementedException($"Unknown AnimGroupElement type {typeId.ToString("X8")}!");
            }

            CGFXDebug.LoadStart(result, utility, hasDynamicType: true);

            // Otherwise, proceed...
            result.TypeId = typeId;
            result.Name = utility.ReadString();
            result.MemberOffset = utility.ReadI32();
            result.BlendOpIndex = utility.ReadI32();
            result.ObjType = (AnimGroupObjType)utility.ReadU32();
            result.MemberType = utility.ReadU32();
            result.MaterialPtr = utility.ReadU32();

            result.LoadInternal(utility);

            return result;
        }

        protected abstract void LoadInternal(Utility utility);

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            utility.Write(TypeId);
            saveContext.StringTable.EnqueueAndWriteTempRel(Name);
            utility.Write(MemberOffset);
            utility.Write(BlendOpIndex);
            utility.Write((uint)ObjType);
            utility.Write(MemberType);
            utility.Write(MaterialPtr);

            SaveInternal(saveContext);
        }

        protected abstract void SaveInternal(SaveContext saveContext);
    }

    public class DICTObjAnimGroupElement : ChunkDICTObject
    {
        public override string Magic => throw new NotImplementedException();    // NOT USED

        public AnimGroupElementBase Content { get; private set; }

        public override void Load(Utility utility)
        {
            // NOTE: This is not actually a shift to another object, this is just 
            // for convenience since the type is dynamic and my system doesn't
            // really support that.
            Content = AnimGroupElementBase.Load(utility);
        }

        public override void Save(SaveContext saveContext)
        {
            // Not actually an indirect to another object, as noted in Load,
            // so we're saving inline
            Content.Save(saveContext);
        }
    }

    public class ChunkDICTAnimGroupElement : ChunkDICT<DICTObjAnimGroupElement>
    {
    }
}
