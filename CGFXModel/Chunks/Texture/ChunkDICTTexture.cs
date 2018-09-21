using CGFXModel.Utilities;
using System;

namespace CGFXModel.Chunks.Texture
{
    public class DICTObjTexture : ChunkDICTObject
    {
        public enum Format
        {
            RGBA8 = 0,
            RGB8 = 1,
            RGBA5551 = 2,
            RGB565 = 3,
            RGBA4 = 4,
            LA8 = 5,
            HILO8 = 6,
            L8 = 7,
            A8 = 8,
            LA4 = 9,
            L4 = 10,
            A4 = 11,
            ETC1 = 12,
            ETC1A4 = 13,
        }

        protected override bool VerifyTypeId(uint typeId)
        {
            return typeId == 0x20000011;    // NOTE -- This is fixed as a "Texture Image" for now, SPICA also defines a 0x20000009 "Texture Cube"
        }
        public override string Magic => "TXOB";

        public uint Height { get; private set; }
        public uint Width { get; private set; }

        // These names came from Ohana3DS, not sure if true
        public uint OpenGLFormat { get; private set; }
        public uint OpenGLType { get; private set; }

        // CHECKME TODO -- Does > 1 mean that multiple texture data will be found??
        public uint MipmapLevels { get; private set; }

        // More names via Ohana3DS, not sure what these are
        // Hint from SPICA, may be game-runtime-only vars
        public uint TextureObject { get; private set; }
        public uint LocationFlags { get; private set; }

        public Format TextureFormat { get; private set; }  // 4B specifier

        // --- FIXME "TextureCube" TypeId 0x20000009 has different data here, see SPICA GfxObject -> GfxTextureCube

        // These didn't even have names in Ohana3DS! (It just reads over them)
        // TODO -- let's run a test where we dump a bunch of textures and look for correlations!!
        // I suspect these will match EXCEPT for ETC1 and ETC1A4 textures...
        public uint Unknown1 { get; private set; }  // Seems to be related to BPP, sometimes equal, sometimes half (maybe to do with ETC1/ETC1A4?)
        public uint Unknown2 { get; private set; }  // Appears to be Height again (maybe compressed height? I think ETC1 decompresses to double height?)
        public uint Unknown3 { get; private set; }  // Appears to be Width again (maybe compressed width? I think ETC1 decompresses to double width?)

        // NOTE: Kept in place of texture length/offset values, this is the texture data
        // in native CGFX format. Can be decoded if desired using TextureCodec class.
        public byte[] TextureCGFXData { get; private set; }

        // NOTE: The following names are from Ohana3DS, not sure if any of
        // them are important. 
        // Hint from SPICA, may be game-runtime-only vars (except BitsPerPixel)
        public uint DynamicAllocator { get; private set; }
        public uint BitsPerPixel { get; private set; }
        public uint LocationAddress { get; private set; }
        public uint MemoryAddress { get; private set; }

        public override void Load(Utility utility)
        {
            base.Load(utility);

            Height = utility.ReadU32();
            Width = utility.ReadU32();

            // These are Ohana3DS names, I don't know if this is really what these are??
            // (And even if they are, not sure how they map to OpenGL exactly)
            OpenGLFormat = utility.ReadU32();
            OpenGLType = utility.ReadU32();

            MipmapLevels = utility.ReadU32();

            // NOTE: Unsure of the exact implication of multiple mipmap levels in terms of data.
            // It seems like this wasn't properly supported by "old" Ohana3DS, so I'd like to know...
            if(MipmapLevels > 1)
            {
                throw new NotImplementedException($"ChunkDICTTexture Load: More than 1 mipmap level ({MipmapLevels}); need to investigate if that means more texture data or what...");
            }

            // Once again, Ohana3DS names, not sure what they are or if I should support them
            TextureObject = utility.ReadU32();
            LocationFlags = utility.ReadU32();

            if(TextureObject != 0)
            {
                // Note to self, if I get here... this may be non-fatal, I'm more worried if it's pointing to data
                throw new NotImplementedException();
            }

            TextureFormat = (Format)utility.ReadU32();

            // Not even Ohana3DS tried to address these!
            Unknown1 = utility.ReadU32();
            Unknown2 = utility.ReadU32();
            Unknown3 = utility.ReadU32();

            // Texture will be stored as raw blob rather than as length/offset values
            var dataLength = utility.ReadU32();
            var dataOffset = utility.ReadOffset();

            CGFXDebug.WriteLog($"NOTE: TextureCGFXData of format {TextureFormat} starts at {dataOffset.ToString("X4")}");
            utility.PushReadPosition();
            utility.SetReadPosition(dataOffset);
            TextureCGFXData = utility.ReadBytes(dataLength);
            utility.PopReadPosition();

            DynamicAllocator = utility.ReadU32();
            BitsPerPixel = utility.ReadU32();
            LocationAddress = utility.ReadU32();
            MemoryAddress = utility.ReadU32();

            //var testRGBA = TextureCodec.ConvertTextureToRGBA(utility, TextureCGFXData, TextureFormat, Width, Height);
            //var textCGFX = TextureCodec.ConvertTextureToCGFX(utility, testRGBA, TextureFormat, Width, Height);
        }

