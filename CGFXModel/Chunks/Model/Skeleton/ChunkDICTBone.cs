using CGFXModel.Chunks.MetaData;
using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGFXModel.Chunks.Model.Skeleton
{
    // Keep a dictionary...
    // "RawOffset" -> "Bone"
    // So if we've loaded this bone before, we can just pull it from the Dict
    // TODO -- does "Index" uniquely identify it? 

    public class DICTObjBone : ChunkDICTObject
    {
        public override string Magic => throw new NotImplementedException();    // NOT USED

        [Flags]
        public enum BoneFlags
        {
            IsIdentity = 1 << 0,
            IsTranslationZero = 1 << 1,
            IsRotationZero = 1 << 2,
            IsScaleVolumeOne = 1 << 3,
            IsScaleUniform = 1 << 4,
            IsSegmentScaleCompensate = 1 << 5,
            IsNeededRendering = 1 << 6,
            IsLocalMtxCalculate = 1 << 7,
            IsWorldMtxCalculate = 1 << 8,
            HasSkinningMtx = 1 << 9
        }

        public enum GfxBillboardMode : uint
        {
            Off = 0,
            World = 2,
            WorldViewpoint = 3,
            Screen = 4,
            ScreenViewpoint = 5,
            YAxial = 6,
            YAxialViewpoint = 7,
        }

        public BoneFlags Flags { get; set; }

        public int Index { get; set; }
        public int ParentIndex { get; private set; }    // private since we'll fix it ourselves on save 

        // NOTE !! Relational data... will have to treat this carefully...
        public DICTObjBone Parent { get; set; }
        public DICTObjBone Child { get; set; }
        public DICTObjBone PrevSibling { get; set; }
        public DICTObjBone NextSibling { get; set; }

        // NOTE -- it would be silly and needlessly recursive to try to load bones
        // by constantly reloading them along all paths, so to handle the referential
        // data, we will only record where they are and resolve them in the end...
        public uint OriginalOffset { get; private set; }    // THIS bone's original offset, for lookup purposes later
        private uint ParentOffset, ChildOffset, PrevSiblingOffset, NextSiblingOffset;   // The referential absolute offsets, for post-patching ONLY

        public Vector3 Scale { get; private set; }
        public Vector3 Rotation { get; private set; }
        public Vector3 Translation { get; private set; }

        public Matrix LocalTransform { get; private set; }
        public Matrix WorldTransform { get; private set; }
        public Matrix InvWorldTransform { get; private set; }

        public GfxBillboardMode BillboardMode { get; private set; }

        public ChunkDICTMetaData MetaDatas { get; private set; }

        public override void Load(Utility utility)
        {
            // Used for later resolving ONLY
            OriginalOffset = utility.GetReadPosition();

            // Unlike most objects loaded from a DICT, bones DO NOT use the stndard
            // Type/Magic/Revision/Name/MetaData header and instead do their own thing...
            // (in short, not calling base.Load() here!!)
            Name = utility.ReadString();

            Flags = (BoneFlags)utility.ReadU32();

            Index = utility.ReadI32();
            ParentIndex = utility.ReadI32();

            // NOTE !! Relational data... will have to treat this carefully...
            // We're UNUSUALLY going to store absolute offsets only here (so
            // we don't pointlessly and recursively reload bones several times),
            // and we'll resolve them LATER...
            ParentOffset = utility.ReadOffset();
            ChildOffset = utility.ReadOffset();
            PrevSiblingOffset = utility.ReadOffset();
            NextSiblingOffset = utility.ReadOffset();

            Scale = Vector3.Read(utility);
            Rotation = Vector3.Read(utility);
            Translation = Vector3.Read(utility);

            LocalTransform = Matrix.Read(utility);
            WorldTransform = Matrix.Read(utility);
            InvWorldTransform = Matrix.Read(utility);

            BillboardMode = (GfxBillboardMode)utility.ReadU32();

            MetaDatas = utility.LoadDICTFromOffset<ChunkDICTMetaData>();
        }

        public static DICTObjBone GetBoneFromMap(Dictionary<uint, DICTObjBone> map, uint offset)
        {
            if (offset != 0)
            {
                if (!map.ContainsKey(offset))
                {
                    throw new InvalidOperationException($"DICTObjBone: Could not resolve bone at offset {offset.ToString("X8")}!");
                }

                return map[offset];
            }
            else
            {
                return null;
            }
        }

        public void LoadFixBoneRefs(Dictionary<uint, DICTObjBone> map)
        {
            Parent = GetBoneFromMap(map, ParentOffset);
            Child = GetBoneFromMap(map, ChildOffset);
            PrevSibling = GetBoneFromMap(map, PrevSiblingOffset);
            NextSibling = GetBoneFromMap(map, NextSiblingOffset);
        }

        public override void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            // Update the internal OriginalOffset; we'll need this to resolve the reference bones later
            OriginalOffset = utility.GetWritePosition();

            // Unlike most objects loaded from a DICT, bones DO NOT use the stndard
            // Type/Magic/Revision/Name/MetaData header and instead do their own thing...
            // (in short, not calling base.Save() here!!)
            saveContext.StringTable.EnqueueAndWriteTempRel(Name);

            utility.Write((uint)Flags);

            utility.Write(Index);

            // Fix ParentIndex in case it's wrong (note that Indexes MUST be correct before Save()!)
            ParentIndex = Parent?.Index ?? -1;  // -1 is used if null (root bone)
            utility.Write(ParentIndex);

            saveContext.WritePointerPlaceholder(Parent);
            saveContext.WritePointerPlaceholder(Child);
            saveContext.WritePointerPlaceholder(PrevSibling);
            saveContext.WritePointerPlaceholder(NextSibling);

            Scale.Write(utility);
            Rotation.Write(utility);
            Translation.Write(utility);

            LocalTransform.Write(utility);
            WorldTransform.Write(utility);
            InvWorldTransform.Write(utility);

            utility.Write((uint)BillboardMode);

            saveContext.WriteDICTPointerPlaceholder(MetaDatas);

            /////////////////////////////
            // Begin saving dependent data

            saveContext.SaveAndMarkReference(MetaDatas);

            MetaDatas?.SaveEntries(saveContext);
        }
    }

    public class ChunkDICTBone : ChunkDICT<DICTObjBone>
    {
    }
}
