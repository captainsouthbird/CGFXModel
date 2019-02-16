using CGFXModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using static CGFXConverter.MilkShape;

namespace CGFXConverter
{
    public static class MilkShapeConverter
    {
        public static MilkShape ToMilkShape(SimplifiedModel sm)
        {
            var milkShape = new MilkShape();

            // Convert the skeleton
            var bones = sm.Bones.Select(b => new ms3d_joint_t
            {
                Name = b.Name,
                ParentName = b.ParentName,
                Rotation = b.Rotation,
                Position = b.Translation,
                KeyFramesRot = new ms3d_keyframe_rot_t[0],
                KeyFramesTrans = new ms3d_keyframe_pos_t[0]
            });

            var allVertices = new List<ms3d_vertex_t>();
            var allTriangles = new List<ms3d_triangle_t>();
            var allMaterials = new List<ms3d_material_t>();
            var allGroups = new List<ms3d_group_t>();

            foreach (var mesh in sm.Meshes)
            {
                // Get current vertex offset as we're accumulating them
                var vertexMeshOffset = allVertices.Count;

                // Get current triangle offset as we're grouping them
                var triangleOffset = allTriangles.Count;

                // Vertices belonging to this mesh
                var vertices = mesh.Vertices
                    .Select(v => new ms3d_vertex_t
                    {
                        Position = TransformPositionByBone(v, v.BoneIndices, sm.Bones, false),
                        BoneIdsAndWeights = GetBoneIndiciesAndWeights(v.BoneIndices, v.Weights),
                    });

                allVertices.AddRange(vertices);

                // Triangles belonging to this mesh
                var triangles = mesh.Triangles
                    .Select(t => new ms3d_triangle_t
                    {
                        VertexIndices = new ushort[]
                        {
                                (ushort)(vertexMeshOffset + t.v1),
                                (ushort)(vertexMeshOffset + t.v2),
                                (ushort)(vertexMeshOffset + t.v3)
                        },

                        VertexNormals = new Vector3[]
                        {
                                mesh.Vertices[t.v1].Normal,
                                mesh.Vertices[t.v2].Normal,
                                mesh.Vertices[t.v3].Normal
                        },

                        TextureCoordinates = new Vector2[]
                        {
                                // NOTE: Textures are "upside-down", so this reverses them... make sure to undo that when saving...
                                new Vector2(mesh.Vertices[t.v1].TexCoord.X, 1.0f - mesh.Vertices[t.v1].TexCoord.Y),
                                new Vector2(mesh.Vertices[t.v2].TexCoord.X, 1.0f - mesh.Vertices[t.v2].TexCoord.Y),
                                new Vector2(mesh.Vertices[t.v3].TexCoord.X, 1.0f - mesh.Vertices[t.v3].TexCoord.Y)
                        },

                        GroupIndex = (byte)allGroups.Count
                    });

                allTriangles.AddRange(triangles);

                // Generate materials from the meshes
                sbyte materialIndex = -1;
                if (mesh.Texture != null)
                {
                    materialIndex = (sbyte)allMaterials.Count;
                    allMaterials.Add(new ms3d_material_t
                    {
                        Name = mesh.Texture.Name,
                        Texture = mesh.Texture.Name + ".png"
                    });
                }

                // We're going to make the "mesh" into a "group" in MilkShape speak
                var group = new ms3d_group_t
                {
                    Name = $"mesh{allGroups.Count}",
                    TriangleIndices = Enumerable.Range(triangleOffset, triangles.Count()).Select(i => (ushort)i).ToArray(),
                    MaterialIndex = materialIndex
                };

                allGroups.Add(group);
            }

            milkShape.Vertices.AddRange(allVertices);
            milkShape.Triangles.AddRange(allTriangles);
            milkShape.Groups.AddRange(allGroups);
            milkShape.Materials.AddRange(allMaterials);
            milkShape.Joints.AddRange(bones);

            return milkShape;
        }

