using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGFXModel.Chunks.Model.Shape
{
    public class BlendShape : ISaveable
    {
        public uint TypeId { get; private set; }
        private uint[] Stubs { get; set; }

        public static BlendShape Load(Utility utility)
        {
            // TODO -- Neither Ohana3DS nor SPICA implemented this other than recognizing
            // it has a list of GfxBlendShapeTargets and a list of GfxVertexBufferTypes.
            // The former of which it currently has no definition for. However, if we
            // get lucky and these just aren't used, then I'm not going to worry about 
            // it right now...

            //public List<GfxBlendShapeTarget> Targets;
            //public List<GfxVertexBufferType> Types;

            var bs = new BlendShape();

            CGFXDebug.LoadStart(bs, utility);

            bs.TypeId = utility.ReadU32();
            if(bs.TypeId != 0x00000000)
            {
                throw new InvalidOperationException($"BlendShape: Unexpected typeId {bs.TypeId}");
            }


            // The 4 UInts here WOULD BE:
            //  Count of Targets
            //  Offset to Targets
            //  Count of Types
            //  Offset to Types
            bs.Stubs = utility.ReadUInts(4);

            // ... but just make sure they're all unused for now
            for(var i = 0; i < 4; i++)
            {
                if(bs.Stubs[i] != 0)
                {
                    throw new NotImplementedException("BlendShape: Trying to fetch data, NOT IMPLEMENTED");
                }
            }

            return bs;
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            utility.Write(TypeId);


            // The 4 UInts here WOULD BE:
            //  Count of Targets
            //  Offset to Targets
            //  Count of Types
            //  Offset to Types
            utility.Write(Stubs);

            // ... but just make sure they're all unused for now
            for (var i = 0; i < 4; i++)
            {
                if (Stubs[i] != 0)
                {
                    throw new NotImplementedException("BlendShape: Trying to fetch data, NOT IMPLEMENTED");
                }
            }
        }
    }
}
