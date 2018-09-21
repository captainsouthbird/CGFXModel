using CGFXModel.Chunks;
using CGFXModel.Chunks.Texture;
using System;

namespace CGFXModel.Utilities
{
    public static class TextureCodec
    {
        // CREDIT: This was taken from the "old" (pre-Rebirth) version of Ohana3DS by gdkchan,
        // converted to C# from its original VB source and stripped of DirectX references.
        // Original source pulled from: https://github.com/dnasdw/Ohana3DS

        // Simplified / adapted for this utility by Southbird

        private static readonly int[] Tile_Order = {
            0,
            1,
            8,
            9,
            2,
            3,
            10,
            11,
            16,
            17,
            24,
            25,
            18,
            19,
            26,
            27,
            4,
            5,
            12,
            13,
            6,
            7,
            14,
            15,
            20,
            21,
            28,
            29,
            22,
            23,
            30,
            31,
            32,
            33,
            40,
            41,
            34,
            35,
            42,
            43,
            48,
            49,
            56,
            57,
            50,
            51,
            58,
            59,
            36,
            37,
            44,
            45,
            38,
            39,
            46,
            47,
            52,
            53,
            60,
            61,
            54,
            55,
            62,
            63
        };
        private static readonly int[,] Modulation_Table = {
            {
                2,
                8,
                -2,
                -8
            },
            {
                5,
                17,
                -5,
                -17
            },
            {
                9,
                29,
                -9,
                -29
            },
            {
                13,
                42,
                -13,
                -42
            },
            {
                18,
                60,
                -18,
                -60
            },
            {
                24,
                80,
                -24,
                -80
            },
            {
                33,
                106,
                -33,
                -106
            },
            {
                47,
                183,
                -47,
                -183
            }
		};

