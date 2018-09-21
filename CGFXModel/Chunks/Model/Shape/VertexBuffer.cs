using CGFXModel.Utilities;
using System;

namespace CGFXModel.Chunks.Model.Shape
{
    public abstract class VertexBuffer : ISaveable
    {
        protected abstract uint ExpectedTypeId { get; }

        public enum PICAAttributeName : uint
        {
            Position,
            Normal,
            Tangent,
            Color,
            TexCoord0,
            TexCoord1,
            TexCoord2,
            BoneIndex,
            BoneWeight,
            UserAttribute0,
            UserAttribute1,
            UserAttribute2,
            UserAttribute3,
            UserAttribute4,
            UserAttribute5,
            UserAttribute6,
            UserAttribute7,
            UserAttribute8,
            UserAttribute9,
            UserAttribute10,
            UserAttribute11,
            Interleave
        }

        public enum GfxVertexBufferType : uint
        {
            None,
            Fixed,
            Interleaved
        }

        public uint TypeId { get; set; }
        public PICAAttributeName AttrName { get; set; }
        public GfxVertexBufferType Type { get; set; }

        protected abstract void LoadInternal(Utility utility);

        public static VertexBuffer Load(Utility utility)
        {
            VertexBuffer result = null;

            // This one's a little unusual because we actually instantiate different 
            // resultant objects by the TypeId we read...
            var typeId = utility.ReadU32();

            if(typeId == 0x40000001)
            {
                result = new VertexAttribute();
            }
            else if(typeId == 0x40000002)
            {
                result = new VertexBufferInterleaved();
            }
            else if(typeId == 0x80000000)
            {
                result = new VertexBufferFixed();
            }
            else
            {
                throw new NotImplementedException($"VertexBuffer Load: Unsupported TypeId {typeId}");
            }

            CGFXDebug.LoadStart(result, utility, hasDynamicType: true);

            // Common values...
            result.TypeId = typeId;
            result.AttrName = (PICAAttributeName)utility.ReadU32();
            result.Type = (GfxVertexBufferType)utility.ReadU32();

            // Verification that the type matches the TypeId..
            if(result.TypeId != result.ExpectedTypeId)
            {
                throw new InvalidOperationException($"Instantiated type {result.GetType().Name} but it expected TypeId {result.ExpectedTypeId.ToString("X8")} instead of {typeId.ToString("X8")}");
            }

            // If all good, continue the load!
            result.LoadInternal(utility);

            return result;
        }

        // Alternate Load in case you already know which one it should be getting back...
        public static TVertexBuffer Load<TVertexBuffer>(Utility utility)
            where TVertexBuffer : VertexBuffer
        {
            // TODO -- verify type before even bothering with a Load...
            var result = Load(utility);

            if (result.GetType() != typeof(TVertexBuffer))
            {
                throw new InvalidOperationException($"Expected to load a {typeof(TVertexBuffer)} but apparently loaded a {result.GetType()}");
            }

            return (TVertexBuffer)result;
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            // Common values...
            utility.Write(TypeId);
            utility.Write((uint)AttrName);
            utility.Write((uint)Type);

            // If all good, continue the save!
            SaveInternal(saveContext);
        }

        protected abstract void SaveInternal(SaveContext saveContext);
    }
}
