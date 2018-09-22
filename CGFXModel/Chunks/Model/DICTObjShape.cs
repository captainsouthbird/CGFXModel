using CGFXModel.Chunks.Model.Shape;
using CGFXModel.Chunks.Model.Skeleton;
using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CGFXModel.Chunks.Model
{
    public class DICTObjShape : ChunkDICTObject
    {
        public class GfxBoundingBox : ISaveable
        {
            public Vector3 Center { get; set; }
            public Matrix3x3 Orientation { get; set; }
            public Vector3 Size { get; set; }
            public float Unknown { get; set; }  // Found during discovery of unread bytes, no idea what this is for

            public static GfxBoundingBox Load(Utility utility)
            {
                var bb = new GfxBoundingBox();

                CGFXDebug.LoadStart(bb, utility);

                bb.Center = Vector3.Read(utility);
                bb.Orientation = Matrix3x3.Read(utility);
                bb.Size = Vector3.Read(utility);
                bb.Unknown = utility.ReadFloat();

                return bb;
            }

            public void Save(SaveContext saveContext)
            {
                var utility = saveContext.Utility;

                CGFXDebug.SaveStart(this, saveContext);

                Center.Write(utility);
                Orientation.Write(utility);
                Size.Write(utility);
                utility.Write(Unknown);
            }
        }

        public class GfxSubMesh : ISaveable
        {
            public enum SubMeshSkinning : uint
            {
                None,
                Rigid,
                Smooth
            }

            // Hiding BoneIndices since it'd be better to reference the Bone objects themselves
            private List<uint> BoneIndices { get; set; }

            // NOTE: This requires the Skeleton to have been loaded before it can be built
            public List<DICTObjBone> BoneReferences { get; private set; }

            public SubMeshSkinning Skinning { get; set; }

            public List<Face> Faces { get; private set; }

            public static GfxSubMesh Load(Utility utility)
            {
                var sm = new GfxSubMesh();

                CGFXDebug.LoadStart(sm, utility);

                // Bone Indices
                sm.BoneIndices = utility.LoadIndirectValueList(() => utility.ReadU32())?.ToList();

                // Skinning
                sm.Skinning = (SubMeshSkinning)utility.ReadU32();

                // Faces
                sm.Faces = utility.LoadIndirectObjList(() => Face.Load(utility)).ToList();

                return sm;
            }

            public void GenerateBoneReferences(IEnumerable<DICTObjBone> bones)
            {
                BoneReferences = new List<DICTObjBone>();

                foreach (var boneIndex in BoneIndices)
                {
                    var bone = bones.Where(b => b.Index == boneIndex).SingleOrDefault();
                    if(bone == null)
                    {
                        throw new InvalidOperationException($"GfxSubMesh GenerateBoneReferences: Could not find a bone with index {boneIndex}");
                    }

                    BoneReferences.Add(bone);
                }
            }

            public void Save(SaveContext saveContext)
            {
                var utility = saveContext.Utility;

                CGFXDebug.SaveStart(this, saveContext);

                // Bone Indices
                if (BoneReferences != null)
                {
                    BoneIndices = BoneReferences.Select(b => (uint)b.Index).ToList();   // Rebuild from referenced objects
                }
                saveContext.WriteValueListPointerPlaceholder(BoneIndices);

                // Skinning
                utility.Write((uint)Skinning);

                // Faces
                saveContext.WriteObjectListPointerPlaceholder(Faces);

                /////////////////////////////
                // Begin saving dependent data

                saveContext.SaveAndMarkReference(BoneIndices);
                saveContext.SaveAndMarkReference(Faces);

                Faces.SaveList(saveContext);
            }
        }

        protected override bool VerifyTypeId(uint typeId)
        {
            return typeId == 0x10000001;
        }
        public override string Magic => "SOBJ";

        public uint Flags { get; private set; }
        public GfxBoundingBox BoundingBox;
        public Vector3 PositionOffset;
        public List<GfxSubMesh> SubMeshes;
        public uint BaseAddress { get; private set; }
        public List<VertexBuffer> VertexBuffers { get; private set; }
        public BlendShape BlendShape { get; private set; }

        public override void Load(Utility utility)
        {
            base.Load(utility);

            Flags = utility.ReadU32();

            utility.LoadIndirect(() =>
            {
                BoundingBox = GfxBoundingBox.Load(utility);
            });

            PositionOffset = Vector3.Read(utility);

            SubMeshes = utility.LoadIndirectObjList(() => GfxSubMesh.Load(utility)).ToList();

            BaseAddress = utility.ReadU32();

            VertexBuffers = utility.LoadIndirectObjList(() => VertexBuffer.Load(utility)).ToList();

            utility.LoadIndirect(() =>
            {
                BlendShape = BlendShape.Load(utility);
            });
        }

        public override void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            base.Save(saveContext);

            utility.Write(Flags);

            saveContext.WritePointerPlaceholder(BoundingBox);

            PositionOffset.Write(utility);

            saveContext.WriteObjectListPointerPlaceholder(SubMeshes);

            utility.Write(BaseAddress);

            saveContext.WriteObjectListPointerPlaceholder(VertexBuffers);

            saveContext.WritePointerPlaceholder(BlendShape);

            /////////////////////////////
            // Begin saving dependent data

            saveContext.SaveAndMarkReference(SubMeshes);
            saveContext.SaveAndMarkReference(VertexBuffers);
            saveContext.SaveAndMarkReference(BoundingBox);

            SubMeshes.SaveList(saveContext);
            VertexBuffers.SaveList(saveContext);

            saveContext.SaveAndMarkReference(BlendShape);
        }
    }
}
