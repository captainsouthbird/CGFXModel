using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.Material
{
    public class FragmentShader : ISaveable
    {
        public class FragLightLUT : ISaveable
        {
            public enum PICALUTInput : uint
            {
                CosNormalHalf,
                CosViewHalf,
                CosNormalView,
                CosLightNormal,
                CosLightSpot,
                CosPhi
            }

            public enum PICALUTScale : uint
            {
                One = 0,
                Two = 1,
                Four = 2,
                Eight = 3,
                Quarter = 6,
                Half = 7
            }

            public PICALUTInput Input { get; set; }
            public PICALUTScale Scale { get; set; }

            public LUTReference Sampler { get; set; }

            public static FragLightLUT Load(Utility utility)
            {
                var fll = new FragLightLUT();

                CGFXDebug.LoadStart(fll, utility);

                fll.Input = (PICALUTInput)utility.ReadU32();
                fll.Scale = (PICALUTScale)utility.ReadU32();

                utility.LoadIndirect(() =>
                {
                    fll.Sampler = LUTReference.Load(utility);
                });

                return fll;
            }

            public void Save(SaveContext saveContext)
            {
                var utility = saveContext.Utility;

                CGFXDebug.SaveStart(this, saveContext);

                utility.Write((uint)Input);
                utility.Write((uint)Scale);

                saveContext.WritePointerPlaceholder(Sampler);

                /////////////////////////////
                // Begin saving dependent data

                saveContext.SaveAndMarkReference(Sampler);
            }
        }

        public enum GfxTexEnvConstant : uint
        {
            Constant0,
            Constant1,
            Constant2,
            Constant3,
            Constant4,
            Constant5,
            Emission,
            Ambient,
            Diffuse,
            Specular0,
            Specular1
        }

        public class TexEnv : ISaveable
        {
            public GfxTexEnvConstant Constant { get; set; }
            public uint[] RawCommands { get; private set; }     // Raw PICA200 GPU commands

            public static TexEnv Load(Utility utility)
            {
                var te = new TexEnv();

                CGFXDebug.LoadStart(te, utility);

                te.Constant = (GfxTexEnvConstant)utility.ReadU32();
                te.RawCommands = utility.ReadUInts(6);

                return te;
            }

            public void Save(SaveContext saveContext)
            {
                var utility = saveContext.Utility;

                CGFXDebug.SaveStart(this, saveContext);

                utility.Write((uint)Constant);
                utility.Write(RawCommands);
            }
        }

        ////////
        public enum GfxFresnelSelector : uint
        {
            No,
            Pri,
            Sec,
            PriSec
        }

        public enum GfxBumpMode : uint
        {
            NotUsed,
            AsBump,
            AsTangent
        }

        public ColorFloat TexEnvBufferColorF { get; private set; }

        // Lighting
        public uint FragmentFlags { get; private set; }
        public TranslucencyKind TranslucencyKind { get; set; }

        public GfxFresnelSelector FresnelSelector { get; set; }

        public int BumpTexture { get; set; }    // ??? Should reference something?

        public GfxBumpMode BumpMode { get; set; }

        public bool IsBumpRenormalize { get; set; }


        public FragLightLUT ReflectanceRSampler { get; private set; }
        public FragLightLUT ReflectanceGSampler { get; private set; }
        public FragLightLUT ReflectanceBSampler { get; private set; }
        public FragLightLUT Distribution0Sampler { get; private set; }
        public FragLightLUT Distribution1Sampler { get; private set; }
        public FragLightLUT FresnelSampler { get; private set; }

        public TexEnv[] TextureEnvironments { get; private set; }

        public uint[] AlphaTestRawCommands { get; private set; }    // Raw PICA200 GPU commands

        public uint[] FragmentShaderRawCommands { get; private set; }   // Raw PICA200 GPU commands

        // Helper properties
        public bool IsClampHighLightEnabled
        {
            get { return Utility.CheckBit(FragmentFlags, 0x00000001U); }
            set { FragmentFlags = Utility.SetBit(FragmentFlags, 0x00000001U, value); }
        }

        public bool IsLUTDist0Enabled
        {
            get { return Utility.CheckBit(FragmentFlags, 0x00000002U); }
            set { FragmentFlags = Utility.SetBit(FragmentFlags, 0x00000002U, value); }
        }

        public bool IsLUTDist1Enabled
        {
            get { return Utility.CheckBit(FragmentFlags, 0x00000004U); }
            set { FragmentFlags = Utility.SetBit(FragmentFlags, 0x00000004U, value); }
        }

        public bool IsLUTGeoFactor0Enabled
        {
            get { return Utility.CheckBit(FragmentFlags, 0x00000008U); }
            set { FragmentFlags = Utility.SetBit(FragmentFlags, 0x00000008U, value); }
        }

        public bool IsLUTGeoFactor1Enabled
        {
            get { return Utility.CheckBit(FragmentFlags, 0x00000010U); }
            set { FragmentFlags = Utility.SetBit(FragmentFlags, 0x00000010U, value); }
        }

        public bool IsLUTReflectionEnabled
        {
            get { return Utility.CheckBit(FragmentFlags, 0x00000020U); }
            set { FragmentFlags = Utility.SetBit(FragmentFlags, 0x00000020U, value); }
        }

        private static FragLightLUT GetFragLightLUT(Utility utility)
        {
            FragLightLUT result = null;

            utility.LoadIndirect(() =>
            {
                result = FragLightLUT.Load(utility);
            });

            return result;
        }

        public static FragmentShader Load(Utility utility)
        {
            var fs = new FragmentShader();

            CGFXDebug.LoadStart(fs, utility);

            fs.TexEnvBufferColorF = ColorFloat.Read(utility);

            // Fragment Shader
            fs.FragmentFlags = utility.ReadU32();
            fs.TranslucencyKind = (TranslucencyKind)utility.ReadU32();
            fs.FresnelSelector = (GfxFresnelSelector)utility.ReadU32();
            fs.BumpTexture = utility.ReadI32();
            fs.BumpMode = (GfxBumpMode)utility.ReadU32();
            fs.IsBumpRenormalize = utility.ReadU32() == 1;

            // Fragment Lighting
            CGFXDebug.LoadStart($"HEADER of list of FragLightLUTs", utility);
            utility.LoadIndirect(() =>
            {
                CGFXDebug.LoadStart($"List of FragLightLUTs", utility);
                fs.ReflectanceRSampler = GetFragLightLUT(utility);
                fs.ReflectanceGSampler = GetFragLightLUT(utility);
                fs.ReflectanceBSampler = GetFragLightLUT(utility);
                fs.Distribution0Sampler = GetFragLightLUT(utility);
                fs.Distribution1Sampler = GetFragLightLUT(utility);
                fs.FresnelSampler = GetFragLightLUT(utility);
            });

            fs.TextureEnvironments = new TexEnv[6];
            for(var i = 0; i < fs.TextureEnvironments.Length; i++)
            {
                fs.TextureEnvironments[i] = TexEnv.Load(utility);
            }

            fs.AlphaTestRawCommands = utility.ReadUInts(2);

            fs.FragmentShaderRawCommands = utility.ReadUInts(6);

            return fs;
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            TexEnvBufferColorF.Save(utility);

            // Fragment Shader
            utility.Write(FragmentFlags);
            utility.Write((uint)TranslucencyKind);
            utility.Write((uint)FresnelSelector);
            utility.Write((uint)BumpTexture);
            utility.Write((uint)BumpMode);
            utility.Write(IsBumpRenormalize ? 1u : 0u);

            // Fragment Lighting
            var pointerTableForFragLightLUTs = new[]
            {
                ReflectanceRSampler,
                ReflectanceGSampler,
                ReflectanceBSampler,
                Distribution0Sampler,
                Distribution1Sampler,
                FresnelSampler
            };

            saveContext.WritePointerPlaceholder(pointerTableForFragLightLUTs);

            for (var i = 0; i < TextureEnvironments.Length; i++)
            {
                TextureEnvironments[i].Save(saveContext);
            }

            utility.Write(AlphaTestRawCommands);

            utility.Write(FragmentShaderRawCommands);

            /////////////////////////////
            // Begin saving dependent data

            saveContext.SaveAndMarkReference(pointerTableForFragLightLUTs);

            pointerTableForFragLightLUTs.SaveList(saveContext);
        }
    }
}
