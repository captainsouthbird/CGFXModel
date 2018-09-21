using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.AnimGroup
{
    public class DICTObjAnimGroup : ChunkDICTObject
    {
        public enum AnimEvaluationTiming
        {
            BeforeWorldUpdate,
            AfterSceneCull
        }

        public override string Magic => throw new NotImplementedException();    // NOT USED

        public uint Flags { get; set; }
        public int MemberType { get; set; }
        public ChunkDICTAnimGroupElement Elements { get; private set; }
        public int[] BlendOperationTypes { get; private set; }
        public AnimEvaluationTiming EvaluationTiming;
        public uint Unknown { get; private set; }   // SPICA doesn't note this, unknown, found through testing

        public override void Load(Utility utility)
        {
            // NOT caling base.Load() as this doesn't use the usual header
            CGFXDebug.LoadStart(this, utility);

            TypeId = utility.ReadU32();
            if(TypeId != 0x80000000)
            {
                throw new InvalidOperationException($"DICTObjAnimGroup: Unexpected TypeId {TypeId.ToString("X8")}");
            }

            Flags = utility.ReadU32();
            Name = utility.ReadString();
            MemberType = utility.ReadI32();

            Elements = utility.LoadDICTFromOffset<ChunkDICTAnimGroupElement>();

            var count = utility.ReadU32();
            BlendOperationTypes = utility.ReadInts(count);

            EvaluationTiming = (AnimEvaluationTiming)utility.ReadU32();

            Unknown = utility.ReadU32();
        }

        public override void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            utility.Write(TypeId);
            utility.Write(Flags);
            saveContext.StringTable.EnqueueAndWriteTempRel(Name);
            utility.Write(MemberType);

            saveContext.WriteDICTPointerPlaceholder(Elements);

            utility.Write(BlendOperationTypes.Length);
            utility.Write(BlendOperationTypes);

            utility.Write((uint)EvaluationTiming);

            utility.Write(Unknown);

            /////////////////////////////
            // Begin saving dependent data

            saveContext.SaveAndMarkReference(Elements);
            Elements?.SaveEntries(saveContext);
        }
    }

    public class ChunkDICTAnimGroup : ChunkDICT<DICTObjAnimGroup>
    {
    }
}