        // NOTE: SimplifiedModel (based on the origin BCRES) is passed in because we can't 
        // guarantee that the model edited file has ALL of the data it originally contained,
        // just the same as the SimplifiedModel isn't enough by itself and still needs the
        // origin BCRES to generate all other data!
        public static void FromMilkShape(SimplifiedModel simplifiedModel, MilkShape milkShape)
        {
            // Take the MilkShape stored bones and remap them to the CGFX bones.
            var milkShapeBoneMap = simplifiedModel.Bones
                .Select(b => new
                {
                    b.Name,
                    MilkShapeJoint = milkShape.Joints.Where(j => j.Name == b.Name),
                    SMBone = b
                })
                .Select(b => new MilkShapeBoneMap
                {
                    Name = b.Name,
                    MilkShapeJointIndex = (b.MilkShapeJoint.Count() == 1) ? milkShape.Joints.IndexOf(b.MilkShapeJoint.Single()) : -1,
                    SMBoneIndex = Array.IndexOf(simplifiedModel.Bones, b.SMBone)
                })
                .ToList();

            var missingBones = milkShapeBoneMap.Where(msbm => msbm.SMBoneIndex == -1);
            if(missingBones.Any())
            {
                throw new KeyNotFoundException($"The following required bones are missing from or ambiguous in the MilkShape file: {string.Join(", ", missingBones.Select(b => b.Name))}");
            }

            // We need to figure out the matching MilkShape groups to the original meshes.
            // There must be a group for each mesh, identified by the given name of "meshN"
            for(var m = 0; m < simplifiedModel.Meshes.Length; m++)
            {
                var mesh = simplifiedModel.Meshes[m];

                // Since it's still useful to HAVE groups (for showing/hiding mainly), I'm allowing 
                // MilkShape groups to be named in ways that will group them together as such:
                //  mesh0       -- Basic name of mesh index 0
                //  mesh0-top   -- Also should be part of mesh index 0, but is the "top" geometry of something etc.
                //
                // ... so in this example, both "mesh0" and "mesh0-top" will be merged together into
                // Mesh Index 0, so you can still use groups in a useful way and reference back the
                // eventual CGFX mesh it's actually supposed to be a part of...
                var meshName = $"mesh{m}";
                var milkShapeGroupMatches = milkShape.Groups.Where(g => g.Name.StartsWith(meshName));
                if(!milkShapeGroupMatches.Any())
                {
                    throw new KeyNotFoundException($"Required MilkShape group {meshName} not found");
                }

                // CGFX stores vertices per mesh, but the vertices were all lumped together for MilkShape.
                // To reverse the process, we need to find out what vertices were used by all triangles in
                // this milkShapeGroup and that becomes our list of vertices for this mesh. Of course, we 
                // still need to have a MilkShape vertex -> CGFX mesh vertex map to translate the triangles.

                // TODO -- Groups in MilkShape define a MaterialIndex, which isn't currently used, but if
                // it were, having different materials across the merged groups wouldn't work. This will
                // call the user out if they do that and eventually someday it might really matter.
                if(milkShapeGroupMatches.Select(g => g.MaterialIndex).Distinct().Count() > 1)
                {
                    throw new InvalidOperationException($"Groups {string.Join(", ", milkShapeGroupMatches.Select(g => g.Name))} are to be merged into {meshName} but they have different materials assigned to them! This is not supported.");
                }

                var milkShapeGroupsTriangleIndices = milkShapeGroupMatches.SelectMany(g => g.TriangleIndices);

                // Triangles in this group
                var triangles = milkShapeGroupsTriangleIndices
                    .Select(ti => milkShape.Triangles[ti])
                    .ToList();

                // The "easy" concept is that we just take the triangles and selected distinct vertexes
                // out of them based on the MilkShape vertex index...
                // Unfortunately for us, MilkShape does texture coordinates PER TRIANGLE VERTEX, 
                // not per vertex, which means to PROPERLY represent it in the mesh we need to "split"
                // vertices that would be otherwise common so they can hold the unique texture coordinates.
                var SMTriangles = new List<SMTriangle>();
                var vertices = new List<MilkShapeTempVertex>();
                foreach (var triangle in triangles)
                {
                    // Get vertices for the triangle's three vertices
                    SMTriangles.Add(new SMTriangle
                    {
                        v1 = GetLocalVertexForMSTriangleVertex(vertices, milkShape, milkShapeBoneMap, triangle, 0),
                        v2 = GetLocalVertexForMSTriangleVertex(vertices, milkShape, milkShapeBoneMap, triangle, 1),
                        v3 = GetLocalVertexForMSTriangleVertex(vertices, milkShape, milkShapeBoneMap, triangle, 2)
                    });
                }

                // Add the generated triangles back in
                mesh.Triangles.Clear();
                mesh.Triangles.AddRange(SMTriangles);

                // As a benefit, the "vertices" collection now contains the set of vertices required to 
                // reconstruct the mesh!
                var SMVertices = vertices.OrderBy(v => v.MeshLocalVertexIndex).Select(v => v.SMVertex).ToList();

                // If the model has a skeleton... then WHOA there! Before we throw them back into the 
                // collection, they need to be un-transformed back to the neutral bone position!!
                var nativeMeshUseColorVerts = mesh.Vertices.Where(v => !ReferenceEquals(v.Color, null)).Any();
                foreach(var vertex in SMVertices)
                {
                    vertex.Position = TransformPositionByBone(vertex, vertex.BoneIndices, simplifiedModel.Bones, true);

                    // Also, while we're here, if the original model supported vertex Color attributes
                    // (which MilkShape does NOT), we'll just patch in an all-white color. It's not the
                    // best but I don't have a lot of option with this format...
                    if(nativeMeshUseColorVerts)
                    {
                        vertex.Color = new Vector4(1, 1, 1, 1);
                    }
                }

                mesh.Vertices.Clear();
                mesh.Vertices.AddRange(SMVertices);
            }
        }

