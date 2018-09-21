using CGFXModel.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CGFXModel
{
    public static class CGFXDebug
    {
        // So we can rewrite in-order
        public class ReadLog
        {
            public uint Location { get; set; }
            public string LogEntry { get; set; }
        }
        private static List<ReadLog> logEntries;

        [Conditional("DEBUG")]
        public static void Init(string dumpLogFile)
        {
            log = new StreamWriter(dumpLogFile);
            logEntries = new List<ReadLog>();
        }

        // hasDynamicType: Kludge because sometimes I need to read a TypeId before I know what it is; if true, reports position as -4 from where it's at (size of TypeId uint)
        [Conditional("DEBUG")]
        public static void LoadStart(object obj, Utility utility, bool hasDynamicType = false)
        {
            if (log != null)
            {
                var entry = $"{new string('\t', utility.ReadPositionStackDepth)}[{(utility.GetReadPosition() - (hasDynamicType ? 4 : 0)).ToString("X4")}]: Read {obj.GetType().Name}";
                log.WriteLine(entry);
                logEntries.Add(new ReadLog { Location = utility.GetReadPosition(), LogEntry = entry });
            }
        }

        // hasDynamicType: Kludge because sometimes I need to read a TypeId before I know what it is; if true, reports position as -4 from where it's at (size of TypeId uint)
        [Conditional("DEBUG")]
        public static void LoadStart(string desc, Utility utility, bool hasDynamicType = false)
        {
            if (log != null)
            {
                var entry = $"{new string('\t', utility.ReadPositionStackDepth)}[{(utility.GetReadPosition() - (hasDynamicType ? 4 : 0)).ToString("X4")}]: Read {desc}";
                log.WriteLine(entry);
                logEntries.Add(new ReadLog { Location = utility.GetReadPosition(), LogEntry = entry });
            }
        }

        [Conditional("DEBUG")]
        public static void LoadDumpOrderedLog()
        {
            if (log != null)
            {
                log.WriteLine("\n\nORDERED LOG");

                foreach (var entry in logEntries.OrderBy(l => l.Location))
                {
                    log.WriteLine(entry.LogEntry);
                }

                log.WriteLine("\n\n");
            }
        }

        [Conditional("DEBUG")]
        public static void SaveStart(object obj, SaveContext saveContext)
        {
            if (log != null)
            {
                log.WriteLine($"[{saveContext.Utility.GetWritePosition().ToString("X4")}]: Wrote {obj.GetType().Name}");
            }
        }

        [Conditional("DEBUG")]
        public static void SaveStart(string desc, SaveContext saveContext)
        {
            if (log != null)
            {
                log.WriteLine($"[{saveContext.Utility.GetWritePosition().ToString("X4")}]: Wrote {desc}");
            }
        }

        [Conditional("DEBUG")]
        public static void WriteLog(string desc)
        {
            if (log != null)
            {
                log.WriteLine(desc);
            }
        }

        [Conditional("DEBUG")]
        public static void Shutdown()
        {
            if(log != null)
            {
                log.Dispose();
                log = null;
            }
        }

        //class LoadStartEntry
        //{
        //    public Type ObjectType { get; set; }
        //    public uint StartOffset { get; set; }
        //    public uint EndOffset { get; set; }
        //}

        private static StreamWriter log;        
    }
}
