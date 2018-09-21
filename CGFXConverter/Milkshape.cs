using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CGFXConverter
{
    public interface IMilkShapeCommentable
    {
        string Comment { get; set; }
    }

    public class MilkShape : IMilkShapeCommentable
    {
        // Based on the MilkShape 3D 1.8.5 File Format Specification
        //
        // Mesh Transformation:
        // 
        // 0. Build the transformation matrices from the rotation and position
        // 1. Multiply the vertices by the inverse of local reference matrix (lmatrix0)
        // 2. then translate the result by (lmatrix0 * keyFramesTrans)
        // 3. then multiply the result by (lmatrix0 * keyFramesRot)
        //
        // For normals skip step 2.
        //
        //
        //
        // NOTE:  this file format may change in future versions!
        //
        //
        // - Mete Ciragan
        //

        const string Magic = "MS3D000000";

        //
        // max values
        //
        const int MAX_VERTICES    = 65534;
        const int MAX_TRIANGLES   = 65534;
        const int MAX_GROUPS      = 255;
        const int MAX_MATERIALS   = 128;
        const int MAX_JOINTS      = 128;

        //
        // flags
        //
        [Flags]
        public enum MilkshapeObjectFlags : byte
        {
            SELECTED = 1,
            HIDDEN = 2,
            SELECTED2 = 4,
            DIRTY = 8
        };

        public List<ms3d_vertex_t> Vertices { get; private set; }
        public List<ms3d_triangle_t> Triangles { get; private set; }
        public List<ms3d_group_t> Groups { get; private set; }
        public List<ms3d_material_t> Materials { get; private set; }
        public List<ms3d_joint_t> Joints { get; private set; }

        // Preserving these values for the format but not terribly useful for my purposes
        float fAnimationFPS;
        float fCurrentTime;
        int iTotalFrames;

        public string Comment { get; set; }     // Comment on the model itself, if any

        // NOTE: This is from extended model data "ms3d_model_ex_t" per spec:
        float jointSize;    // joint size, since subVersion == 1
        int transparencyMode; // 0 = simple, 1 = depth buffered with alpha ref, 2 = depth sorted triangles, since subVersion == 1
        float alphaRef; // alpha reference value for transparencyMode = 1, since subVersion == 1

        private static string ReadString(Utility utility, uint charCount)
        {
            var str = Encoding.ASCII.GetString(utility.ReadBytes(charCount));

            // Milkshape uses fixed length strings, and extraneous character spaces
            // are nulls, so to properly represent this, we need to cut if off at first null
            var indexOfNull = str.IndexOf('\0');
            if(indexOfNull >= 0)
            {
                str = str.Substring(0, indexOfNull);
            }

            return str;
        }

        private static void WriteString(Utility utility, string str, uint charCount)
        {
            var data = (str != null) ? Encoding.ASCII.GetBytes(str) : new byte[0];

            if(data.Length > charCount)
            {
                throw new InvalidOperationException($"Milkshape WriteString: Exceeded max length of string; string is {str.Length} characters, but MilkShape only supports {charCount} characters in this context");
            }
            else if(data.Length < charCount)
            {
                // NOTE: I'm not sure if MilkShape NEEDS the null terminator (effectively reducing the max length of the input string to charCount - 1)
                // HOWEVER, comments are an exception that do NOT store the null terminator as they have lengths instead

                // If we're short on characters, we need to pad with nulls
                var padding = (int)(charCount - data.Length);
                data = data.Concat(Enumerable.Repeat<byte>(0, padding)).ToArray();
            }

            utility.Write(data);
        }

        private static Vector2[] ReadTextureCoordinates(Utility utility)
        {
            // MilkShape stores 3 Ss and then 3 Ts
            var s = utility.ReadFloats(3);
            var t = utility.ReadFloats(3);

            return new Vector2[]
            {
                new Vector2(s[0], t[0]),
                new Vector2(s[1], t[1]),
                new Vector2(s[2], t[2])
            };
        }

        public MilkShape()
        {
            Vertices = new List<ms3d_vertex_t>();
            Triangles = new List<ms3d_triangle_t>();
            Groups = new List<ms3d_group_t>();
            Materials = new List<ms3d_material_t>();
            Joints = new List<ms3d_joint_t>();
        }

        public static MilkShape Load(BinaryReader br)
        {
            var ms = new MilkShape();
            var utility = new Utility(br, null, Endianness.Little);     // Milkshape is tied to Win32, so definitely little endian

            // First comes the header (sizeof(ms3d_header_t) == 14)
            var magic = ReadString(utility, 10);
            if(magic != Magic)
            {
                throw new InvalidOperationException($"Milkshape Load: Bad magic, expected {Magic}, got {magic}");
            }

            var version = utility.ReadU32();
            if(version != 4)
            {
                throw new InvalidOperationException($"Milkshape Load: Unsupported version, expected 4, got {version}");
            }

            // Then comes the number of vertices
            var nNumVertices = utility.ReadU16();

            // Then come nNumVertices times ms3d_vertex_t structs (sizeof(ms3d_vertex_t) == 15)
            ms.Vertices = new List<ms3d_vertex_t>(nNumVertices);
            for(var v = 0; v < nNumVertices; v++)
            {
                var vertex = new ms3d_vertex_t();
                vertex.Flags = (MilkshapeObjectFlags)utility.ReadByte();    // SELECTED | SELECTED2 | HIDDEN
                vertex.Position = Vector3.Read(utility);

                // NOTE: I'm merging the different specs / extended attributes here; will look confusing
                vertex.BoneIdsAndWeights[0].BoneId = utility.ReadSByte();

                vertex.ReferenceCount = utility.ReadByte();

                ms.Vertices.Add(vertex);
            }

            // Then comes the number of triangles
            var nNumTriangles = utility.ReadU16(); // 2 bytes

            // Then come nNumTriangles times ms3d_triangle_t structs (sizeof(ms3d_triangle_t) == 70)
            ms.Triangles = new List<ms3d_triangle_t>(nNumTriangles);
            for(var t = 0; t < nNumTriangles; t++)
            {
                ms.Triangles.Add(new ms3d_triangle_t
                { 
                    Flags = (MilkshapeObjectFlags)utility.ReadU16(),     // SELECTED | SELECTED2 | HIDDEN
                    VertexIndices = utility.ReadU16Ints(3),
                    VertexNormals = new[] { Vector3.Read(utility), Vector3.Read(utility), Vector3.Read(utility) },
                    TextureCoordinates = ReadTextureCoordinates(utility),
                    SmoothingGroup = utility.ReadByte(),                             // 1 - 32
                    GroupIndex = utility.ReadByte()                                 //
                });
            }

            // Then comes the number of groups
            var nNumGroups = utility.ReadU16(); // 2 bytes

            // Then come nNumGroups times groups (the sizeof a group is dynamic, because of triangleIndices is numtriangles long)
            ms.Groups = new List<ms3d_group_t>(nNumGroups);
            for(var g = 0; g < nNumGroups; g++)
            {
                var group = new ms3d_group_t();

                group.Flags = (MilkshapeObjectFlags)utility.ReadByte();     // SELECTED | HIDDEN
                group.Name = ReadString(utility, 32);

                var numtriangles = utility.ReadU16();
                group.TriangleIndices = utility.ReadU16Ints(numtriangles);
                group.MaterialIndex = utility.ReadSByte();

                ms.Groups.Add(group);
            }

            // number of materials
            var nNumMaterials = utility.ReadU16(); // 2 bytes

            // Then come nNumMaterials times ms3d_material_t structs (sizeof(ms3d_material_t) == 361)
            ms.Materials = new List<ms3d_material_t>(nNumMaterials);
            for(var m = 0; m < nNumMaterials; m++)
            {
                ms.Materials.Add(new ms3d_material_t
                {
                    Name = ReadString(utility, 32),
                    Ambient = utility.ReadFloats(4),
                    Diffuse = utility.ReadFloats(4),
                    Specular = utility.ReadFloats(4),
                    Emissive = utility.ReadFloats(4),
                    Shininess = utility.ReadFloat(),
                    Transparency = utility.ReadFloat(),
                    Mode = utility.ReadSByte(),

                    // NOTE: Examining a file written by MilkShape, I saw garbage beyond 
                    // the null terminator of these strings. Harmless, just FYI.
                    Texture = ReadString(utility, 128),
                    Alphamap = ReadString(utility, 128)
                });
            }

            // save some keyframer data
            ms.fAnimationFPS = utility.ReadFloat();
            ms.fCurrentTime = utility.ReadFloat();
            ms.iTotalFrames = utility.ReadI32();

            // number of joints
            var nNumJoints = utility.ReadU16(); // 2 bytes

            // Then come nNumJoints joints (the size of joints are dynamic, because each joint has a differnt count of keys
            ms.Joints = new List<ms3d_joint_t>(nNumJoints);
            for(var j = 0; j < nNumJoints; j++)
            {
                var joint = new ms3d_joint_t();

                joint.Flags = (MilkshapeObjectFlags)utility.ReadByte();
                joint.Name = ReadString(utility, 32);
                joint.ParentName = ReadString(utility, 32);
                joint.Rotation = Vector3.Read(utility);
                joint.Position = Vector3.Read(utility);

                var numKeyFramesRot = utility.ReadU16();
                var numKeyFramesTrans = utility.ReadU16();

                joint.KeyFramesRot = new ms3d_keyframe_rot_t[numKeyFramesRot];
                for(var r = 0; r < numKeyFramesRot; r++)
                {
                    joint.KeyFramesRot[r] = new ms3d_keyframe_rot_t
                    {
                        Time = utility.ReadFloat(),
                        Rotation = Vector3.Read(utility)
                    };
                }

                joint.KeyFramesTrans = new ms3d_keyframe_pos_t[numKeyFramesTrans];
                for(var t = 0; t < numKeyFramesTrans; t++)
                {
                    joint.KeyFramesTrans[t] = new ms3d_keyframe_pos_t
                    {
                        Time = utility.ReadFloat(),
                        Position = Vector3.Read(utility)
                    };
                }

                ms.Joints.Add(joint);
            }

            try
            {
                // subVersion specifying whether comment data exists
                var subVersion = utility.ReadU32();

                if (subVersion == 1)
                {
                    // Group comments
                    ReadComments(utility, ms.Groups);

                    // Material comments
                    ReadComments(utility, ms.Materials);

                    // Joint comments
                    ReadComments(utility, ms.Joints);

                    // Then comes the number of model comments, which is always 0 or 1
                    ReadComments(utility, new[] { ms }, true);
                }

                // subVersion specifying whether extended vertex data exists and to what extent
                subVersion = utility.ReadU32();

                if (subVersion > 0)
                {
                    // Then comes nNumVertices times ms3d_vertex_ex_t structs (sizeof(ms3d_vertex_ex_t) == 14)

                    // NOTE: I'm merging extended vertex data spec stuff from this mess:
                    //    sbyte[] boneIds = new sbyte[3];                                    // index of joint or -1, if -1, then that weight is ignored, since subVersion 1
                    //    byte[] weights = new byte[3];                                    // vertex weight ranging from 0 - 100, last weight is computed by 1.0 - sum(all weights), since subVersion 1
                    //                                                    // weight[0] is the weight for boneId in ms3d_vertex_t
                    //                                                    // weight[1] is the weight for boneIds[0]
                    //                                                    // weight[2] is the weight for boneIds[1]
                    //                                                    // 1.0f - weight[0] - weight[1] - weight[2] is the weight for boneIds[2]

                    // NOTE: "extra" depends in subVersion; 1 element if 2, 2 elements if 3
                    //    uint[] extra = new uint[2];									// vertex extra, which can be used as color or anything else, since subVersion 3

                    for (var v = 0; v < nNumVertices; v++)
                    {
                        var vertex = ms.Vertices[v];

                        // These are ADDITIONAL bone Ids
                        vertex.BoneIdsAndWeights[1].BoneId = utility.ReadSByte();
                        vertex.BoneIdsAndWeights[2].BoneId = utility.ReadSByte();
                        vertex.BoneIdsAndWeights[3].BoneId = utility.ReadSByte();

                        // These are WEIGHTS which were previously unavailable
                        vertex.BoneIdsAndWeights[0].Weight = utility.ReadByte();
                        vertex.BoneIdsAndWeights[1].Weight = utility.ReadByte();
                        vertex.BoneIdsAndWeights[2].Weight = utility.ReadByte();

                        // Final bone weight is computed -- NOTE, spec says 1.0 - [...], but I think it meant 100
                        vertex.BoneIdsAndWeights[3].Weight = (byte)(100 - vertex.BoneIdsAndWeights[0].Weight - vertex.BoneIdsAndWeights[1].Weight - vertex.BoneIdsAndWeights[2].Weight);

                        // How much "extra" data is here depends on subVersion...
                        var extraCount = subVersion - 1;
                        vertex.Extra = utility.ReadUInts(extraCount);
                    }
                }

                // subVersion specifying whether joints have color
                subVersion = utility.ReadU32();

                if (subVersion == 1)
                {
                    for (var j = 0; j < nNumJoints; j++)
                    {
                        var joint = ms.Joints[j];

                        joint.Color = new ColorFloat
                        {
                            R = utility.ReadFloat(),
                            G = utility.ReadFloat(),
                            B = utility.ReadFloat(),
                            A = 1.0f    // Not stored
                        };
                    }
                }

                // subVersion specifying whether model extended data exists
                subVersion = utility.ReadU32();

                if (subVersion == 1)
                {
                    ms.jointSize = utility.ReadFloat();    // joint size, since subVersion == 1
                    ms.transparencyMode = utility.ReadI32(); // 0 = simple, 1 = depth buffered with alpha ref, 2 = depth sorted triangles, since subVersion == 1
                    ms.alphaRef = utility.ReadFloat(); // alpha reference value for transparencyMode = 1, since subVersion == 1
                }
            }
            catch(IndexOutOfRangeException)
            {
                // This is a dirty hack because any file that doesn't have the extended data
                // will throw IndexOutOfRangeException but I really should be doing EOF checks
            }

            return ms;
        }

        public void Save(BinaryWriter bw)
        {
            var utility = new Utility(null, bw, Endianness.Little);

            WriteString(utility, Magic, (uint)Magic.Length);   // Magic
            bw.Write(4u);    // Version

            // Vertices
            utility.Write((ushort)Vertices.Count);
            foreach(var v in Vertices)
            {
                utility.Write((byte)v.Flags);
                v.Position.Write(utility);
                utility.Write(v.BoneIdsAndWeights[0].BoneId);
                utility.Write(v.ReferenceCount);
            }

            // Triangles
            utility.Write((ushort)Triangles.Count);
            foreach (var t in Triangles)
            {
                utility.Write((ushort)t.Flags);
                utility.Write(t.VertexIndices);
                t.VertexNormals[0].Write(utility);
                t.VertexNormals[1].Write(utility);
                t.VertexNormals[2].Write(utility);
                utility.Write(t.TextureCoordinates.Select(tc => tc.X).ToArray());
                utility.Write(t.TextureCoordinates.Select(tc => tc.Y).ToArray());
                utility.Write(t.SmoothingGroup);
                utility.Write(t.GroupIndex);
            }

            // Groups
            utility.Write((ushort)Groups.Count);
            foreach(var g in Groups)
            {
                utility.Write((byte)g.Flags);
                WriteString(utility, g.Name, 32);
                utility.Write((ushort)g.TriangleIndices.Length);
                utility.Write(g.TriangleIndices);
                utility.Write(g.MaterialIndex);
            }

            // Materials
            utility.Write((ushort)Materials.Count);
            foreach (var m in Materials)
            {
                WriteString(utility, m.Name, 32);
                utility.Write(m.Ambient);
                utility.Write(m.Diffuse);
                utility.Write(m.Specular);
                utility.Write(m.Emissive);
                utility.Write(m.Shininess);
                utility.Write(m.Transparency);
                utility.Write(m.Mode);
                WriteString(utility, m.Texture, 128);
                WriteString(utility, m.Alphamap, 128);
            }

            // Keyframe stuff 
            utility.Write(fAnimationFPS);
            utility.Write(fCurrentTime);
            utility.Write(iTotalFrames);

            // Joints
            utility.Write((ushort)Joints.Count);
            foreach(var j in Joints)
            {
                utility.Write((byte)j.Flags);
                WriteString(utility, j.Name, 32);
                WriteString(utility, j.ParentName, 32);
                j.Rotation.Write(utility);
                j.Position.Write(utility);

                utility.Write((ushort)j.KeyFramesRot.Length);
                utility.Write((ushort)j.KeyFramesTrans.Length);

                for (var r = 0; r < j.KeyFramesRot.Length; r++)
                {
                    utility.Write(j.KeyFramesRot[r].Time);
                    j.KeyFramesRot[r].Rotation.Write(utility);
                }

                for (var t = 0; t < j.KeyFramesTrans.Length; t++)
                {
                    utility.Write(j.KeyFramesTrans[t].Time);
                    j.KeyFramesTrans[t].Position.Write(utility);
                }
            }

            // We're always pushing subVersion = 1 (has comments)
            utility.Write(1u);

            // Group comments
            WriteComments(utility, Groups);

            // Material comments
            WriteComments(utility, Materials);

            // Joint comments
            WriteComments(utility, Joints);

            // Model comment
            WriteComments(utility, new[] { this }, true);

            // We're always pushing subVersion = 3 (full extended vertex data)
            utility.Write(3u);

            for (var v = 0; v < Vertices.Count; v++)
            {
                var vertex = Vertices[v];

                // These are ADDITIONAL bone Ids
                utility.Write(vertex.BoneIdsAndWeights[1].BoneId);
                utility.Write(vertex.BoneIdsAndWeights[2].BoneId);
                utility.Write(vertex.BoneIdsAndWeights[3].BoneId);

                // These are WEIGHTS which were previously unavailable
                // NOTE: vertex.BoneWeights[3].Weight is always computed, not stored; see spec
                utility.Write(vertex.BoneIdsAndWeights[0].Weight);
                utility.Write(vertex.BoneIdsAndWeights[1].Weight);
                utility.Write(vertex.BoneIdsAndWeights[2].Weight);

                // How much "extra" data is here depends on subVersion...
                for(var extra = 0; extra < 2; extra++)
                {
                    if(extra < vertex.Extra.Length)
                    {
                        utility.Write(vertex.Extra[extra]);
                    }
                    else
                    {
                        utility.Write(0u);
                    }
                }
            }

            // We're always pushing subVersion = 1 (joint colors)
            utility.Write(1u);

            for(var j = 0; j < Joints.Count; j++)
            {
                var joint = Joints[j];

                utility.Write(joint.Color.R);
                utility.Write(joint.Color.G);
                utility.Write(joint.Color.B);
            }

            // We're always pushing subVersion = 1 (model extended data)
            utility.Write(1u);

            utility.Write(jointSize);
            utility.Write(transparencyMode);
            utility.Write(alphaRef);
        }

        public struct ms3d_vertex_t_BWs
        {
            public sbyte BoneId { get; set; }   // index of joint or -1, if -1, then that weight is ignored
            public byte Weight { get; set; }    // vertex weight ranging from 0 - 100
        }

        public class ms3d_vertex_t
        {
            public ms3d_vertex_t()
            {
                BoneIdsAndWeights = new ms3d_vertex_t_BWs[4];
                Extra = new uint[2];
            }

            public MilkshapeObjectFlags Flags { get; set; }     // SELECTED | SELECTED2 | HIDDEN
            public Vector3 Position { get; set; }               // Array of 3 floats named "vertex" in original space
            //public sbyte BoneId { get; set; }                   // -1 = no bone
            public byte ReferenceCount { get; set; }

            // NOTE: This was stored in variations of ms3d_vertex_ex_t in the spec,
            // but it really belongs here; this will envelope the BoneId usually
            // specified all by itself as well
            public ms3d_vertex_t_BWs[] BoneIdsAndWeights { get; set; }

            public uint[] Extra { get; set; }   // "vertex extra, which can be used as color or anything else"; No elements if subVersion 1, 1 element if subVersion 2, 2 elements if subVersion 3

            //    sbyte[] boneIds = new sbyte[3];                                    // index of joint or -1, if -1, then that weight is ignored, since subVersion 1
            //    byte[] weights = new byte[3];                                    // vertex weight ranging from 0 - 100, last weight is computed by 1.0 - sum(all weights), since subVersion 1
            //                                                    // weight[0] is the weight for boneId in ms3d_vertex_t
            //                                                    // weight[1] is the weight for boneIds[0]
            //                                                    // weight[2] is the weight for boneIds[1]
            //                                                    // 1.0f - weight[0] - weight[1] - weight[2] is the weight for boneIds[2]
            //    uint[] extra = new uint[2];									// vertex extra, which can be used as color or anything else, since subVersion 3
        };

        public class ms3d_triangle_t
        {
            public ms3d_triangle_t()
            {
                VertexIndices = new ushort[3];
                VertexNormals = new Vector3[3];
            }

            public MilkshapeObjectFlags Flags { get; set; }     // SELECTED | SELECTED2 | HIDDEN
            public ushort[] VertexIndices { get; set; }         
            public Vector3[] VertexNormals { get; set; }        // Was a [3, 3] float array in original spec
            public Vector2[] TextureCoordinates { get; set; }      // S/float[3] and T/float[3] in original spec
            public byte SmoothingGroup { get; set; }                             // 1 - 32
            public byte GroupIndex { get; set; }                                 //
        };

        public class ms3d_group_t : IMilkShapeCommentable
        {
            public MilkshapeObjectFlags Flags { get; set; }     // SELECTED | HIDDEN
            public string Name { get; set; }                    // x32
            // ushort numtriangles;                             // No need to keep this in the structure, just be aware it's there
            public ushort[] TriangleIndices { get; set; }       // x numtriangles     // the groups group the triangles
            public sbyte MaterialIndex { get; set; }                   // -1 = no material

            public string Comment { get; set; }     // Spec didn't put it here, but it's convenient
        };


        public class ms3d_material_t : IMilkShapeCommentable
        {
            public ms3d_material_t()
            {
                Ambient = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };
                Diffuse = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };
                Specular = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };
                Emissive = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };
                Transparency = 1.0f;
            }

            public string Name { get; set; }                           // x32
            public float[] Ambient { get; set; }                         //
            public float[] Diffuse { get; set; }                         //
            public float[] Specular { get; set; }                        //
            public float[] Emissive { get; set; }                        //
            public float Shininess { get; set; }                          // 0.0f - 128.0f
            public float Transparency { get; set; }                       // 0.0f - 1.0f
            public sbyte Mode { get; set; }                               // 0, 1, 2 is unused now
            public string Texture { get; set; }                        // x128 texture.bmp
            public string Alphamap { get; set; }                       // x128 alpha.bmp

            public string Comment { get; set; }     // Spec didn't put it here, but it's convenient
        };

        public class ms3d_keyframe_rot_t // 16 bytes
        {
            public float Time { get; set; }         // time in seconds
            public Vector3 Rotation { get; set; }   // x, y, z Euler angles in radians (was float[3] in spec)
                                                    // rotation order: X then Y then Z
                                                    // around fixed axes (extrinsic)
        };

        public class ms3d_keyframe_pos_t // 16 bytes
        {
            public float Time { get; set; }         // time in seconds
            public Vector3 Position { get; set; }   // local position
        };

        public class ms3d_joint_t : IMilkShapeCommentable
        {
            public MilkshapeObjectFlags Flags { get; set; }     // SELECTED | DIRTY
            public string Name { get; set; }                    // x32
            public string ParentName { get; set; }              // x32
            public Vector3 Rotation { get; set; }               // local reference matrix (was float[3] in spec)
                                                                // x, y, z Euler angles in radians
                                                                // rotation order: X then Y then Z
                                                                // around fixed axes (extrinsic)
            public Vector3 Position { get; set; }               // was float[3] in spec

            public ms3d_keyframe_rot_t[] KeyFramesRot { get; set; } // x [numKeyFramesRot];      // local animation matrices
            public ms3d_keyframe_pos_t[] KeyFramesTrans { get; set; } // x [numKeyFramesTrans];  // local animation matrices

            public string Comment { get; set; }     // Spec didn't put it here, but it's convenient

            // NOTE: This is EXTENDED data in spec, formerly part of ms3d_joint_ex_t
            public ColorFloat Color { get; set; }
        };

        public class ms3d_comment_t
        {
            public int Index { get; set; }              // index of group, material or joint
            public string Comment { get; set; }         // x [commentLength];   // comment
        };

        // All comment blocks follow the same pattern
        private static void ReadComments(Utility utility, IEnumerable<IMilkShapeCommentable> commentableObjects, bool ignoreIndexHack = false)
        {
            var numComments = utility.ReadI32();
            var comments = new ms3d_comment_t[numComments];

            for(var c = 0; c < numComments; c++)
            {
                var comment = new ms3d_comment_t();

                // ignoreIndexHack: Model comment does not have an index
                comment.Index = !ignoreIndexHack ? utility.ReadI32() : 0;

                // NOTE -- comments do NOT store a null terminator, so they are sized exactly
                var commentLength = utility.ReadU32();
                comment.Comment = ReadString(utility, commentLength);

                comments[c] = comment;
            }

            foreach (var comment in comments)
            {
                commentableObjects.ElementAt(comment.Index).Comment = comment.Comment;
            }
        }

        private static void WriteComments(Utility utility, IEnumerable<IMilkShapeCommentable> commentableObjects, bool ignoreIndexHack = false)
        {
            var comments = new List<ms3d_comment_t>();

            // Collect all the comments and the respective indexes
            var index = 0;
            foreach(var obj in commentableObjects)
            {
                if (obj.Comment != null)
                {
                    comments.Add(new ms3d_comment_t
                    {
                        Index = index,
                        Comment = obj.Comment
                    });
                }

                index++;
            }

            // Now write them out
            utility.Write(comments.Count);
            foreach (var comment in comments)
            {
                if (!ignoreIndexHack)   // Model comment does not have an Index
                {
                    utility.Write(comment.Index);
                }

                var commentLen = (uint)comment.Comment.Length;
                utility.Write(commentLen);
                WriteString(utility, comment.Comment, commentLen);
            }
        }


        // Then comes the subversion of the model extra information
        //int subVersion;		// subVersion is = 1, 4 bytes

        // ms3d_model_ex_t for subVersion == 1
        //class ms3d_model_ex_t
        //{
        //    float jointSize;    // joint size, since subVersion == 1
        //    int transparencyMode; // 0 = simple, 1 = depth buffered with alpha ref, 2 = depth sorted triangles, since subVersion == 1
        //    float alphaRef; // alpha reference value for transparencyMode = 1, since subVersion == 1
        //};
    }
}
