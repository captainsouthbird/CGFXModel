using CGFXModel.Utilities;
using System;

namespace CGFXModel.Chunks.Model.Material
{
    public class DICTObjModelMaterial : ChunkDICTObject
    {
        protected override bool VerifyTypeId(uint typeId)
        {
            return typeId == 0x08000000;
        }
        public override string Magic => "MTOB";

        public uint MaterialFlags { get; private set; }

        public enum TextCoordConfig     // TODO -- meaning?? (I think it's vertex counts or something?)
        {
            Config0120,
            Config0110,
            Config0111,
            Config0112,
            Config0121,
            Config0122
        }
        public TextCoordConfig TextureCoordinatesConfig { get; private set; }
        public TranslucencyKind TranslucencyKind { get; private set; }

        public MaterialColorContainer MaterialColor { get; private set; }
        public RasterizationContainer Rasterization { get; private set; }
        public FragmentOperationContainer FragmentOperation { get; private set; }
        
        public uint UsedTextureCoordinates { get; private set; }    // FIXME -- what's this? (Ohana3DS name)
        public TextureCoord[] TextureCoords { get; private set; }
        public TextureMapper[] TextureMappers { get; private set; }

        public ShaderReference ShaderReference { get; private set; }
        public FragmentShader FragmentShader { get; private set; }

        public int ShaderProgramDescIndex { get; set; } // Reference???

        // NOT SUPPORTED
        public uint ShaderParametersCount { get; private set; }
        public uint ShaderParametersPointerTableOffset { get; private set; }


        public int LightSetIndex { get; set; }  // Reference??
        public int FogIndex { get; set; }   // Reference??

        private uint MaterialFlagsHash { get; set; }
        private uint ShaderParamsHash { get; set; }
        private uint TextureCoordsHash { get; set; }
        private uint TextureSamplersHash { get; set; }
        private uint TextureMappersHash { get; set; }
        private uint MaterialColorsHash { get; set; }
        private uint RasterizationHash { get; set; }
        private uint FragLightHash { get; set; }
        private uint FragLightLUTHash { get; set; }
        private uint FragLightLUTSampHash { get; set; }
        private uint TextureEnvironmentHash { get; set; }
        private uint AlphaTestHash { get; set; }
        private uint FragOpHash { get; set; }
        private uint UniqueId { get; set; }

        // Helper properties
        public bool IsFragmentLightEnabled
        {
            get { return Utility.CheckBit(MaterialFlags, 1); }
            set { MaterialFlags = Utility.SetBit(MaterialFlags, 1, value); }
        }

        public bool IsVertexLightEnabled
        {
            get { return Utility.CheckBit(MaterialFlags, 2); }
            set { MaterialFlags = Utility.SetBit(MaterialFlags, 2, value); }
        }

        public bool IsHemiSphereLightEnabled
        {
            get { return Utility.CheckBit(MaterialFlags, 4); }
            set { MaterialFlags = Utility.SetBit(MaterialFlags, 4, value); }
        }

        public bool IsHemiSphereOcclusionEnabled
        {
            get { return Utility.CheckBit(MaterialFlags, 8); }
            set { MaterialFlags = Utility.SetBit(MaterialFlags, 8, value); }
        }

        public bool IsFogEnabled
        {
            get { return Utility.CheckBit(MaterialFlags, 0x10); }
            set { MaterialFlags = Utility.SetBit(MaterialFlags, 0x10, value); }
        }

        // NOTE: Ohana3DS tied this to the same flag used by rasterization data...
        // Are they supposed to be the same? Should they match??
        public bool IsPolygonOffsetEnabled
        {
            get { return Utility.CheckBit(MaterialFlags, 0x20); }
            set { MaterialFlags = Utility.SetBit(MaterialFlags, 0x20, value); }
        }


