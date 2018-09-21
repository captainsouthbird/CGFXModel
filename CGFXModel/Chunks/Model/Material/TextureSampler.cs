using CGFXModel.Utilities;
using System;

namespace CGFXModel.Chunks.Model.Material
{
    public class TextureSampler : ISaveable
    {
        public enum TextureMinFilter
        {
            Nearest,
            Linear,
            NearestMipmapNearest,
            NearestMipmapLinear,
            LinearMipmapNearest,
            LinearMipmapLinear
        }

        public TextureMapper Parent { get; private set; }   // Needed to offset back to the parent object 

        public uint TypeId { get; private set; }
        public TextureMinFilter MinFilter { get; set; }
        public ColorFloat BorderColor { get; set; }
        public float LODBias { get; set; }

        public static TextureSampler Load(TextureMapper parent, Utility utility)
        {
            var s = new TextureSampler();

            CGFXDebug.LoadStart(s, utility);

            // This has a TypeId here...
            s.TypeId = utility.ReadU32();
            if(s.TypeId != 0x80000000)
            {
                throw new InvalidOperationException($"TextureSampler: Expected type 0x80000000, got {s.TypeId}");
            }

            // Just reading the offset but we'll resolve this later
            var ownerModelOffset = utility.ReadOffset();
            s.Parent = parent;

            s.MinFilter = (TextureMinFilter)utility.ReadU32();

            s.BorderColor = ColorFloat.Read(utility);
            s.LODBias = utility.ReadFloat();
            
            return s;
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            utility.Write(TypeId);

            saveContext.WritePointerPlaceholder(Parent);

            utility.Write((uint)MinFilter);

            BorderColor.Save(utility);
            utility.Write(LODBias);
        }
    }
}