        // Convert CGFX format texture data to RGBA.
        // NOTE: Utility class just passed in for the sake of endianness use,
        // does not try to read from raw CGFX file.
        public static byte[] ConvertTextureToRGBA(Utility utility, byte[] Data, DICTObjTexture.Format Format, int Width, int Height)
        {
            byte[] Out = new byte[(Width * Height * 4)];    // i.e. RGBA data
            int Offset = 0;     // NOTE: Used to be offset "within" the file, but we'll always start at zero
            bool Low_High_Toggle = false;

            //ETC1 (iPACKMAN)
            if (Format == DICTObjTexture.Format.ETC1 | Format == DICTObjTexture.Format.ETC1A4)
            {
                byte[] Temp_Buffer = new byte[((Width * Height) / 2)];
                byte[] Alphas = new byte[Temp_Buffer.Length];
                if (Format == DICTObjTexture.Format.ETC1)
                {
                    Buffer.BlockCopy(Data, Offset, Temp_Buffer, 0, Temp_Buffer.Length);
                    for (int j = 0; j <= Alphas.Length - 1; j++)
                    {
                        Alphas[j] = 0xff;
                    }
                }
                else
                {
                    int k = 0;
                    for (int j = 0; j <= (Width * Height) - 1; j++)
                    {
                        Buffer.BlockCopy(Data, Offset + j + 8, Temp_Buffer, k, 8);
                        Buffer.BlockCopy(Data, Offset + j, Alphas, k, 8);
                        k += 8;
                        j += 15;
                    }
                }
                byte[] Temp_2 = ETC1_Decompress(utility, Temp_Buffer, Alphas, Width, Height);

                //Os tiles com compressão ETC1 no 3DS estão embaralhados
                int[] Tile_Scramble = Get_ETC1_Scramble(Width, Height);

                int i = 0;
                for (int Tile_Y = 0; Tile_Y <= (Height / 4) - 1; Tile_Y++)
                {
                    for (int Tile_X = 0; Tile_X <= (Width / 4) - 1; Tile_X++)
                    {
                        int TX = Tile_Scramble[i] % (Width / 4);
                        int TY = (Tile_Scramble[i] - TX) / (Width / 4);
                        for (int Y = 0; Y <= 3; Y++)
                        {
                            for (int X = 0; X <= 3; X++)
                            {
                                int Out_Offset = ((Tile_X * 4) + X + (((Height - 1) - ((Tile_Y * 4) + Y)) * Width)) * 4;
                                int Image_Offset = ((TX * 4) + X + (((TY * 4) + Y) * Width)) * 4;

                                Out[Out_Offset] = Temp_2[Image_Offset];
                                Out[Out_Offset + 1] = Temp_2[Image_Offset + 1];
                                Out[Out_Offset + 2] = Temp_2[Image_Offset + 2];
                                Out[Out_Offset + 3] = Temp_2[Image_Offset + 3];
                            }
                        }
                        i += 1;
                    }
                }
            }
            else
            {
                for (int Tile_Y = 0; Tile_Y <= (Height / 8) - 1; Tile_Y++)
                {
                    for (int Tile_X = 0; Tile_X <= (Width / 8) - 1; Tile_X++)
                    {
                        for (int i = 0; i <= 63; i++)
                        {
                            int X = Tile_Order[i] % 8;
                            int Y = (Tile_Order[i] - X) / 8;
                            int Out_Offset = ((Tile_X * 8) + X + (((Height - 1) - ((Tile_Y * 8) + Y)) * Width)) * 4;
                            switch (Format)
                            {
                                case DICTObjTexture.Format.RGBA8:
                                    {
                                        //R8G8B8A8
                                        Buffer.BlockCopy(Data, Offset + 1, Out, Out_Offset, 3);
                                        Out[Out_Offset + 3] = Data[Offset];
                                        Offset += 4;
                                        break;
                                    }
                                case DICTObjTexture.Format.RGB8:
                                    {
                                        //R8G8B8 (sem transparência)
                                        Buffer.BlockCopy(Data, Offset, Out, Out_Offset, 3);
                                        Out[Out_Offset + 3] = 0xff;
                                        Offset += 3;
                                        break;
                                    }
                                case DICTObjTexture.Format.RGBA5551:
                                    {
                                        //R5G5B5A1
                                        int Pixel_Data = utility.GetU16(Data, Offset);
                                        Out[Out_Offset + 2] = Convert.ToByte(((Pixel_Data >> 11) & 0x1f) * 8);
                                        Out[Out_Offset + 1] = Convert.ToByte(((Pixel_Data >> 6) & 0x1f) * 8);
                                        Out[Out_Offset] = Convert.ToByte(((Pixel_Data >> 1) & 0x1f) * 8);
                                        Out[Out_Offset + 3] = Convert.ToByte((Pixel_Data & 1) * 0xff);
                                        Offset += 2;
                                        break;
                                    }
                                case DICTObjTexture.Format.RGB565:
                                    {
                                        //R5G6B5
                                        int Pixel_Data = utility.GetU16(Data, Offset);
                                        Out[Out_Offset + 2] = Convert.ToByte(((Pixel_Data >> 11) & 0x1f) * 8);
                                        Out[Out_Offset + 1] = Convert.ToByte(((Pixel_Data >> 5) & 0x3f) * 4);
                                        Out[Out_Offset] = Convert.ToByte(((Pixel_Data) & 0x1f) * 8);
                                        Out[Out_Offset + 3] = 0xff;
                                        Offset += 2;
                                        break;
                                    }
                                case DICTObjTexture.Format.RGBA4:
                                    {
                                        //R4G4B4A4
                                        int Pixel_Data = utility.GetU16(Data, Offset);
                                        Out[Out_Offset + 2] = Convert.ToByte(((Pixel_Data >> 12) & 0xf) * 0x11);
                                        Out[Out_Offset + 1] = Convert.ToByte(((Pixel_Data >> 8) & 0xf) * 0x11);
                                        Out[Out_Offset] = Convert.ToByte(((Pixel_Data >> 4) & 0xf) * 0x11);
                                        Out[Out_Offset + 3] = Convert.ToByte((Pixel_Data & 0xf) * 0x11);
                                        Offset += 2;
                                        break;
                                    }
                                case DICTObjTexture.Format.HILO8:
                                    //HILO8
                                    // FIXME TODO -- Verify this -- "old" Ohana3DS had no code for HILO8,
                                    // "Rebirth" Ohana3DS had it match LA8... is this correct??
                                case DICTObjTexture.Format.LA8:
                                    {
                                        //L8A8
                                        byte Pixel_Data = Data[Offset + 1];
                                        Out[Out_Offset] = Pixel_Data;
                                        Out[Out_Offset + 1] = Pixel_Data;
                                        Out[Out_Offset + 2] = Pixel_Data;
                                        Out[Out_Offset + 3] = Data[Offset];
                                        Offset += 2;
                                        break;
                                    }
                                case DICTObjTexture.Format.L8:
                                    {
                                        //L8
                                        Out[Out_Offset] = Data[Offset];
                                        Out[Out_Offset + 1] = Data[Offset];
                                        Out[Out_Offset + 2] = Data[Offset];
                                        Out[Out_Offset + 3] = 0xff;
                                        Offset += 1;
                                        break;
                                    }
                                case DICTObjTexture.Format.A8:
                                    {
                                        //A8
                                        Out[Out_Offset] = 0xff;
                                        Out[Out_Offset + 1] = 0xff;
                                        Out[Out_Offset + 2] = 0xff;
                                        Out[Out_Offset + 3] = Data[Offset];
                                        Offset += 1;
                                        break;
                                    }
                                case DICTObjTexture.Format.LA4:
                                    {
                                        //L4A4
                                        int Luma = Data[Offset] & 0xf;
                                        int Alpha = (Data[Offset] & 0xf0) >> 4;
                                        Out[Out_Offset] = Convert.ToByte((Luma << 4) + Luma);
                                        Out[Out_Offset + 1] = Convert.ToByte((Luma << 4) + Luma);
                                        Out[Out_Offset + 2] = Convert.ToByte((Luma << 4) + Luma);
                                        Out[Out_Offset + 3] = Convert.ToByte((Alpha << 4) + Alpha);
                                        break;
                                    }
                                case DICTObjTexture.Format.L4:
                                    {
                                        //L4
                                        int Pixel_Data = 0;
                                        if (Low_High_Toggle)
                                        {
                                            Pixel_Data = Data[Offset] & 0xf;
                                            Offset += 1;
                                        }
                                        else
                                        {
                                            Pixel_Data = (Data[Offset] & 0xf0) >> 4;
                                        }
                                        Out[Out_Offset] = Convert.ToByte(Pixel_Data * 0x11);
                                        Out[Out_Offset + 1] = Convert.ToByte(Pixel_Data * 0x11);
                                        Out[Out_Offset + 2] = Convert.ToByte(Pixel_Data * 0x11);
                                        Out[Out_Offset + 3] = 0xff;
                                        Low_High_Toggle = !Low_High_Toggle;
                                        break;
                                    }

                                case DICTObjTexture.Format.A4:
                                    {
                                        // A4
                                        // NOTE: This was missing from "old" Ohana, this is just a guess and not tested,
                                        // based on L4 above!
                                        int Pixel_Data = 0;
                                        if (Low_High_Toggle)
                                        {
                                            Pixel_Data = Data[Offset] & 0xf;
                                            Offset += 1;
                                        }
                                        else
                                        {
                                            Pixel_Data = (Data[Offset] & 0xf0) >> 4;
                                        }
                                        Out[Out_Offset] = 0xff;
                                        Out[Out_Offset + 1] = 0xff;
                                        Out[Out_Offset + 2] = 0xff;
                                        Out[Out_Offset + 3] = Convert.ToByte(Pixel_Data * 0x11);
                                        Low_High_Toggle = !Low_High_Toggle;

                                        break;
                                    }

                                default:
                                    throw new InvalidOperationException("ConvertTextureToRGBA: Unsupported texture format " + Format);
                            }
                        }
                    }
                }
            }

            return Out;
        }

