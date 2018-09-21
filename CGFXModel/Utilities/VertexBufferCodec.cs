using CGFXModel.Chunks.Model;
using CGFXModel.Chunks.Model.Shape;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CGFXModel.Utilities
{
    // Imported/adapted from SPICA

    public enum PICAAttributeFormat : uint
    {
        Byte,
        Ubyte,
        Short,
        Float
    }

    static class GfxGLDataTypeExtensions
    {
        public static PICAAttributeFormat ToPICAAttributeFormat(this GLDataType Format)
        {
            switch (Format)
            {
                case GLDataType.GL_BYTE: return PICAAttributeFormat.Byte;
                case GLDataType.GL_UNSIGNED_BYTE: return PICAAttributeFormat.Ubyte;
                case GLDataType.GL_SHORT: return PICAAttributeFormat.Short;
                case GLDataType.GL_FLOAT: return PICAAttributeFormat.Float;

                default: throw new ArgumentException($"Invalid format {Format}!");
            }
        }
    }


    public static class VertexBufferCodec
    {
        public struct BoneIndices
        {
            public int b0;
            public int b1;
            public int b2;
            public int b3;

            public int this[int Index]
            {
                get
                {
                    switch (Index)
                    {
                        case 0: return b0;
                        case 1: return b1;
                        case 2: return b2;
                        case 3: return b3;

                        default: throw new ArgumentOutOfRangeException();
                    }
                }
                set
                {
                    switch (Index)
                    {
                        case 0: b0 = value; break;
                        case 1: b1 = value; break;
                        case 2: b2 = value; break;
                        case 3: b3 = value; break;

                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        public struct BoneWeights
        {
            public float w0;
            public float w1;
            public float w2;
            public float w3;

            public float this[int Index]
            {
                get
                {
                    switch (Index)
                    {
                        case 0: return w0;
                        case 1: return w1;
                        case 2: return w2;
                        case 3: return w3;

                        default: throw new ArgumentOutOfRangeException();
                    }
                }
                set
                {
                    switch (Index)
                    {
                        case 0: w0 = value; break;
                        case 1: w1 = value; break;
                        case 2: w2 = value; break;
                        case 3: w3 = value; break;

                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        public struct PICAVertex
        {
            public Vector4 Position;
            public Vector4 Normal;
            public Vector4 Tangent;
            public Vector4 Color;
            public Vector4 TexCoord0;
            public Vector4 TexCoord1;
            public Vector4 TexCoord2;
            public BoneIndices Indices;
            public BoneWeights Weights;
        }

        public struct PICAAttribute
        {
            public VertexBuffer.PICAAttributeName Name;
            public PICAAttributeFormat Format;
            public int Elements;
            public float Scale;

            public static PICAAttribute GetPICAAttribute(VertexAttribute vertexAttribute)
            {
                return new PICAAttribute
                {
                    Name = vertexAttribute.AttrName,
                    Format = vertexAttribute.Format.ToPICAAttributeFormat(),
                    Elements = vertexAttribute.Elements,
                    Scale = vertexAttribute.Scale
                };
            }

            public static List<PICAAttribute> GetAttributes(params VertexBuffer.PICAAttributeName[] Names)
            {
                List<PICAAttribute> Output = new List<PICAAttribute>();

                foreach (VertexBuffer.PICAAttributeName Name in Names)
                {
                    switch (Name)
                    {
                        case VertexBuffer.PICAAttributeName.Position:
                        case VertexBuffer.PICAAttributeName.Normal:
                        case VertexBuffer.PICAAttributeName.Tangent:
                            Output.Add(new PICAAttribute()
                            {
                                Name = Name,
                                Format = PICAAttributeFormat.Float,
                                Elements = 3,
                                Scale = 1
                            });
                            break;

                        case VertexBuffer.PICAAttributeName.TexCoord0:
                        case VertexBuffer.PICAAttributeName.TexCoord1:
                        case VertexBuffer.PICAAttributeName.TexCoord2:
                            Output.Add(new PICAAttribute()
                            {
                                Name = Name,
                                Format = PICAAttributeFormat.Float,
                                Elements = 2,
                                Scale = 1
                            });
                            break;

                        case VertexBuffer.PICAAttributeName.Color:
                            Output.Add(new PICAAttribute()
                            {
                                Name = VertexBuffer.PICAAttributeName.Color,
                                Format = PICAAttributeFormat.Ubyte,
                                Elements = 4,
                                Scale = 1f / 255
                            });
                            break;

                        case VertexBuffer.PICAAttributeName.BoneIndex:
                            Output.Add(new PICAAttribute()
                            {
                                Name = VertexBuffer.PICAAttributeName.BoneIndex,
                                Format = PICAAttributeFormat.Ubyte,
                                Elements = 4,
                                Scale = 1
                            });
                            break;

                        case VertexBuffer.PICAAttributeName.BoneWeight:
                            Output.Add(new PICAAttribute()
                            {
                                Name = VertexBuffer.PICAAttributeName.BoneWeight,
                                Format = PICAAttributeFormat.Ubyte,
                                Elements = 4,
                                Scale = 0.01f
                            });
                            break;
                    }
                }

                return Output;
            }
        }

        private static void AlignStream(Stream Strm, PICAAttributeFormat Fmt)
        {
            //Short and Float types needs to be aligned into 2 bytes boundaries.
            //TODO: Float may actually need a 4 bytes alignment, need to test later.
            if (Fmt != PICAAttributeFormat.Byte &&
                Fmt != PICAAttributeFormat.Ubyte)
            {
                Strm.Position += Strm.Position & 1;
            }
        }


        public static PICAVertex[] GetVertices(DICTObjShape shape, int vertexBufferIndex)
        {
            if(!(shape.VertexBuffers[vertexBufferIndex] is VertexBufferInterleaved))
            {
                throw new InvalidOperationException($"Currently only supports VertexBufferInterleaved, but this is a {shape.VertexBuffers[vertexBufferIndex].GetType().Name}");
            }

            var vertexBuffer = (VertexBufferInterleaved)shape.VertexBuffers[vertexBufferIndex];

            if (vertexBuffer.RawBuffer.Length == 0) return new PICAVertex[0];

            float[] Elems = new float[4];

            PICAVertex[] Output = new PICAVertex[vertexBuffer.RawBuffer.Length / vertexBuffer.VertexStride];

            using (MemoryStream MS = new MemoryStream(vertexBuffer.RawBuffer))
            {
                BinaryReader Reader = new BinaryReader(MS);

                for (int Index = 0; Index < Output.Length; Index++)
                {
                    PICAVertex Out = new PICAVertex();

                    MS.Seek(Index * vertexBuffer.VertexStride, SeekOrigin.Begin);

                    int bi = 0;
                    int wi = 0;

                    foreach (PICAAttribute Attrib in vertexBuffer.Attributes.Select(a => PICAAttribute.GetPICAAttribute(a)))
                    {
                        AlignStream(MS, Attrib.Format);

                        for (int Elem = 0; Elem < Attrib.Elements; Elem++)
                        {
                            switch (Attrib.Format)
                            {
                                case PICAAttributeFormat.Byte: Elems[Elem] = Reader.ReadSByte(); break;
                                case PICAAttributeFormat.Ubyte: Elems[Elem] = Reader.ReadByte(); break;
                                case PICAAttributeFormat.Short: Elems[Elem] = Reader.ReadInt16(); break;
                                case PICAAttributeFormat.Float: Elems[Elem] = Reader.ReadSingle(); break;
                            }
                        }

                        Vector4 v = new Vector4(Elems[0], Elems[1], Elems[2], Elems[3]);

                        v *= Attrib.Scale;

                        if (Attrib.Name == VertexBuffer.PICAAttributeName.Position)
                        {
                            v.X += shape.PositionOffset.X;
                            v.Y += shape.PositionOffset.Y;
                            v.Z += shape.PositionOffset.Z;
                        }

                        switch (Attrib.Name)
                        {
                            case VertexBuffer.PICAAttributeName.Position: Out.Position = v; break;
                            case VertexBuffer.PICAAttributeName.Normal: Out.Normal = v; break;
                            case VertexBuffer.PICAAttributeName.Tangent: Out.Tangent = v; break;
                            case VertexBuffer.PICAAttributeName.Color: Out.Color = v; break;
                            case VertexBuffer.PICAAttributeName.TexCoord0: Out.TexCoord0 = v; break;
                            case VertexBuffer.PICAAttributeName.TexCoord1: Out.TexCoord1 = v; break;
                            case VertexBuffer.PICAAttributeName.TexCoord2: Out.TexCoord2 = v; break;

                            case VertexBuffer.PICAAttributeName.BoneIndex:
                                Out.Indices[bi++] = (int)v.X; if (Attrib.Elements == 1) break;
                                Out.Indices[bi++] = (int)v.Y; if (Attrib.Elements == 2) break;
                                Out.Indices[bi++] = (int)v.Z; if (Attrib.Elements == 3) break;
                                Out.Indices[bi++] = (int)v.W; break;

                            case VertexBuffer.PICAAttributeName.BoneWeight:
                                Out.Weights[wi++] = v.X; if (Attrib.Elements == 1) break;
                                Out.Weights[wi++] = v.Y; if (Attrib.Elements == 2) break;
                                Out.Weights[wi++] = v.Z; if (Attrib.Elements == 3) break;
                                Out.Weights[wi++] = v.W; break;
                        }
                    }

                    // TODO -- missing FixedAttribute support

                    //if (Mesh.FixedAttributes != null)
                    //{
                    //    bool HasFixedIndices = Mesh.FixedAttributes.Any(x => x.Name == VertexBuffer.PICAAttributeName.BoneIndex);
                    //    bool HasFixedWeights = Mesh.FixedAttributes.Any(x => x.Name == VertexBuffer.PICAAttributeName.BoneWeight);

                    //    if (HasFixedIndices || HasFixedWeights)
                    //    {
                    //        foreach (PICAFixedAttribute Attr in Mesh.FixedAttributes)
                    //        {
                    //            switch (Attr.Name)
                    //            {
                    //                case VertexBuffer.PICAAttributeName.BoneIndex:
                    //                    Out.Indices[0] = (int)Attr.Value.X;
                    //                    Out.Indices[1] = (int)Attr.Value.Y;
                    //                    Out.Indices[2] = (int)Attr.Value.Z;
                    //                    break;

                    //                case VertexBuffer.PICAAttributeName.BoneWeight:
                    //                    Out.Weights[0] = Attr.Value.X;
                    //                    Out.Weights[1] = Attr.Value.Y;
                    //                    Out.Weights[2] = Attr.Value.Z;
                    //                    break;
                    //            }
                    //        }
                    //    }
                    //}

                    Output[Index] = Out;
                }
            }

            return Output;
        }

        private static void Write(BinaryWriter Writer, PICAAttribute Attrib, Vector4 v, int i)
        {
            switch (i)
            {
                case 0: Write(Writer, Attrib, v.X); break;
                case 1: Write(Writer, Attrib, v.Y); break;
                case 2: Write(Writer, Attrib, v.Z); break;
                case 3: Write(Writer, Attrib, v.W); break;
            }
        }

        private static void Write(BinaryWriter Writer, PICAAttribute Attrib, float Value)
        {
            Value /= Attrib.Scale;

            if (Attrib.Format != PICAAttributeFormat.Float)
            {
                //Due to float lack of precision it's better to round the number,
                //because directly casting it will always use the lowest number that
                //may cause issues for values that float can't represent (like 0.1).
                Value = (float)Math.Round(Value);
            }

            switch (Attrib.Format)
            {
                case PICAAttributeFormat.Byte: Writer.Write((sbyte)Value); break;
                case PICAAttributeFormat.Ubyte: Writer.Write((byte)Value); break;
                case PICAAttributeFormat.Short: Writer.Write((short)Value); break;
                case PICAAttributeFormat.Float: Writer.Write(Value); break;
            }
        }

        public static byte[] GetBuffer(IEnumerable<PICAVertex> Vertices, IEnumerable<PICAAttribute> Attributes)
        {
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                foreach (PICAVertex Vertex in Vertices)
                {
                    int bi = 0;
                    int wi = 0;

                    foreach (PICAAttribute Attrib in Attributes)
                    {
                        AlignStream(MS, Attrib.Format);

                        for (int i = 0; i < Attrib.Elements; i++)
                        {
                            switch (Attrib.Name)
                            {
                                case VertexBuffer.PICAAttributeName.Position: Write(Writer, Attrib, Vertex.Position, i); break;
                                case VertexBuffer.PICAAttributeName.Normal: Write(Writer, Attrib, Vertex.Normal, i); break;
                                case VertexBuffer.PICAAttributeName.Tangent: Write(Writer, Attrib, Vertex.Tangent, i); break;
                                case VertexBuffer.PICAAttributeName.Color: Write(Writer, Attrib, Vertex.Color, i); break;
                                case VertexBuffer.PICAAttributeName.TexCoord0: Write(Writer, Attrib, Vertex.TexCoord0, i); break;
                                case VertexBuffer.PICAAttributeName.TexCoord1: Write(Writer, Attrib, Vertex.TexCoord1, i); break;
                                case VertexBuffer.PICAAttributeName.TexCoord2: Write(Writer, Attrib, Vertex.TexCoord2, i); break;
                                case VertexBuffer.PICAAttributeName.BoneIndex: Write(Writer, Attrib, Vertex.Indices[bi++]); break;
                                case VertexBuffer.PICAAttributeName.BoneWeight: Write(Writer, Attrib, Vertex.Weights[wi++]); break;

                                default: Write(Writer, Attrib, 0); break;
                            }
                        }
                    }
                }

                return MS.ToArray();
            }
        }
    }
}
