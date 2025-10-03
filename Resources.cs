using Avalonia.Markup.Xaml.Templates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModManagerGUI
{
    public class Resources
    {
        public static void ExtractResource(Assembly assembly, string name, string outputPath)
        {
            if (name.EndsWith(".zstd"))
            {
                using (var input = assembly.GetManifestResourceStream(name))
                using (var decoder = new ZstdSharp.DecompressionStream(input))
                using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    decoder.CopyTo(output, 4096);
                }
            }
            else
            {
                byte[] buffer = new byte[4096];
                using (var input = assembly.GetManifestResourceStream(name))
                using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    input.CopyTo(output);
                }
            }
        }


        public static string GetResource(Assembly assembly, string name)
        {
            if (name.EndsWith(".zstd"))
            {
                using (var input = assembly.GetManifestResourceStream(name))
                using (var decoder = new ZstdSharp.DecompressionStream(input))
                using (var output = new MemoryStream())
                {
                    decoder.CopyTo(output, 4096);
                    return System.Text.Encoding.UTF8.GetString(output.ToArray());
                }
            }
            else
            {
                byte[] buffer = new byte[4096];
                using (var input = assembly.GetManifestResourceStream(name))
                using (var output = new MemoryStream())
                {
                    input.CopyTo(output);
                    return System.Text.Encoding.UTF8.GetString(output.ToArray());
                }
            }
        }

        public static byte[] GetResourceBytes(Assembly assembly, string name)
        {
            if (name.EndsWith(".zstd"))
            {
                using (var input = assembly.GetManifestResourceStream(name))
                using (var decoder = new ZstdSharp.DecompressionStream(input))
                using (var output = new MemoryStream())
                {
                    decoder.CopyTo(output, 4096);
                    return output.ToArray();
                }
            }
            else
            {
                byte[] buffer = new byte[4096];
                using (var input = assembly.GetManifestResourceStream(name))
                using (var output = new MemoryStream())
                {
                    input.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
    }
}
