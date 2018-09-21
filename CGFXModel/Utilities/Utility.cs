using CGFXModel.Chunks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CGFXModel.Utilities
{
    public enum Endianness
    {
        Little,
        Big
    }

    public static class ChunkListHelper
    {
        public static TChunk GetChunk<TChunk>(this List<Chunk> chunkList)
            where TChunk : Chunk
        {
            TChunk chunk;
            var chunkInList = chunkList.Where(c => c is TChunk);

            if (!chunkInList.Any())
            {
                throw new ArgumentException($"Requested chunk of Type {typeof(TChunk).Name} not found (did you forget to add it?)");
            }
            else
            {
                chunk = (TChunk)chunkInList.First();

                if (chunkInList.Count() > 1)
                {
                    throw new ArgumentException($"Ambiguous {Utility.GetMagicString(chunk.Magic)} chunks");
                }
            }

            return chunk;
        }
    }

    public class Utility
    {
        private BinaryReader br;
        private BinaryWriter bw;
        public Endianness Endianness { get; private set; }
        private Stack<uint> readPositionStack;
        private Stack<uint> writePositionStack;
        public int ReadPositionStackDepth { get { return readPositionStack.Count; } }

        public Utility(BinaryReader br, BinaryWriter bw, Endianness endianness = Endianness.Little)
        {
            this.br = br;
            this.bw = bw;
            this.Endianness = endianness;
            readPositionStack = new Stack<uint>();
            writePositionStack = new Stack<uint>();
        }

        public uint GetReadPosition()
        {
            return (uint)br.BaseStream.Position;
        }

        public void SetReadPosition(uint position)
        {
            // Assumption that if you're setting zero here you're doing a NULL read!
            if(position == 0)
            {
                throw new InvalidOperationException("Null read attempted!");
            }

            br.BaseStream.Seek(position, SeekOrigin.Begin);
        }

        public void PushReadPosition()
        {
            readPositionStack.Push(GetReadPosition());
        }

        public void PopReadPosition()
        {
            SetReadPosition(readPositionStack.Pop());
        }

        public uint GetWritePosition()
        {
            return (uint)bw.BaseStream.Position;
        }

        public void SetWritePosition(uint position)
        {
            // Assumption that if you're setting zero here you're doing a NULL write!
            if (position == 0)
            {
                throw new InvalidOperationException("Null write attempted!");
            }

            bw.BaseStream.Seek(position, SeekOrigin.Begin);
        }

        public void PushWritePosition()
        {
            writePositionStack.Push(GetWritePosition());
        }

        public void PopWritePosition()
        {
            SetWritePosition(writePositionStack.Pop());
        }

        // Align to nearest BlockSize; apparently some data is simply aligned as such!
        public void AlignRead(int BlockSize)
        {
            long Remainder = br.BaseStream.Position % BlockSize;

            if (Remainder != 0)
            {
                //br.BaseStream.Seek(BlockSize - Remainder, SeekOrigin.Current);
                ReadBytes((uint)(BlockSize - Remainder));   // Throwaway read because this signals a test stream object I'm using that these bytes weren't simply missed
            }
        }

        // stepBack: Provided for IMAG chunk, which aligns its data passed its header to 128 byte boundary
        public void AlignWrite(int BlockSize, int stepBack = 0)
        {
            if(stepBack > BlockSize)
            {
                throw new ArgumentException($"stepBack ({stepBack}) cannot exceed BlockSize {BlockSize}", "stepBack");
            }

            long Remainder = bw.BaseStream.Position % BlockSize;

            if (Remainder != 0)
            {
                var pad = (uint)(BlockSize - Remainder) - stepBack;
                while (pad-- > 0)
                {
                    Write((byte)0);
                }
            }
        }

        // Read a relative offset and compute the absolute
        // NOTE: Unused relative offsets are 0, so this will return 0 so it can be detected
        public uint ReadOffset()
        {
            var positionAbs = GetReadPosition();
            var offset = ReadU32();
            return (offset != 0) ? (positionAbs + offset) : 0U;
        }

        // NOTE: This doesn't write an in-place self-offset (because that doesn't really make sense),
        // but rather jumps to location "target" and writes a self-relative offset to that location
        // to get back to where we stand right now. 
        public void WriteOffset(uint target, bool isNull = false)
        {
            var currentPos = GetWritePosition();

            PushWritePosition();
            SetWritePosition(target);

            var relativeOffset = !isNull ? (currentPos - target) : 0U;
            Write(relativeOffset);

            PopWritePosition();
        }

        public void ReadEndiannessByteMarker()
        {
            var data = br.ReadBytes(2);

            if (data[0] == 0xFF && data[1] == 0xFE)
            {
                Endianness = Endianness.Little;
            }
            else
            {
                Endianness = Endianness.Big;
            }
        }

        public void WriteEndiannessByteMarker()
        {
            if(Endianness == Endianness.Little)
            {
                bw.Write(new byte[] { 0xFF, 0xFE });
            }
            else
            {
                bw.Write(new byte[] { 0xFE, 0xFF });
            }
        }

        public byte[] ReadBytes(uint length)
        {
            return br.ReadBytes((int)length);
        }

        public int[] ReadInts(uint count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadI32();
            }

            return result;
        }

        public ushort[] ReadU16Ints(uint count)
        {
            var result = new ushort[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadU16();
            }

            return result;
        }

        public uint[] ReadUInts(uint count)
        {
            var result = new uint[count];

            for(var i = 0; i < count; i++)
            {
                result[i] = ReadU32();
            }

            return result;
        }

        public float[] ReadFloats(uint count)
        {
            var result = new float[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadFloat();
            }

            return result;
        }

        public void Write(byte[] data)
        {
            bw.Write(data);
        }

        public void Write(int[] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                bw.Write(data[i]);
            }
        }

        public void Write(ushort[] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                bw.Write(data[i]);
            }
        }

        public void Write(uint[] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                bw.Write(data[i]);
            }
        }

        public void Write(float[] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                bw.Write(data[i]);
            }
        }

        public float ReadFloat()
        {
            // FIXME -- doesn't respect endianness
            return br.ReadSingle();
        }

        public void Write(float data)
        {
            // FIXME -- doesn't respect endianness
            bw.Write(data);
        }


        public byte ReadByte()
        {
            return br.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return (sbyte)br.ReadByte();
        }

        public ushort GetU16(byte[] data, int offset = 0)
        {
            if (Endianness == Endianness.Little)
            {
                return (ushort)
                    (
                        (data[offset + 0] << 0) |
                        (data[offset + 1] << 8)
                    );
            }
            else
            {
                return (ushort)
                    (
                        (data[offset + 0] << 8) |
                        (data[offset + 1] << 0)
                    );
            }
        }

        public byte[] GetU16(ushort value)
        {
            if (Endianness == Endianness.Little)
            {
                return new byte[]
                {
                    (byte)((value >> 0) & 0xFF),
                    (byte)((value >> 8) & 0xFF)
                };
            }
            else
            {
                return new byte[]
                {
                    (byte)((value >> 8) & 0xFF),
                    (byte)((value >> 0) & 0xFF)
                };
            }
        }

        public ushort ReadU16()
        {
            var data = br.ReadBytes(2);
            return GetU16(data);
        }

        public void Write(int data)
        {
            bw.Write(GetU32((uint)data));
        }

        public void Write(ushort data)
        {
            bw.Write(GetU16(data));
        }

        public void Write(sbyte data)
        {
            bw.Write(data);
        }

        public void Write(byte data)
        {
            bw.Write(data);
        }

        public uint GetU32(byte[] data, int offset = 0)
        {
            if (Endianness == Endianness.Little)
            {
                return (uint)
                    (
                        (data[offset + 0] << 0) |
                        (data[offset + 1] << 8) |
                        (data[offset + 2] << 16) |
                        (data[offset + 3] << 24)
                    );
            }
            else
            {
                return (uint)
                    (
                        (data[offset + 0] << 24) |
                        (data[offset + 1] << 16) |
                        (data[offset + 2] << 8) |
                        (data[offset + 3] << 0)
                    );
            }
        }

        public int GetI32(byte[] data, int offset = 0)
        {
            return (int)GetU32(data, offset);
        }

        public byte[] GetU32(uint value)
        {
            if (Endianness == Endianness.Little)
            {
                return new byte[]
                {
                    (byte)((value >> 0) & 0xFF),
                    (byte)((value >> 8) & 0xFF),
                    (byte)((value >> 16) & 0xFF),
                    (byte)((value >> 24) & 0xFF)
                };
            }
            else
            {
                return new byte[]
                {
                    (byte)((value >> 24) & 0xFF),
                    (byte)((value >> 16) & 0xFF),
                    (byte)((value >> 8) & 0xFF),
                    (byte)((value >> 0) & 0xFF)
                };
            }
        }

        public uint ReadU32()
        {
            var data = br.ReadBytes(4);
            return GetU32(data);
        }

        public int ReadI32()
        {
            return (int)ReadU32();
        }

        public void Write(uint data)
        {
            bw.Write(GetU32(data));
        }

        // Reads a string assuming that we're sitting at a relative offset into the string table
        public string ReadString()
        {
            var result = (string)null;
            var currentPos = GetReadPosition();
            var offset = ReadU32();     // Not using ReadOffset() here because it might be zero, meaning null string

            if (offset != 0)    // if zero, null string
            {
                PushReadPosition();

                // Jump to string
                SetReadPosition(currentPos + offset);

                // Read string up to null terminator
                var sb = new StringBuilder();
                byte ch;

                while ((ch = ReadByte()) != 0)
                {
                    sb.Append((char)ch);
                }

                result = sb.ToString();

                PopReadPosition();
            }

            return result;
        }

        public static uint MakeMagic(byte[] data)
        {
            if (data.Length != 4)
            {
                throw new ArgumentException("Magic ID must be exactly 4 bytes", nameof(data));
            }

            // This needs to be done reliably against any and all endianness considerations!
            // (Especially since the original magic is read BEFORE the endianness byte marker)
            return (uint)((data[0] << 0) | (data[1] << 8) | (data[2] << 16) | (data[3] << 24));
        }

        public static byte[] GetMagic(string magic)
        {
            return GetMagic(MakeMagic(magic));
        }

        public static byte[] GetMagic(uint magic)
        {
            return new byte[]
            {
                (byte)((magic >> 0) & 0xFF),
                (byte)((magic >> 8) & 0xFF),
                (byte)((magic >> 16) & 0xFF),
                (byte)((magic >> 24) & 0xFF)
            };
        }

        public static uint MakeMagic(string magic)
        {
            if (magic.Length != 4)
            {
                throw new ArgumentException("Magic ID must be exactly 4 characters", nameof(magic));
            }

            return MakeMagic(Encoding.ASCII.GetBytes(magic));
        }

        public static string GetMagicString(uint magic)
        {
            var data = GetMagic(magic);
            return Encoding.ASCII.GetString(data);
        }

        public uint PeekMagic()
        {
            uint magic;

            PushReadPosition();
            magic = ReadMagic();
            PopReadPosition();

            return magic;
        }

        public uint ReadMagic()
        {
            var data = br.ReadBytes(4);
            return MakeMagic(data);
        }

        public void WriteMagic(uint magic)
        {
            Write(GetMagic(magic));
        }

        public void WriteMagic(string magic)
        {
            Write(GetMagic(magic));
        }

        public static bool CheckBit(uint flags, uint bit)
        {
            return (flags & 0x00000080U) != 0;
        }

        public static uint SetBit(uint flags, uint bit, bool set)
        {
            return ((flags & ~bit) | (set ? bit : 0x00000000U));
        }

        // Load a single non-ChunkDICTObject from an offset pointer
        public void LoadIndirect(Action loadAction)
        {
            // Read offset to this object
            var offset = ReadOffset();
            if (offset > 0)
            {
                PushReadPosition();
                SetReadPosition(offset);

                loadAction();

                PopReadPosition();
            }
        }

        // Loads a list of (simple) values starting from an indirect location
        public TVal[] LoadIndirectValueList<TVal>(Func<TVal> loadAction)
        {
            TVal[] result = null;

            CGFXDebug.LoadStart($"HEADER of list of {typeof(TVal).Name} [LoadIndirectValueList]", this);
            var numObjects = ReadU32();
            var offset = ReadOffset();

            if (offset > 0)
            {
                PushReadPosition();
                SetReadPosition(offset);

                CGFXDebug.LoadStart($"List of {typeof(TVal).Name} [LoadIndirectValueList]", this);

                result = new TVal[numObjects];
                for (var objIndex = 0; objIndex < numObjects; objIndex++)
                {
                    result[objIndex] = loadAction();
                }

                PopReadPosition();
            }

            return result;
        }

        // Load a list of non-ChunkDICTObject arbitrary objects from a list of pointers
        public TObj[] LoadIndirectObjList<TObj>(Func<TObj> loadAction)
        {
            TObj[] result = null;

            CGFXDebug.LoadStart($"HEADER of list of {typeof(TObj).Name} [LoadIndirectObjList]", this);

            var numObjects = ReadU32();
            var offset = ReadOffset();

            if (offset > 0)
            {
                PushReadPosition();
                SetReadPosition(offset);

                CGFXDebug.LoadStart($"List of {typeof(TObj).Name} [LoadIndirectObjList]", this);

                result = new TObj[numObjects];
                for (var objIndex = 0; objIndex < numObjects; objIndex++)
                {
                    var objOffset = ReadOffset();
                    PushReadPosition();
                    SetReadPosition(objOffset);

                    result[objIndex] = loadAction();

                    PopReadPosition();
                }

                PopReadPosition();
            }

            return result;
        }

        // Load standard pattern of offset to another object elsewhere (not inline)
        public TDICTObj LoadDICTObj<TDICTObj>()
            where TDICTObj : ChunkDICTObject, new()
        {
            TDICTObj result = null;

            LoadIndirect(() =>
            {
                var obj = new TDICTObj();
                obj.Load(this);
                result = obj;
            });

            return result;
        }

        // Load the standard pattern of [UINT COUNT][UINT OFFSET TO TABLE] -> [UINT OFFSET 1][UINT OFFSET 2]...
        public IEnumerable<TDICTObj> LoadDICTObjList<TDICTObj>()
            where TDICTObj : ChunkDICTObject, new()
        {
            TDICTObj[] result = null;

            CGFXDebug.LoadStart($"HEADER of list of {typeof(TDICTObj).Name} [LoadDICTObjList]", this);

            var count = ReadU32();
            var locationTable = ReadOffset();

            if (count > 0)
            {
                result = new TDICTObj[count];

                PushReadPosition();
                SetReadPosition(locationTable);

                CGFXDebug.LoadStart($"List of {typeof(TDICTObj).Name} [LoadDICTObjList]", this);

                for (var index = 0; index < count; index++)
                {
                    result[index] = LoadDICTObj<TDICTObj>();
                }

                PopReadPosition();
            }

            return result;
        }

        // Standard pattern used to load a dictionary object
        public TDICT LoadDICTFromOffset<TDICT>()
            where TDICT : Chunk, IChunkDICT, new()
        {
            TDICT dict = null;

            // Number of entries in DICT
            var numEntries = ReadU32();

            // Offset to dictionary chunk start
            var offsetToDict = ReadOffset();

            // NOTE: If numEntries == 0, there's nothing to fetch, so we'll skip trying to read this DICT...
            if (numEntries > 0)
            {
                // Remember where we are so we can come back after reading DICT!
                PushReadPosition();
                SetReadPosition(offsetToDict);

                // Jump to and read DICT...
                dict = Chunk.Load<TDICT>(this);

                // Return back to where we were...
                PopReadPosition();
            }

            return dict;
        }
    }
}
