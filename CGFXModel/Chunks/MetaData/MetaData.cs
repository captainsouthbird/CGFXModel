using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGFXModel.Chunks.MetaData
{
    public enum MetaDataType : uint
    {
        Single,
        Integer,
        String,
        Vector3,
        Color
    }

    public enum StringFormat : uint
    {
        Ascii,
        Utf8,
        Utf16LE,
        Utf16BE
    }

    public class MetaDataSingle : MetaDataBase
    {
        public List<float> Values { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            var count = utility.ReadU32();
            Values = utility.ReadFloats(count).ToList();
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.Utility.Write(Values.Count);
            saveContext.Utility.Write(Values.ToArray());
        }
    }

    public class MetaDataColor : MetaDataBase
    {
        public List<Vector4> Values { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            var count = utility.ReadU32();
            Values = new List<Vector4>((int)count);

            for(var i = 0; i < count; i++)
            {
                Values.Add(Vector4.Read(utility));
            }
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.Utility.Write(Values.Count);

            for (var i = 0; i < Values.Count; i++)
            {
                Values[i].Write(saveContext.Utility);
            }
        }
    }

    public class MetaDataInteger : MetaDataBase
    {
        public List<int> Values { get; private set; }

        protected override void LoadInternal(Utility utility)
        {
            var count = utility.ReadU32();
            Values = utility.ReadInts(count).ToList();
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            saveContext.Utility.Write(Values.Count);
            saveContext.Utility.Write(Values.ToArray());
        }
    }

    public class MetaDataString : MetaDataBase
    {
        public StringFormat Format { get; private set; }

        //[Inline]
        //[TypeChoiceName("Format")]
        //[TypeChoice((uint)GfxStringFormat.Ascii, typeof(List<string>))]
        //[TypeChoice((uint)GfxStringFormat.Utf8, typeof(List<GfxStringUtf8>))]
        //[TypeChoice((uint)GfxStringFormat.Utf16LE, typeof(List<GfxStringUtf16LE>))]
        //[TypeChoice((uint)GfxStringFormat.Utf16BE, typeof(List<GfxStringUtf16BE>))]
        //public readonly IList Values;

        //public GfxMetaDataString()
        //{
        //    Values = new List<string>();
        //}

        protected override void LoadInternal(Utility utility)
        {
            // Whew... so this has a subtype??

            throw new NotImplementedException("String MetaData NOT IMPLEMENTED (but it could be, see SPICA tips)");
        }

        protected override void SaveInternal(SaveContext saveContext)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class MetaDataBase : ISaveable
    {
        public uint TypeId { get; private set; }
        public string Name { get; set; }
        public MetaDataType Type { get; private set; }

        public static MetaDataBase Load(Utility utility)
        {
            MetaDataBase result = null;

            // We need to load the TypeId because it tells us what kind of MetaData to expect...
            var typeId = utility.ReadU32();
            if(typeId == 0x80000000u)
            {
                result = new MetaDataSingle();
            }
            else if(typeId == 0x40000000)
            {
                result = new MetaDataColor();
            }
            else if(typeId == 0x20000000)
            {
                result = new MetaDataInteger();
            }
            else if(typeId == 0x10000000)
            {
                result = new MetaDataString();
            }

            if(result == null)
            {
                throw new NotImplementedException($"Unknown MetaData type {typeId.ToString("X8")}!");
            }

            CGFXDebug.LoadStart(result, utility, hasDynamicType: true);

            // Otherwise, proceed...
            result.TypeId = typeId;
            result.Name = utility.ReadString();
            result.Type = (MetaDataType)utility.ReadU32();

            result.LoadInternal(utility);

            return result;
        }

        protected abstract void LoadInternal(Utility utility);

        public virtual void Save(SaveContext saveContext)
        {
            CGFXDebug.SaveStart(this, saveContext);

            var utility = saveContext.Utility;

            utility.Write(TypeId);
            saveContext.StringTable.EnqueueAndWriteTempRel(Name);
            utility.Write((uint)Type);

            SaveInternal(saveContext);
        }

        protected abstract void SaveInternal(SaveContext saveContext);
    }

    public class DICTObjMetaData : ChunkDICTObject
    {
        public override string Magic => throw new NotImplementedException();    // NOT USED

        public MetaDataBase Content { get; private set; }

        public override void Load(Utility utility)
        {
            // NOTE: This is not actually a shift to another object, this is just 
            // for convenience since the type is dynamic and my system doesn't
            // really support that.
            Content = MetaDataBase.Load(utility);
        }

        public override void Save(SaveContext saveContext)
        {
            // Not actually an indirect to another object, as noted in Load,
            // so we're saving inline
            Content.Save(saveContext);
        }
    }

    public class ChunkDICTMetaData : ChunkDICT<DICTObjMetaData>
    {

    }
}
