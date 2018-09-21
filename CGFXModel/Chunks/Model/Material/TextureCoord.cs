using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.Material
{
    public class TextureCoord
    {
        public enum TextureMappingType : uint
        {
            UvCoordinateMap,
            CameraCubeEnvMap,
            CameraSphereEnvMap,
            ProjectionMap,
            Shadow,
            ShadowBox
        }

        public enum TextureTransformType : uint     // ????
        {
            DccMaya,
            DccSoftImage,
            Dcc3dsMax
        }


        public int SourceCoordIndex { get; set; }            // ???

        public TextureMappingType MappingType { get; set; }

        public int ReferenceCameraIndex { get; set; }        // ???

        public TextureTransformType TransformType { get; set; }

        public Vector2 Scale { get; set; }
        public float Rotation { get; set; }
        public Vector2 Translation { get; set; }

        public uint Flags { get; set; } //Enabled/Dirty, set by game, SBZ

        public Matrix Transform { get; set; }
    }
}
