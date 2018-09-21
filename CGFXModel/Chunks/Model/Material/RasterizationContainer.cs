using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.Material
{
    public class RasterizationContainer
    {
        public enum CullMode
        {
            FrontFace,
            BackFace,
            Always,
            Never
        }

        // NOTE: Ohana3DS tied this to the same flag in the main Material data...
        // Are they supposed to be the same? Should they match??
        public bool IsPolygonOffsetEnabled { get; set; }    // Actually U32 sized, 0 = false, 1 = true
        public CullMode FaceCulling { get; set; }
        public float PolygonOffsetUnit { get; set; }    // ????

        public uint[] FaceCullingCommand { get; private set; }  // NOTE -- Ohana3DS had THREE unknown uints, but SPICA only reads TWO uints here

        public static RasterizationContainer Load(Utility utility)
        {
            var rc = new RasterizationContainer();

            CGFXDebug.LoadStart(rc, utility);

            rc.IsPolygonOffsetEnabled = (utility.ReadU32() & 1) > 0;
            rc.FaceCulling = (CullMode)utility.ReadU32();
            rc.PolygonOffsetUnit = utility.ReadFloat();

            rc.FaceCullingCommand = utility.ReadUInts(2);

            return rc;
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            utility.Write(IsPolygonOffsetEnabled ? 1U : 0U);
            utility.Write((uint)FaceCulling);
            utility.Write(PolygonOffsetUnit);

            utility.Write(FaceCullingCommand);
        }
    }
}
