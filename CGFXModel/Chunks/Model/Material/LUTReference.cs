using CGFXModel.Utilities;
using System;

namespace CGFXModel.Chunks.Model.Material
{
    public class LUTReference : ISaveable
    {
        public uint TypeId { get; private set; }
        public string SamplerName { get; set; }
        public string TableName { get; set; }

        public ChunkDICTLUT LUT { get; private set; }

        public static LUTReference Load(Utility utility)
        {
            var lr = new LUTReference();

            CGFXDebug.LoadStart(lr, utility);

            lr.TypeId = utility.ReadU32();
            if(lr.TypeId != 0x40000000)
            {
                throw new InvalidOperationException($"Unexpected TypeId {lr.TypeId}");
            }

            lr.SamplerName = utility.ReadString();
            lr.TableName = utility.ReadString();

            lr.LUT = utility.LoadDICTFromOffset<ChunkDICTLUT>();

            return lr;
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            utility.Write(TypeId);

            saveContext.StringTable.EnqueueAndWriteTempRel(SamplerName);
            saveContext.StringTable.EnqueueAndWriteTempRel(TableName);

            saveContext.WritePointerPlaceholder(LUT);
        }
    }
}
