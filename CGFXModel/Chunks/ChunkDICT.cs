using CGFXModel.Chunks.MetaData;
using CGFXModel.Utilities;
using System;
using System.Collections.Generic;

namespace CGFXModel.Chunks
{
    public interface IChunkDICT
    {
        void Save(SaveContext saveContext);
        uint NumEntries { get; }
        List<DICTEntry> Entries { get; }
    }

    public abstract class ChunkDICTObject : ISaveable   // AKA GfxObject in SPICA
    {
        protected virtual bool VerifyTypeId(uint typeId) { return true; }   // Implement this to do a verification of valid types

        // Common properties:
        protected virtual uint TypeId { get; set; }  // Called "Flags" in Ohana3DS, but SPICA recognizes this as a unique identifier of the type
        public abstract string Magic { get; }       // Fill this in with appropriate magic string!
        public uint Revision { get; protected set; }
        public string Name { get; set; }       // This is a 4B relative offset in the data

        // MetaData belonging to DICT...
        public ChunkDICTMetaData MetaDatas { get; private set; }

        public virtual void Load(Utility utility)
        {
            CGFXDebug.LoadStart(this, utility);

            TypeId = utility.ReadU32();
            if(!VerifyTypeId(TypeId))
            {
                throw new InvalidOperationException($"ChunkDICTObject Load: ERROR reading DICT -- unexpected type ID '{TypeId.ToString("X8")}'");
            }


            var magic = utility.ReadMagic();
            if (magic != Utility.MakeMagic(Magic))
            {
                throw new InvalidOperationException($"ChunkDICTObject Load: ERROR reading DICT -- expected magic '{Magic}', got '{Utility.GetMagicString(magic)}'");
            }

            Revision = utility.ReadU32();
            Name = utility.ReadString();

            MetaDatas = utility.LoadDICTFromOffset<ChunkDICTMetaData>();
        }

        public virtual void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            utility.Write(TypeId);
            utility.WriteMagic(Magic);
            utility.Write(Revision);

            saveContext.StringTable.EnqueueAndWriteTempRel(Name);

            saveContext.WriteDICTPointerPlaceholder(MetaDatas);
        }
    }

    public class DICTEntry : IPatriciaTreeNode
    {
        // NOTE: These define a Patricia/Radix tree for rapid name look up in the DICT.
        public uint ReferenceBit { get; set; } //Radix tree
        public ushort LeftNodeIndex { get; set; }
        public ushort RightNodeIndex { get; set; }

        // NOTE: In the native format, this is an offset to the name, not the literal string
        public string Name { get; set; }

        // NOTE: In the native format, this is an offset to the object
        public ISaveable EntryObject { get; set; }
    }

    public abstract class ChunkDICT<TObj> : Chunk, IChunkDICT
        where TObj : ChunkDICTObject, new()
    {
        public uint NumEntries { get { return (uint)Entries.Count; } }

        public List<DICTEntry> Entries { get; private set; }

        private DICTEntry RootEntry { get; set; }

        public ChunkDICT()
            : base(Utility.MakeMagic("DICT"))
        {
        }

        protected override void LoadInternal(Utility utility, uint chunkSize)
        {
            var numEntries = utility.ReadU32();
            Entries = new List<DICTEntry>((int)numEntries);

            RootEntry = new DICTEntry
            {
                ReferenceBit = utility.ReadU32(),
                LeftNodeIndex = utility.ReadU16(),
                RightNodeIndex = utility.ReadU16(),
                Name = utility.ReadString(),        // NOTE: Expected to always be null
                EntryObject = LoadObject(utility)   // NOTE: Expected to always be null
            };

            for (var i = 0; i < numEntries; i++)
            {
                Entries.Add(new DICTEntry
                {
                    ReferenceBit = utility.ReadU32(),
                    LeftNodeIndex = utility.ReadU16(),
                    RightNodeIndex = utility.ReadU16(),
                    Name = utility.ReadString(),
                    EntryObject = LoadObject(utility)
                });
            }
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            var numEntries = Entries.Count;
            utility.Write(numEntries);

            RebuildTree();

            utility.Write(RootEntry.ReferenceBit);
            utility.Write(RootEntry.LeftNodeIndex);
            utility.Write(RootEntry.RightNodeIndex);
            if (RootEntry.Name != null) // NOTE: This is always expected to be null, I'm just being needlessly thorough
            {
                saveContext.StringTable.EnqueueAndWriteTempRel(RootEntry.Name);
            }
            else
            {
                saveContext.Utility.Write(0u);
            }
            saveContext.WritePointerPlaceholder(RootEntry.EntryObject);
            
            for (var i = 0; i < numEntries; i++)
            {
                var entry = Entries[i];
                utility.Write(entry.ReferenceBit);
                utility.Write(entry.LeftNodeIndex);
                utility.Write(entry.RightNodeIndex);
                saveContext.StringTable.EnqueueAndWriteTempRel(entry.Name);
                saveContext.WritePointerPlaceholder(entry.EntryObject);
            }
        }

        private TObj LoadObject(Utility utility)
        {
            TObj obj = null;

            // At this point we're going to read a self-relative offset to the object in question.
            // So we need to read it, jump to it, let it do its own thing, then return.
            var offset = utility.ReadOffset();

            // NOTE: I'm making an assumption that offset == 0 would be a null object pointer,
            // but I don't actually know if this ever happens. (It'd be bad in any case.)
            if (offset > 0)
            {
                obj = new TObj();

                utility.PushReadPosition();
                utility.SetReadPosition(offset);

                obj.Load(utility);

                utility.PopReadPosition();
            }

            return obj;
        }

        private void RebuildTree()
        {
            var Nodes = new List<DICTEntry>();

            // Adapted from SPICA's code

            if (Entries.Count > 0)
                Nodes.Add(new DICTEntry { ReferenceBit = uint.MaxValue });
            else
                Nodes.Add(new DICTEntry());

            int MaxLength = 0;

            foreach (var Value in Entries)
            {
                if (MaxLength < Value.Name.Length)
                    MaxLength = Value.Name.Length;
            }

            foreach (var Value in Entries)
            {
                var Node = new DICTEntry
                {
                    Name = Value.Name,
                    //EntryObject = Value
                };

                PatriciaTree.Insert(Nodes, Node, MaxLength);
            }


            // TODO: Bit of a hacky implementation here, but this "should work"
            // and at least I thankfully get this functionality, courtesy of SPICA

            // Nodes index 0 will be the root node, the others should line up with
            // the subsequent entries...
            RootEntry = Nodes[0];
            for(var entryIndex = 0; entryIndex < Entries.Count; entryIndex++)
            {
                var node = Nodes[entryIndex + 1];   // +1 because index zero is the root
                var entry = Entries[entryIndex];
                if (node.Name != entry.Name)
                {
                    throw new InvalidOperationException("RebuildTree: Name mismatch in node");
                }

                entry.ReferenceBit = node.ReferenceBit;
                entry.LeftNodeIndex = node.LeftNodeIndex;
                entry.RightNodeIndex = node.RightNodeIndex;
            }
        }
    }
}
