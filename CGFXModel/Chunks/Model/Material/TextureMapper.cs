using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.Material
{
    public class TextureMapper : ISaveable
    {
        public uint TypeId { get; private set; }
        public uint DynamicAllocator { get; private set; }

        public TextureReference TextureReference { get; private set; }


        // NOTE: The following are all intertwined; changes to TextureSampler  values
        // actually require regeneration of Commands (see SPICA's GfxTextureMapper);
        // right now not supporting any of this...
        // To support this I need to import SPICA's PICACommandWriter...
        private TextureSampler TextureSampler { get; set; }
        private uint[] Commands { get; set; }
        private uint CommandsLength { get; set; }

        public static TextureMapper Load(Utility utility)
        {
            var tm = new TextureMapper();

            CGFXDebug.LoadStart(tm, utility);

            tm.TypeId = utility.ReadU32();
            if(tm.TypeId != 0x80000000)
            {
                throw new InvalidOperationException($"TextureMapper: Expected type 0x80000000, got {tm.TypeId}");
            }

            tm.DynamicAllocator = utility.ReadU32();

            // Texture Reference
            tm.TextureReference = utility.LoadDICTObj<TextureReference>();

            // Sampler
            utility.LoadIndirect(() =>
            {
                tm.TextureSampler = TextureSampler.Load(tm, utility);
            });

            tm.Commands = utility.ReadUInts(14);
            tm.CommandsLength = utility.ReadU32();  // Seems to be length of the aforementioned "Commands"?

            // I think this is a fair sanity check??
            if(tm.CommandsLength != (14 * 4))
            {
                throw new InvalidOperationException("CommandsLength mismatch");
            }

            return tm;
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            utility.Write(TypeId);

            utility.Write(DynamicAllocator);

            // Texture Reference
            saveContext.WritePointerPlaceholder(TextureReference);

            // Sampler
            saveContext.WritePointerPlaceholder(TextureSampler);


            utility.Write(Commands);
            utility.Write(CommandsLength);  // Seems to be length of the aforementioned "Commands"?

            /////////////////////////////
            // Begin saving dependent data

            saveContext.SaveAndMarkReference(TextureReference);
            saveContext.SaveAndMarkReference(TextureSampler);
        }
    }
}
