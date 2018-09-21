using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGFXModel.Chunks.Model.Material
{
    public class FragmentOperationContainer
    {
        ///////// DEPTH /////////
        public uint DepthFlags { get; private set; }
        public uint[] DepthPICACommandsRaw { get; private set; }    // 4


        ///////// BLEND /////////
        public enum BlendModeType : uint
        {
            None,
            Blend,
            BlendSeparate,
            LogicalOp
        }
        public BlendModeType BlendMode { get; private set; }
        public ColorFloat BlendColor { get; private set; }
        public uint[] BlendPICACommandsRaw { get; private set; }    // 6


        ///////// STENCIL /////////
        public uint[] StencilOperationRaw { get; private set; }     // 4


        // Helper properties
        public bool IsTestEnabled
        {
            get { return Utility.CheckBit(DepthFlags, 1); }
            set { DepthFlags = Utility.SetBit(DepthFlags, 1, value); }
        }

        public bool IsMaskEnabled
        {
            get { return Utility.CheckBit(DepthFlags, 2); }
            set { DepthFlags = Utility.SetBit(DepthFlags, 2, value); }
        }

        public static FragmentOperationContainer Load(Utility utility)
        {
            var foc = new FragmentOperationContainer();

            CGFXDebug.LoadStart(foc, utility);

            ///////// DEPTH /////////
            // See also SPICA GfxFragOp / GfxFragOpDepth
            foc.DepthFlags = utility.ReadU32();
            foc.DepthPICACommandsRaw = utility.ReadUInts(4);


            ///////// BLEND /////////
            // See also SPICA GfxFragOp / GfxFragOpBlend
            foc.BlendMode = (BlendModeType)utility.ReadU32();
            foc.BlendColor = ColorFloat.Read(utility);
            foc.BlendPICACommandsRaw = utility.ReadUInts(6);


            ///////// STENCIL /////////
            // See also SPICA GfxFragOp / GfxFragOpStencil
            foc.StencilOperationRaw = utility.ReadUInts(4);

            return foc;
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            ///////// DEPTH /////////
            // See also SPICA GfxFragOp / GfxFragOpDepth
            utility.Write(DepthFlags);
            utility.Write(DepthPICACommandsRaw);


            ///////// BLEND /////////
            // See also SPICA GfxFragOp / GfxFragOpBlend
            utility.Write((uint)BlendMode);
            BlendColor.Save(utility);
            utility.Write(BlendPICACommandsRaw);


            ///////// STENCIL /////////
            // See also SPICA GfxFragOp / GfxFragOpStencil
            utility.Write(StencilOperationRaw);
        }
    }
}
