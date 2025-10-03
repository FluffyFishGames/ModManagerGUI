using DynamicData;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
#if WINDOWS
using Microsoft.Win32;
#endif
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            string steamDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Application Support/Steam")).Replace("~/", "");
            AnalyzeSteamManifest(steamDirectory);

            
#endif
#if LINUX
            string steamDirectory = System.IO.Path.GetFullPath(System.Environment.GetEnvironmentVariable("HOME") + "/.local/share/Steam");
            AnalyzeSteamManifest(steamDirectory);
#endif
            var languages = ModManager.Mod.GetSupportedLanguages();
            for (var i = 0; i < AvailableLanguageCodes.Count; i++)
            {
                if (languages.Contains(AvailableLanguageCodes[i]))
                {
                    _Languages.Add(AvailableLanguages[i]);
                    LanguageCodes.Add(AvailableLanguageCodes[i]);
                }
            }

            var debugActions = ModManager.Mod.GetDebugActions();
#if DEBUG
            _Debug.Add("Copy tool log");
            DebugActions.Add("Copy tool log", () =>
            {
                TextCopy.ClipboardService.SetText(MainWindow.LogText.ToString());
            });
#endif
            foreach (var action in debugActions)
            {
                _Debug.Add(action.Item1);
                DebugActions.Add(action.Item1, action.Item2);
            }
        }

        private List<string> AvailableLanguages = new List<string>() { "English", "Deutsch", "Български", "中国", "český jazyk", "Dansk", "Nederlands", "eesti keel", "suomen kieli", "Français", "ελληνικά", "magyar nyelv", "Bahasa Indonesia", "italiano", "日本語", "한국어", "latviešu valoda", "lietuvių kalba", "norsk", "polski", "Português", "Português (BR)", "Armâneaşti", "Русский язык", "slovenčina", "slovenščina", "español", "svenska", "Türkçe", "українська мова" };
        private List<string> AvailableLanguageCodes = new List<string>() { "EN", "DE", "BG", "ZH", "CS", "DA", "NL", "ET", "FI", "FR", "EL", "HU", "ID", "IT", "JA", "KO", "LV", "LT", "NB", "PL", "PT", "PT-BR", "RO", "RU", "SK", "SL", "ES", "SV", "TR", "UK" };

        public List<string> _Languages = new List<string>() { };
        public List<string> LanguageCodes = new List<string>() { };
        public List<string> Languages
        {
            get => _Languages;
        }

        public void ExecuteDebug(string name)
        {
            if (DebugActions.ContainsKey(name))
                DebugActions[name]();
        }
        private Dictionary<string, Action> DebugActions = new Dictionary<string, Action>();
        public List<string> _Debug = new List<string>() { };
        public List<string> Debug
        {
            get => _Debug;
        }
        public int _CurrentLanguage = 0;

        public int CurrentLanguage
        {
            get => _CurrentLanguage;
            set => this.RaiseAndSetIfChanged<MainWindowModel, int>(ref _CurrentLanguage, value, "CurrentLanguage");
        }
        private void AnalyzeSteamManifest(string steamDirectory)
        {
            var libraryFile = System.IO.Path.Combine(steamDirectory, "steamapps", "libraryfolders.vdf");

            if (System.IO.File.Exists(libraryFile))
            {
                var libraryFileContent = System.IO.File.ReadAllText(libraryFile);
                VProperty folders = VdfConvert.Deserialize(libraryFileContent);
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