        private class MilkShapeBoneMap
        {
            public string Name { get; set; }
            public int MilkShapeJointIndex { get; set; }
            public int SMBoneIndex { get; set; }
        }

        // This provides a container that holds the MilkShape vertex index along with a 
        // converted-from-triangle SMVertex. The idea is that if two triangles request 
        // the same MilkShape vertex index, they will hold the same position and are 
        // "probably" the same vertex, EXCEPT that MilkShape stores texture coordinates 
        // per TRIANGLE VERTEX, not per VERTEX... 
        //
        // Which means that for proper representation of the user's texture choice, we 
        // need to consider the texture coordinates as part of the "sameness", which 
        // could result in a common MilkShape vertex requiring splitting into two.
        private class MilkShapeTempVertex
        {
            // Index into MilkShape's vertex set
            public int MilkShapeVertexIndex { get; set; }

            // Index into the local mesh list we're building
            public int MeshLocalVertexIndex { get; set; }

            // SimplifiedModel vertex data for eventual storage
            public SMVertex SMVertex { get; set; }
        }

        // Gets or generates an SMVertex for a vertex on a MilkShape triangle
        private static ushort GetLocalVertexForMSTriangleVertex(List<MilkShapeTempVertex> milkShapeTempVertices, MilkShape milkShape, List<MilkShapeBoneMap> milkShapeBoneMap, ms3d_triangle_t t, int triangleVertex)
        {
            // This will provide the "close enough" rating of texture coordinates
            // to decide that a vertex with the same MilkShape index as well as
            // "close enough" texture coordinates is the same overall vertex.
            const float texCoordEpsilon = 1.0f / 128.0f;    // This is assuming that we're dealing with textures 128x128 and under, which is probably true

            // Get the texture coordinates used by this vertex of the triangle,
            // as it will be part of the consideration of "sameness" of other vertices
            var triangleVertexTexCoord = t.TextureCoordinates[triangleVertex];

            // The primary consideration is MilkShape's own vertex index
            var triangleVertexIndex = t.VertexIndices[triangleVertex];

            var resultVertex = milkShapeTempVertices
                .Where(tv =>

                    // Must come from same vertex in MilkShape's pool...
                    tv.MilkShapeVertexIndex == triangleVertexIndex && 
                    
                    // ... and be "close enough" with the texture coordinates
                    Math.Abs(triangleVertexTexCoord.X - tv.SMVertex.TexCoord.X) < texCoordEpsilon &&
                    Math.Abs((1.0f - triangleVertexTexCoord.Y) - tv.SMVertex.TexCoord.Y) < texCoordEpsilon
                ).SingleOrDefault();

            if(resultVertex == null)
            {
                // If we don't have one quite like this, then we need to create it!
                resultVertex = new MilkShapeTempVertex
                {
                    MilkShapeVertexIndex = triangleVertexIndex,
                    MeshLocalVertexIndex = milkShapeTempVertices.Count,
                    SMVertex = GetSMVertexFromTriangle(milkShape, milkShapeBoneMap, t, triangleVertex)
                };

                milkShapeTempVertices.Add(resultVertex);
            }

            return (ushort)resultVertex.MeshLocalVertexIndex;
        }

