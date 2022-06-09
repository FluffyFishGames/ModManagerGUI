using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ModManagerGUI
{
    public class ModManager
    {
        public static IMod Mod;
        public static Configuration Configuration;
        public static void Start(IMod mod, Configuration configuration)
        {
            Mod = mod;
            Configuration = configuration;
            NativeLibrary.SetDllImportResolver(Assembly.GetAssembly(typeof(ModManager)), (libraryName, assembly, searchPath) => {
                IntPtr handle;
                var paths = new List<string>() { Environment.CurrentDirectory, System.IO.Path.Combine(Environment.CurrentDirectory, "libs") };
                foreach (var path in paths)
                {
                    System.Console.WriteLine(path);
                    System.Console.WriteLine(searchPath);
                    System.Console.WriteLine(assembly);
                    if (NativeLibrary.TryLoad(System.IO.Path.Combine(path, libraryName), assembly, searchPath, out handle))
                        return handle;
                }
                return IntPtr.Zero;
            });

            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(new string[] { });
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();

    }
}
