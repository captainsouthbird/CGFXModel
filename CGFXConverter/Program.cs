using CGFXModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace CGFXConverter
{
    class Program
    {
        enum Operations
        {
            Unknown,
            ExportFromCGFX,
            ImportToCGFX,
        }

        class OperationInfo
        {
            public Operations Operation { get; set; }
            public int MinArgs { get; set; }
            public int MaxArgs { get; set; }
        }

        static int Main(string[] args)
        {
            var operationArg = (args.Length >= 1) ? args[0].ToLower() : "";
            if(operationArg.StartsWith("-") || operationArg.StartsWith("/"))
            {
                operationArg = operationArg.Substring(1);
            }

            var operationMap = new Dictionary<string, OperationInfo>
            {
                { "", new OperationInfo { Operation = Operations.Unknown } },

                // export: [infile] [outfile]
                { "export", new OperationInfo { Operation = Operations.ExportFromCGFX, MinArgs = 3, MaxArgs = 3 } },

                // import: [base] [infile] [outfile]
                { "import", new OperationInfo { Operation = Operations.ImportToCGFX, MinArgs = 4, MaxArgs = 4 } }
            };

            var opInfo = operationMap.ContainsKey(operationArg) ? operationMap[operationArg] : operationMap.First().Value;

            if (args.Length < opInfo.MinArgs || args.Length > opInfo.MaxArgs)
            {
                if(opInfo.Operation != Operations.Unknown)
                {
                    Console.Error.WriteLine("Incorrect number of arguments; see Usage.\n");
                }
                else
                {
                    Console.Error.WriteLine($@"Unknown operation ""{operationArg}""");
                }

                Console.Error.WriteLine(@"USAGE: 

------------------------------------------------------------------------------

CGFXConverter export [CGFX input file] [output model file]

[input model file]  = Source BCRES/CGFX filename to convert from
[output model file] = Destination filename to convert to

Supported output file types are: ms3d (MilkShape)

------------------------------------------------------------------------------

CGFXConverter import [CGFX base file] [CGFX input file] [output model file]

[CGFX base file]    = Unmodified original BCRES/CGFX file (ensures bugs in the process don't accumulate)
[input model file]  = Source model filename to convert from 
[output model file] = Destination BCRES/CGFX filename to convert to

Supported input file types are: ms3d (MilkShape)

------------------------------------------------------------------------------

");

                return 1;
            }

            try
            {
                if (opInfo.Operation == Operations.ExportFromCGFX || opInfo.Operation == Operations.ImportToCGFX)
                {
                    ExportImportCGX(opInfo, args);
                }
                else
                {
                    // Unimplemented operation
                    throw new NotImplementedException();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("FAILED: " + e.Message + "\n" + e.StackTrace);
                return 1;
            }
        }

        private static void ExportImportCGX(OperationInfo opInfo, string[] args)
        {
            // The base, input, and output files
            var baseFile = args[1];
            var inFile = args[args.Length == 4 ? 2 : 1];
            var outFile = args[args.Length == 4 ? 3 : 2];

            var inFileExt = Path.GetExtension(inFile).ToLower();
            var outFileExt = Path.GetExtension(outFile).ToLower();

            // As we must have a BCRES for "backing", it will be loaded either way
            // before we do anything else.
            CGFX cgfxBase;
            SimplifiedModel simplifiedModel;
            using (var br = new BinaryReader(File.Open(baseFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                // Load the BCRES (CGFX)
                cgfxBase = CGFX.Load(br);

                // Create a "SimplifiedModel" out of the base CGFX
                simplifiedModel = new SimplifiedModel(cgfxBase);
            }

            try
            {
                Console.WriteLine($"Converting {inFile} to {outFile}...");

                // If we're converting FROM a BCRES TO another file type...
                if (opInfo.Operation == Operations.ExportFromCGFX)
                {
                    // Convert it and save it to the requested file type
                    using (var bw = new BinaryWriter(File.Open(outFile, FileMode.Create)))
                    {
                        if (outFileExt == ".ms3d")
                        {
                            var milkShape = MilkShapeConverter.ToMilkShape(simplifiedModel);
                            milkShape.Save(bw);

                        }
                        else
                        {
                            throw new NotImplementedException($"Unsupported Destination filetype {outFileExt}");
                        }
                    }

                    // Dump textures
                    if (simplifiedModel.Textures != null)
                    {
                        var dumpTextureDir = Path.GetDirectoryName(outFile);
                        foreach (var texture in simplifiedModel.Textures)
                        {
                            Console.WriteLine($"Exporting texture {texture.Name}...");
                            texture.TextureBitmap.Save(Path.Combine(dumpTextureDir, texture.Name + ".png"));
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine("Done.");
                    Console.WriteLine();
                    Console.WriteLine("Note, if there are any textures you do NOT want to modify, you can delete the respective PNG files");
                    Console.WriteLine("and they'll be skipped on import.");
                    Console.WriteLine();
                    Console.WriteLine();
                }
                else
                {
                    // Converting from a model file TO a BCRES...

                    if(baseFile == outFile)
                    {
                        // The only reason I'm actually blocking this is lack of trust in that if
                        // there's a bug that causes data loss, it may compound over subsequent
                        // writes. So until I trust this code more, we'll force an "unmodified" base
                        // to be used. (Of course, I can't KNOW the base is "unmodified"...)
                        throw new InvalidOperationException("Currently writing to the base BCRES/CGFX file is prohibited. May lift this restriction in the future.");
                    }

                    using (var br = new BinaryReader(File.Open(inFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    using (var bw = new BinaryWriter(File.Open(outFile, FileMode.Create)))
                    {
                        if (inFileExt == ".ms3d")
                        {
                            var milkShape = MilkShape.Load(br);
                            MilkShapeConverter.FromMilkShape(simplifiedModel, milkShape);
                        }
                        else
                        {
                            throw new NotImplementedException($"Unsupported Source filetype {inFileExt}");
                        }

                        // Import textures
                        var dumpTextureDir = Path.GetDirectoryName(inFile);

                        // Find WHAT textures we have (user has option to not include any or all)
                        if (simplifiedModel.Textures != null)
                        {
                            var importTextures = simplifiedModel.Textures
                                .Select(t => new
                                {
                                    Filename = Path.Combine(dumpTextureDir, t.Name + ".png"),
                                    Texture = t
                                })
                                .Where(t => File.Exists(t.Filename))
                                .Select(t => new
                                {
                                    t.Texture.Name,
                                    TextureBitmap = Image.FromFile(t.Filename)
                                })
                                .ToList();

                            foreach (var texture in importTextures)
                            {
                                Console.WriteLine($"Importing texture {texture.Name}...");

                                // Corresponding texture in SimplifiedModel
                                var smTexture = simplifiedModel.Textures.Where(t => t.Name == texture.Name).Single();
                                smTexture.TextureBitmap = (Bitmap)texture.TextureBitmap;
                            }
                        }

                        simplifiedModel.RecomputeVertexNormals();
                        simplifiedModel.ApplyChanges();
                        cgfxBase.Save(bw);
                    }


                    Console.WriteLine();
                    Console.WriteLine("Done.");
                    Console.WriteLine();
                }
                
            }
            catch
            {
                if (File.Exists(outFile))
                {
                    File.Delete(outFile);
                }

                throw;
            }
        }
    }
}
