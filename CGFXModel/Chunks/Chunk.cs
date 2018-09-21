using CGFXModel.Utilities;

namespace CGFXModel.Chunks
{
    // A generic representation of a chunk (e.g. DATA, IMAG, ...)
    // This usually shouldn't be used on its own unless the data type is simply unknown.
    public class Chunk : ISaveable
    {
        // "Magic" of the chunk
        public uint Magic { get; private set; }
        public string MagicString { get { return Utility.GetMagicString(Magic); } }     // <-- Intended more for debugging

        // The following is used ONLY if this chunk isn't superclassed and just holds
        // unprocessed raw data. Otherwise it's expected the superclasses will produce
        // their own raw binary data.
        private byte[] rawData;

        // Typical chunk initialization specifying its magic
        public Chunk(uint magic)
        {
            Magic = magic;
        }

        // This will load a chunk generically if it's unknown
        public static Chunk Load(Utility utility)
        {
            var chunkMagic = utility.PeekMagic();
            return Load(utility, new Chunk(chunkMagic));
        }

        // This will load a chunk using a specific Chunk Type
        public static TChunk Load<TChunk>(Utility utility)
            where TChunk : Chunk, new()
        {
            return Load(utility, new TChunk());
        }

        // Either way, chunks are always read the same...
        private static TChunk Load<TChunk>(Utility utility, TChunk chunk)
            where TChunk : Chunk
        {
            CGFXDebug.LoadStart(chunk, utility);

            var startPosition = utility.GetReadPosition();
            var magic = utility.ReadMagic();
            var chunkSize = utility.ReadU32();

            // Call Chunk's load routine
            chunk.LoadInternal(utility, chunkSize);

            // No matter what, make sure we've seeked to the end of chunk by this point!
            utility.SetReadPosition(startPosition + chunkSize);

            return chunk;
        }

        // Internal load routine to be defined by the chunk in context
        protected virtual void LoadInternal(Utility utility, uint chunkSize)
        {
            // This just reads the chunk raw and stores it
            var readLength = chunkSize - 4 - 4;  // -4 for magic, -4 for size itself
            rawData = utility.ReadBytes(readLength);
        }

        // This writes the Chunk, calling SaveInternal to write the raw data from superclasses
        public void Save(SaveContext saveContext)
        {
            CGFXDebug.SaveStart(this, saveContext);

            var utility = saveContext.Utility;

            // We'll need to patch in the length later, so we need to know where this chunk
            // is starting now...
            var chunkStart = utility.GetWritePosition();

            utility.WriteMagic(Magic);
            utility.Write(0U);  // Placeholder for chunk length

            // Write this chunk based on all superclass functionality...
            SaveInternal(saveContext);

            // Now we can compute and write out the chunk length
            var chunkEnd = utility.GetWritePosition();
            utility.PushWritePosition();
            utility.SetWritePosition(chunkStart + 4);
            utility.Write(chunkEnd - chunkStart);
            utility.PopWritePosition();
        }

        // Internal save routine to be defined by the chunk in context
        protected virtual void SaveInternal(SaveContext saveContext)
        {
            saveContext.Utility.Write(rawData);
        }
    }
}
