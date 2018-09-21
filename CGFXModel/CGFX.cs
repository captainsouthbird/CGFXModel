using CGFXModel.Chunks;
using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace CGFXModel
{
    public class CGFX
    {
        private uint revision;  // BCRES revision

        public ChunkDATA Data
        {
            get
            {
                return Chunks.GetChunk<ChunkDATA>();
            }
        }

        public List<Chunk> Chunks { get; private set; }

        private CGFX()
        {
            Chunks = new List<Chunk>();
        }

        public CGFX(uint revision)
            : this()
        {
            this.revision = revision;
        }

        public static CGFX Load(BinaryReader br)
        {
            var utility = new Utility(br, null);
            var cgfx = new CGFX();

            CGFXDebug.LoadStart(cgfx, utility);

            var magic = utility.ReadMagic();
            if (magic != Utility.MakeMagic("CGFX"))
            {
                throw new InvalidOperationException("Wrong magic");
            }

            utility.ReadEndiannessByteMarker();

            // Size of header
            var headerSize = br.ReadUInt16();

            // Revision of file
            cgfx.revision = br.ReadUInt32();

            // Size of file
            var fileSize = br.ReadUInt32();
            var actualFileSize = (uint)br.BaseStream.Length;
            if (actualFileSize != fileSize)
            {
                throw new InvalidOperationException($"CGFX file header says file should be {fileSize} bytes, but it's actually {actualFileSize} bytes!");
            }

            // Number of entries (typically [?] 2, being DATA and IMAG)
            var entries = br.ReadUInt32();

            // Ensure at end of header
            br.BaseStream.Seek(headerSize, SeekOrigin.Begin);

            // ------------------------------
            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                // At this level, we're expecting either a DATA or IMAG chunk only.
                // Everything will go into the Chunks bucket regardless...

                var chunkMagic = utility.PeekMagic();
                if (chunkMagic == Utility.MakeMagic("DATA"))
                {
                    cgfx.Chunks.Add(Chunk.Load<ChunkDATA>(utility));
                }
                else if (chunkMagic == Utility.MakeMagic("IMAG"))
                {
                    cgfx.Chunks.Add(Chunk.Load<ChunkIMAG>(utility));
                }
                else
                {
                    // Unknown chunk type, just read raw...
                    cgfx.Chunks.Add(Chunk.Load(utility));
                }

            }

            return cgfx;
        }

        public void Save(BinaryWriter bw)
        {
            var utility = new Utility(null, bw);
            var saveContext = new SaveContext(utility);

            CGFXDebug.SaveStart(this, saveContext);

            // Magic
            utility.WriteMagic("CGFX");

            // Endianness
            utility.WriteEndiannessByteMarker();

            // Header size
            utility.Write((ushort)0x14);    // FIXME constant

            // Revision
            utility.Write(revision);

            // File Size
            // For now, we'll just write zeroes for the file size and patch it back in afterwards
            var fileSizeLoc = bw.BaseStream.Position;
            utility.Write((uint)0);

            // Number of entries
            utility.Write((uint)Chunks.Count);

            foreach(var chunk in Chunks)
            {
                chunk.Save(saveContext);
            }

            // OKAY, now we can patch in that filesize!
            var fileSize = bw.BaseStream.Position;
            bw.BaseStream.Seek(fileSizeLoc, SeekOrigin.Begin);
            utility.Write((uint)fileSize);
        }
    }
}
