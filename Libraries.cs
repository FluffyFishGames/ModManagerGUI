using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Skia;
using System.Reflection;

namespace ModManagerGUI
{
    public class Libraries
    {
        private static bool Initialzed = false;
        private static readonly HashSet<string> SaneLibraries = new HashSet<string>()
        {
#if LINUXX64
            "libtinyfiledialogs"
#endif
#if WINDOWSX64
            "tinyfiledialogs64"
#endif
#if MACOSX64
            "tinyfiledialogsIntel"
#endif
#if MACOSARM64
            "tinyfiledialogsAppleSilicon"
#endif
        };

        public static void Initialize()
        {
            if (Initialzed)
                return;
            try
            {
                Initialzed = true;
                var assembly = typeof(Libraries).Assembly;
                var names = assembly.GetManifestResourceNames();
#if WINDOWSX64 || DEBUG
                var extension = ".dll.zstd";
#endif
#if MACOS
                var extension = ".dylib.zstd";
#endif
#if LINUXX64
                var extension = ".so.zstd";
#endif
                var prefix = "ModManagerGUI.libs.";
                var tempPath = Path.Combine(Path.GetTempPath(), "Translator");
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);
                
                foreach (var name in names)
                {
                    if (name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(extension, StringComparison.Ordinal))
                    {
                        var fileName = name.Substring(prefix.Length, name.Length - prefix.Length - extension.Length);
                        if (SaneLibraries.Contains(fileName))
                        {
                            byte[] buffer = new byte[4096];
                            var outputPath = Path.Combine(tempPath, fileName + extension.Replace(".zstd", ""));
                            Resources.ExtractResource(assembly, name, outputPath);
                            if (!NativeLibrary.TryLoad(outputPath, typeof(SkiaSharp.SkiaSharpVersion).Assembly, DllImportSearchPath.SafeDirectories | DllImportSearchPath.UserDirectories, out var handle))
                            {
                            }
                            else
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    System.IO.File.WriteAllText("error.log", e.ToString());
                }
                catch (Exception e2)
                {

                }
            }
        }
    }
}