        private static SMVertex GetSMVertexFromTriangle(MilkShape milkShape, List<MilkShapeBoneMap> milkShapeBoneMap, ms3d_triangle_t t, int triangleVertex)
        {
            // Only focusing on one of the three triangle vertices
            var triVertIdx = t.VertexIndices[triangleVertex];

            return new SMVertex
            {
                // Get position of vertex
                Position = milkShape.Vertices[triVertIdx].Position,

                // Get normal of vertex (stored with triangle in Milkshape for some reason)
                Normal = t.VertexNormals[triangleVertex],

                // Texture coordinate needs "reversal" since I "reversed" it earlier
                // Also the (unused?) Z coordinate always appears to reflect the vertex Z, although
                // I don't know if that's a requirement or a quirk, but either way...
                TexCoord = new Vector3(t.TextureCoordinates[triangleVertex].X, 1.0f - t.TextureCoordinates[triangleVertex].Y, milkShape.Vertices[triVertIdx].Position.Z),

                // This uses the bone map just in case the MilkShape indexes don't line up with
                // the intended indexes...
                BoneIndices = milkShape.Vertices[triVertIdx].BoneIdsAndWeights
                    .Where(biw => biw.BoneId != -1)
                    .Select(biw => milkShapeBoneMap.Where(bm => bm.MilkShapeJointIndex == biw.BoneId).Single().SMBoneIndex)
                    .ToArray(),

                // Finally the MilkShape weights are stored as 0-100 byte-sized values, and we
                // must convert back (granted with loss)
                Weights = milkShape.Vertices[triVertIdx].BoneIdsAndWeights
                    .Where(biw => biw.BoneId != -1)
                    .Select(biw => biw.Weight / 100.0f).ToArray()
            };
        }

        private static Vector3 TransformPositionByBone(SMVertex vertex, int[] boneIndices, SMBone[] bones, bool invert)
        {
            var position = vertex.Position;
            var p = new Vector3(position);

            if (boneIndices != null && boneIndices.Length > 0)
            {
                int weightIndex = 0;
                float weightSum = 0;
                foreach (int boneIndex in boneIndices)
                {
                    var bone = bones[boneIndex];
                    var m = bone.LocalTransform;

                    var parentName = bone.ParentName;
                    while (parentName != null)
                    {
                        bone = bones.Where(b => b.Name == parentName).Single();
                        m *= bone.LocalTransform;
                        parentName = bone.ParentName;
                    }

                    float weight = 0;
                    if (weightIndex < vertex.Weights.Length)
                    {
                        weight = vertex.Weights[weightIndex++];
                    }

                    // A weight of zero creates a zero matrix and ruins everything
                    // ACNL's Isabelle incidentally seems to have a secondary (unused?) bone assignment
                    // to every vertex in mesh 0 but it has a zero weight so it shouldn't be employed.
                    // But it breaks this adjustment...
                    if (weight > 0)
                    {
                        weightSum += weight;
                        var matrixTransform = m * Matrix.scale(weight);

                        // Inverting the final transformation matrix is used to "undo" the bone
                        // transformation and restore the vertex back to its "bone neutral" position
                        if (invert)
                        {
                            matrixTransform = matrixTransform.invert();
                        }

                        p = Vector3.transform(p, matrixTransform);
                    }
                }

                // FIXME: Dunno if this is correct (if it ever even gets used in any case)
                // but I copied this out of some Ohana code
                if (weightSum < 1)
                {
                    p += position * (1 - weightSum);
                }
            }

            return p;
        }

        private static ms3d_vertex_t_BWs[] GetBoneIndiciesAndWeights(int[] boneIndices, float[] weights)
        {
            var result = new ms3d_vertex_t_BWs[4];

            for (var i = 0; i < 4; i++)
            {
                result[i].BoneId = (sbyte)((i < boneIndices.Length) ? boneIndices[i] : -1);
                result[i].Weight = (byte)((i < weights.Length) ? weights[i] * 100.0f : 0);
            }

            return result;
        }
    }
}
