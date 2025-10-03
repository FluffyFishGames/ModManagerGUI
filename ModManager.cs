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
            Libraries.Initialize();
            Mod = mod;
            Configuration = configuration;
            var assemblies = new Assembly[] { typeof(ModManager).Assembly };
            //NativeLibrary.Load("libs/libSkiaSharp.dll");
            //NativeLibrary.Load("libs/libHarfBuzzSharp.dll");
            foreach (var assembly in assemblies)
            {
                NativeLibrary.SetDllImportResolver(Assembly.GetAssembly(typeof(ModManager)), ResolveLibrary);
            }
            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(new string[] { });
        }
        
        private static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            IntPtr handle;
            var temp = System.IO.Path.GetTempPath();
            var paths = new List<string>() { System.IO.Path.GetDirectoryName(Environment.ProcessPath), System.IO.Path.Combine(temp, "Translator") };
            foreach (var path in paths)
            {
                if (NativeLibrary.TryLoad(System.IO.Path.Combine(path, libraryName), assembly, searchPath, out handle))
                    return handle;
            }
            return IntPtr.Zero;
        }

// Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();

    }
}
