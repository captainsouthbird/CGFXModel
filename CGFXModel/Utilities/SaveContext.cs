using CGFXModel.Chunks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CGFXModel.Utilities
{
    public static class SaveContextHelpers
    {
        public static void SaveList(this IEnumerable<ISaveable> items, SaveContext saveContext)
        {
            if(items != null)
            {
                foreach(var item in items)
                {
                    saveContext.SaveAndMarkReference(item);
                }
            }
        }

        public static void SaveEntries(this IChunkDICT dict, SaveContext saveContext)
        {
            if (dict != null)
            {
                foreach (var dictEntry in dict.Entries)
                {
                    saveContext.SaveAndMarkReference(dictEntry.EntryObject);
                }
            }
        }
    }

    // Extra carrier data required for saving especially to resolve various self-relative offsets
    public class SaveContext
    {
        public SaveContext(Utility utility)
        {
            Utility = utility;

            // Need to globalize these since they are not written inline like the other DATA chunk data
            StringTable = new RelativeDataQueueString(this);
            IMAGData = new RelativeDataQueueIMAG(this);

            PointerReferenceList = new List<PointerReference>();
            ObjectReferences = new Dictionary<object, uint>();
        }

        public void DumpStringTable()
        {
            StringTable.WriteQueuedData();
        }

        public void DumpIMAGData()
        {
            IMAGData.WriteQueuedData();
        }

        // This sets the reference point to an object, assuming it's about to be written.
        // Used for non-ISaveable types, generally list objects. Enforces the fact you need
        // to write it immediately for this reference to be valid by "writeAction."
        public void SaveAndMarkReference(object obj, Action writeAction)
        {
            if (obj != null)
            {
                var objectReference = ObjectReferences.Where(kv => ReferenceEquals(kv.Key, obj));
                if (objectReference.Any())
                {
                    throw new InvalidOperationException($"A reference to this {obj.GetType().Name} has already been made!");
                }

                ObjectReferences[obj] = Utility.GetWritePosition();
                writeAction();
            }
        }

        // A shortcut for a list of uints, typically the pointer list
        public void SaveAndMarkReference(IEnumerable<uint> items)
        {
            CGFXDebug.SaveStart("list", this);
            SaveAndMarkReference(items, () => { Utility.Write(items.ToArray()); });
        }

        // A shortcut for a list of floats
        public void SaveAndMarkReference(IEnumerable<float> items)
        {
            CGFXDebug.SaveStart("list", this);
            SaveAndMarkReference(items, () => { Utility.Write(items.ToArray()); });
        }

        // List of items, writes pointer reference per item
        public void SaveAndMarkReference(IEnumerable<ISaveable> items)
        {
            var itemNames = (items != null) ?
                items.Where(i => i != null).Select(i => i.GetType().Name).Distinct().ToList()
                : new[] { "<null>" }.ToList();

            CGFXDebug.SaveStart($"List of {string.Join(", ", itemNames)} [WriteObjectListPointerPlaceholder]", this);

            SaveAndMarkReference(items, () =>
            {
                foreach (var item in items)
                {
                    WritePointerPlaceholder(item);
                }
            });
        }

        // A shortcut for ISaveable objects since we know how to write them
        public void SaveAndMarkReference(ISaveable obj)
        {
            SaveAndMarkReference(obj, () => { obj.Save(this); });
        }

        public void WritePointerPlaceholder(object obj)
        {
            PointerReferenceList.Add(new PointerReference { Reference = obj, Location = Utility.GetWritePosition() });
            Utility.Write(0u);  // Placeholder
        }

        public void WriteDICTPointerPlaceholder<TDICT>(TDICT dict)
            where TDICT : Chunk, IChunkDICT, new()
        {
            // Number of entries in DICT
            Utility.Write(dict?.NumEntries ?? 0);

            // Offset to dictionary chunk start
            WritePointerPlaceholder(dict);
        }

        // Intended for simple lists of ints, floats, etc.
        public void WriteValueListPointerPlaceholder<TVal>(IEnumerable<TVal> values)
        {
            CGFXDebug.SaveStart($"HEADER of list of {typeof(TVal).Name} [WriteValueListPointerPlaceholder]", this);

            // Number of values
            var length = values?.Count() ?? 0;
            Utility.Write(length);

            // Reference the value list
            WritePointerPlaceholder(values);
        }

        // For lists of ISaveable objects 
        public void WriteObjectListPointerPlaceholder(IEnumerable<ISaveable> items)
        {
            var itemNames = (items != null) ?
                items.Where(i => i != null).Select(i => i.GetType().Name).Distinct().ToList()                
                : new[] { "<null>" }.ToList();

            CGFXDebug.SaveStart($"HEADER of list of {string.Join(", ", itemNames)} [WriteObjectListPointerPlaceholder]", this);

            // Number of items
            var length = items?.Count() ?? 0;
            Utility.Write(length);

            if (length > 0)
            {
                // Write what will become a pointer to a list of pointers
                WritePointerPlaceholder(items);
            }
            else
            {
                // No list made if no items available (ESPECIALLY if it's null)
                Utility.Write(0u);  // Null pointer
            }
        }
  
        public void ResolvePointerReferences()
        {
            // Go through PointerReferenceList and resolve all pointers. 
            foreach(var pointerReference in PointerReferenceList)
            {
                // Only act if not null. Otherwise placeholder functions already wrote zeroes and that's good enough.
                if (pointerReference.Reference != null)
                {
                    // Check if this object was committed so we can resolve the pointer.
                    var objectReference = ObjectReferences.Where(kv => ReferenceEquals(kv.Key, pointerReference.Reference));
                    if (!objectReference.Any())
                    {
                        throw new KeyNotFoundException($"Failed to resolve a committed reference to a {pointerReference.Reference.GetType().Name}");
                    }

                    // Resolve pointer
                    var patchLocation = pointerReference.Location;
                    var objectLocation = objectReference.Single().Value;

                    var relativeOffset = objectLocation - patchLocation;

                    Utility.PushWritePosition();
                    Utility.SetWritePosition(patchLocation);
                    Utility.Write(relativeOffset);
                    Utility.PopWritePosition();
                }
            }

            PointerReferenceList.Clear();
        }

        public Utility Utility { get; private set; }
        public RelativeDataQueueString StringTable { get; private set; }
        public RelativeDataQueueIMAG IMAGData { get; private set; }

        // Used while saving data. This stores the location that will be a reference to 
        // an object that another object is depending upon. It holds the reference of an
        // object and the location where the self-relative pointer needs to be written.
        class PointerReference
        {
            public object Reference { get; set; }
            public uint Location { get; set; }
        }
        private List<PointerReference> PointerReferenceList { get; set; }

        // This holds the actual locations of objects sought by PointerReferenceList.
        // If PointerReferenceList points to something NOT in this list, that's an
        // error, as it means it wasn't saved and is thus not accessible.
        private Dictionary<object, uint> ObjectReferences { get; set; }
    }
}
