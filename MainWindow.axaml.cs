using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ModManagerGUI
{
    public partial class MainWindow : Window
    {

        public const string DLL = "tinyfiledialogs64.dll";
        [DllImport(DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_selectFolderDialog(string aTitle, string aDefaultPathAndFile);
        [DllImport(DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_openFileDialog(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription, int aAllowMultipleSelects);


        private MainWindowModel Model;
        private List<CheckBox> ModCheckboxes = new List<CheckBox>();
        private string LogText = "";
        public MainWindow()
        {
            Model = new MainWindowModel();
            Model.PropertyChanged += Model_PropertyChanged;
            this.DataContext = Model; /** Yea, not beautiful.... but I don't care :D */
            this.InitializeComponent();
            this.FillTexts();
            this.AddMods();
            CheckPath();
            ModManager.Mod.OnLog += Log;
        }

        private void Log(string text)
        {
            LogText += (LogText != "" ? "\r\n" : "") + text;
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                ProgressText.Text = LogText;
                Task.Run(() => {
                    Thread.Sleep(1);
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                        ProgressScroll.ScrollToEnd();
                    });
                });
            });
        }

        private void Model_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Directory")
            {
                CheckPath();
            }
        }

        private void AddMods()
        {
            var language = GetLanguage();
            for (var i = 0; i < ModManager.Configuration.AdditionalMods.Length; i++)
            {
                var stackPanel = new StackPanel();
                stackPanel.Orientation = Avalonia.Layout.Orientation.Horizontal;
                var checkBox = new CheckBox();
                checkBox.Margin = new Avalonia.Thickness(0, 0, 10, 0);
                var textBlock = new TextBlock();
                if (language == "DE")
                    textBlock.Text = ModManager.Configuration.AdditionalMods[i][0];
                else if (language == "EN")
                    textBlock.Text = ModManager.Configuration.AdditionalMods[i][1];
                ModCheckboxes.Add(checkBox);
                stackPanel.Children.Add(checkBox);
                stackPanel.Children.Add(textBlock);
                this.Mods.Children.Add(stackPanel);
            }
        }
        private void CheckPath()
        {
            var language = GetLanguage();
            var directory = Model.Directory;
            System.IO.FileAttributes attributes = 0;
            try
            {
                attributes = System.IO.File.GetAttributes(directory);
            }
            catch (Exception e)
            {

            }
            if ((attributes & System.IO.FileAttributes.Directory) != System.IO.FileAttributes.Directory)
            {
                directory = System.IO.Path.GetDirectoryName(directory);
            }
            if (ModManager.Mod.Verify(directory))
            {
                if (language == "DE")
                    this.GameFoundText.Text = "Spiel gefunden!";
                if (language == "EN")
                    this.GameFoundText.Text = "Game found!";
                Continue0Button.IsEnabled = true;
            }
            else
            {
                if (language == "DE")
                    this.GameFoundText.Text = "Spiel nicht gefunden!";
                if (language == "EN")
                    this.GameFoundText.Text = "Game not found!";
                Continue0Button.IsEnabled = false;
            }
        }

        private string GetLanguage()
        {
            return CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.ToLowerInvariant() == "de" ? "DE" : "EN";
        }
        private void FillTexts()
        {
            var language = GetLanguage();

            this.Title = ModManager.Configuration.ApplicationName;
            this.TitleText.Text = ModManager.Configuration.ApplicationName;
            if (language == "DE")
            {
                this.WelcomeText0.Text = $"Willkommen beim {ModManager.Configuration.ApplicationName}.";
                this.WelcomeText1.Text = $"Diese Software ist KOSTENLOS. Das heißt, wenn Du hierfür bezahlt hast, wurdest Du betrogen.";
                this.WelcomeText2.Text = $"Die offizielle Downloadseite für diese Mod ist https://www.potatoepet.de";
                this.WelcomeText3.Text = $"Du kannst den Quellcode dieser quelloffenen Mod auf https://www.github.com/FluffyFishGames finden";
                this.WelcomeText4.Text = $"Diese Mod wird Deine Spieldateien verändern. Deswegen musst Du einige Dinge wissen. Nicht jeder Fehler, den Du im Spiel findest, tritt unbedingt in einem unmodifizierten Spiel auf. Bevor Du also einen Fehler an {ModManager.Configuration.DeveloperName} meldest, stelle sicher, dass der Fehler auch in einer unmodifizierten Version des Spiels auftritt.";
                this.WelcomeText5.Text = $"Sollte es ein Spielupdate geben, wird die Mod nicht mehr funktionieren. Der beste Weg mit einem Update umzugehen, ist zu warten, bis der Mod aktualisiert wird.";
                this.PathText0.Text = $"Um Dein Spiel zu modifizieren, wird der Pfad zu Deinen Spieldateien benötigt.";
                this.PathText1.Text = $"Solltest Du Probleme haben, Deine Spieldateien zu finden, so kannst Du ihn in Steam finden. Klicke dazu einfach mit einem Rechtsklick auf Dein Spiel und wähle \"Eigenschaften\" aus.";
                this.PathText2.Text = $"In dem Fenster, das sich öffnet, wählst Du auf der linken Seite \"Lokale Dateien\" aus.";
                this.PathText3.Text = $"Anschließend klickst du auf den Knopf \"Durchsuchen\" auf der rechten Seite.";
                this.PathText4.Text = $"Es öffnet sich ein Windows Explorer-Fenster. Klicke oben auf den weißen Bereich des Pfads. Kopiere den nun selektierten Pfad und trage ihn in das Programm ein.";
                this.ModsText0.Text = $"Zusätzlich bietet diese Mod einige kleine Anpassungen, die Du am Spiel vornehmen kannst.";
                this.ModsText1.Text = $"Wähle einfach aus der Liste aus, welche zusätzlichen Funktionen Du nutzen möchtest:";
                this.UnderstoodText.Text = "Verstanden";
                this.BrowseText.Text = "Durchsuchen";
                this.Continue0Text.Text = "Weiter";
                this.Continue1Text.Text = "Weiter";
                this.FinishText.Text = "Schließen";
            }
            else if (language == "EN")
            {
                this.WelcomeText0.Text = $"Welcome to {ModManager.Configuration.ApplicationName}.";
                this.WelcomeText1.Text = $"This software is FREE. That means if you paid for this, you were scammed.";
                this.WelcomeText2.Text = $"The official download page for this mod is https://www.potatoepet.de";
                this.WelcomeText3.Text = $"You can find the source code of this open source mod at https://www.github.com/FluffyFishGames";
                this.WelcomeText4.Text = $"This mod will change your game files. That's why you need to know some things. Not every bug you find in the game will necessarily occur in an unmodified game. So before you report a bug to {ModManager.Configuration.DeveloperName}, make sure that the bug also occurs in an unmodified version of the game.";
                this.WelcomeText5.Text = $"If there is a game update, the mod will no longer work. The best way to deal with an update is to wait until the mod is updated.";
                this.PathText0.Text = $"To modify your game, the path to your game files is needed.";
                this.PathText1.Text = $"If you have problems finding your game files, you can find it in Steam.Just right-click on your game and select \"Properties\".";
                this.PathText2.Text = $"In the window that opens, select \"Local Files\" on the left side.";
                this.PathText3.Text = $"Then click on the \"Browse\" button on the right side.";
                this.PathText4.Text = $"A Windows Explorer window will open. Click on the white area of the path at the top.Copy the now selected path and enter it into the program.";
                this.ModsText0.Text = $"Additionally, this mod offers some small adjustments that you can make to the game.";
                this.ModsText1.Text = $"Just choose from the list which additional features you want to use:";
                this.UnderstoodText.Text = "Understood";
                this.BrowseText.Text = "Browse";
                this.Continue0Text.Text = "Continue";
                this.Continue1Text.Text = "Continue";
                this.FinishText.Text = "Close";
            }
        }

        private static string StringFromANSI(IntPtr ptr) // for UTF-8/char
        {
            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
        }

        public void MoveWindow(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            /*System.Console.WriteLine(e);
            System.Console.WriteLine(e.RoutedEvent);
            Grid g;
            g.PointerPressed += G_PointerPressed;*/
                this.BeginMoveDrag(e);
        }
        public void Understood(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.IntroductionPanel.IsVisible = false;
            this.SelectGamePathPanel.IsVisible = true;
        }

        public void ContinueToMods(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.SelectGamePathPanel.IsVisible = false;

            if (ModManager.Configuration.AdditionalMods.Length > 0)
            {
                this.SelectModsPanel.IsVisible = true;
            }
            else
                ContinueToApply(sender, e);
        }

        public void ContinueToApply(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.SelectModsPanel.IsVisible = false;
            this.ProgressPanel.IsVisible = true;
            var options = new HashSet<int>();
            for (var i = 0; i < ModCheckboxes.Count; i++)
                if (ModCheckboxes[i].IsChecked.HasValue && ModCheckboxes[i].IsChecked.Value)
                    options.Add(i);
            Task.Run(() =>
            {
                try
                {
                    ModManager.Mod.Apply(Model.Directory, options);
                }
                catch (Exception ex)
                {
                    Log("An error occured :(");
                    Log(ex.ToString());
                }

                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FinishButton.IsEnabled = true;
                });
            });
            //this.ApplyModsPanel.IsVisible = true;
        }

        public void Finish(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
            //Log("Test");
            //this.SelectModsPanel.IsVisible = false;
            //this.ApplyModsPanel.IsVisible = true;
        }

        public void Browse(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var filter = new string[ModManager.Configuration.FileNames.Length];
            for (var i = 0; i < ModManager.Configuration.FileNames.Length; i++)
                filter[i] = ModManager.Configuration.FileNames[i] + ".exe";
            var ptr = tinyfd_openFileDialog("Please select game exe", this.Model.Directory, filter.Length, filter, "Game EXE", 0);
            //var ptr = tinyfd_selectFolderDialog("Please select game path", this.Model.Directory);
            var newValue = StringFromANSI(ptr);
            if (newValue != null)
                this.Model.Directory = newValue;
        }
    }
}
