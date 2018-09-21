using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CGFXModel.Chunks.Model.Shape
{
    public class Face : ISaveable
    {
        public List<FaceDescriptor> FaceDescriptors { get; private set; }

        private uint[] BufferObjs; //One for each FaceDescriptor
        private uint Flags;
        private uint CommandAlloc;

        public static Face Load(Utility utility)
        {
            var f = new Face();

            CGFXDebug.LoadStart(f, utility);

            f.FaceDescriptors = utility.LoadIndirectObjList(() => FaceDescriptor.Load(utility)).ToList();

            // FIXME CHECKME -- I hope these aren't offsets??? (I just got a single 0x00000000 from one file, so hoping it's nothing)
            f.BufferObjs = utility.LoadIndirectValueList(() => utility.ReadU32());

            // This is a rule?
            if (f.BufferObjs.Length != f.FaceDescriptors.Count)
            {
                throw new InvalidOperationException("Count mismatch between bufferObjCount and numFaceDescriptors");
            }

            f.Flags = utility.ReadU32();
            f.CommandAlloc = utility.ReadU32();

            return f;
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            saveContext.WriteObjectListPointerPlaceholder(FaceDescriptors);

            saveContext.WriteValueListPointerPlaceholder(BufferObjs);

            utility.Write(Flags);
            utility.Write(CommandAlloc);

            /////////////////////////////
            // Begin saving dependent data

            saveContext.SaveAndMarkReference(FaceDescriptors);
            saveContext.SaveAndMarkReference(BufferObjs);

            FaceDescriptors.SaveList(saveContext);
        }
    }

}
