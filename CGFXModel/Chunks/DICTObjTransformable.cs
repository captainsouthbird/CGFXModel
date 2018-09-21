using CGFXModel.Chunks.Model.AnimGroup;
using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGFXModel.Chunks
{
    public abstract class DICTObjTransformable : ChunkDICTObject
    {
        // Ohana didn't make this anything
        public uint Unknown1 { get; private set; }

        public uint BranchVisible { get; private set; }

        // FIXME -- count of what? Should be related to list?
        public uint ChildCount { get; private set; }
        public uint ChildOffset { get; private set; }

        public ChunkDICTAnimGroup AnimGroup { get; private set; }

        public Vector3 TransformScale { get; private set; }
        public Vector3 TransformRotate { get; private set; }
        public Vector3 TransformTranslate { get; private set; }
        public Matrix LocalMatrix { get; private set; }
        public Matrix WorldMatrix { get; private set; }

        public bool IsBranchVisible
        {
            get { return Utility.CheckBit(BranchVisible, 0x00000001U); }
            set { BranchVisible = Utility.SetBit(BranchVisible, 0x00000001U, value); }
        }

        public DICTObjTransformable()
        {
            TransformScale = new Vector3();
            TransformRotate = new Vector3();
            TransformTranslate = new Vector3();
        }

        public override void Load(Utility utility)
        {
            base.Load(utility);

            Unknown1 = utility.ReadU32();
            BranchVisible = utility.ReadU32();

            // FIXME according to SPICA this is an arbitrary list of child "GfxObjects"
            ChildCount = utility.ReadU32(); // ???
            ChildOffset = utility.ReadOffset();   // Offset to pointer list (?) of children I assume
            if (ChildCount != 0 && ChildOffset != 0)
            {
                // PROTIP: GfxObject.cs in SPICA has all the TypeIds and I guess it's possible
                // that this would be a list pointing to any of them, but we can address it
                // one at a time if it comes up (because I certainly haven't implemented all
                // of these...)

                //[TypeChoice(0x01000000, typeof(GfxMesh))]
                //[TypeChoice(0x02000000, typeof(GfxSkeleton))]
                //[TypeChoice(0x04000000, typeof(GfxLUT))]
                //[TypeChoice(0x08000000, typeof(GfxMaterial))]
                //[TypeChoice(0x10000001, typeof(GfxShape))]
                //[TypeChoice(0x20000004, typeof(GfxTextureReference))]
                //[TypeChoice(0x20000009, typeof(GfxTextureCube))]
                //[TypeChoice(0x20000011, typeof(GfxTextureImage))]
                //[TypeChoice(0x4000000a, typeof(GfxCamera))]
                //[TypeChoice(0x40000012, typeof(GfxModel))]
                //[TypeChoice(0x40000092, typeof(GfxModelSkeletal))]
                //[TypeChoice(0x400000a2, typeof(GfxFragmentLight))]
                //[TypeChoice(0x40000122, typeof(GfxHemisphereLight))]
                //[TypeChoice(0x40000222, typeof(GfxVertexLight))]
                //[TypeChoice(0x40000422, typeof(GfxAmbientLight))]
                //[TypeChoice(0x80000001, typeof(GfxShaderReference))]

                throw new NotImplementedException("Child objects not implemented");
            }

            AnimGroup = utility.LoadDICTFromOffset<ChunkDICTAnimGroup>();

            TransformScale = Vector3.Read(utility);
            TransformRotate = Vector3.Read(utility);
            TransformTranslate = Vector3.Read(utility);
            LocalMatrix = Matrix.Read(utility);
            WorldMatrix = Matrix.Read(utility);
        }

        public override void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            base.Save(saveContext);

            utility.Write(Unknown1);
            utility.Write(BranchVisible);

            // FIXME according to SPICA this is an arbitrary list of child "GfxObjects" (AKA ChunkDICTObject in my world)
            // Dunno if it's freeform (unlikely) or has context to specific types (in which case we'll need some clever implementation)
            utility.Write(ChildCount); // ???
            utility.Write(ChildOffset);   // Offset to pointer list (?) of children I assume
            if (ChildCount != 0 && ChildOffset != 0)
            {
                throw new NotImplementedException("Child objects not implemented");
            }

            saveContext.WriteDICTPointerPlaceholder(AnimGroup);

            TransformScale.Write(utility);
            TransformRotate.Write(utility);
            TransformTranslate.Write(utility);
            LocalMatrix.Write(utility);
            WorldMatrix.Write(utility);
        }
    }
}
