using CGFXModel;
using CGFXModel.Chunks.Model;
using CGFXModel.Chunks.Model.Shape;
using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGFXTestBed
{
    class Program
    {
        //IDEA:
        //	Export USEFUL model data to OBJ (or whatever)	
        //	"Leftovers" need to be stored in an augment XML (byte-encoded chunks that weren't preserved by the OBJ/whatever)	
        //	Both will be required of course to not lose any information	

        #region TestStream to find unreead bytes
        class TestStream : Stream
        {
            long position;
            bool[] map;
            byte[] fileData;
            public TestStream(string filename)
            {
                fileData = File.ReadAllBytes(filename);
                map = new bool[fileData.Length];
                position = 0;
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override long Length => fileData.LongLength;

            public override long Position { get => position; set => position = value; }

            public override void Flush()
            {
                
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                for(var i = 0; i < count; i++)
                {
                    map[position] = true;
                    buffer[offset + i] = fileData[position++];
                }

                return count;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch(origin)
                {
                    case SeekOrigin.Begin:
                        position = offset;
                        break;

                    case SeekOrigin.Current:
                        position += offset;
                        break;

                    case SeekOrigin.End:
                        position = fileData.Length - offset;
                        break;
                }

                return position;
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public class UnreadMap
            {
                public UnreadMap(long start, long end)
                {
                    Start = start;
                    End = end;
                }

                public long Start { get; private set; }
                public long End { get; private set; }
            }

            public List<UnreadMap> GetUnreadMap()
            {
                var result = new List<UnreadMap>();
                var pos = 0;
                var state = 0;

                long curStart = 0;

                while(pos < map.Length)
                {
                    if(state == 0)
                    {
                        if(!map[pos])   // Unread start
                        {
                            curStart = pos;
                            state = 1;
                        }
                    }
                    else if(state == 1)
                    {
                        if(map[pos])    // Unread end
                        {
                            result.Add(new UnreadMap(curStart, pos - 1));
                            state = 0;
                        }
                    }

                    pos++;
                }

                if(state == 1)
                {
                    result.Add(new UnreadMap(curStart, pos - 1));
                }

                return result;
            }
        }
        #endregion

        static void Main(string[] args)
        {
            var input = @"sza.bcres";
            var output = @"sza-test.bcres";

            var stream = new TestStream(input);
            using (var br = new BinaryReader(stream))
            //using (var br = new BinaryReader(File.Open(input, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            using (var bw = new BinaryWriter(File.Open(output, FileMode.Create)))
            {
                CGFXDebug.Init(@"dump.log");
                var cgfx = CGFX.Load(br);

                CGFXDebug.LoadDumpOrderedLog();

                if (br.BaseStream is TestStream)
                {
                    foreach (var um in (br.BaseStream as TestStream).GetUnreadMap())
                    {
                        CGFXDebug.WriteLog($"UNREAD ZONE ${um.Start.ToString("X4")} - ${um.End.ToString("X4")}");
                    }
                }

                cgfx.Save(bw);
                CGFXDebug.Shutdown();
            }

            // TODO -- Future test
            //  Dump several bcres's, and any that have textures, dump the BPP/Unknown vals specs.
            //  Try to find a correlation to BPP/Unknowns vs. the texture format so we can implement
            //  a proper BPP/Unknown setting in SetTexture when trying to absolutely replace a texture.

            // VERIFY
            var inData = File.ReadAllBytes(input);
            var outData = File.ReadAllBytes(output);

            if (inData.Length != outData.Length)
            {
                throw new InvalidOperationException("Mismatched file size!");
            }

            for (var i = 0; i < inData.Length; i++)
            {
                if (inData[i] != outData[i])
                {
                    throw new InvalidOperationException($"Byte mismatch ${i.ToString("X4")}");
                }
            }


            //// EXPERIMENT HERE...
            //var outputB = @"shtL-WIPB.bcres";
            //using (var br = new BinaryReader(File.OpenRead(input)))
            //using (var bw = new BinaryWriter(File.OpenWrite(outputB)))
            //{
            //    var cgfx = CGFX.Load(br);

            //    var model = (DICTObjModel)cgfx.Data.Entries[0].Entries.First().EntryObject;

            //    foreach (var shape in model.Shapes)
            //    {
            //        var vertexBuffer = (shape.VertexBuffers[0] as VertexBufferInterleaved);
            //        var vertices = VertexBufferCodec.GetVertices(shape, 0);



            //        vertexBuffer.RawBuffer = VertexBufferCodec.GetBuffer(vertices, vertexBuffer.Attributes.Select(a => VertexBufferCodec.PICAAttribute.GetPICAAttribute(a)));
            //    }

            //    cgfx.Save(bw);
            //}


        }
    }
}
