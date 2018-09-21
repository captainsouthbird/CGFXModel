using CGFXModel.Chunks.Model.Material;
using CGFXModel.Chunks.Model.Skeleton;
using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGFXModel.Chunks.Model
{
    public class DICTObjModel : DICTObjTransformable
    {
        protected override bool VerifyTypeId(uint typeId)
        {
            return (typeId == 0x40000012) || (typeId == 0x40000092);    // Latter is Skeleton-containing variant
        }
        public override string Magic => "CMDL";

        public enum PICAFaceCulling : uint
        {
            Never,
            FrontFace,
            BackFace
        }

        public DICTObjMesh[] Meshes { get; private set; }
        public ChunkDICTMaterial ModelMaterials { get; private set; }
        public List<DICTObjShape> Shapes { get; private set; }
        public List<DICTObjMeshNodeVisibility> MeshNodeVisibilities { get; private set; }
        public uint Flags { get; private set; }
        public PICAFaceCulling FaceCulling { get; set; }
        public int LayerId { get; set; }

        // The following is used ONLY if it has a Skeleton (typeId == 0x40000092 AKA HasSkeleton = true)
        public DICTObjSkeleton Skeleton { get; private set; }

        // Helper properties
        public bool HasSkeleton
        {
            get { return Utility.CheckBit(TypeId, 0x00000080U); }
        }

        public bool IsVisible
        {
            get { return Utility.CheckBit(Flags, 0x00000001U); }
            set { Flags = Utility.SetBit(Flags, 0x00000001U, value); }
        }

        public bool IsNonUniformScalable
        {
            get { return Utility.CheckBit(Flags, 0x00000002U); }
            set { Flags = Utility.SetBit(Flags, 0x00000002U, value); }
        }

        public override void Load(Utility utility)
        {
            base.Load(utility);

            Meshes = utility.LoadDICTObjList<DICTObjMesh>()?.ToArray();
            foreach(var mesh in Meshes)
            {
                mesh.Model = this;
            }

            // Materials
            ModelMaterials = utility.LoadDICTFromOffset<ChunkDICTMaterial>();

            // Shapes
            Shapes = utility.LoadDICTObjList<DICTObjShape>()?.ToList();

            // Mesh Node Visibilities
            MeshNodeVisibilities = utility.LoadDICTObjList<DICTObjMeshNodeVisibility>()?.ToList();

            Flags = utility.ReadU32();

            FaceCulling = (PICAFaceCulling)utility.ReadU32();

            LayerId = utility.ReadI32();

            // Load a Skeleton if this model has one
            if(HasSkeleton)
            {
                Skeleton = utility.LoadDICTObj<DICTObjSkeleton>();

                // Now we can go back and patch meshes that referenced bones...
                foreach(var shape in Shapes)
                {
                    foreach(var subMesh in shape.SubMeshes)
                    {
                        subMesh.GenerateBoneReferences(Skeleton.Bones.Entries.Select(e => e.EntryObject).Cast<DICTObjBone>());
                    }
                }
            }
        }

        public override void Save(SaveContext saveContext)
        {
            base.Save(saveContext);

            var utility = saveContext.Utility;

            saveContext.WriteObjectListPointerPlaceholder(Meshes);

            // Materials
            saveContext.WriteDICTPointerPlaceholder(ModelMaterials);

            // Shapes
            saveContext.WriteObjectListPointerPlaceholder(Shapes);

            // Mesh Node Visibilities
            saveContext.WriteObjectListPointerPlaceholder(MeshNodeVisibilities);

            utility.Write(Flags);

            utility.Write((uint)FaceCulling);

            utility.Write(LayerId);

            // Load a Skeleton if this model has one
            if (HasSkeleton)
            {
                saveContext.WritePointerPlaceholder(Skeleton);
            }

            /////////////////////////////
            // Begin saving dependent data

            // Save Lists
            saveContext.SaveAndMarkReference(Meshes);
            saveContext.SaveAndMarkReference(Shapes);
            saveContext.SaveAndMarkReference(MeshNodeVisibilities);      // I'm not SURE this is where this goes (no test data at the moment I'm writing this) but all lists seem to follow here

            // Save DICT headers
            saveContext.SaveAndMarkReference(MetaDatas);
            saveContext.SaveAndMarkReference(AnimGroup);
            saveContext.SaveAndMarkReference(ModelMaterials);

            // Now for other interior data...
            MetaDatas?.SaveEntries(saveContext);
            AnimGroup?.SaveEntries(saveContext);

            Meshes.SaveList(saveContext);
            ModelMaterials?.SaveEntries(saveContext);
            Shapes.SaveList(saveContext);
            MeshNodeVisibilities.SaveList(saveContext);

            if(HasSkeleton)
            {
                saveContext.SaveAndMarkReference(Skeleton);
            }
        }
    }

    public class ChunkDICTModel : ChunkDICT<DICTObjModel>
    {
    }
}
