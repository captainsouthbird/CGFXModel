using CGFXModel.Chunks.Model;
using CGFXModel.Chunks.Model.Skeleton;
using CGFXModel.Chunks.Texture;
using CGFXModel.Utilities;
using System;
using System.Linq;

namespace CGFXModel.Chunks
{
    public class ChunkDATA : Chunk
    {
        public enum EntryType
        {
            Model,
            Texture,
            LUT,
            Material,
            Shader,
            Camera,
            Light,
            Fog,
            Environment,
            SkeletonAnim,
            TextureAnim,
            VisibilityAnim,
            CameraAnim,
            LightAnim,
            Emitter,
            Unknown,

            // TODO: Notes suggest this may vary by revision??
            TotalEntries
        }

        public IChunkDICT[] Entries { get; private set; }


        public ChunkDATA()
            : base(Utility.MakeMagic("DATA"))
        {
            Entries = new IChunkDICT[(int)EntryType.TotalEntries];
        }

        protected override void LoadInternal(Utility utility, uint chunkSize)
        {
            // The DATA chunk simply has a list that points to DICT chunks.
            // This can contain up to 16 DICTs, always in the same order:
            //
            //0   Models
            //1   Textures
            //2   LUTS(Material / Color / Shader look - up tables ?)
            //3   Materials
            //4   Shaders
            //5   Cameras
            //6   Lights
            //7   Fog
            //8   Environments
            //9   Skeleton animations
            //10  Texture animations
            //11  Visibility animations
            //12  Camera animations
            //13  Light animations
            //14  Emitters
            //15  Unknown
            //
            // These entries point to further subchunks that define the specific data.

            // Each DATA list entry is as follows:
            //  [4B] Number of entries in DICT
            //  [4B] Offset (self-relative) to DICT
            //  NOTE: Any unused entry should be zeroed out
            //
            // As noted, we should always have the same number of entries as above,
            // barring notes that suggest it may vary by revision. 
            //
            // NOTE: We could possibly look for the first DICT to figure how many
            // entries we have, but I'm not sure that's 100% reliable...
            for (var entry = EntryType.Model; entry < EntryType.TotalEntries; entry++)
            {
                // Jump to and read DICT...
                IChunkDICT dict = null;

                if (entry == EntryType.Model)
                {
                    dict = utility.LoadDICTFromOffset<ChunkDICTModel>();
                }
                else if (entry == EntryType.Texture)
                {
                    dict = utility.LoadDICTFromOffset<ChunkDICTTexture>();
                }
                else
                {
                    var numEntries = utility.ReadU32();
                    var offsetToDict = utility.ReadOffset();

                    // It only matters if there's data here to load that we're not handling!
                    if (numEntries > 0)
                    {
                        throw new NotImplementedException($"EntryType {entry} does not have a DICT loader implemented!");
                    }
                }

                Entries[(int)entry] = dict;
            }
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            // A little maintenance... if we have any models with bones, let's
            // make sure their indexes are correct...
            var models = (ChunkDICTModel)Entries[(int)EntryType.Model];
            foreach (var modelEntry in models.Entries)
            {
                var model = (DICTObjModel)modelEntry.EntryObject;

                if (model.HasSkeleton)
                {
                    var bones = model.Skeleton.Bones;
                    for (var i = 0; i < bones.Entries.Count; i++)
                    {
                        (bones.Entries[i].EntryObject as DICTObjBone).Index = i;
                    }

                    // TODO -- fix all SubMeshes with BoneIndices here now!
                }
            }


            var utility = saveContext.Utility;

            // Write out zeroes to accomodate all entries, reserving the space
            var dictLUTStartPos = utility.GetWritePosition();

            for (var entry = EntryType.Model; entry < EntryType.TotalEntries; entry++)
            {
                var numEntries = Entries[(int)entry]?.NumEntries ?? 0U;
                utility.Write(numEntries);      // We can write number of entries NOW...
                utility.Write(0U);  // ... but we can't know the offset yet!
            }

            // First the dictionary headers are written out
            for (var entry = EntryType.Model; entry < EntryType.TotalEntries; entry++)
            {
                var dict = Entries[(int)entry];

                if (dict != null && dict.NumEntries > 0)
                {
                    // From start of LUT to 4 + entry * 8 bytes inward...
                    var dictLUTTarget = (uint)(dictLUTStartPos + 4 + ((int)entry * 8));
                    utility.WriteOffset(dictLUTTarget);

                    // Save DICT chunk
                    dict.Save(saveContext);
                }
            }

            // Now we need to explicitly write out the dictionary contents
            for (var entry = EntryType.Model; entry < EntryType.TotalEntries; entry++)
            {
                var dict = Entries[(int)entry];

                dict?.SaveEntries(saveContext);
            }

            // Resolve all references
            saveContext.ResolvePointerReferences();

            // Finally, write out the string table
            saveContext.DumpStringTable();

            // The IMAG chunk seems to have its data (passed the 8 byte header) aligned to nearest 
            // 128 byte boundary. So we need to make sure we're aligned there before writing it.
            // We need to do this here (unfortunately kinda) because it needs to be included in 
            // the recorded size of the DATA chunk.
            utility.AlignWrite(128, 8);
        }
    }
}
