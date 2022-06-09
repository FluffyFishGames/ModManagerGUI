using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
#if WINDOWS
using Microsoft.Win32;
#endif
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManagerGUI
{
    public class MainWindowModel : ReactiveObject
    {
        public MainWindowModel()
        {
#if WINDOWS
            string steamDirectory = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null);
            if (steamDirectory != null)
            {
                AnalyzeSteamManifest(steamDirectory);
            }
#endif
#if MACOS
            string steamDirectory = System.IO.Path.GetFullPath("~/Library/Application Support/Steam/");
            AnalyzeSteamManifest(steamDirectory);
            
#endif
#if LINUX
            string steamDirectory = System.IO.Path.GetFullPath("~/.local/share/Steam");
            AnalyzeSteamManifest(steamDirectory);
#endif
        }

        private void AnalyzeSteamManifest(string steamDirectory)
        {
            var libraryFile = System.IO.Path.Combine(steamDirectory, "steamapps", "libraryfolders.vdf");
            if (System.IO.File.Exists(libraryFile))
            {
                VProperty folders = VdfConvert.Deserialize(System.IO.File.ReadAllText(libraryFile));
                foreach (var kv in folders.Value as VObject)
                {
                    if (kv.Value is Gameloop.Vdf.Linq.VObject obj)
                    {
                        if (kv.Value["path"] != null)
                        {
                            var path = kv.Value["path"].ToString();
                            if (FindGame(path, out var gamePath))
                            {
                                _Directory = gamePath;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private bool FindGame(string path, out string gamePath)
        {
            var manifestFile = System.IO.Path.Combine(path, "steamapps", "appmanifest_" + ModManager.Configuration.SteamAppID + ".acf");
            if (System.IO.File.Exists(manifestFile))
            {
                VProperty manifest = VdfConvert.Deserialize(System.IO.File.ReadAllText(manifestFile));
                if (manifest.Value is VObject mObj)
                {
                    if (mObj["installdir"] != null)
                    {
                        gamePath = System.IO.Path.Combine(path, "steamapps", "common", mObj["installdir"].ToString());
                        return true;
                    }
                }
            }
            gamePath = null;
            return false;
        }
        private string _Directory;
        public string Directory { get { return _Directory; } set { this.RaiseAndSetIfChanged<MainWindowModel, string>(ref _Directory, value, nameof(Directory)); } }
    }
}