        // Converts the CGFX texture data to RGBA for exporting
        // NOTE: "utility" is just used for endianness, it's not reading/writing a CGFX file
        public byte[] GetTextureRGBA(Utility utility)
        {
            return TextureCodec.ConvertTextureToRGBA(utility, TextureCGFXData, TextureFormat, (int)Width, (int)Height);
        }

        // Imports the RGBA texture data converting it to CGFX and storing it.
        // NOTE: "utility" is just used for endianness, it's not reading/writing a CGFX file
        public void SetTexture(Utility utility, byte[] data, bool safetyCheck = true, uint? width = null, uint? height = null, Format? format = null)
        {
            // TODO: I wonder how feasible it is to use a different size or format...
            // (e.g., in particular, "upgrading" a grayscale texture to full color ...)
            // Unknown whether that might cause a game to crash if it's unexpected or too large.

            if(safetyCheck && 
                (
                    (width.HasValue && width.Value != Width) ||
                    (height.HasValue && height.Value != Height) ||
                    (format.HasValue && format != TextureFormat)
                )
            )
            {
                throw new InvalidOperationException("ChunkDICTTexture SetTexture: The texture you are trying to import does not match the original width/height/format; if you're SURE about this, set safetyCheck = false");
            }


            // NOTE!! Actually changing the texture format is untested and may cause problems / crashes...
            if(width.HasValue)
            {
                // WARNING: This is a GUESS
                if (Width == Unknown3)
                {
                    Unknown3 = Width;
                }
                else
                {
                    throw new NotImplementedException($"ChunkDICTTexture SetTexture: Assumption that 'Unknown3' relates to Width is WRONG (Width={Width}, Unknown3={Unknown3})");
                }

                Width = width.Value;
            }

            if (height.HasValue)
            {
                // WARNING: This is a GUESS
                if(Height == Unknown2)
                {
                    Unknown2 = Height;
                }
                else
                {
                    throw new NotImplementedException($"ChunkDICTTexture SetTexture: Assumption that 'Unknown2' relates to Height is WRONG (Height={Height}, Unknown2={Unknown2})");
                }

                Height = height.Value;
            }

            if (format.HasValue)
            {
                TextureFormat = format.Value;
            }

            // TODO -- BPP??!! (and Unknown1?)

            // TODO -- changing format doesn't update BitsPerPixel yet, not sure how critical, will need generic way to get it
            // ... really need to figure out how it correlates to the texture format ...
            // SEE ALSO: Unknown1, which appears to correlate to BPP "somehow" (but not necessarily identical)

            var newCGFXData = TextureCodec.ConvertTextureToCGFX(utility, data, TextureFormat, (int)Width, (int)Height);

            if(safetyCheck && newCGFXData.Length != TextureCGFXData.Length)
            {
                throw new InvalidOperationException("ChunkDICTTexture SetTexture: The texture data came back in a different raw data size than the original!");
            }

            TextureCGFXData = newCGFXData;
        }

        public override void Save(SaveContext saveContext)
        {
            base.Save(saveContext);

            var utility = saveContext.Utility;

            utility.Write(Height);
            utility.Write(Width);

            // These are Ohana3DS names, I don't know if this is really what these are??
            // (And even if they are, not sure how they map to OpenGL exactly)
            utility.Write(OpenGLFormat);
            utility.Write(OpenGLType);

            utility.Write(MipmapLevels);

            // NOTE: Unsure of the exact implication of multiple mipmap levels in terms of data.
            // It seems like this wasn't properly supported by "old" Ohana3DS, so I'd like to know...
            if (MipmapLevels > 1)
            {
                throw new NotImplementedException($"ChunkDICTTexture Save: More than 1 mipmap level ({MipmapLevels}); need to investigate if that means more texture data or what...");
            }

            // Once again, Ohana3DS names, not sure what they are or if I should support them
            utility.Write(TextureObject);
            utility.Write(LocationFlags);

            if (TextureObject != 0)
            {
                // Note to self, if I get here... this may be non-fatal, I'm more worried if it's pointing to data
                throw new NotImplementedException();
            }

            utility.Write((uint)TextureFormat);

            // Not even Ohana3DS tried to address these!
            // I wonder if these are specifications to the texture before it's "decoded"?
            // NOTE: It LOOKS like they might be:
            //  Unknown1 - Seems related to BPP; sometimes it matches, sometimes it doesn't, e.g. ETC1A4 texture has BPP=8 and Unknown1=4; maybe an alpha depth or compressed depth or something?
            //  Unknown2/3 - Seem to generally be equal to Height and Width, respectively; maybe a "compressed width/height" or something?
            utility.Write(Unknown1);
            utility.Write(Unknown2);
            utility.Write(Unknown3);

            // Texture stores length as well as the usual self-relative offset here...
            utility.Write((uint)TextureCGFXData.Length);
            saveContext.IMAGData.EnqueueAndWriteTempRel(TextureCGFXData, 128);

            utility.Write(DynamicAllocator);
            utility.Write(BitsPerPixel);
            utility.Write(LocationAddress);
            utility.Write(MemoryAddress);
        }
    }

    public class ChunkDICTTexture : ChunkDICT<DICTObjTexture>
    {
    }
}
