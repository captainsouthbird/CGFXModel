using CGFXModel;
using CGFXModel.Chunks.Model;
using CGFXModel.Chunks.Model.Material;
using CGFXModel.Chunks.Model.Shape;
using CGFXModel.Chunks.Model.Skeleton;
using CGFXModel.Chunks.Texture;
using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CGFXConverter
{
    // Partly because I figure it wouldn't function in the game anyway, we're going to 
    // impose a rule that this "simplified" class will not support changing the number 
    // of "meshes" or "shapes" that the model is using.

    // Another limitation: Only supporting one vertex buffer per shape and it must be VertexBufferInterleaved

    public class SMVertex
    {
        public Vector4 Color;       // NOTE: Null if not supplied; not all modelers can support "Color"
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 TexCoord;
        public int[] BoneIndices;
        public float[] Weights;
    };

    public class SMTriangle
    {
        public ushort v1, v2, v3;
    }

    // Just looking to barely represent the bone, not worried about modifying the skeleton since
    // that would go into other territories like animations etc.
    public class SMBone
    {
        public string ParentName;
        public string Name;
        public Vector3 Translation;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Matrix LocalTransform;
    }

    public class SMTexture
    {
        public SMTexture(string name, Bitmap textureBitmap)
        {
            Name = name;
            TextureBitmap = textureBitmap;
        }

        public string Name { get; private set; }
        public Bitmap TextureBitmap { get; set; }        
    }

    public class SMMesh
    {
        public SMMesh(IEnumerable<SMVertex> vertices, IEnumerable<SMTriangle> triangles, DICTObjShape shape, int vertexBufferIndex, SMTexture texture)
        {
            Vertices = vertices.ToList();
            Triangles = triangles.ToList();
            Shape = shape;
            VertexBufferIndex = vertexBufferIndex;
            Texture = texture;
        }

        public List<SMVertex> Vertices { get; private set; }
        public List<SMTriangle> Triangles { get; private set; }
        public DICTObjShape Shape { get; private set; }
        public int VertexBufferIndex { get; private set; }
        public SMTexture Texture { get; private set; }
    }

    public class SimplifiedModel
    {
        private CGFX cgfx;
        public SMMesh[] Meshes { get; private set; }
        public SMBone[] Bones { get; private set; }
        public SMTexture[] Textures { get; private set; }

        // This REQUIRES a backing CGFX file and doesn't contain enough data to regenerate one from scratch
        public SimplifiedModel(CGFX cgfx)
        {
            this.cgfx = cgfx;

            // Models are always the first entry per CGFX standard
            var models = cgfx.Data.Entries[0]?.Entries;

            // Textures are the second
            var textures = cgfx.Data.Entries[1]?.Entries.Select(e => e.EntryObject).Cast<DICTObjTexture>().ToList();

            if(textures != null && textures.Count > 0)
            {
                Textures = new SMTexture[textures.Count];

                for (var t = 0; t < textures.Count; t++)
                {
                    Bitmap textureBitmap = null;
                    var name = textures[t].Name;
                    var textureData = textures[t];

                    if (textureData != null)
                    {
                        var textureRGBA = TextureCodec.ConvertTextureToRGBA(new Utility(null, null, Endianness.Little), textureData.TextureCGFXData, textureData.TextureFormat, (int)textureData.Width, (int)textureData.Height);

                        textureBitmap = new Bitmap((int)textureData.Width, (int)textureData.Height, PixelFormat.Format32bppArgb);
                        var imgData = textureBitmap.LockBits(new Rectangle(0, 0, textureBitmap.Width, textureBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                        Marshal.Copy(textureRGBA, 0, imgData.Scan0, textureRGBA.Length);
                        textureBitmap.UnlockBits(imgData);
                        textureBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }

                    var smTexture = new SMTexture(name, textureBitmap);
                    Textures[t] = smTexture;
                }
            }

            if (models != null && models.Count > 0)
            {
                // This probably isn't a difficult problem to work around, but it's out of my scope at this time
                if(models.Count != 1)
                {
                    throw new InvalidOperationException("File contains more than one model; only supporting one for now.");
                }

                var model = (DICTObjModel)models.First().EntryObject;

                // NOTE: Currently NOT committing the skeleton back, we're just keeping it for model software
                var bones = model.Skeleton?.Bones.Entries.Select(e => e.EntryObject).Cast<DICTObjBone>().ToList() ?? new List<DICTObjBone>();

                Bones = bones.Select(b => new SMBone
                {
                    Name = b.Name,
                    ParentName = b.Parent?.Name,
                    Rotation = b.Rotation,
                    Translation = b.Translation,
                    Scale = b.Scale,
                    LocalTransform = b.LocalTransform
                }).ToArray();

                Meshes = new SMMesh[model.Meshes.Length];

                for(var m = 0; m < model.Meshes.Length; m++)
                {
                    var mesh = model.Meshes[m];
                    var shape = model.Shapes[mesh.ShapeIndex];

                    // There might be some clever way of handling multiple vertex buffers 
                    // (if it actually happens) but I'm not worried about it. Only looking
                    // for a single one that is VertexBufferInterleaved.
                    var vertexBuffersInterleaved = shape.VertexBuffers.Where(vb => vb is VertexBufferInterleaved);
                    if (vertexBuffersInterleaved.Count() != 1)
                    {
                        throw new InvalidOperationException("Unsupported count of VertexBuffers in VertexBufferInterleaved format");
                    }

                    // Only expecting / supporting 1 SubMesh entry
                    if(shape.SubMeshes.Count != 1)
                    {
                        throw new InvalidOperationException("Unsupported amount of SubMeshes");
                    }

                    var subMesh = shape.SubMeshes[0];

                    // The BoneReferences in the SubMesh are what the vertex's local index references.
                    var boneReferences = subMesh.BoneReferences;

                    // These aren't "faces" in the geometrical sense, but rather a header of sorts
                    if (subMesh.Faces.Count != 1)
                    {
                        throw new InvalidOperationException("Unsupported amount of Faces");
                    }

                    var faceHeader = subMesh.Faces[0];

                    // Again, just one FaceDescriptor...
                    if(faceHeader.FaceDescriptors.Count != 1)
                    {
                        throw new InvalidOperationException("Unsupported amount of FaceDescriptors");
                    }

                    var faceDescriptor = faceHeader.FaceDescriptors[0];

                    // We're also only supporting triangles at this point; the model format probably
                    // allows for more groups of geometry, but again, out of my scope
                    if(faceDescriptor.PrimitiveMode != FaceDescriptor.PICAPrimitiveMode.Triangles)
                    {
                        throw new InvalidOperationException("Only supporting triangles format");
                    }

                    // Vertices are stored (in GPU-compatible form) 
                    var vertexBuffer = (VertexBufferInterleaved)vertexBuffersInterleaved.Single();
                    var vertexBufferIndex = shape.VertexBuffers.IndexOf(vertexBuffer);
                    var attributes = vertexBuffer.Attributes.Select(a => VertexBufferCodec.PICAAttribute.GetPICAAttribute(a)).ToList();

                    // The following are the only VertexAttributes we are supporting at this time
                    var supportedAttributes = new List<VertexBuffer.PICAAttributeName>
                    {
                        VertexBuffer.PICAAttributeName.Position,
                        VertexBuffer.PICAAttributeName.Normal,
                        VertexBuffer.PICAAttributeName.TexCoord0,
                        VertexBuffer.PICAAttributeName.BoneIndex,
                        VertexBuffer.PICAAttributeName.BoneWeight,

                        // Caution: Vertex color may not be supported by all model editors!
                        VertexBuffer.PICAAttributeName.Color
                    };

                    // Check if any unsupported attributes are in use
                    var unsupportedAttributes = attributes.Where(a => !supportedAttributes.Contains(a.Name)).Select(a => a.Name);
                    if (unsupportedAttributes.Any())
                    {
                        throw new InvalidOperationException($"This model is using the following unsupported attributes: {string.Join(", ", unsupportedAttributes)}");
                    }

                    var nativeVertices = VertexBufferCodec.GetVertices(shape, vertexBufferIndex);

                    // Convert to the simplified vertices
                    var boneIndexCount = GetElementsOfAttribute(attributes, VertexBuffer.PICAAttributeName.BoneIndex);  // How many bone indices are actually used
                    var boneWeightCount = GetElementsOfAttribute(attributes, VertexBuffer.PICAAttributeName.BoneWeight);  // How many bone weights are actually used

                    // FIXME? There seems to be, on occasion, a bone relationship that points to
                    // the entire mesh but not assigned to any of the vertices. So basically the 
                    // vertices are recorded as having no bone indices but in fact are all dependent
                    // upon associating with bone index 0 (?) This will force it to use at least
                    // one bone index even if zero is specified for this case.
                    if (boneReferences != null && boneReferences.Count > 0)
                    {
                        boneIndexCount = Math.Max(boneIndexCount, 1);
                    }

                    var vertices = nativeVertices.Select(v => new SMVertex
                    {
                        Position = new Vector3(v.Position.X, v.Position.Y, v.Position.Z),
                        Normal = new Vector3(v.Normal.X, v.Normal.Y, v.Normal.Z),
                        TexCoord = new Vector3(v.TexCoord0.X, v.TexCoord0.Y, v.TexCoord0.Z),
                        BoneIndices = (new[] { v.Indices.b0, v.Indices.b1, v.Indices.b2, v.Indices.b3 }).Take(boneIndexCount).ToArray(),
                        Weights = (new[] { v.Weights.w0, v.Weights.w1, v.Weights.w2, v.Weights.w3 }).Take(boneWeightCount).ToArray(),
                        Color = v.Color     // Caution! Not all 3D model editors may support vertex color!
                    }).ToList();

                    // The vertices use relative bone indices based on the SubMesh definitions,
                    // which we're going to make absolute now
                    for(var v = 0; v < vertices.Count; v++)
                    {
                        var vertex = vertices[v];

                        for(var i = 0; i < vertex.BoneIndices.Length; i++)
                        {
                            vertex.BoneIndices[i] = boneReferences[vertex.BoneIndices[i]].Index;

                            // Also, if no bone weights are available, assign a weight of 1 to the first bone.
                            // This won't be stored ultimately as the PICA attributes won't specify it.
                            if(vertex.Weights.Length == 0)
                            {
                                vertex.Weights = new float[] { 1.0f };
                            }
                        }
                    }

                    // Deconstruct into triangle faces
                    var triangles = new List<SMTriangle>();
                    var indices = faceDescriptor.Indices;
                    for (var i = 0; i < indices.Count; i += 3)
                    {
                        triangles.Add(new SMTriangle
                        {
                            v1 = indices[i + 0],
                            v2 = indices[i + 1],
                            v3 = indices[i + 2]
                        });
                    }

                    // Finally, assign material, if available (mostly for the model editor's benefit)
                    var material = model.ModelMaterials.Entries.Select(e => e.EntryObject).Cast<DICTObjModelMaterial>().ToList()[mesh.MaterialId];
                    var texture = material.TextureMappers.First().TextureReference;
                    var name = texture.ReferenceName;
                    var smTexture = Textures.Where(t => t.Name == name).SingleOrDefault();

                    Meshes[m] = new SMMesh(vertices, triangles, shape, vertexBufferIndex, smTexture);
                }
            }
        }

        private static int GetElementsOfAttribute(IEnumerable<VertexBufferCodec.PICAAttribute> attributes, VertexBuffer.PICAAttributeName name)
        {
            var attribute = attributes.Where(a => a.Name == name);
            if(attribute.Any())
            {
                return attribute.Single().Elements;
            }
            else
            {
                return 0;
            }
        }

        
        public void ApplyChanges()
        {
            // Models are always the first entry per CGFX standard
            var models = cgfx.Data.Entries[0]?.Entries;

            // Only one model per file for now
            var model = (DICTObjModel)models.First().EntryObject;

            // Textures are the second
            var textures = cgfx.Data.Entries[1]?.Entries.Select(e => e.EntryObject).Cast<DICTObjTexture>().ToList();

            if (textures != null && textures.Count > 0)
            {
                for (var t = 0; t < textures.Count; t++)
                {
                    if (Textures[t].TextureBitmap != null)
                    {
                        var textureBitmap = Textures[t].TextureBitmap;
                        var textureData = textures[t];

                        if (textureData != null)
                        {
                            var imgData = textureBitmap.LockBits(new Rectangle(0, 0, (int)textureData.Width, (int)textureData.Height), ImageLockMode.ReadOnly, textureBitmap.PixelFormat);
                            var rawRGBAData = new byte[(imgData.Height * imgData.Stride)];
                            Marshal.Copy(imgData.Scan0, rawRGBAData, 0, rawRGBAData.Length);
                            textureBitmap.UnlockBits(imgData);

                            var utility = new Utility(null, null, Endianness.Little);
                            textureData.SetTexture(utility, rawRGBAData);       // TODO -- try disabling the safety checks...
                        }
                    }
                }
            }

            for (var m = 0; m < model.Meshes.Length; m++)
            {
                var mesh = model.Meshes[m];
                var shape = model.Shapes[mesh.ShapeIndex];

                var SMMesh = Meshes[m];

                // NOTE: These are all making the same assumptions as noted in the constructor
                var vertexBuffer = shape.VertexBuffers.Where(vb => vb is VertexBufferInterleaved).Cast<VertexBufferInterleaved>().Single();

                var subMesh = shape.SubMeshes[0];
                var faceHeader = subMesh.Faces[0];
                var faceDescriptor = faceHeader.FaceDescriptors[0];

                // Vertices are stored (in GPU-compatible form) 
                var vertexBufferIndex = shape.VertexBuffers.IndexOf(vertexBuffer);
                var attributes = vertexBuffer.Attributes.Select(a => VertexBufferCodec.PICAAttribute.GetPICAAttribute(a)).ToList();

                // Convert to the simplified vertices
                var boneIndexCount = GetElementsOfAttribute(attributes, VertexBuffer.PICAAttributeName.BoneIndex);  // How many bone indices are actually used
                var boneWeightCount = GetElementsOfAttribute(attributes, VertexBuffer.PICAAttributeName.BoneWeight);  // How many bone weights are actually used

                var vertices = SMMesh.Vertices.Select(v => new VertexBufferCodec.PICAVertex
                {
                    Position = new Vector4(v.Position.X, v.Position.Y, v.Position.Z, 0),
                    Normal = new Vector4(v.Normal.X, v.Normal.Y, v.Normal.Z, 0),
                    TexCoord0 = new Vector4(v.TexCoord.X, v.TexCoord.Y, v.TexCoord.Z, 0),
                    Indices = GetBoneIndices(v.BoneIndices),
                    Weights = GetBoneWeights(v.Weights),
                    Color = v.Color
                }).ToArray();

                // Same as when we loaded it in the first place, the bone indices need to be changed into a list
                var boneReferences =
                    // Get the absolute indices
                    SMMesh.Vertices.SelectMany(v => v.BoneIndices).Distinct().ToList()

                    // Remap to the bones
                    .SelectMany(smshape => model.Skeleton.Bones.Entries.Select(b => b.EntryObject).Cast<DICTObjBone>().Where(b => b.Index == smshape))

                    .ToList();

                var boneReferenceIndexes = boneReferences.Select(b => b.Index).ToList();

                // Reassign the indices so they're relative to the bone references pool
                for (var v = 0; v < vertices.Length; v++)
                {
                    for (var bi = 0; bi < boneIndexCount; bi++)
                    {
                        var absoluteIndex = vertices[v].Indices[bi];

                        vertices[v].Indices[bi] = boneReferenceIndexes.IndexOf(absoluteIndex);
                    }
                }

                // Convert the vertices back to native interleaved format
                vertexBuffer.RawBuffer = VertexBufferCodec.GetBuffer(vertices, vertexBuffer.Attributes.Select(a => VertexBufferCodec.PICAAttribute.GetPICAAttribute(a)));

                // Align if ending on odd byte
                if((vertexBuffer.RawBuffer.Length & 1) == 1)
                {
                    vertexBuffer.RawBuffer = vertexBuffer.RawBuffer.Concat(new byte[] { 0 }).ToArray();
                }

                // Replace the bone references in the SubMesh with the generated list
                // NOTE: Don't do this if boneIndexCount = 0, because apparently there
                // is always one defined even if not explicitly used...
                //
                // NOTE 2: I handle this during the Load case but not here but just
                // assuming if the submesh has bone refs then the object probably does
                // want to use them even if this is zero; seems MAYBE that means that
                // vertices don't specify a bone reference if it applies to the entire
                // mesh. Could use some more research to verify this...
                // 
                // The big issue is if the editor tries to assign vertices in this mesh
                // to other bones and the assignment will BE IGNORED.
                if (boneIndexCount > 0)
                {
                    subMesh.BoneReferences.Clear();
                    subMesh.BoneReferences.AddRange(boneReferences);
                }

                //// Get indicies of triangle faces
                var indices = SMMesh.Triangles.SelectMany(t => new[] { t.v1, t.v2, t.v3 }).ToList();

                faceDescriptor.Indices.Clear();
                faceDescriptor.Indices.AddRange(indices);
            }
        }

        public void RecomputeVertexNormals()
        {
            foreach(var mesh in Meshes)
            {
                var triangleFaceNormals = mesh.Triangles
                    .Select(t => new
                    {
                        Triangle = t,
                        Normal = CalcFaceNormal(mesh, t)
                    })
                    .ToList();

                for(var v = 0; v < mesh.Vertices.Count; v++)
                {
                    var allFaceNormals = triangleFaceNormals.Where(t => t.Triangle.v1 == v || t.Triangle.v2 == v || t.Triangle.v3 == v)
                        .Select(t => t.Normal);

                    var vertexNormal = new Vector3(
                        allFaceNormals.Sum(n => n.X),
                        allFaceNormals.Sum(n => n.Y),
                        allFaceNormals.Sum(n => n.Z)
                    );

                    var len = Math.Sqrt(vertexNormal.X * vertexNormal.X + vertexNormal.Y * vertexNormal.Y + vertexNormal.Z * vertexNormal.Z);
                    vertexNormal.X /= (float)len;
                    vertexNormal.Y /= (float)len;
                    vertexNormal.Z /= (float)len;

                    mesh.Vertices[v].Normal = vertexNormal;
                }
            }
        }

        private Vector3 CalcFaceNormal(SMMesh mesh, SMTriangle triangle)
        {
            var v1 = mesh.Vertices[triangle.v1];
            var v2 = mesh.Vertices[triangle.v2];
            var v3 = mesh.Vertices[triangle.v3];

            var UX = v2.Position.X - v1.Position.X;
            var UY = v2.Position.Y - v1.Position.Y;
            var UZ = v2.Position.Z - v1.Position.Z;

            var VX = v3.Position.X - v1.Position.X;
            var VY = v3.Position.Y - v1.Position.Y;
            var VZ = v3.Position.Z - v1.Position.Z;

            var NX = UY * VZ - UZ * VY;
            var NY = UZ * VX - UX * VZ;
            var NZ = UX * VY - UY * VX;

            var len = Math.Sqrt(NX * NX + NY * NY + NZ * NZ);

            return new Vector3
            {
                X = (float)(NX / len),
                Y = (float)(NY / len),
                Z = (float)(NZ / len)
            };
        }

        private static VertexBufferCodec.BoneIndices GetBoneIndices(int[] indices)
        {
            var result = new VertexBufferCodec.BoneIndices();

            for(var i = 0; i < (indices?.Length ?? 0); i++)
            {
                result[i] = indices[i];
            }

            return result;
        }

        private static VertexBufferCodec.BoneWeights GetBoneWeights(float[] weights)
        {
            var result = new VertexBufferCodec.BoneWeights();

            for (var i = 0; i < (weights?.Length ?? 0); i++)
            {
                result[i] = weights[i];
            }

            return result;
        }
    }
}