        // Convert raw RGBA texture back into CGFX format.
        // WARNING: May lose color fidelity or color altogether, depending on specific format!
        // NOTE: Utility class just passed in for the sake of endianness use,
        // does not try to read from raw CGFX file.
        public static byte[] ConvertTextureToCGFX(Utility utility, byte[] Data, DICTObjTexture.Format Format, int Width, int Height)
        {
            int BPP = 32;   // NOTE -- "old" Ohana3DS supported importing 24bpp images, in this case we're assuming always 32bpp
            bool Low_High_Toggle = false;

            byte[] Out_Data = null;

            switch (Format)
            {
                case DICTObjTexture.Format.ETC1:
                case DICTObjTexture.Format.ETC1A4:
                    {
                        //Os tiles com compressão ETC1 no 3DS estão embaralhados
                        byte[] Out = new byte[(Width * Height * 4)];
                        int[] Tile_Scramble = Get_ETC1_Scramble(Width, Height);

                        int i = 0;
                        for (int Tile_Y = 0; Tile_Y <= (Height / 4) - 1; Tile_Y++)
                        {
                            for (int Tile_X = 0; Tile_X <= (Width / 4) - 1; Tile_X++)
                            {
                                int TX = Tile_Scramble[i] % (Width / 4);
                                int TY = (Tile_Scramble[i] - TX) / (Width / 4);
                                for (int Y = 0; Y <= 3; Y++)
                                {
                                    for (int X = 0; X <= 3; X++)
                                    {
                                        int Out_Offset = ((TX * 4) + X + ((((TY * 4) + Y)) * Width)) * 4;
                                        int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * (BPP / 8);

                                        Out[Out_Offset] = Data[Image_Offset + 2];
                                        Out[Out_Offset + 1] = Data[Image_Offset + 1];
                                        Out[Out_Offset + 2] = Data[Image_Offset];
                                        if (BPP == 32)
                                            Out[Out_Offset + 3] = Data[Image_Offset + 3];
                                        else
                                            Out[Out_Offset + 3] = 0xff;
                                    }
                                }
                                i += 1;
                            }
                        }


                        Out_Data = new byte[((Width * Height) / (Format == DICTObjTexture.Format.ETC1 ? 2 : 1))];
                        int Out_Data_Offset = 0;

                        for (int Tile_Y = 0; Tile_Y <= (Height / 4) - 1; Tile_Y++)
                        {
                            for (int Tile_X = 0; Tile_X <= (Width / 4) - 1; Tile_X++)
                            {
                                bool Flip = false;
                                bool Difference = false;
                                int Block_Top = 0;
                                int Block_Bottom = 0;

                                //Teste do Difference Bit
                                int Diff_Match_V = 0;
                                int Diff_Match_H = 0;
                                for (int Y = 0; Y <= 3; Y++)
                                {
                                    for (int X = 0; X <= 1; X++)
                                    {
                                        int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                        int Image_Offset_2 = ((Tile_X * 4) + (2 + X) + (((Tile_Y * 4) + Y) * Width)) * 4;

                                        byte Bits_R1 = Convert.ToByte(Out[Image_Offset_1] & 0xf8);
                                        byte Bits_G1 = Convert.ToByte(Out[Image_Offset_1 + 1] & 0xf8);
                                        byte Bits_B1 = Convert.ToByte(Out[Image_Offset_1 + 2] & 0xf8);

                                        byte Bits_R2 = Convert.ToByte(Out[Image_Offset_2] & 0xf8);
                                        byte Bits_G2 = Convert.ToByte(Out[Image_Offset_2 + 1] & 0xf8);
                                        byte Bits_B2 = Convert.ToByte(Out[Image_Offset_2 + 2] & 0xf8);

                                        if ((Bits_R1 == Bits_R2) & (Bits_G1 == Bits_G2) & (Bits_B1 == Bits_B2))
                                            Diff_Match_V += 1;
                                    }
                                }
                                for (int Y = 0; Y <= 1; Y++)
                                {
                                    for (int X = 0; X <= 3; X++)
                                    {
                                        int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                        int Image_Offset_2 = ((Tile_X * 4) + X + (((Tile_Y * 4) + (2 + Y)) * Width)) * 4;

                                        byte Bits_R1 = Convert.ToByte(Out[Image_Offset_1] & 0xf8);
                                        byte Bits_G1 = Convert.ToByte(Out[Image_Offset_1 + 1] & 0xf8);
                                        byte Bits_B1 = Convert.ToByte(Out[Image_Offset_1 + 2] & 0xf8);

                                        byte Bits_R2 = Convert.ToByte(Out[Image_Offset_2] & 0xf8);
                                        byte Bits_G2 = Convert.ToByte(Out[Image_Offset_2 + 1] & 0xf8);
                                        byte Bits_B2 = Convert.ToByte(Out[Image_Offset_2 + 2] & 0xf8);

                                        if ((Bits_R1 == Bits_R2) & (Bits_G1 == Bits_G2) & (Bits_B1 == Bits_B2))
                                            Diff_Match_H += 1;
                                    }
                                }
                                //Difference + Flip
                                if (Diff_Match_H == 8)
                                {
                                    Difference = true;
                                    Flip = true;
                                    //Difference
                                }
                                else if (Diff_Match_V == 8)
                                {
                                    Difference = true;
                                    //Individual
                                }
                                else
                                {
                                    int Test_R1 = 0;
                                    int Test_G1 = 0;
                                    int Test_B1 = 0;
                                    int Test_R2 = 0;
                                    int Test_G2 = 0;
                                    int Test_B2 = 0;
                                    for (int Y = 0; Y <= 1; Y++)
                                    {
                                        for (int X = 0; X <= 1; X++)
                                        {
                                            int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                            int Image_Offset_2 = ((Tile_X * 4) + (2 + X) + (((Tile_Y * 4) + (2 + Y)) * Width)) * 4;

                                            Test_R1 += Out[Image_Offset_1];
                                            Test_G1 += Out[Image_Offset_1 + 1];
                                            Test_B1 += Out[Image_Offset_1 + 2];

                                            Test_R2 += Out[Image_Offset_2];
                                            Test_G2 += Out[Image_Offset_2 + 1];
                                            Test_B2 += Out[Image_Offset_2 + 2];
                                        }
                                    }

                                    Test_R1 /= 8;
                                    Test_G1 /= 8;
                                    Test_B1 /= 8;

                                    Test_R2 /= 8;
                                    Test_G2 /= 8;
                                    Test_B2 /= 8;

                                    int Test_Luma_1 = Convert.ToInt32(0.299f * Test_R1 + 0.587f * Test_G1 + 0.114f * Test_B1);
                                    int Test_Luma_2 = Convert.ToInt32(0.299f * Test_R2 + 0.587f * Test_G2 + 0.114f * Test_B2);
                                    int Test_Flip_Diff = Math.Abs(Test_Luma_1 - Test_Luma_2);
                                    if (Test_Flip_Diff > 48)
                                        Flip = true;
                                }

                                int Avg_R1 = 0;
                                int Avg_G1 = 0;
                                int Avg_B1 = 0;
                                int Avg_R2 = 0;
                                int Avg_G2 = 0;
                                int Avg_B2 = 0;

                                //Primeiro, cálcula a média de cores de cada bloco
                                if (Flip)
                                {
                                    for (int Y = 0; Y <= 1; Y++)
                                    {
                                        for (int X = 0; X <= 3; X++)
                                        {
                                            int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                            int Image_Offset_2 = ((Tile_X * 4) + X + (((Tile_Y * 4) + (2 + Y)) * Width)) * 4;

                                            Avg_R1 += Out[Image_Offset_1];
                                            Avg_G1 += Out[Image_Offset_1 + 1];
                                            Avg_B1 += Out[Image_Offset_1 + 2];

                                            Avg_R2 += Out[Image_Offset_2];
                                            Avg_G2 += Out[Image_Offset_2 + 1];
                                            Avg_B2 += Out[Image_Offset_2 + 2];
                                        }
                                    }
                                }
                                else
                                {
                                    for (int Y = 0; Y <= 3; Y++)
                                    {
                                        for (int X = 0; X <= 1; X++)
                                        {
                                            int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                            int Image_Offset_2 = ((Tile_X * 4) + (2 + X) + (((Tile_Y * 4) + Y) * Width)) * 4;

                                            Avg_R1 += Out[Image_Offset_1];
                                            Avg_G1 += Out[Image_Offset_1 + 1];
                                            Avg_B1 += Out[Image_Offset_1 + 2];

                                            Avg_R2 += Out[Image_Offset_2];
                                            Avg_G2 += Out[Image_Offset_2 + 1];
                                            Avg_B2 += Out[Image_Offset_2 + 2];
                                        }
                                    }
                                }

                                Avg_R1 /= 8;
                                Avg_G1 /= 8;
                                Avg_B1 /= 8;

                                Avg_R2 /= 8;
                                Avg_G2 /= 8;
                                Avg_B2 /= 8;

                                if (Difference)
                                {
                                    //+============+
                                    //| Difference |
                                    //+============+
                                    if ((Avg_R1 & 7) > 3) { Avg_R1 = Clip(Avg_R1 + 8); Avg_R2 = Clip(Avg_R2 + 8); }
                                    if ((Avg_G1 & 7) > 3) { Avg_G1 = Clip(Avg_G1 + 8); Avg_G2 = Clip(Avg_G2 + 8); }
                                    if ((Avg_B1 & 7) > 3) { Avg_B1 = Clip(Avg_B1 + 8); Avg_B2 = Clip(Avg_B2 + 8); }

                                    Block_Top = (Avg_R1 & 0xf8) | (((Avg_R2 - Avg_R1) / 8) & 7);
                                    Block_Top = Block_Top | (((Avg_G1 & 0xf8) << 8) | ((((Avg_G2 - Avg_G1) / 8) & 7) << 8));
                                    Block_Top = Block_Top | (((Avg_B1 & 0xf8) << 16) | ((((Avg_B2 - Avg_B1) / 8) & 7) << 16));

                                    //Vamos ter certeza de que os mesmos valores obtidos pelo descompressor serão usados na comparação (modo Difference)
                                    Avg_R1 = Block_Top & 0xf8;
                                    Avg_G1 = (Block_Top & 0xf800) >> 8;
                                    Avg_B1 = (Block_Top & 0xf80000) >> 16;

                                    int R = Signed_Byte(Convert.ToByte(Avg_R1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 7) << 5)) >> 5);
                                    int G = Signed_Byte(Convert.ToByte(Avg_G1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 0x700) >> 3)) >> 5);
                                    int B = Signed_Byte(Convert.ToByte(Avg_B1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 0x70000) >> 11)) >> 5);