        public override void Load(Utility utility)
        {
            base.Load(utility);

            MaterialFlags = utility.ReadU32();

            TextureCoordinatesConfig = (TextCoordConfig)utility.ReadU32();
            TranslucencyKind = (TranslucencyKind)utility.ReadU32();

            MaterialColor = MaterialColorContainer.Load(utility);
            Rasterization = RasterizationContainer.Load(utility);
            FragmentOperation = FragmentOperationContainer.Load(utility);

            // Texture coordinates
            UsedTextureCoordinates = utility.ReadU32();     // ???
            TextureCoords = new TextureCoord[3];

            for (var i = 0; i < TextureCoords.Length; i++)
            {
                var textureCoord = new TextureCoord
                {
                    SourceCoordIndex = utility.ReadI32(),
                    MappingType = (TextureCoord.TextureMappingType)utility.ReadU32(),
                    ReferenceCameraIndex = utility.ReadI32(),
                    TransformType = (TextureCoord.TextureTransformType)utility.ReadU32(),
                    Scale = Vector2.Read(utility),
                    Rotation = utility.ReadFloat(),
                    Translation = Vector2.Read(utility),
                    Flags = utility.ReadU32(),
                    Transform = Matrix.Read(utility),
                };

                TextureCoords[i] = textureCoord;
            }

            // Texture mappers
            TextureMappers = new TextureMapper[4];
            for (var i = 0; i < TextureMappers.Length; i++)
            {
                utility.LoadIndirect(() =>
                {
                    if (i < 3)
                    {
                        TextureMappers[i] = TextureMapper.Load(utility);
                    }
                    else
                    {
                        // FIXME -- "Procedural texture mapper" ???
                        // Not implemented at all in Ohana3DS
                        // According to SPICA, this is what the fourth slot points to, however
                        throw new NotImplementedException("Procedural texture mapper not implemented");
                    }
                });
            }


            ShaderReference = utility.LoadDICTObj<ShaderReference>();

            utility.LoadIndirect(() =>
            {
                FragmentShader = FragmentShader.Load(utility);
            });

            ShaderProgramDescIndex = utility.ReadI32();

            // NOT SUPPORTED
            ShaderParametersCount = utility.ReadU32();
            ShaderParametersPointerTableOffset = utility.ReadOffset();
            if(ShaderParametersCount != 0 || ShaderParametersPointerTableOffset != 0)
            {
                throw new NotImplementedException($"ModelMaterial Load: Shader Parameters UNSUPPORTED");
            }


            LightSetIndex = utility.ReadI32();  // Reference??
            FogIndex = utility.ReadI32();   // Reference??

            // NOTE -- See SPICA GfxMaterial.cs for computations involving these hash functions.
            // I ASSUME if I never change any values in the material these never need recomputed.
            // Let's try to get by without needing this support right now...
            MaterialFlagsHash = utility.ReadU32();
            ShaderParamsHash = utility.ReadU32();
            TextureCoordsHash = utility.ReadU32();
            TextureSamplersHash = utility.ReadU32();
            TextureMappersHash = utility.ReadU32();
            MaterialColorsHash = utility.ReadU32();
            RasterizationHash = utility.ReadU32();
            FragLightHash = utility.ReadU32();
            FragLightLUTHash = utility.ReadU32();
            FragLightLUTSampHash = utility.ReadU32();
            TextureEnvironmentHash = utility.ReadU32();
            AlphaTestHash = utility.ReadU32();
            FragOpHash = utility.ReadU32();
            UniqueId = utility.ReadU32();
        }


        public override void Save(SaveContext saveContext)
        {
            base.Save(saveContext);

            var utility = saveContext.Utility;

            utility.Write(MaterialFlags);

            utility.Write((uint)TextureCoordinatesConfig);
            utility.Write((uint)TranslucencyKind);

            // NOTE: These are inline, not pointered to
            MaterialColor.Save(saveContext);
            Rasterization.Save(saveContext);
            FragmentOperation.Save(saveContext);

            // Texture coordinates
            utility.Write(UsedTextureCoordinates);

            for (var i = 0; i < TextureCoords.Length; i++)
            {
                var tc = TextureCoords[i];

                utility.Write(tc.SourceCoordIndex);
                utility.Write((uint)tc.MappingType);
                utility.Write(tc.ReferenceCameraIndex);
                utility.Write((uint)tc.TransformType);
                tc.Scale.Write(utility);
                utility.Write(tc.Rotation);
                tc.Translation.Write(utility);
                utility.Write(tc.Flags);
                tc.Transform.Write(utility);
            }

            // Texture mappers
            for (var i = 0; i < TextureMappers.Length; i++)
            {
                saveContext.WritePointerPlaceholder(TextureMappers[i]);
            }

            saveContext.WritePointerPlaceholder(ShaderReference);

            saveContext.WritePointerPlaceholder(FragmentShader);

            utility.Write(ShaderProgramDescIndex);

            // NOT SUPPORTED
            utility.Write(ShaderParametersCount);
            utility.Write(ShaderParametersPointerTableOffset);
            if (ShaderParametersCount != 0 || ShaderParametersPointerTableOffset != 0)
            {
                throw new NotImplementedException($"ModelMaterial Save: Shader Parameters UNSUPPORTED");
            }


            utility.Write(LightSetIndex);  // Reference??
            utility.Write(FogIndex);   // Reference??

            // NOTE -- See SPICA GfxMaterial.cs for computations involving these hash functions.
            // I ASSUME if I never change any values in the material these never need recomputed.
            // Let's try to get by without needing this support right now...
            utility.Write(MaterialFlagsHash);
            utility.Write(ShaderParamsHash);
            utility.Write(TextureCoordsHash);
            utility.Write(TextureSamplersHash);
            utility.Write(TextureMappersHash);
            utility.Write(MaterialColorsHash);
            utility.Write(RasterizationHash);
            utility.Write(FragLightHash);
            utility.Write(FragLightLUTHash);
            utility.Write(FragLightLUTSampHash);
            utility.Write(TextureEnvironmentHash);
            utility.Write(AlphaTestHash);
            utility.Write(FragOpHash);
            utility.Write(UniqueId);

            /////////////////////////////
            // Begin saving dependent data

            TextureMappers.SaveList(saveContext);
            saveContext.SaveAndMarkReference(ShaderReference);
            saveContext.SaveAndMarkReference(FragmentShader);
        }
    }

    public class ChunkDICTMaterial : ChunkDICT<DICTObjModelMaterial>
    {
    }
}
