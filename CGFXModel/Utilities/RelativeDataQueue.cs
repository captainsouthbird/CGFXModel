using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CGFXModel.Utilities
{
    // The CGFX format uses "self-relative" data addressing throughout for various purposes.
    // Sometimes it's referencing data from a lookup table that we just cannot know until
    // we're actually writing that lookup table. As such, when writing a CGFX file, we need 
    // to record WHERE the self-relative offset needs to be and what data it will be when
    // the lookup table is built. That's the purpose of this container!

    // All saveable (queuable) objects must implement this
    public interface ISaveable
    {
        // Method to save data; will be queued so it writes in order
        void Save(SaveContext saveContext);
    }

    // An implementation of the RelativeDataQueue that benefits the string table specifically
    public class RelativeDataQueueString : RelativeDataQueue
    {
        // Wrapper for strings, used in the string section queue
        private class SaveString : ISaveable
        {
            private string str;

            public SaveString(string str)
            {
                this.str = str;

                if (str == null)
                {
                    // NOTE: We should NOT queue null strings, since that's instead handled by
                    // a NULL pointer in the data. Null strings have no point to be queued.
                    throw new ArgumentException("SaveString: str is NULL", "str");
                }
            }

            public override bool Equals(object obj)
            {
                if (obj.GetType() == typeof(SaveString))
                {
                    return str == (obj as SaveString).str;
                }
                else
                {
                    return base.Equals(obj);
                }
            }

            public override int GetHashCode()
            {
                return str.GetHashCode();
            }

            public void Save(SaveContext saveContext)
            {
                if (!string.IsNullOrEmpty(str))
                {
                    saveContext.Utility.Write(Encoding.ASCII.GetBytes(str));    // TODO -- not sure if ASCII or UTF8 in string table
                }

                saveContext.Utility.Write((byte)0);     // Null terminator
            }
        }

        public RelativeDataQueueString(SaveContext saveContext)
            : base(saveContext, true, true)
        {

        }

        public void EnqueueAndWriteTempRel(string desiredData)
        {
            EnqueueAndWriteTempRel(new SaveString(desiredData));
        }
    }
    

    // IMAG chunk queue
    public class RelativeDataQueueIMAG : RelativeDataQueue
    {
        // Wrapper for raw data, used in the IMAG section queue
        private class SaveRaw : ISaveable
        {
            private byte[] data;

            public SaveRaw(byte[] data)
            {
                this.data = data;
            }
            
            public void Save(SaveContext saveContext)
            {
                if (data != null)
                {
                    saveContext.Utility.Write(data);
                }
            }
        }

        public RelativeDataQueueIMAG(SaveContext saveContext)
            : base(saveContext, false, false)
        {

        }

        public void EnqueueAndWriteTempRel(byte[] desiredData, int alignment = 4)
        {
            if (desiredData != null)
            {
                EnqueueAndWriteTempRel(new SaveRaw(desiredData), alignment);
            }
            else
            {
                saveContext.Utility.Write(0u);  // null data
            }
        }
    }

    public abstract class RelativeDataQueue
    {
        [DebuggerDisplay("TypeName = {DesiredDataTypeName}")]
        public class DataQueueItem
        {
            public DataQueueItem(uint position, ISaveable desiredData, int alignment)
            {
                Position = position;
                DesiredData = desiredData;
                Alignment = alignment;
            }

            public string DesiredDataTypeName { get { return DesiredData?.GetType().Name ?? "<null>"; } }
            public uint Position { get; private set; }
            public ISaveable DesiredData { get; private set; }
            public int Alignment { get; private set; }
        }

        private bool nullOK;
        private bool noDupes;   // To be clear, this means if a duplicate occurs, reuse the same map instead of reinserting data (e.g. text strings)
        protected SaveContext saveContext;
        private Queue<DataQueueItem> queue;
        public int QueueDepth { get { return queue.Count; } }

        protected RelativeDataQueue(SaveContext saveContext, bool nullOK, bool noDupes)
        {
            this.saveContext = saveContext;
            this.nullOK = nullOK;
            this.noDupes = noDupes;
            queue = new Queue<DataQueueItem>();
        }

        // This will enqueue a data item and as a bonus write a zeroed-out self-relative position,
        // since we'll absolutely need that placeholder eventually...
        public void EnqueueAndWriteTempRel(ISaveable desiredData, int alignment = 0)
        {
            var utility = saveContext.Utility;

            if (desiredData != null)
            {
                queue.Enqueue(new DataQueueItem(utility.GetWritePosition(), desiredData, alignment));
            }
            else if (!nullOK)
            {
                throw new ArgumentException("RelativeDataQueue Enqueue: Null reference not accepted in this queue", nameof(desiredData));
            }

            // Write out the stub self-relative position, to be patched later (unless null)
            utility.Write(0U);
        }

        protected virtual void SaveItem(ISaveable obj, object saveItemParameter)
        {
            obj.Save(saveContext);
        }

        public void WriteQueuedData(object saveItemParameter = null)
        {
            var utility = saveContext.Utility;

            if (noDupes)
            {
                // "No dupes" mode will group by the intended data so that we 
                // patch the same result in multiple places (main usage would
                // be the string table, where sometimes the same string is
                // referenced more than once.)
                var groupedQueueItems = queue.GroupBy(q => q.DesiredData).ToList();

                foreach(var queueItem in groupedQueueItems)
                {
                    var dataItem = queueItem.First();   // Since the data is identical across the group, First() is arbitrary and fine here

                    // Align FIRST since this could change the position of the target
                    if (dataItem.Alignment > 0)
                    {
                        utility.AlignWrite(dataItem.Alignment);
                    }

                    // Write the same patch everywhere first!
                    foreach (var request in queueItem)
                    {
                        // Write self-relative offset where this data was requested
                        utility.WriteOffset(request.Position, request.DesiredData == null);

                        // Sanity check
                        if(request.Alignment != dataItem.Alignment)
                        {
                            throw new InvalidOperationException("In noDupe queue, alignment changed across same-key items");
                        }
                    }

                    // Now write the data
                    if (dataItem.DesiredData != null)
                    {
                        SaveItem(dataItem.DesiredData, saveItemParameter);
                    }
                }

                queue.Clear();  // Clear it since we've pulled everything
            }
            else
            {
                // Standard dequeue behavior
                while (queue.Count > 0)
                {
                    var request = queue.Dequeue();

                    // Align FIRST since this could change the position of the target
                    if(request.Alignment > 0)
                    {
                        utility.AlignWrite(request.Alignment);
                    }

                    // Write self-relative offset where this data was requested
                    utility.WriteOffset(request.Position, request.DesiredData == null);

                    // Now write the data
                    if (request.DesiredData != null)
                    {
                        SaveItem(request.DesiredData, saveItemParameter);
                    }
                }
            }
        }
    }
}
