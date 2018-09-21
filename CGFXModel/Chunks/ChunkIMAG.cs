using CGFXModel.Utilities;

namespace CGFXModel.Chunks
{
    public class ChunkIMAG : Chunk
    {
        public ChunkIMAG() 
            : base(Utility.MakeMagic("IMAG"))
        {
        }

        protected override void LoadInternal(Utility utility, uint chunkSize)
        {
            // The IMAG chunk is really just a chunk containing raw CGFX format texture data.
            // It's just a bucket with no particular structure otherwise. As such, it really
            // can't "load" anything in a useful sense (as this software has inline loaders that
            // will fetch the texture data on-demand while it loads the CGFX file instead.)
            //
            // TL;DR: This "LoadInternal" does nothing
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.DumpIMAGData();
        }
    }
}