                                    Avg_R2 = R;
                                    Avg_G2 = G;
                                    Avg_B2 = B;

                                    Avg_R1 = Avg_R1 + (Avg_R1 >> 5);
                                    Avg_G1 = Avg_G1 + (Avg_G1 >> 5);
                                    Avg_B1 = Avg_B1 + (Avg_B1 >> 5);

                                    Avg_R2 = (Avg_R2 << 3) + (Avg_R2 >> 2);
                                    Avg_G2 = (Avg_G2 << 3) + (Avg_G2 >> 2);
                                    Avg_B2 = (Avg_B2 << 3) + (Avg_B2 >> 2);
                                }
                                else
                                {
                                    //+============+
                                    //| Individual |
                                    //+============+
                                    if ((Avg_R1 & 0xf) > 7)
                                        Avg_R1 = Clip(Avg_R1 + 0x10);
                                    if ((Avg_G1 & 0xf) > 7)
                                        Avg_G1 = Clip(Avg_G1 + 0x10);
                                    if ((Avg_B1 & 0xf) > 7)
                                        Avg_B1 = Clip(Avg_B1 + 0x10);
                                    if ((Avg_R2 & 0xf) > 7)
                                        Avg_R2 = Clip(Avg_R2 + 0x10);
                                    if ((Avg_G2 & 0xf) > 7)
                                        Avg_G2 = Clip(Avg_G2 + 0x10);
                                    if ((Avg_B2 & 0xf) > 7)
                                        Avg_B2 = Clip(Avg_B2 + 0x10);

                                    Block_Top = ((Avg_R2 & 0xf0) >> 4) | (Avg_R1 & 0xf0);
                                    Block_Top = Block_Top | (((Avg_G2 & 0xf0) << 4) | ((Avg_G1 & 0xf0) << 8));
                                    Block_Top = Block_Top | (((Avg_B2 & 0xf0) << 12) | ((Avg_B1 & 0xf0) << 16));

                                    //Vamos ter certeza de que os mesmos valores obtidos pelo descompressor serão usados na comparação (modo Individual)
                                    Avg_R1 = (Avg_R1 & 0xf0) + ((Avg_R1 & 0xf0) >> 4);
                                    Avg_G1 = (Avg_G1 & 0xf0) + ((Avg_G1 & 0xf0) >> 4);
                                    Avg_B1 = (Avg_B1 & 0xf0) + ((Avg_B1 & 0xf0) >> 4);

                                    Avg_R2 = (Avg_R2 & 0xf0) + ((Avg_R2 & 0xf0) >> 4);
                                    Avg_G2 = (Avg_G2 & 0xf0) + ((Avg_G2 & 0xf0) >> 4);
                                    Avg_B2 = (Avg_B2 & 0xf0) + ((Avg_B2 & 0xf0) >> 4);
                                }

                                if (Flip)
                                    Block_Top = Block_Top | 0x1000000;
                                if (Difference)
                                    Block_Top = Block_Top | 0x2000000;

                                //Seleciona a melhor tabela para ser usada nos blocos
                                int Mod_Table_1 = 0;
                                int[] Min_Diff_1 = new int[8];
                                for (int a = 0; a <= 7; a++)
                                {
                                    Min_Diff_1[a] = 0;
                                }
                                for (int Y = 0; Y <= (Flip ? 1 : 3); Y++)
                                {
                                    for (int X = 0; X <= (Flip ? 3 : 1); X++)
                                    {
                                        int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                        int Luma = Convert.ToInt32(0.299f * Out[Image_Offset] + 0.587f * Out[Image_Offset + 1] + 0.114f * Out[Image_Offset + 2]);

                                        for (int a = 0; a <= 7; a++)
                                        {
                                            int Optimal_Diff = 255 * 4;
                                            for (int b = 0; b <= 3; b++)
                                            {
                                                int CR = Clip(Avg_R1 + Modulation_Table[a, b]);
                                                int CG = Clip(Avg_G1 + Modulation_Table[a, b]);
                                                int CB = Clip(Avg_B1 + Modulation_Table[a, b]);

                                                int Test_Luma = Convert.ToInt32(0.299f * CR + 0.587f * CG + 0.114f * CB);
                                                int Diff = Math.Abs(Luma - Test_Luma);
                                                if (Diff < Optimal_Diff)
                                                    Optimal_Diff = Diff;
                                            }
                                            Min_Diff_1[a] += Optimal_Diff;
                                        }
                                    }
                                }

