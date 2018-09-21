using CGFXModel.Utilities;

namespace CGFXModel.Chunks.Model.Material
{
    // NOTE -- appears that Material Color is stored as both "float"-based color and "byte"-based color.
    // Keep this in mind if you're changing these that they probably should stay in sync.
    // I'm not sure which is used by games, either or both...
    public class MaterialColorContainer
    {
        public ColorFloat EmissionF { get; set; }
        public ColorFloat AmbientF { get; set; }
        public ColorFloat DiffuseF { get; set; }
        public ColorFloat Specular0F { get; set; }
        public ColorFloat Specular1F { get; set; }
        public ColorFloat Constant0F { get; set; }
        public ColorFloat Constant1F { get; set; }
        public ColorFloat Constant2F { get; set; }
        public ColorFloat Constant3F { get; set; }
        public ColorFloat Constant4F { get; set; }
        public ColorFloat Constant5F { get; set; }

        public Color Emission { get; set; }
        public Color Ambient { get; set; }
        public Color Diffuse { get; set; }
        public Color Specular0 { get; set; }
        public Color Specular1 { get; set; }
        public Color Constant0 { get; set; }
        public Color Constant1 { get; set; }
        public Color Constant2 { get; set; }
        public Color Constant3 { get; set; }
        public Color Constant4 { get; set; }
        public Color Constant5 { get; set; }

        public uint CommandCache { get; private set; }

        public static MaterialColorContainer Load(Utility utility)
        {
            var mcc = new MaterialColorContainer();

            CGFXDebug.LoadStart(mcc, utility);

            mcc.EmissionF = ColorFloat.Read(utility);
            mcc.AmbientF = ColorFloat.Read(utility);
            mcc.DiffuseF = ColorFloat.Read(utility);
            mcc.Specular0F = ColorFloat.Read(utility);
            mcc.Specular1F = ColorFloat.Read(utility);
            mcc.Constant0F = ColorFloat.Read(utility);
            mcc.Constant1F = ColorFloat.Read(utility);
            mcc.Constant2F = ColorFloat.Read(utility);
            mcc.Constant3F = ColorFloat.Read(utility);
            mcc.Constant4F = ColorFloat.Read(utility);
            mcc.Constant5F = ColorFloat.Read(utility);

            mcc.Emission = Color.Read(utility);
            mcc.Ambient = Color.Read(utility);
            mcc.Diffuse = Color.Read(utility);
            mcc.Specular0 = Color.Read(utility);
            mcc.Specular1 = Color.Read(utility);
            mcc.Constant0 = Color.Read(utility);
            mcc.Constant1 = Color.Read(utility);
            mcc.Constant2 = Color.Read(utility);
            mcc.Constant3 = Color.Read(utility);
            mcc.Constant4 = Color.Read(utility);
            mcc.Constant5 = Color.Read(utility);

            mcc.CommandCache = utility.ReadU32();

            return mcc;
        }

        public void Save(SaveContext saveContext)
        {
            var utility = saveContext.Utility;

            CGFXDebug.SaveStart(this, saveContext);

            EmissionF.Save(utility);
            AmbientF.Save(utility);
            DiffuseF.Save(utility);
            Specular0F.Save(utility);
            Specular1F.Save(utility);
            Constant0F.Save(utility);
            Constant1F.Save(utility);
            Constant2F.Save(utility);
            Constant3F.Save(utility);
            Constant4F.Save(utility);
            Constant5F.Save(utility);

            Emission.Save(utility);
            Ambient.Save(utility);
            Diffuse.Save(utility);
            Specular0.Save(utility);
            Specular1.Save(utility);
            Constant0.Save(utility);
            Constant1.Save(utility);
            Constant2.Save(utility);
            Constant3.Save(utility);
            Constant4.Save(utility);
            Constant5.Save(utility);

            utility.Write(CommandCache);
        }
    }
}
