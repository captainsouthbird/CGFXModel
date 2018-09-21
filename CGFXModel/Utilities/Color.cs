namespace CGFXModel.Utilities
{
    // "Old" Ohana3DS used System.Drawing's Color, but seems silly to reference that just for this
    public struct Color
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public static Color Read(Utility utility)
        {
            return new Color
            {
                R = utility.ReadByte(),
                G = utility.ReadByte(),
                B = utility.ReadByte(),
                A = utility.ReadByte()
            };
        }

        public void Save(Utility utility)
        {
            utility.Write(R);
            utility.Write(G);
            utility.Write(B);
            utility.Write(A);
        }

        public static Color FromRGBA(uint color)
        {
            return new Color
            {
                R = (byte)(color & 0xff),
                G = (byte)((color >> 8) & 0xff),
                B = (byte)((color >> 16) & 0xff),
                A = (byte)(color >> 24)
            };
        }

        // In case float-color is more useful to you
        public ColorFloat ToFloatColor()
        {
            return new ColorFloat
            {
                R = R / 255.0f,
                G = G / 255.0f,
                B = B / 255.0f,
                A = A / 255.0f
            };
        }
    }

    // Ohana3DS mushed floating point colors into the regular byte-sized RGBA, but that risks info loss!
    // Better to keep it native floating point and just allow conversion if it helps.
    public struct ColorFloat
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }

        public static ColorFloat Read(Utility utility)
        {
            return new ColorFloat
            {
                R = utility.ReadFloat(),
                G = utility.ReadFloat(),
                B = utility.ReadFloat(),
                A = utility.ReadFloat()
            };
        }

        public void Save(Utility utility)
        {
            utility.Write(R);
            utility.Write(G);
            utility.Write(B);
            utility.Write(A);
        }

        // In case byte-sized RGBA is more useful to you
        public Color ToColor()
        {
            return new Color
            {
                R = (byte)(R * 0xff),
                G = (byte)(G * 0xff),
                B = (byte)(B * 0xff),
                A = (byte)(A * 0xff)
            };
        }
    }
}