                                int Temp_1 = 255 * 8;
                                for (int a = 0; a <= 7; a++)
                                {
                                    if (Min_Diff_1[a] < Temp_1)
                                    {
                                        Temp_1 = Min_Diff_1[a];
                                        Mod_Table_1 = a;
                                    }
                                }

                                int Mod_Table_2 = 0;
                                int[] Min_Diff_2 = new int[8];
                                for (int a = 0; a <= 7; a++)
                                {
                                    Min_Diff_2[a] = 0;
                                }
                                for (int Y = Flip ? 2 : 0; Y <= 3; Y++)
                                {
                                    for (int X = Flip ? 0 : 2; X <= 3; X++)
                                    {
                                        int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                        int Luma = Convert.ToInt32(0.299f * Out[Image_Offset] + 0.587f * Out[Image_Offset + 1] + 0.114f * Out[Image_Offset + 2]);

                                        for (int a = 0; a <= 7; a++)
                                        {
                                            int Optimal_Diff = 255 * 4;
                                            for (int b = 0; b <= 3; b++)
                                            {
                                                int CR = Clip(Avg_R2 + Modulation_Table[a, b]);
                                                int CG = Clip(Avg_G2 + Modulation_Table[a, b]);
                                                int CB = Clip(Avg_B2 + Modulation_Table[a, b]);

                                                int Test_Luma = Convert.ToInt32(0.299f * CR + 0.587f * CG + 0.114f * CB);
                                                int Diff = Math.Abs(Luma - Test_Luma);
                                                if (Diff < Optimal_Diff)
                                                    Optimal_Diff = Diff;
                                            }
                                            Min_Diff_2[a] += Optimal_Diff;
                                        }
                                    }
                                }

                                int Temp_2 = 255 * 8;
                                for (int a = 0; a <= 7; a++)
                                {
                                    if (Min_Diff_2[a] < Temp_2)
                                    {
                                        Temp_2 = Min_Diff_2[a];
                                        Mod_Table_2 = a;
                                    }
                                }

                                Block_Top = Block_Top | (Mod_Table_1 << 29);
                                Block_Top = Block_Top | (Mod_Table_2 << 26);

