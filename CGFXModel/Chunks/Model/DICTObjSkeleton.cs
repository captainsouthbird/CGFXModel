using System.Linq;
using CGFXModel.Chunks.Model.Skeleton;
using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model
{
    public class DICTObjSkeleton : ChunkDICTObject
    {
        protected override bool VerifyTypeId(uint typeId)
        {
            return typeId == 0x02000000;
        }
        public override string Magic => "SOBJ";

        public enum SkeletonScalingRule : uint
        {
            Standard,
            Maya,
            SoftImage
        }

        public ChunkDICTBone Bones { get; private set; }

        public DICTObjBone RootBone { get; private set; }

        public SkeletonScalingRule ScalingRule { get; private set; }

        public uint Flags { get; private set; }

        // Helper properties
        public bool IsTranslationAnimEnabled
        {
            get => Utility.CheckBit(Flags, 0x00000002u);
            set => Flags = Utility.SetBit(Flags, 0x00000002u, value);
        }


        public override void Load(Utility utility)
        {
            base.Load(utility);

            Bones = utility.LoadDICTFromOffset<ChunkDICTBone>();

            // Now that all bones have been loaded, we can build a dictionary of the entire 
            // set so we can rationalize the data and make references appropriately...
            var offsetToBoneMap = Bones.Entries.Select(x => x.EntryObject).Cast<DICTObjBone>()
                .Select(x => new
                {
                    Offset = x.OriginalOffset,
                    Bone = x
                })
                .ToDictionary(k => k.Offset, v => v.Bone);

            // Now go back and fix all the bone refs
            foreach(var bone in Bones.Entries)
            {
                (bone.EntryObject as DICTObjBone).LoadFixBoneRefs(offsetToBoneMap);
            }

            // Pull root bone by its offset directly
            var rootBoneOffset = utility.ReadOffset();
            RootBone = DICTObjBone.GetBoneFromMap(offsetToBoneMap, rootBoneOffset);

            ScalingRule = (SkeletonScalingRule)utility.ReadU32();

            Flags = utility.ReadU32();
        }

        public override void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            base.Save(saveContext);

            saveContext.WriteDICTPointerPlaceholder(Bones);

            // Root bone
            saveContext.WritePointerPlaceholder(RootBone);

            utility.Write((uint)ScalingRule);

            utility.Write(Flags);

            /////////////////////////////
            // Begin saving dependent data

            saveContext.SaveAndMarkReference(Bones);

            Bones?.SaveEntries(saveContext);
        }
    }
}