                                //Seleciona o melhor valor da tabela que mais se aproxima com a cor original
                                for (int Y = 0; Y <= (Flip ? 1 : 3); Y++)
                                {
                                    for (int X = 0; X <= (Flip ? 3 : 1); X++)
                                    {
                                        int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                        int Luma = Convert.ToInt32(0.299f * Out[Image_Offset] + 0.587f * Out[Image_Offset + 1] + 0.114f * Out[Image_Offset + 2]);

                                        int Col_Diff = 255;
                                        int Pix_Table_Index = 0;
                                        for (int b = 0; b <= 3; b++)
                                        {
                                            int CR = Clip(Avg_R1 + Modulation_Table[Mod_Table_1, b]);
                                            int CG = Clip(Avg_G1 + Modulation_Table[Mod_Table_1, b]);
                                            int CB = Clip(Avg_B1 + Modulation_Table[Mod_Table_1, b]);

                                            int Test_Luma = Convert.ToInt32(0.299f * CR + 0.587f * CG + 0.114f * CB);
                                            int Diff = Math.Abs(Luma - Test_Luma);
                                            if (Diff < Col_Diff)
                                            {
                                                Col_Diff = Diff;
                                                Pix_Table_Index = b;
                                            }
                                        }

                                        int Index = X * 4 + Y;
                                        if (Index < 8)
                                        {
                                            Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index + 8));
                                            Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 24));
                                        }
                                        else
                                        {
                                            Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index - 8));
                                            Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 8));
                                        }
                                    }
                                }

                                for (int Y = Flip ? 2 : 0; Y <= 3; Y++)
                                {
                                    for (int X = Flip ? 0 : 2; X <= 3; X++)
                                    {
                                        int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                        int Luma = Convert.ToInt32(0.299f * Out[Image_Offset] + 0.587f * Out[Image_Offset + 1] + 0.114f * Out[Image_Offset + 2]);

                                        int Col_Diff = 255;
                                        int Pix_Table_Index = 0;
                                        for (int b = 0; b <= 3; b++)
                                        {
                                            int CR = Clip(Avg_R2 + Modulation_Table[Mod_Table_2, b]);
                                            int CG = Clip(Avg_G2 + Modulation_Table[Mod_Table_2, b]);
                                            int CB = Clip(Avg_B2 + Modulation_Table[Mod_Table_2, b]);

                                            int Test_Luma = Convert.ToInt32(0.299f * CR + 0.587f * CG + 0.114f * CB);
                                            int Diff = Math.Abs(Luma - Test_Luma);
                                            if (Diff < Col_Diff)
                                            {
                                                Col_Diff = Diff;
                                                Pix_Table_Index = b;
                                            }
                                        }

                                        int Index = X * 4 + Y;
                                        if (Index < 8)
                                        {
                                            Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index + 8));
                                            Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 24));
                                        }
                                        else
                                        {
                                            Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index - 8));
                                            Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 8));
                                        }
                                    }
                                }

                                //Copia dados para a saída
                                byte[] Block = new byte[8];
                                Buffer.BlockCopy(BitConverter.GetBytes(Block_Top), 0, Block, 0, 4);
                                Buffer.BlockCopy(BitConverter.GetBytes(Block_Bottom), 0, Block, 4, 4);
                                byte[] New_Block = new byte[8];
                                for (int j = 0; j <= 7; j++)
                                {
                                    New_Block[7 - j] = Block[j];
                                }
                                if (Format == DICTObjTexture.Format.ETC1A4)
                                {
                                    byte[] Alphas = new byte[8];
                                    int Alpha_Offset = 0;
                                    for (int TX = 0; TX <= 3; TX++)
                                    {
                                        for (int TY = 0; TY <= 3; TY += 2)
                                        {
                                            int Img_Offset_1 = (Tile_X * 4 + TX + ((Tile_Y * 4 + TY) * Width)) * 4;
                                            int Img_Offset_2 = (Tile_X * 4 + TX + ((Tile_Y * 4 + TY + 1) * Width)) * 4;

                                            byte Alpha_1 = (byte)(Out[Img_Offset_1 + 3] >> 4);
                                            byte Alpha_2 = (byte)(Out[Img_Offset_2 + 3] >> 4);

                                            Alphas[Alpha_Offset] = (byte)(Alpha_1 | (Alpha_2 << 4));

                                            Alpha_Offset += 1;
                                        }
                                    }

                                    Buffer.BlockCopy(Alphas, 0, Out_Data, Out_Data_Offset, 8);
                                    Buffer.BlockCopy(New_Block, 0, Out_Data, Out_Data_Offset + 8, 8);
                                    Out_Data_Offset += 16;
                                }
                                else if (Format == DICTObjTexture.Format.ETC1)
                                {
                                    Buffer.BlockCopy(New_Block, 0, Out_Data, Out_Data_Offset, 8);
                                    Out_Data_Offset += 8;
                                }
                            }
                        }

                        break;
                    }
                default:
                    {
                        switch (Format)
                        {
                            case DICTObjTexture.Format.RGBA8:
                                Out_Data = new byte[(Width * Height * 4)];
                                break;
                            case DICTObjTexture.Format.RGB8:
                                Out_Data = new byte[(Width * Height * 3)];
                                break;
                            case DICTObjTexture.Format.RGBA5551:
                            case DICTObjTexture.Format.RGB565:
                            case DICTObjTexture.Format.RGBA4:
                            case DICTObjTexture.Format.LA8:
                                Out_Data = new byte[(Width * Height * 2)];
                                break;

                            case DICTObjTexture.Format.HILO8:  // NOTE -- "old" Ohana3DS didn't support HILO8, "Rebirth" Ohana3DS treated it same as L8, not sure if correct!
                            case DICTObjTexture.Format.L8:
                            case DICTObjTexture.Format.A8:
                            case DICTObjTexture.Format.LA4:
                                Out_Data = new byte[(Width * Height)];
                                break;

                            // SB NOTE: These were missing from "old" Ohana3DS, not tested
                            // Both are two sets of 4-bit values in one byte, so I THINK this is correct
                            case DICTObjTexture.Format.L4:
                            case DICTObjTexture.Format.A4:
                                Out_Data = new byte[((Width / 2) * Height)];
                                break;

                            default:
                                throw new InvalidOperationException("ConvertTextureToCGFX: Unsupported texture format " + Format);
                        }
                        int Out_Data_Offset = 0;
                        for (int Tile_Y = 0; Tile_Y <= (Height / 8) - 1; Tile_Y++)
                        {
                            for (int Tile_X = 0; Tile_X <= (Width / 8) - 1; Tile_X++)
                            {
                                for (int i = 0; i <= 63; i++)
                                {
                                    int X = Tile_Order[i] % 8;
                                    int Y = (Tile_Order[i] - X) / 8;
                                    int Img_Offset = ((Tile_X * 8) + X + (((Tile_Y * 8) + Y)) * Width) * (BPP / 8);
                                    switch (Format)
                                    {
                                        case DICTObjTexture.Format.RGBA8:
                                            {
                                                //R8G8B8A8
                                                if (BPP == 32)
                                                    Out_Data[Out_Data_Offset] = Data[Img_Offset + 3];
                                                else
                                                    Out_Data[Out_Data_Offset] = 0xff;
                                                Buffer.BlockCopy(Data, Img_Offset, Out_Data, Out_Data_Offset + 1, 3);
                                                Out_Data_Offset += 4;
                                                break;
                                            }
                                        case DICTObjTexture.Format.RGB8:
                                            {
                                                //R8G8B8 (sem transparência)
                                                Buffer.BlockCopy(Data, Img_Offset, Out_Data, Out_Data_Offset, 3);
                                                Out_Data_Offset += 3;
                                                break;
                                            }
                                        case DICTObjTexture.Format.RGBA5551:
                                            {
                                                //R5G5B5A1
                                                Out_Data[Out_Data_Offset + 1] = Convert.ToByte((Data[Img_Offset + 1] & 0xe0) >> 5);
                                                Out_Data[Out_Data_Offset + 1] += Convert.ToByte(Data[Img_Offset + 2] & 0xf8);
                                                Out_Data[Out_Data_Offset] = Convert.ToByte((Data[Img_Offset] & 0xf8) >> 2);
                                                Out_Data[Out_Data_Offset] += Convert.ToByte((Data[Img_Offset + 1] & 0x18) << 3);
                                                if ((BPP == 32 & Data[Img_Offset + 3] == 0xff) | BPP == 24)
                                                    Out_Data[Out_Data_Offset] += Convert.ToByte(1);
                                                Out_Data_Offset += 2;
                                                break;
                                            }
                                        case DICTObjTexture.Format.RGB565:
                                            {
                                                //R5G6B5
                                                Out_Data[Out_Data_Offset + 1] = Convert.ToByte((Data[Img_Offset + 1] & 0xe0) >> 5);
                                                Out_Data[Out_Data_Offset + 1] += Convert.ToByte(Data[Img_Offset + 2] & 0xf8);
                                                Out_Data[Out_Data_Offset] = Convert.ToByte(Data[Img_Offset] >> 3);
                                                Out_Data[Out_Data_Offset] += Convert.ToByte((Data[Img_Offset + 1] & 0x1c) << 3);
                                                Out_Data_Offset += 2;
                                                break;
                                            }
                                        case DICTObjTexture.Format.RGBA4:
                                            {
                                                //R4G4B4A4
                                                Out_Data[Out_Data_Offset + 1] = Convert.ToByte((Data[Img_Offset + 1] & 0xf0) >> 4);
                                                Out_Data[Out_Data_Offset + 1] += Convert.ToByte(Data[Img_Offset + 2] & 0xf0);
                                                Out_Data[Out_Data_Offset] = Convert.ToByte(Data[Img_Offset] & 0xf0);
                                                if (BPP == 32)
                                                {
                                                    Out_Data[Out_Data_Offset] += Convert.ToByte((Data[Img_Offset + 3] & 0xf0) >> 4);
                                                }
                                                else
                                                {
                                                    Out_Data[Out_Data_Offset] += Convert.ToByte(0xf);
                                                }
                                                Out_Data_Offset += 2;
                                                break;
                                            }
                                        case DICTObjTexture.Format.LA8:
                                            {
                                                //L8A8
                                                byte Luma = Convert.ToByte(0.299f * Data[Img_Offset] + 0.587f * Data[Img_Offset + 1] + 0.114f * Data[Img_Offset + 2]);
                                                Out_Data[Out_Data_Offset + 1] = Luma;
                                                if (BPP == 32)
                                                    Out_Data[Out_Data_Offset] = Data[Img_Offset + 3];
                                                else
                                                    Out_Data[Out_Data_Offset] = 0xff;
                                                Out_Data_Offset += 2;
                                                break;
                                            }

                                        case DICTObjTexture.Format.HILO8:
                                            // NOTE -- HILO8 was missing from "old" Ohana3DS, but "Rebirth" treats it the same
                                            // as L8, not sure if correct!!

                                        case DICTObjTexture.Format.L8:
                                            {
                                                //L8
                                                byte Luma = Convert.ToByte(0.299f * Data[Img_Offset] + 0.587f * Data[Img_Offset + 1] + 0.114f * Data[Img_Offset + 2]);
                                                Out_Data[Out_Data_Offset] = Luma;
                                                Out_Data_Offset += 1;
                                                break;
                                            }
                                        case DICTObjTexture.Format.A8:
                                            {
                                                //A8
                                                if (BPP == 32)
                                                {
                                                    Out_Data[Out_Data_Offset] = Data[Img_Offset + 3];
                                                }
                                                else
                                                {
                                                    Out_Data[Out_Data_Offset] = 0xff;
                                                }
                                                Out_Data_Offset += 1;
                                                break;
                                            }

                                        case DICTObjTexture.Format.LA4:
                                            {
                                                //L4A4

                                                // SB NOTE: I had to implement this one myself, it was missing from old Ohana
                                                // It's a grayscale format (packed shade/alpha, basically) so it won't support
                                                // color, unfortunately...

                                                byte Luma = Convert.ToByte(0.299f * Data[Img_Offset] + 0.587f * Data[Img_Offset + 1] + 0.114f * Data[Img_Offset + 2]);
                                                byte Alpha = Convert.ToByte(Data[Img_Offset + 3] & 0xF0);

                                                Luma >>= 4; // SB: I could probably do this more clever, but fuck it

                                                Out_Data[Out_Data_Offset] = Convert.ToByte(Luma | Alpha);

                                                Out_Data_Offset += 1;
                                                break;
                                            }

                                        case DICTObjTexture.Format.L4:
                                            {
                                                // SB NOTE: MISSING from "old" Ohana3DS, but should be trivial...
                                                // Basically each byte is two 4-bit shades of gray (no alpha)
                                                // High order bits first.

                                                //L4

                                                byte Luma = Convert.ToByte(0.299f * Data[Img_Offset] + 0.587f * Data[Img_Offset + 1] + 0.114f * Data[Img_Offset + 2]);

                                                Luma >>= 4; // SB: I could probably do this more clever, but fuck it

                                                if (Low_High_Toggle)
                                                {
                                                    // Low order bits second
                                                    Out_Data[Out_Data_Offset] |= Convert.ToByte(Luma);
                                                    Out_Data_Offset += 1;
                                                }
                                                else
                                                {
                                                    // High order bits first
                                                    Out_Data[Out_Data_Offset] = Convert.ToByte(Luma << 4);
                                                }

                                                Low_High_Toggle = !Low_High_Toggle;

                                                break;
                                            }

                                        case DICTObjTexture.Format.A4:
                                            {
                                                // SB NOTE: Same deal as L4, see above notes.
                                                // This is a two 4-bit alpha level packed format instead.

                                                //A4

                                                byte Alpha = Convert.ToByte((BPP == 32) ? Data[Img_Offset + 3] : 0xff);

                                                Alpha >>= 4; // SB: I could probably do this more clever, but fuck it

                                                if (Low_High_Toggle)
                                                {
                                                    // Low order bits second
                                                    Out_Data[Out_Data_Offset] |= Convert.ToByte(Alpha);
                                                    Out_Data_Offset += 1;
                                                }
                                                else
                                                {
                                                    // High order bits first
                                                    Out_Data[Out_Data_Offset] = Convert.ToByte(Alpha << 4);
                                                }

                                                Low_High_Toggle = !Low_High_Toggle;

                                                break;
                                            }

                                        default:
                                            // NOTE -- if you get here, see if ConvertTextureToRGBA implemented the missing format

                                            throw new InvalidOperationException("ConvertTextureToCGFX: Unsupported texture format " + Format);
                                    }
                                }
                            }
                        }
                    }

                    break;
            }


            return Out_Data;
        }

        private static byte[] ETC1_Decompress(Utility utility, byte[] Data, byte[] Alphas, int Width, int Height)
        {
            byte[] Out = new byte[(Width * Height * 4)];
            int Offset = 0;
            for (int Y = 0; Y <= (Height / 4) - 1; Y++)
            {
                for (int X = 0; X <= (Width / 4) - 1; X++)
                {
                    byte[] Block = new byte[8];
                    byte[] Alphas_Block = new byte[8];
                    for (int i = 0; i <= 7; i++)
                    {
                        Block[7 - i] = Data[Offset + i];
                        Alphas_Block[i] = Alphas[Offset + i];
                    }
                    Offset += 8;
                    Block = ETC1_Decompress_Block(utility, Block);

                    bool Low_High_Toggle = false;
                    int Alpha_Offset = 0;
                    for (int TX = 0; TX <= 3; TX++)
                    {
                        for (int TY = 0; TY <= 3; TY++)
                        {
                            int Out_Offset = (X * 4 + TX + ((Y * 4 + TY) * Width)) * 4;
                            int Block_Offset = (TX + (TY * 4)) * 4;
                            Buffer.BlockCopy(Block, Block_Offset, Out, Out_Offset, 3);

                            int Alpha_Data = 0;
                            if (Low_High_Toggle)
                            {
                                Alpha_Data = (Alphas_Block[Alpha_Offset] & 0xf0) >> 4;
                                Alpha_Offset += 1;
                            }
                            else
                            {
                                Alpha_Data = Alphas_Block[Alpha_Offset] & 0xf;
                            }
                            Low_High_Toggle = !Low_High_Toggle;
                            Out[Out_Offset + 3] = Convert.ToByte((Alpha_Data << 4) + Alpha_Data);
                        }
                    }
                }
            }
            return Out;
        }
        private static byte[] ETC1_Decompress_Block(Utility utility, byte[] Data)
        {
            //Ericsson Texture Compression
            int Block_Top = utility.GetI32(Data, 0);
            int Block_Bottom = utility.GetI32(Data, 4);

            bool Flip = (Block_Top & 0x1000000) > 0;
            bool Difference = (Block_Top & 0x2000000) > 0;

            int R1 = 0;
            int G1 = 0;
            int B1 = 0;
            int R2 = 0;
            int G2 = 0;
            int B2 = 0;
            int R = 0;
            int G = 0;
            int B = 0;

            if (Difference)
            {
                R1 = Block_Top & 0xf8;
                G1 = (Block_Top & 0xf800) >> 8;
                B1 = (Block_Top & 0xf80000) >> 16;

                R = Signed_Byte(Convert.ToByte(R1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 7) << 5)) >> 5);
                G = Signed_Byte(Convert.ToByte(G1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 0x700) >> 3)) >> 5);
                B = Signed_Byte(Convert.ToByte(B1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 0x70000) >> 11)) >> 5);

                R2 = R;
                G2 = G;
                B2 = B;

                R1 = R1 + (R1 >> 5);
                G1 = G1 + (G1 >> 5);
                B1 = B1 + (B1 >> 5);

                R2 = (R2 << 3) + (R2 >> 2);
                G2 = (G2 << 3) + (G2 >> 2);
                B2 = (B2 << 3) + (B2 >> 2);
            }
            else
            {
                R1 = Block_Top & 0xf0;
                R1 = R1 + (R1 >> 4);
                G1 = (Block_Top & 0xf000) >> 8;
                G1 = G1 + (G1 >> 4);
                B1 = (Block_Top & 0xf00000) >> 16;
                B1 = B1 + (B1 >> 4);

                R2 = (Block_Top & 0xf) << 4;
                R2 = R2 + (R2 >> 4);
                G2 = (Block_Top & 0xf00) >> 4;
                G2 = G2 + (G2 >> 4);
                B2 = (Block_Top & 0xf0000) >> 12;
                B2 = B2 + (B2 >> 4);
            }

            int Mod_Table_1 = (Block_Top >> 29) & 7;
            int Mod_Table_2 = (Block_Top >> 26) & 7;

            byte[] Out = new byte[(4 * 4 * 4)];
            if (Flip == false)
            {
                for (int Y = 0; Y <= 3; Y++)
                {
                    for (int X = 0; X <= 1; X++)
                    {
                        Color Col_1 = Modify_Pixel(R1, G1, B1, X, Y, Block_Bottom, Mod_Table_1);
                        Color Col_2 = Modify_Pixel(R2, G2, B2, X + 2, Y, Block_Bottom, Mod_Table_2);
                        Out[(Y * 4 + X) * 4] = Col_1.R;
                        Out[((Y * 4 + X) * 4) + 1] = Col_1.G;
                        Out[((Y * 4 + X) * 4) + 2] = Col_1.B;
                        Out[(Y * 4 + X + 2) * 4] = Col_2.R;
                        Out[((Y * 4 + X + 2) * 4) + 1] = Col_2.G;
                        Out[((Y * 4 + X + 2) * 4) + 2] = Col_2.B;
                    }
                }
            }
            else
            {
                for (int Y = 0; Y <= 1; Y++)
                {
                    for (int X = 0; X <= 3; X++)
                    {
                        Color Col_1 = Modify_Pixel(R1, G1, B1, X, Y, Block_Bottom, Mod_Table_1);
                        Color Col_2 = Modify_Pixel(R2, G2, B2, X, Y + 2, Block_Bottom, Mod_Table_2);
                        Out[(Y * 4 + X) * 4] = Col_1.R;
                        Out[((Y * 4 + X) * 4) + 1] = Col_1.G;
                        Out[((Y * 4 + X) * 4) + 2] = Col_1.B;
                        Out[((Y + 2) * 4 + X) * 4] = Col_2.R;
                        Out[(((Y + 2) * 4 + X) * 4) + 1] = Col_2.G;
                        Out[(((Y + 2) * 4 + X) * 4) + 2] = Col_2.B;
                    }
                }
            }

            return Out;
        }
        private static Color Modify_Pixel(int R, int G, int B, int X, int Y, int Mod_Block, int Mod_Table)
        {
            int Index = X * 4 + Y;
            int Pixel_Modulation = 0;
            int MSB = Mod_Block << 1;

            if (Index < 8)
            {
                Pixel_Modulation = Modulation_Table[Mod_Table, ((Mod_Block >> (Index + 24)) & 1) + ((MSB >> (Index + 8)) & 2)];
            }
            else
            {
                Pixel_Modulation = Modulation_Table[Mod_Table, ((Mod_Block >> (Index + 8)) & 1) + ((MSB >> (Index - 8)) & 2)];
            }

            R = Clip(R + Pixel_Modulation);
            G = Clip(G + Pixel_Modulation);
            B = Clip(B + Pixel_Modulation);

            return new Color
            {
                R = (byte)B,
                G = (byte)G,
                B = (byte)R,
                A = 255
            };
        }
        private static byte Clip(int Value)
        {
            if (Value > 0xff)
            {
                return 0xff;
            }
            else if (Value < 0)
            {
                return 0;
            }
            else
            {
                return Convert.ToByte(Value & 0xff);
            }
        }

        private static sbyte Signed_Byte(byte Byte_To_Convert)
        {
            if ((Byte_To_Convert < 0x80))
                return Convert.ToSByte(Byte_To_Convert);
            return Convert.ToSByte(Byte_To_Convert - 0x100);
        }
        private static int Signed_Short(int Short_To_Convert)
        {
            if ((Short_To_Convert < 0x8000))
                return Short_To_Convert;
            return Short_To_Convert - 0x10000;
        }

        private static int[] Get_ETC1_Scramble(int Width, int Height)
        {
            int[] Tile_Scramble = new int[((Width / 4) * (Height / 4))];
            int Base_Accumulator = 0;
            int Line_Accumulator = 0;
            int Base_Number = 0;
            int Line_Number = 0;

            for (int Tile = 0; Tile <= Tile_Scramble.Length - 1; Tile++)
            {
                if ((Tile % (Width / 4) == 0) & Tile > 0)
                {
                    if (Line_Accumulator < 1)
                    {
                        Line_Accumulator += 1;
                        Line_Number += 2;
                        Base_Number = Line_Number;
                    }
                    else
                    {
                        Line_Accumulator = 0;
                        Base_Number -= 2;
                        Line_Number = Base_Number;
                    }
                }

                Tile_Scramble[Tile] = Base_Number;

                if (Base_Accumulator < 1)
                {
                    Base_Accumulator += 1;
                    Base_Number += 1;
                }
                else
                {
                    Base_Accumulator = 0;
                    Base_Number += 3;
                }
            }

            return Tile_Scramble;
        }
    }
}
