using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using Tmds.DBus;
using Avalonia.OpenGL;
using Avalonia.Styling;
using DynamicData.Tests;
using DynamicData;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using System.Timers;
using Avalonia.Media;
using System.Collections;
using System.Diagnostics.Metrics;
using System.Reactive;
using System.Data;
using System.Reactive.Joins;
using Avalonia.Animation;
using Mono.Cecil.Cil;
using System.ComponentModel;
using System.Net;
using System.Text;
using static ModManagerGUI.IMod;
using Avalonia.Controls.Shapes;
using DynamicData.Aggregation;
using HarfBuzzSharp;
using Metsys.Bson;
using System.Text.RegularExpressions;
using DynamicData.Kernel;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using static Gameloop.Vdf.VdfReader;
using System.Security.Cryptography;
using Avalonia.Media.TextFormatting;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

namespace ModManagerGUI
{
    public partial class MainWindow : Window
    {
#if WINDOWSX64
        public const string DLL = "tinyfiledialogs64.dll";
#endif
#if LINUXX64
        public const string DLL = "libtinyfiledialogs.so";
#endif
#if MACOSX64
        public const string DLL = "tinyfiledialogsIntel.dylib";
#endif
#if MACOSARM64
        public const string DLL = "tinyfiledialogsAppleSilicon.dylib";
#endif
        [DllImport(DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_selectFolderDialog(string aTitle, string aDefaultPathAndFile);
        [DllImport(DLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_openFileDialog(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription, int aAllowMultipleSelects);


        private MainWindowModel Model;
        private List<CheckBox> ModCheckboxes = new List<CheckBox>();
        private List<TextBlock> ModTextBlocks = new List<TextBlock>();
        public static StringBuilder LogText = new StringBuilder();
        public MainWindow()
        {
            Model = new MainWindowModel();
            var ind = Model.LanguageCodes.IndexOf(this.GetLanguage());
            if (ind == -1)
                ind = 0;
            Model.CurrentLanguage = ind;
            Model.PropertyChanged += Model_PropertyChanged;
            this.DataContext = Model; /** Yea, not beautiful.... but I don't care :D */
            this.InitializeComponent();
#if MACOS
            TopBorder.Classes.Add("MacOS");
#endif
#if DEBUG
            this.AttachDevTools();
#endif
            if (this.Model.Debug.Count == 0)
                this.DebugOptions.IsVisible = false;
            else
            {
                this.DebugOptions.SelectionChanged += DebugOptions_SelectionChanged;
            }
            this.FillTexts();
            this.AddMods();
            CheckPath();
            ModManager.Mod.OnLog += Log;
            ModManager.Mod.OnProgress += Progress;
            if (ModManager.Configuration.ShowExtract)
                this.ExtractButton.IsVisible = true;

            Task r = new Task(() =>
            {
                while (true)
                {
                    Thread.Sleep(100);
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                        var change = false;
                        while (Logs.TryDequeue(out var log))
                        {
                            change = true;
                            var textBlock = new TextBlock();
                            var className = "Log";
                            if (log.StartsWith("Warning: ", StringComparison.Ordinal))
                                className = "Warning";
                            if (log.StartsWith("Error: ", StringComparison.Ordinal))
                                className = "Error";
                            textBlock.Classes = new Classes(className);
                            textBlock.Text = log;
                            ProgressStack.Children.Add(textBlock);
                        }
                        if (change)
                        {
                            Task.Run(() =>
                            {
                                Thread.Sleep(5);
                                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    ProgressScroll.ScrollToEnd();
                                });
                            });
                        }
                    });
                }
            });
            r.Start();
        }

        private void DebugOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                this.Model.ExecuteDebug((string)e.AddedItems[0]);
                this.DebugOptions.SelectedItem = null;
            }
        }

        private ConcurrentQueue<string> Logs = new ConcurrentQueue<string>();
        private void Log(string text)
        {
            Logs.Enqueue(text);
            LogText.Append(text + "\r\n");
        }

        private void Progress(float percent, string text)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                ProgressBar.Value = percent;
                if (text != null && text != "")
                    ProgressText.Text = text;
            });
        }

        private void Model_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Directory")
            {
                CheckPath();
            }
            if (e.PropertyName == "CurrentLanguage")
                this.FillTexts();
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

                if (ModManager.Configuration.AdditionalMods[i].Length > Model.CurrentLanguage)
                    textBlock.Text = ModManager.Configuration.AdditionalMods[i][Model.CurrentLanguage];
                else
                    textBlock.Text = ModManager.Configuration.AdditionalMods[i][0];
                ModTextBlocks.Add(textBlock);
                checkBox.IsChecked = ModManager.Configuration.StandardMods.Contains(i);
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
                else
                    this.GameFoundText.Text = "Game found!";
                Continue0Button.IsEnabled = true;
            }
            else
            {
                if (language == "DE")
                    this.GameFoundText.Text = "Spiel nicht gefunden!";
                else
                    this.GameFoundText.Text = "Game not found!";
                Continue0Button.IsEnabled = false;
            }
        }

        private string GetLanguage()
        {
            return CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.ToUpperInvariant();
        }
        private void FillTexts()
        {
            var language = this.Model.LanguageCodes[this.Model.CurrentLanguage];

            this.Title = ModManager.Configuration.ApplicationName;
            this.TitleText.Text = ModManager.Configuration.ApplicationName;
            string[] translations = new string[0];
            for (var i = 0; i < ModTextBlocks.Count; i++)
            {
                if (ModManager.Configuration.AdditionalMods[i].Length > Model.CurrentLanguage)
                    ModTextBlocks[i].Text = ModManager.Configuration.AdditionalMods[i][Model.CurrentLanguage];
                else
                    ModTextBlocks[i].Text = ModManager.Configuration.AdditionalMods[i][0];
            }
            if (language == "DE")
            {
                translations = new string[] {
                    $"Willkommen beim {ModManager.Configuration.ApplicationName}.",
                    $"Diese Software ist KOSTENLOS. Das heißt, wenn Du hierfür bezahlt hast, wurdest Du betrogen.",
                    $"Die offizielle Downloadseite für diese Mod ist https://www.potatoepet.de",
                    $"Du kannst den Quellcode dieser quelloffenen Mod auf https://www.github.com/FluffyFishGames finden",
                    $"Diese Mod wird Deine Spieldateien verändern. Deswegen musst Du einige Dinge wissen. Nicht jeder Fehler, den Du im Spiel findest, tritt unbedingt in einem unmodifizierten Spiel auf. Bevor Du also einen Fehler an {ModManager.Configuration.DeveloperName} meldest, stelle sicher, dass der Fehler auch in einer unmodifizierten Version des Spiels auftritt.",
                    $"Sollte es ein Spielupdate geben, wird die Mod nicht mehr funktionieren. Der beste Weg mit einem Update umzugehen, ist zu warten, bis der Mod aktualisiert wird.",
                    $"Um Dein Spiel zu modifizieren, wird der Pfad zu Deinen Spieldateien benötigt.",
                    $"Solltest Du Probleme haben, Deine Spieldateien zu finden, so kannst Du ihn in Steam finden. Klicke dazu einfach mit einem Rechtsklick auf Dein Spiel und wähle \"Eigenschaften\" aus.",
                    $"In dem Fenster, das sich öffnet, wählst Du auf der linken Seite \"Lokale Dateien\" aus.",
                    $"Anschließend klickst du auf den Knopf \"Durchsuchen\" auf der rechten Seite.",
                    $"Es öffnet sich ein Windows Explorer-Fenster. Klicke oben auf den weißen Bereich des Pfads. Kopiere den nun selektierten Pfad und trage ihn in das Programm ein.",
                    $"Zusätzlich bietet diese Mod einige kleine Anpassungen, die Du am Spiel vornehmen kannst.",
                    $"Wähle einfach aus der Liste aus, welche zusätzlichen Funktionen Du nutzen möchtest:",
                    $"Verstanden",
                    $"Durchsuchen",
                    $"Weiter",
                    $"Weiter",
                    $"Schließen",
                    $"Texte extrahieren"
                };
            }
            else if (language == "EN")
            {
                translations = new string[] {
                    $"Welcome to {ModManager.Configuration.ApplicationName}.",
                    $"This software is FREE. That means if you paid for this, you were scammed.",
                    $"The official download page for this mod is https://www.potatoepet.de",
                    $"You can find the source code of this open source mod at https://www.github.com/FluffyFishGames",
                    $"This mod will change your game files. That's why you need to know some things. Not every bug you find in the game will necessarily occur in an unmodified game. So before you report a bug to {ModManager.Configuration.DeveloperName}, make sure that the bug also occurs in an unmodified version of the game.",
                    $"If there is a game update, the mod will no longer work. The best way to deal with an update is to wait until the mod is updated.",
                    $"To modify your game, the path to your game files is needed.",
                    $"If you have problems finding your game files, you can find it in Steam. Just right-click on your game and select \"Properties\".",
                    $"In the window that opens, select \"Local Files\" on the left side.",
                    $"Then click on the \"Browse\" button on the right side.",
                    $"A Windows Explorer window will open. Click on the white area of the path at the top.Copy the now selected path and enter it into the program.",
                    $"Additionally, this mod offers some small adjustments that you can make to the game.",
                    $"Just choose from the list which additional features you want to use:",
                    $"Understood",
                    $"Browse",
                    $"Continue",
                    $"Continue",
                    $"Close",
                    $"Extract texts"
                };
            }
            else if (language == "BG")
            {
                translations = new string[]
                {
                    $"Добре дошли в {ModManager.Configuration.ApplicationName}.",
                    $"Този софтуер е БЕЗПЛАТЕН. Това означава, че ако сте платили за него, сте били измамени.",
                    $"Официалната страница за изтегляне на този мод е https://www.potatoepet.de",
                    $"Можете да намерите изходния код на този мод с отворен код на адрес https://www.github.com/FluffyFishGames",
                    $"Този мод ще промени файловете на играта ви. Ето защо трябва да знаете някои неща. Не е задължително всеки бъг, който откриете в играта, да се появи и в немодифицирана игра. Затова, преди да съобщите за грешка на {ModManager.Configuration.DeveloperName}, се уверете, че грешката се среща и в немодифицирана версия на играта.",
                    $"Ако има актуализация на играта, модът вече няма да работи. Най-добрият начин да се справите с актуализация е да изчакате, докато модът бъде актуализиран.",
                    $"За да модифицирате играта си, е необходим пътят до файловете на играта.",
                    $"Ако имате проблеми с намирането на файловете на играта, можете да ги намерите в Steam. просто щракнете с десния бутон на мишката върху играта си и изберете \"Свойства\".",
                    $"В прозореца, който се отваря, изберете \"Локални файлове\" от лявата страна.",
                    $"След това щракнете върху бутона \" Преглед\" от дясната страна.",
                    $"Ще се отвори прозорец на Windows Explorer. Кликнете върху бялата област на пътя в горната част. копирайте сега избрания път и го въведете в програмата.",
                    $"Освен това този мод предлага някои малки корекции, които можете да направите в играта.",
                    $"Просто изберете от списъка кои допълнителни функции искате да използвате:",
                    $"Разбрах",
                    $"Преглед на",
                    $"Продължи",
                    $"Продължи",
                    $"Затвори",
                    $"Извлечете текстове"
                };
            }
            else if (language == "ZH")
            {
                translations = new string[]
                {
                    $"欢迎使用 {ModManager.Configuration.ApplicationName}。",
                    $"本软件是免费的。如果你为此付费了，意味着你就被骗了。",
                    $"本mod的官方下载页面是https://www.potatoepet.de",
                    $"你可以在https://www.github.com/FluffyFishGames 找到这个开源MOD的源代码",
                    $"这个mod会修改你的游戏文件。这就是为什么你需要明白注意事项。不是每个你在游戏中发现的BUG都一定会出现在未修改的游戏中。所以在你向 {ModManager.Configuration.DeveloperName} 报告一个BUG之前，要确保这个BUG也出现在未修改的游戏版本中。",
                    $"如果游戏更新了，该MOD将不再生效。处理更新的最好方法是等待MOD的更新。",
                    $"要修改你的游戏，需要你的游戏文件的路径。",
                    $"如果你找不到你的游戏文件，你可以在Steam中找到它。只要右键点击你的游戏，选择 \"属性\"。",
                    $"在打开的窗口中，选择左边的 \"本地文件\"。",
                    $"然后点击右侧的 \"浏览\"按钮。",
                    $"一个Windows Explorer窗口将打开。点击顶部的路径。并复制的路径，并将其粘贴到程序中。",
                    $"此外，这个MOD提供了一些小小的辅助功能，可以对游戏改善体验。",
                    $"只要从列表中选择你想使用的额外功能即可。",
                    $"理解",
                    $"浏览",
                    $"继续",
                    $"继续",
                    $"关闭",
                    $"摘录文本"
                };
            }
            else if (language == "CS")
            {
                translations = new string[]
                {
                    $"Vítejte v {ModManager.Configuration.ApplicationName}.",
                    $"Tento software je ZDARMA. To znamená, že pokud jste za něj zaplatili, byli jste podvedeni.",
                    $"Oficiální stránka pro stažení tohoto módu je https://www.potatoepet.de.",
                    $"Zdrojový kód tohoto open source módu najdete na adrese https://www.github.com/FluffyFishGames.",
                    $"Tento mod změní vaše herní soubory. Proto musíte vědět některé věci. Ne každá chyba, kterou ve hře najdete, se musí nutně vyskytovat i v nemodifikované hře. Než tedy nahlásíte chybu společnosti {ModManager.Configuration.DeveloperName}, ujistěte se, že se chyba vyskytuje i v nemodifikované verzi hry.",
                    $"Pokud dojde k aktualizaci hry, mod již nebude fungovat. Nejlepší způsob, jak se s aktualizací vypořádat, je počkat, až bude mod aktualizován.",
                    $"K modifikaci hry je potřeba znát cestu k souborům hry.",
                    $"Pokud máte problémy s nalezením herních souborů, můžete je najít ve službě Steam: Stačí kliknout pravým tlačítkem myši na hru a vybrat \"Vlastnosti\".",
                    $"V okně, které se otevře, vyberte na levé straně položku \"Místní soubory\".",
                    $"Poté klikněte na tlačítko \"Procházet\" na pravé straně.",
                    $"Otevře se okno Průzkumníka Windows. Klikněte na bílou oblast cesty v horní části. zkopírujte nyní vybranou cestu a zadejte ji do programu.",
                    $"Tento mod navíc nabízí několik drobných úprav, které můžete ve hře provést.",
                    $"Stačí si ze seznamu vybrat, které další funkce chcete použít:",
                    $"Rozumím.",
                    $"Procházet",
                    $"Pokračovat",
                    $"Pokračovat",
                    $"Zavřít",
                    $"Výpis textů"
                };
            }
            else if (language == "DA")
            {
                translations = new string[]
                {
                    $"Velkommen til {ModManager.Configuration.ApplicationName}.",
                    $"Denne software er GRATIS. Det betyder, at hvis du har betalt for dette, er du blevet snydt.",
                    $"Den officielle download - side for denne mod er https://www.potatoepet.de",
                    $"Du kan finde kildekoden til dette open source mod på https://www.github.com/FluffyFishGames",
                    $"Denne mod vil ændre dine spilfiler.Derfor er du nødt til at vide nogle ting. Ikke alle fejl, du finder i spillet, vil nødvendigvis forekomme i et umodificeret spil.Så før du rapporterer en fejl til {ModManager.Configuration.DeveloperName}, skal du sikre dig, at fejlen også forekommer i en ikke - modificeret version af spillet.",
                    $"Hvis der kommer en opdatering af spillet, vil mod'et ikke længere fungere. Den bedste måde at håndtere en opdatering på er at vente, indtil mod'et er opdateret.",
                    $"For at ændre dit spil skal du bruge stien til dine spilfiler.",
                    $"Hvis du har problemer med at finde dine spilfiler, kan du finde dem i Steam.du skal bare højreklikke på dit spil og vælge \"Egenskaber\".",
                    $"I det vindue, der åbnes, skal du vælge \"Lokale filer\" i venstre side.",
                    $"Klik derefter på knappen \"Gennemse\" i højre side.",
                    $"Et vindue i Windows Stifinder vil åbne.Klik på det hvide område af stien øverst.kopier den nu valgte sti, og indtast den i programmet.",
                    $"Derudover tilbyder dette mod nogle små justeringer, som du kan foretage i spillet.",
                    $"Du skal blot vælge fra listen, hvilke ekstra funktioner du ønsker at bruge:",
                    $"Forstået",
                    $"Browse",
                    $"Fortsæt",
                    $"Fortsæt",
                    $"Luk",
                    $"Uddrag af tekster"
                };
            }
            else if (language == "NL")
            {
                translations = new string[]
                {
                    $"Welkom bij {ModManager.Configuration.ApplicationName}.",
                    $"Deze software is GRATIS. Dat betekent dat als je hiervoor betaald hebt, je opgelicht bent.",
                    $"De officiële downloadpagina voor deze mod is https://www.potatoepet.de",
                    $"Je kunt de broncode van deze open source mod vinden op https://www.github.com/FluffyFishGames",
                    $"Deze mod zal je spelbestanden veranderen. Daarom moet je een aantal dingen weten. Niet elke bug die je in het spel vindt zal noodzakelijkerwijs ook voorkomen in een ongewijzigd spel. Dus voordat je een bug meldt aan {ModManager.Configuration.DeveloperName}, moet je er zeker van zijn dat de bug ook voorkomt in een ongewijzigde versie van het spel.",
                    $"Als er een spelupdate is, werkt de mod niet meer. De beste manier om met een update om te gaan is te wachten tot de mod is bijgewerkt.",
                    $"Om je spel aan te passen is het pad naar je spelbestanden nodig.",
                    $"Als je problemen hebt met het vinden van je spelbestanden, kun je die vinden in Steam.Klik gewoon met de rechtermuisknop op je spel en kies \"Eigenschappen\".",
                    $"In het venster dat opent, selecteer je aan de linkerkant \"Lokale bestanden\".",
                    $"Klik dan op de knop \"Bladeren\" aan de rechterkant.",
                    $"Een venster van de Windows Verkenner wordt geopend. Klik op het witte vlak van het pad bovenaan. Kopieer het nu geselecteerde pad en voer het in het programma in.",
                    $"Daarnaast biedt deze mod enkele kleine aanpassingen die je in het spel kunt aanbrengen.",
                    $"Kies gewoon uit de lijst welke extra functies je wilt gebruiken:",
                    $"Begrepen",
                    $"Bladeren",
                    $"Doorgaan",
                    $"Doorgaan",
                    $"Sluiten",
                    $"Teksten uitpakken"
                };
            }
            else if (language == "ET")
            {
                translations = new string[]
                {
                    $"Tere tulemast {ModManager.Configuration.ApplicationName}.",
                    $"See tarkvara on TASUTA. See tähendab, et kui te selle eest makssite, siis teid peteti.",
                    $"Selle modi ametlik allalaadimisleht on https://www.potatoepet.de.",
                    $"Selle avatud lähtekoodiga modi lähtekoodi leiate aadressilt https://www.github.com/FluffyFishGames.",
                    $"See mod muudab teie mängufaile. Sellepärast peate te teadma mõningaid asju. Mitte iga viga, mida te mängus leiate, ei pruugi ilmneda modimata mängus. Seega enne kui te teatate veast {ModManager.Configuration.DeveloperName}, veenduge, et viga esineb ka mängu modifitseerimata versioonis.",
                    $"Kui mängu uuendatakse, ei tööta mod enam. Parim viis uuendusega tegelemiseks on oodata, kuni modi uuendatakse.",
                    $"Mängu modimiseks on vaja oma mängufailide tee.",
                    $"Kui teil on probleeme oma mängufailide leidmisega, saate selle leida Steamis. lihtsalt tehke oma mängul paremklõps ja valige \"Omadused\".",
                    $"Avanevas aknas valige vasakult \"Kohalikud failid\".",
                    $"Seejärel klõpsake paremal pool nuppu \"Sirvi\".",
                    $"Avaneb Windows Explorer aken. Klõpsa üleval valgel alal tee. kopeeri nüüd valitud tee ja sisesta see programmi.",
                    $"Lisaks pakub see mod mõningaid väikseid kohandusi, mida sa saad mängus teha.",
                    $"Lihtsalt vali nimekirjast, milliseid lisafunktsioone sa soovid kasutada:",
                    $"Arusaadav",
                    $"Sirvi",
                    $"Jätka",
                    $"Jätka",
                    $"Sulge",
                    $"Väljavõte tekstidest"
                };
            }
            else if (language == "FI")
            {
                translations = new string[]
                {
                    $"Tervetuloa {ModManager.Configuration.ApplicationName}.",
                    $"Tämä ohjelmisto on ILMAINEN. Se tarkoittaa, että jos maksoit tästä, sinua huijattiin.",
                    $"Tämän modin virallinen lataussivu on https://www.potatoepet.de.",
                    $"Löydät tämän avoimen lähdekoodin modin lähdekoodin osoitteesta https://www.github.com/FluffyFishGames.",
                    $"Tämä modi muuttaa pelitiedostojasi. Siksi sinun on tiedettävä joitakin asioita. Kaikki pelissä havaitsemasi bugit eivät välttämättä esiinny muokkaamattomassa pelissä. Joten ennen kuin ilmoitat bugista {ModManager.Configuration.DeveloperName}, varmista, että bugi esiintyy myös pelin muokkaamattomassa versiossa.",
                    $"Jos peliin tulee päivitys, modi ei enää toimi. Paras tapa käsitellä päivitystä on odottaa, kunnes modi on päivitetty.",
                    $"Pelin modaamiseen tarvitaan polku pelitiedostoihin.",
                    $"Jos sinulla on ongelmia pelitiedostojesi löytämisessä, löydät ne Steamista: Klikkaa peliäsi hiiren kakkospainikkeella ja valitse \"Ominaisuudet\".",
                    $"Valitse avautuvassa ikkunassa vasemmalta puolelta \"Paikalliset tiedostot\".",
                    $"Napsauta sitten oikealla puolella olevaa \"Selaa\" -painiketta.",
                    $"Windows Explorer -ikkuna avautuu. Klikkaa ylhäällä olevaa polun valkoista aluetta. kopioi nyt valittu polku ja syötä se ohjelmaan.",
                    $"Lisäksi tämä modi tarjoaa joitain pieniä säätöjä, joita voit tehdä peliin.",
                    $"Valitse vain listasta, mitä lisäominaisuuksia haluat käyttää:",
                    $"Ymmärretty",
                    $"Selaa",
                    $"Jatka",
                    $"Jatka",
                    $"Sulje",
                    $"Ote teksteistä"
                };
            }
            else if (language == "FR")
            {
                translations = new string[]
                {
                    $"Bienvenue sur {ModManager.Configuration.ApplicationName}.",
                    $"Ce mod est un logiciel libre GRATUIT. Si vous avez payé pour l'obtenir vous avez été arnaqué.",
                    $"La page de téléchargement officielle de ce mod est https://www.potatoepet.de",
                    $"Ce mod est open source, le code source est disponible à l'adresse https://www.github.com/FluffyFishGames",
                    $"Ce mod modifie les fichiers du jeu, ce qui peut éventuellement causer l'apparition de bogues non présents dans la version vanilla. Avant de signaler un bogue à {ModManager.Configuration.DeveloperName}, assurez vous que celui-ci soit également présent dans la version non moddée (vanilla).",
                    $"Si le jeu est mis à jour ce mod ne fonctionnera plus : Le plus simple est d'attendre que le mod soit mis à jour également.",
                    $"Pour installer ce mod vous devez connaître l'emplacement des fichiers du jeu.",
                    $"Si vous avez des difficultés à trouver l'emplacement des fichiers du jeu, vous pouvez le trouver via Steam : Faites clic droit sur le jeu puis sélectionnez \"Propriétés\".",
                    $"Dans la fenêtre qui s'ouvre, sélectionnez \"Fichiers locaux\" dans le menu de gauche.",
                    $"Enfin cliquez sur le bouton \"Parcourir\" à droite.",
                    $"Une fenêtre de l'exporateur Windows s'ouvre : En haut de cette fenêtre, cliquez sur la zone blanche de la barre d'adresse (à droite du nom du dossier) afin de faire apparaitre le chemin d'accès. Sélectionnez le chemin d'accès puis copiez-collez le dans le logiciel.",
                    $"Ce mod vous permet aussi d'apporter quelques petits ajustements au jeu.",
                    $"Il vous suffit de choisir dans la liste ci-dessous les fonctionnalités que vous souhaitez utiliser :",
                    $"Compris",
                    $"Parcourir",
                    $"Continuer",
                    $"Continuer",
                    $"Fermer",
                    $"Extraire les textes"
                };
            }
            else if (language == "EL")
            {
                translations = new string[]
                {
                    $"Καλώς ήρθατε στο {ModManager.Configuration.ApplicationName}.",
                    $"Αυτό το λογισμικό είναι ΔΩΡΕΑΝ. Αυτό σημαίνει ότι αν πληρώσατε γι' αυτό, σας εξαπάτησαν.",
                    $"Η επίσημη σελίδα λήψης για αυτό το mod είναι https://www.potatoepet.de",
                    $"Μπορείτε να βρείτε τον πηγαίο κώδικα αυτού του mod ανοιχτού κώδικα στη διεύθυνση https://www.github.com/FluffyFishGames.",
                    $"Αυτό το mod θα αλλάξει τα αρχεία του παιχνιδιού σας. Γι' αυτό πρέπει να γνωρίζετε κάποια πράγματα. Δεν είναι απαραίτητο ότι κάθε σφάλμα που θα βρείτε στο παιχνίδι θα εμφανιστεί και σε ένα μη τροποποιημένο παιχνίδι. Επομένως, πριν αναφέρετε ένα σφάλμα στην {ModManager.Configuration.DeveloperName}, βεβαιωθείτε ότι το σφάλμα εμφανίζεται και σε μια μη τροποποιημένη έκδοση του παιχνιδιού.",
                    $"Εάν υπάρξει ενημέρωση του παιχνιδιού, το mod δεν θα λειτουργεί πλέον. Ο καλύτερος τρόπος για να αντιμετωπίσετε μια ενημέρωση είναι να περιμένετε μέχρι να ενημερωθεί το mod.",
                    $"Για να τροποποιήσετε το παιχνίδι σας, απαιτείται η διαδρομή προς τα αρχεία του παιχνιδιού σας.",
                    $"Αν έχετε προβλήματα με την εύρεση των αρχείων του παιχνιδιού σας, μπορείτε να τα βρείτε στο Steam. απλά κάντε δεξί κλικ στο παιχνίδι σας και επιλέξτε \"Ιδιότητες\".",
                    $"Στο παράθυρο που ανοίγει, επιλέξτε \"Τοπικά αρχεία\" στην αριστερή πλευρά.",
                    $"Στη συνέχεια, κάντε κλικ στο κουμπί \"Αναζήτηση\" στη δεξιά πλευρά.",
                    $"Θα ανοίξει ένα παράθυρο της Εξερεύνησης των Windows. Κάντε κλικ στη λευκή περιοχή της διαδρομής στο επάνω μέρος. αντιγράψτε τη διαδρομή που έχει πλέον επιλεγεί και εισάγετε την στο πρόγραμμα.",
                    $"Επιπλέον, αυτό το mod προσφέρει κάποιες μικρές προσαρμογές που μπορείτε να κάνετε στο παιχνίδι.",
                    $"Απλά επιλέξτε από τη λίστα ποιες πρόσθετες λειτουργίες θέλετε να χρησιμοποιήσετε:",
                    $"Κατανοητό",
                    $"Αναζήτηση",
                    $"Συνέχεια",
                    $"Συνέχεια",
                    $"Κλείσιμο",
                    $"Απόσπασμα κειμένων"
                };
            }
            else if (language == "HU")
            {
                translations = new string[]
                {
                    $"Üdvözöljük a {ModManager.Configuration.ApplicationName}-ban.",
                    $"Ez a szoftver INGYENES. Ez azt jelenti, hogy ha fizettél érte, akkor átvertek.",
                    $"A mod hivatalos letöltési oldala a https://www.potatoepet.de.",
                    $"Ennek a nyílt forráskódú modnak a forráskódját a https://www.github.com/FluffyFishGames oldalon találod.",
                    $"Ez a mod megváltoztatja a játékfájljaidat. Ezért tudnod kell néhány dolgot. Nem minden hiba, amit a játékban találsz, feltétlenül fog előfordulni egy nem módosított játékban. Mielőtt tehát hibát jelentenél a {ModManager.Configuration.DeveloperName}-nak, győződj meg róla, hogy a hiba a játék egy nem módosított változatában is előfordul.",
                    $"Ha van egy játékfrissítés, a mod már nem fog működni. A legjobb módja a frissítés kezelésének, ha megvárod, amíg a mod frissül.",
                    $"A játék módosításához szükség van a játékfájlok elérési útvonalára.",
                    $"Ha problémái vannak a játékfájlok megtalálásával, akkor a Steamben megtalálhatja őket. egyszerűen kattintson a jobb gombbal a játékra, és válassza a \"Tulajdonságok\" lehetőséget.",
                    $"A megnyíló ablakban válaszd ki a bal oldalon a \"Helyi fájlok\" lehetőséget.",
                    $"Ezután kattints a jobb oldali \"Tallózás\" gombra.",
                    $"Megnyílik a Windows Explorer ablak. Kattints az útvonal fehér területére a tetején. másold ki a most kiválasztott útvonalat, és add be a programba.",
                    $"Ezen kívül ez a mod néhány apró módosítást is kínál, amit a játékban elvégezhetsz.",
                    $"Csak válaszd ki a listából, hogy milyen kiegészítő funkciókat szeretnél használni:",
                    $"Megértett",
                    $"Böngészés",
                    $"Folytatás",
                    $"Folytatás",
                    $"Bezárás",
                    $"Szövegek kivonata"
                };
            }
            else if (language == "ID")
            {
                translations = new string[]
                {
                    $"Selamat datang di {ModManager.Configuration.ApplicationName}.",
                    $"Perangkat lunak ini GRATIS. Itu berarti jika Anda membayar untuk ini, Anda ditipu.",
                    $"Halaman unduhan resmi untuk mod ini adalah https://www.potatoepet.de",
                    $"Anda dapat menemukan kode sumber mod sumber terbuka ini di https://www.github.com/FluffyFishGames",
                    $"Mod ini akan mengubah file game Anda. Itulah mengapa Anda perlu mengetahui beberapa hal. Tidak semua bug yang Anda temukan di dalam game akan muncul di game yang tidak dimodifikasi. Jadi sebelum Anda melaporkan bug ke {ModManager.Configuration.DeveloperName}, pastikan bahwa bug tersebut juga terjadi pada versi game yang tidak dimodifikasi.",
                    $"Jika ada pembaruan game, mod tidak akan berfungsi lagi. Cara terbaik untuk menangani pembaruan adalah menunggu hingga mod diperbarui.",
                    $"Untuk memodifikasi game Anda, diperlukan jalur ke file game Anda.",
                    $"Jika Anda mengalami masalah dalam menemukan file game Anda, Anda dapat menemukannya di Steam, cukup klik kanan pada game Anda dan pilih \"Properti\".",
                    $"Di jendela yang terbuka, pilih \"File Lokal\" di sisi kiri.",
                    $"Kemudian klik tombol \"Jelajahi\" di sisi kanan.",
                    $"Jendela Windows Explorer akan terbuka. Klik pada area putih jalur di bagian atas, salin jalur yang sekarang dipilih dan masukkan ke dalam program.",
                    $"Selain itu, mod ini menawarkan beberapa penyesuaian kecil yang dapat Anda lakukan pada game.",
                    $"Pilih saja dari daftar fitur tambahan mana yang ingin Anda gunakan:",
                    $"Dipahami",
                    $"Jelajahi",
                    $"Lanjutkan",
                    $"Lanjutkan",
                    $"Menutup",
                    $"Ekstrak teks"
                };
            }
            else if (language == "IT")
            {
                translations = new string[]
                {
                    $"Benvenuto a {ModManager.Configuration.ApplicationName}.",
                    $"Questo software è GRATUITO. Ciò significa che se hai pagato per questo, sei stato truffato.",
                    $"La pagina ufficiale per il download di questa mod è https://www.potatoepet.de.",
                    $"Puoi trovare il codice sorgente di questa mod open source all'indirizzo https://www.github.com/FluffyFishGames.",
                    $"Questa mod modificherà i tuoi file di gioco. Per questo motivo devi sapere alcune cose. Non tutti i bug riscontrati nel gioco si verificheranno necessariamente anche in un gioco non modificato. Quindi, prima di segnalare un bug a {ModManager.Configuration.DeveloperName}, assicurati che il bug si verifichi anche in una versione non modificata del gioco.",
                    $"Se il gioco viene aggiornato, la mod non funzionerà più. Il modo migliore per affrontare un aggiornamento è aspettare che la mod venga aggiornata.",
                    $"Per modificare il gioco, è necessario conoscere il percorso dei file di gioco.",
                    $"Se hai problemi a trovare i file di gioco, puoi trovarli in Steam: fai clic con il tasto destro del mouse sul tuo gioco e seleziona \"Proprietà\".",
                    $"Nella finestra che si apre, seleziona \"File locali\" sul lato sinistro.",
                    $"Poi clicca sul pulsante \"Sfoglia\" sul lato destro.",
                    $"Si aprirà una finestra di Windows Explorer. Copia il percorso selezionato e inseriscilo nel programma.",
                    $"Inoltre, questa mod offre alcune piccole modifiche che puoi apportare al gioco.",
                    $"Basta scegliere dall'elenco le funzioni aggiuntive che desideri utilizzare:",
                    $"Capito",
                    $"Sfoglia",
                    $"Continua",
                    $"Continua",
                    $"Chiudi",
                    $"Estrai i testi"
                };
            }
            else if (language == "JA")
            {
                translations = new string[]
                {
                    $"{ModManager.Configuration.ApplicationName}』へようこそ。",
                    $"このソフトは無料です。つまり、もしあなたがこれにお金を払ったなら、あなたは詐欺に遭ったということです。",
                    $"このMODの公式ダウンロードページは https://www.potatoepet.de です。",
                    $"このオープンソースMODのソースコードは https://www.github.com/FluffyFishGames で見ることができます。",
                    $"このMODは、あなたのゲームファイルを変更します。そのため、いくつかのことを知る必要があります。あなたがゲーム内で見つけたすべてのバグが、必ずしも未改造のゲームでも発生するとは限りません。ですから、「{ModManager.Configuration.DeveloperName}」にバグを報告する前に、そのバグが未改造のゲームバージョンでも発生するかどうかを確認してください。",
                    $"ゲームのアップデートがあった場合、MODは動作しなくなります。アップデートに対処する最善の方法は、MODがアップデートされるまで待つことです。",
                    $"ゲームを改造するには、ゲームファイルへのパスが必要です。",
                    $"ゲームファイルを見つけるのに問題がある場合、Steamで見つけることができます。ゲームを右クリックして、「プロパティ」を選択します。",
                    $"開いたウィンドウで、左側にある「ローカルファイル」を選択します。",
                    $"次に、右側の「参照」ボタンをクリックします。",
                    $"Windowsのエクスプローラーウィンドウが開きます。上部のパスの白い部分をクリックします。今選択したパスをコピーして、プログラムに入力します。",
                    $"さらに、このMODは、ゲームに加えることができるいくつかの小さな調整を提供します。",
                    $"どの追加機能を使用したいかをリストから選択するだけです。",
                    $"理解される",
                    $"ブラウズ",
                    $"続き",
                    $"続き",
                    $"閉じる",
                    $"テキストを抜粋"
                };
            }
            else if (language == "KO")
            {
                translations = new string[]
                {
                    $"{ModManager.Configuration.ApplicationName}에 오신 것을 환영합니다.",
                    $"이 소프트웨어는 무료입니다. 즉, 비용을 지불했다면 사기를 당한 것입니다.",
                    $"이 모드의 공식 다운로드 페이지는 https://www.potatoepet.de",
                    $"이 오픈 소스 모드의 소스 코드는 https://www.github.com/FluffyFishGames 에서 찾을 수 있습니다.",
                    $"이 모드는 게임 파일을 변경합니다. 그렇기 때문에 몇 가지 사항을 알아야합니다. 게임에서 발견한 모든 버그가 수정하지 않은 게임에서 반드시 발생하는 것은 아닙니다. 따라서 {ModManager.Configuration.DeveloperName}에 버그를 신고하기 전에 해당 버그가 수정되지 않은 버전의 게임에서도 발생하는지 확인하세요.",
                    $"게임 업데이트가 있으면 모드가 더 이상 작동하지 않습니다. 업데이트에 대처하는 가장 좋은 방법은 모드가 업데이트될 때까지 기다리는 것입니다.",
                    $"게임을 수정하려면 게임 파일 경로가 필요합니다.",
                    $"게임 파일을 찾는 데 문제가 있는 경우 Steam에서 찾을 수 있습니다. 게임을 마우스 오른쪽 버튼으로 클릭하고 \"속성\"을 선택하면 됩니다.",
                    $"열리는 창에서 왼쪽에 있는 \"로컬 파일\"을 선택합니다.",
                    $"그런 다음 오른쪽의 \"찾아보기\" 버튼을 클릭합니다.",
                    $"Windows 탐색기 창이 열립니다. 상단의 경로의 흰색 영역을 클릭하고 지금 선택한 경로를 복사하여 프로그램에 입력합니다.",
                    $"또한이 모드는 게임에 적용 할 수있는 몇 가지 작은 조정을 제공합니다.",
                    $"목록에서 사용하려는 추가 기능을 선택하기 만하면됩니다:",
                    $"이해",
                    $"찾아보기",
                    $"계속",
                    $"계속",
                    $"닫기",
                    $"텍스트 추출"
                };
            }
            else if (language == "LV")
            {
                translations = new string[]
                {
                    $"Laipni lūdzam {ModManager.Configuration.ApplicationName}.",
                    $"Šī programmatūra ir BEZMAKSAS. Tas nozīmē, ka, ja jūs maksājāt par to, jums bija scammed.",
                    $"Oficiālā šī moda lejupielādes lapa ir https://www.potatoepet.de.",
                    $"Šī atvērtā koda moda pirmkodu var atrast vietnē https://www.github.com/FluffyFishGames.",
                    $"Šī modifikācija izmainīs jūsu spēles failus. Tāpēc jums ir jāzina dažas lietas. Ne visas kļūdas, ko jūs atradīsiet spēlē, obligāti parādīsies arī nemodificētā spēlē. Tāpēc, pirms ziņojat par kļūdu {ModManager.Configuration.DeveloperName}, pārliecinieties, ka kļūda parādās arī nemodificētā spēles versijā.",
                    $"Ja tiks veikts spēles atjauninājums, modifikācija vairs nedarbosies. Vislabākais veids, kā rīkoties atjauninājuma gadījumā, ir pagaidīt, kamēr mods tiks atjaunināts.",
                    $"Lai modificētu spēli, ir nepieciešams norādīt ceļu līdz spēles failiem.",
                    $"Ja jums ir problēmas ar spēles failu atrašanu, to varat atrast Steam. vienkārši noklikšķiniet ar peles labo pogu uz spēles un izvēlieties \"Īpašības\".",
                    $"Atvērtajā logā kreisajā pusē izvēlieties \"Vietējie faili\".",
                    $"Pēc tam noklikšķiniet uz pogas \"Pārlūkot\" labajā pusē.",
                    $"Atvērsies Windows Explorer logs. Noklikšķiniet uz ceļa baltā laukuma augšpusē. nokopējiet tagad izvēlēto ceļu un ievadiet to programmā.",
                    $"Turklāt šī modifikācija piedāvā dažus nelielus pielāgojumus, ko varat veikt spēlē.",
                    $"Vienkārši izvēlieties no saraksta, kuras papildu funkcijas vēlaties izmantot:",
                    $"Saprotams",
                    $"Pārlūkot",
                    $"Turpināt",
                    $"Turpināt",
                    $"Aizvērt",
                    $"Izvilkuma teksti"
                };
            }
            else if (language == "LT")
            {
                translations = new string[]
                {
                    $"Sveiki atvykę į {ModManager.Configuration.ApplicationName}.",
                    $"Ši programinė įranga yra NEMOKAMA. Tai reiškia, kad jei už ją sumokėjote, buvote apgauti.",
                    $"Oficialus šios modifikacijos atsisiuntimo puslapis yra https://www.potatoepet.de",
                    $"Šio atvirojo kodo modifikacijos išeities kodą galite rasti adresu https://www.github.com/FluffyFishGames.",
                    $"Šis modas pakeis jūsų žaidimo failus. Todėl turite žinoti kai kuriuos dalykus. Ne kiekviena klaida, kurią rasite žaidime, būtinai atsiras ir nemodifikuotame žaidime. Todėl prieš pranešdami apie klaidą {ModManager.Configuration.DeveloperName}, įsitikinkite, kad klaida pasitaiko ir nemodifikuotoje žaidimo versijoje.",
                    $"Jei žaidimas bus atnaujintas, modifikacija nebeveiks. Geriausias būdas susidoroti su atnaujinimu - palaukti, kol modas bus atnaujintas.",
                    $"Norint modifikuoti žaidimą, reikia nurodyti kelią iki žaidimo failų.",
                    $"Jei kyla problemų ieškant savo žaidimo failų, juos galite rasti \"Steam\" sistemoje. tiesiog dešiniuoju pelės klavišu spustelėkite žaidimą ir pasirinkite \"savybės\".",
                    $"Atsidariusiame lange kairėje pusėje pasirinkite \"vietiniai failai\".",
                    $"Tada dešinėje pusėje spustelėkite mygtuką \"naršyti\".",
                    $"Atsidarys \"Windows Explorer\" langas. Spustelėkite viršuje esančią baltą kelio sritį. nukopijuokite dabar pasirinktą kelią ir įveskite jį į programą.",
                    $"Be to, šis modifikavimas siūlo keletą nedidelių pakeitimų, kuriuos galite atlikti žaidime.",
                    $"Tiesiog pasirinkite iš sąrašo, kokias papildomas funkcijas norite naudoti:",
                    $"Suprantama",
                    $"Naršykite",
                    $"Tęsti",
                    $"Tęsti",
                    $"Uždaryti",
                    $"Ištraukti tekstus"
                };
            }
            else if (language == "NB")
            {
                translations = new string[]
                {
                    $"Velkommen til {ModManager.Configuration.ApplicationName}.",
                    $"Denne programvaren er GRATIS. Det betyr at hvis du betalte for dette, ble du svindlet.",
                    $"Den offisielle nedlastingssiden for denne modusen er https://www.potatoepet.de",
                    $"Du kan finne kildekoden til denne open source mod på https://www.github.com/FluffyFishGames",
                    $"Denne moden vil endre spillfilene dine.Derfor må du vite noen ting.Ikke alle feil du finner i spillet vil nødvendigvis forekomme i et umodifisert spill.Så før du rapporterer en feil til {ModManager.Configuration.DeveloperName}, må du forsikre deg om at feilen også forekommer i en umodifisert versjon av spillet.",
                    $"Hvis det er en spilloppdatering, vil modet ikke lenger fungere.Den beste måten å håndtere en oppdatering på er å vente til modet er oppdatert.",
                    $"For å endre spillet ditt, er banen til spillfilene dine nødvendig.",
                    $"Hvis du har problemer med å finne spillfilene dine, kan du finne det i Steam.bare høyreklikk på spillet ditt og velg \"Egenskaper\".",
                    $"I vinduet som åpnes velger du \"Lokale filer\" på venstre side.",
                    $"Klikk deretter på \"Bla gjennom\" - knappen på høyre side.",
                    $"Et Windows Utforsker - vindu åpnes.Klikk på det hvite området på banen øverst.Kopier den nå valgte banen og skriv den inn i programmet.",
                    $"I tillegg tilbyr denne moden noen små justeringer du kan gjøre i spillet.",
                    $"Bare velg fra listen hvilke tilleggsfunksjoner du vil bruke:",
                    $"Forstått",
                    $"Bla gjennom",
                    $"Fortsett",
                    $"Fortsett",
                    $"Lukk",
                    $"Utdrag av tekster"
                };
            }
            else if (language == "PL")
            {
                translations = new string[]
                {
                    $"Witamy w {ModManager.Configuration.ApplicationName}.",
                    $"To oprogramowanie jest DARMOWE. Oznacza to, że jeśli zapłaciłeś za niego, to zostałeś oszukany.",
                    $"Oficjalna strona pobierania tego moda to https://www.potatoepet.de.",
                    $"Kod źródłowy tego moda open source możesz znaleźć na stronie https://www.github.com/FluffyFishGames.",
                    $"Ten mod zmieni twoje pliki gry. Dlatego musisz wiedzieć kilka rzeczy. Nie każdy błąd, który znajdziesz w grze, będzie koniecznie występował w niezmodyfikowanej grze. Zanim więc zgłosisz błąd do {ModManager.Configuration.DeveloperName}, upewnij się, że występuje on także w niezmodyfikowanej wersji gry.",
                    $"Jeśli gra zostanie zaktualizowana, mod przestanie działać. Najlepszym sposobem radzenia sobie z aktualizacjami jest czekanie, aż mod zostanie zaktualizowany.",
                    $"Do zmodyfikowania gry potrzebna jest ścieżka dostępu do plików gry.",
                    $"Jeśli masz problemy ze znalezieniem plików gry, możesz je znaleźć w serwisie Steam.Kliknij prawym przyciskiem myszy na swoją grę i wybierz \"Właściwości\".",
                    $"W oknie, które się otworzy, wybierz po lewej stronie \"Pliki lokalne\".",
                    $"Następnie kliknij przycisk \"Przeglądaj\" po prawej stronie.",
                    $"Otworzy się okno Eksploratora Windows.Kliknij na biały obszar ścieżki u góry, skopiuj wybraną ścieżkę i wprowadź ją do programu.",
                    $"Dodatkowo mod ten oferuje kilka małych zmian, które możesz wprowadzić do gry.",
                    $"Po prostu wybierz z listy te dodatkowe funkcje, których chcesz użyć:",
                    $"Zrozumiałe",
                    $"Przeglądaj",
                    $"Kontynuuj",
                    $"Kontynuuj",
                    $"Zamknij",
                    $"Wyciągnij teksty"
                };
            }
            else if (language == "PT")
            {
                translations = new string[]
                {
                    $"Bem-vindo ao {ModManager.Configuration.ApplicationName}.",
                    $"Este software é GRATUITO. Isso significa que se pagaste por isto, foste enganado.",
                    $"A página oficial de download para este mod é https://www.potatoepet.de.",
                    $"Podes encontrar o código fonte deste mod de código aberto em https://www.github.com/FluffyFishGames",
                    $"Este mod irá alterar os teus ficheiros de jogo. É por isso que precisas de saber algumas coisas. Nem todos os erros que encontrares no jogo irão necessariamente ocorrer num jogo não modificado. Por isso, antes de reportares um bug à {ModManager.Configuration.DeveloperName}, certifica-te de que o bug também ocorre numa versão não modificada do jogo.",
                    $"Se houver uma actualização do jogo, o mod não funcionará mais.A melhor maneira de lidar com uma actualização é esperar até que o mod seja actualizado.",
                    $"Para modificar o teu jogo, o caminho para os teus ficheiros de jogo é necessário.",
                    $"Se tiveres problemas em encontrar os teus ficheiros de jogo, podes encontrá-lo em Steam.Basta clicares com o botão direito do rato no teu jogo e seleccionares \"Propriedades\".",
                    $"Na janela que se abre, selecciona \"Ficheiros Locais\" no lado esquerdo.",
                    $"Depois clica no botão \"Procura\" do lado direito.",
                    $"Uma janela do Windows Explorer irá abrir - se.Clica na área branca do caminho no topo.Copia o caminho agora seleccionado e insere-o no programa.",
                    $"Adicionalmente, este mod oferece alguns pequenos ajustes que podes fazer no jogo.",
                    $"Basta escolheres da lista quais as funcionalidades adicionais que queres utilizar:",
                    $"Entendido",
                    $"Procura",
                    $"Continuar",
                    $"Continuar",
                    $"Fechar",
                    $"Extrair textos"
                };
            }
            else if (language == "PT-BR")
            {
                translations = new string[]
                {
                    $"Bem-vindo ao {ModManager.Configuration.ApplicationName}.",
                    $"Este software é GRATUITO. Isso significa que se você pagou por isso, você foi enganado.",
                    $"A página oficial de download para este mod é https://www.potatoepet.de.",
                    $"Você pode encontrar o código fonte deste mod de código aberto em https://www.github.com/FluffyFishGames",
                    $"Este mod irá mudar seus arquivos de jogo. É por isso que você precisa saber algumas coisas. Nem todo bug que você encontrar no jogo irá necessariamente ocorrer em um jogo não modificado. Então, antes de você reportar um bug para a {ModManager.Configuration.DeveloperName}, certifique-se de que o bug também ocorra em uma versão não modificada do jogo.",
                    $"Se houver uma atualização do jogo, o mod não vai mais funcionar. A melhor maneira de lidar com uma atualização é esperar até que o mod seja atualizado.",
                    $"Para modificar seu jogo, o caminho para seus arquivos de jogo é necessário.",
                    $"Se você tiver problemas para encontrar seus arquivos de jogo, você pode encontrá-lo em Steam.Basta clicar com o botão direito do mouse em seu jogo e selecionar \"Propriedades\".",
                    $"Na janela que se abre, selecione \"Arquivos Locais\" no lado esquerdo.",
                    $"Depois clique no botão \"Navegar\" no lado direito.",
                    $"Uma janela do Windows Explorer se abrirá.Clique na área branca do caminho no topo.Copie o caminho agora selecionado e digite-o no programa.",
                    $"Além disso, este mod oferece alguns pequenos ajustes que você pode fazer no jogo.",
                    $"Basta escolher da lista quais recursos adicionais você quer usar:",
                    $"Entendido",
                    $"Navegue",
                    $"Continuar",
                    $"Continuar",
                    $"Fechar",
                    $"Extrair textos",
                };
            }
            else if (language == "RO")
            {
                translations = new string[]
                {
                    $"Bine ați venit la {ModManager.Configuration.ApplicationName}.",
                    $"Acest software este GRATUIT. Asta înseamnă că dacă ați plătit pentru asta, ați fost înșelat.",
                    $"Pagina oficială de descărcare pentru acest mod este https://www.potatoepet.de",
                    $"Puteți găsi codul sursă al acestui mod open source la https://www.github.com/FluffyFishGames",
                    $"Acest mod vă va schimba fișierele de joc. De aceea, trebuie să știți câteva lucruri. Nu toate bug-urile pe care le găsiți în joc vor apărea neapărat într-un joc nemodificat. Așadar, înainte de a raporta un bug la {ModManager.Configuration.DeveloperName}, asigurați-vă că acel bug apare și într-o versiune nemodificată a jocului.",
                    $"Dacă există o actualizare a jocului, mod-ul nu va mai funcționa. Cel mai bun mod de a face față unei actualizări este să așteptați până când mod-ul este actualizat.",
                    $"Pentru a modifica jocul, este nevoie de calea către fișierele de joc.",
                    $"Dacă aveți probleme în găsirea fișierelor de joc, le puteți găsi în Steam. dați clic dreapta pe joc și selectați \"Proprietăți\".",
                    $"În fereastra care se deschide, selectați \"Fișiere locale\" în partea stângă.",
                    $"Apoi faceți clic pe butonul \"Răsfoiește\" din partea dreaptă.",
                    $"Se va deschide o fereastră Windows Explorer. Faceți clic pe zona albă a căii din partea de sus. copiați calea acum selectată și introduceți-o în program.",
                    $"În plus, acest mod oferă câteva mici ajustări pe care le puteți face la joc.",
                    $"Trebuie doar să alegeți din listă ce caracteristici suplimentare doriți să utilizați:",
                    $"Înțeles",
                    $"Browse",
                    $"Continuați",
                    $"Continuați",
                    $"Închideți",
                    $"Extras de texte"
                };
            }
            else if (language == "RU")
            {
                translations = new string[]
                {
                    $"Добро пожаловать в {ModManager.Configuration.ApplicationName}.",
                    $"Эта программа является БЕСПЛАТНОЙ. Это значит, что если ты заплатил за это, то тебя обманули.",
                    $"Официальная страница загрузки этого мода - https://www.potatoepet.de.",
                    $"Ты можешь найти исходный код этого мода с открытым исходным кодом по адресу https://www.github.com/FluffyFishGames.",
                    $"Этот мод изменит твои игровые файлы. Поэтому тебе нужно знать некоторые вещи. Не каждый баг, который ты обнаружишь в игре, обязательно возникнет в немодифицированной игре. Поэтому прежде чем сообщить об ошибке в {ModManager.Configuration.DeveloperName}, убедись, что эта ошибка также встречается в немодифицированной версии игры.",
                    $"Если произойдет обновление игры, мод перестанет работать. Лучший способ справиться с обновлением - подождать, пока мод обновится.",
                    $"Для модификации игры необходим путь к твоим игровым файлам.",
                    $"Если у тебя возникли проблемы с поиском файлов твоей игры, ты можешь найти его в Steam.Просто щелкни правой кнопкой мыши на своей игре и выбери \"Свойства\".",
                    $"В открывшемся окне с левой стороны выбери \"Локальные файлы\".",
                    $"Затем нажми на кнопку \"Обзор\" с правой стороны.",
                    $"Откроется окно проводника Windows. Щелкни по белой области пути в верхней части.Скопируй теперь выбранный путь и введи его в программу.",
                    $"Кроме того, этот мод предлагает несколько небольших настроек, которые ты можешь внести в игру.",
                    $"Просто выбери из списка, какие дополнительные возможности ты хочешь использовать:",
                    $"Понятно",
                    $"Просмотреть",
                    $"Продолжить",
                    $"Продолжить",
                    $"Закрыть",
                    $"Тексты для извлечения",
                };
            }
            else if (language == "SK")
            {
                translations = new string[]
                {
                    $"Vitajte na stránke {ModManager.Configuration.ApplicationName}.",
                    $"Tento softvér je ZDARMA. To znamená, že ak ste zaň zaplatili, boli ste podvedení.",
                    $"Oficiálna stránka na stiahnutie tohto módu je https://www.potatoepet.de.",
                    $"Zdrojový kód tohto open source módu nájdete na adrese https://www.github.com/FluffyFishGames.",
                    $"Tento mod zmení vaše herné súbory. Preto musíte vedieť niektoré veci. Nie každá chyba, ktorú v hre nájdete, sa musí nevyhnutne vyskytovať aj v nemodifikovanej hre. Preto skôr, ako nahlásite chybu spoločnosti {ModManager.Configuration.DeveloperName}, uistite sa, že sa chyba vyskytuje aj v nemodifikovanej verzii hry.",
                    $"Ak dôjde k aktualizácii hry, mod už nebude fungovať. Najlepší spôsob, ako sa vysporiadať s aktualizáciou, je počkať, kým sa mod aktualizuje.",
                    $"Na modifikáciu hry je potrebná cesta k herným súborom.",
                    $"Ak máte problémy s nájdením súborov hry, môžete ich nájsť v službe Steam. stačí kliknúť pravým tlačidlom myši na hru a vybrať položku \"Vlastnosti\".",
                    $"V okne, ktoré sa otvorí, vyberte na ľavej strane položku \"Miestne súbory\".",
                    $"Potom kliknite na tlačidlo \"Prehľadávať\" na pravej strane.",
                    $"Otvorí sa okno Prieskumníka systému Windows. Kliknite na bielu oblasť cesty v hornej časti. skopírujte teraz vybranú cestu a zadajte ju do programu.",
                    $"Okrem toho tento mód ponúka niekoľko drobných úprav, ktoré môžete v hre vykonať.",
                    $"Stačí si zo zoznamu vybrať, ktoré dodatočné funkcie chcete použiť:",
                    $"Rozumiete",
                    $"Prehľadávať",
                    $"Pokračovať",
                    $"Pokračovať",
                    $"Zatvoriť",
                    $"Výpis textov"
                };
            }
            else if (language == "SL")
            {
                translations = new string[]
                {
                    $"Dobrodošli v {ModManager.Configuration.ApplicationName}.",
                    $"Ta programska oprema je BREZPLAČNA. To pomeni, da če ste za to plačali, ste bili prevarani.",
                    $"Uradna stran za prenos tega modula je https://www.potatoepet.de",
                    $"Izvorno kodo tega odprtokodnega modula lahko najdete na spletni strani https://www.github.com/FluffyFishGames.",
                    $"Ta modus bo spremenil vaše datoteke v igri. Zato morate vedeti nekaj stvari. Ni nujno, da se bo vsak hrošč, ki ga najdete v igri, pojavil tudi v nespremenjeni igri. Zato se pred prijavo napake podjetju {ModManager.Configuration.DeveloperName} prepričajte, da se napaka pojavi tudi v nespremenjeni različici igre.",
                    $"Če bo igra posodobljena, mod ne bo več deloval. Najboljši način za reševanje posodobitve je, da počakate, da se mod posodobi.",
                    $"Za spreminjanje igre potrebujete pot do datotek igre.",
                    $"Če imate težave z iskanjem datotek igre, jih lahko poiščete v storitvi Steam.kliknite z desno tipko miške na svojo igro in izberite \"Lastnosti\".",
                    $"V oknu, ki se odpre, na levi strani izberite \"Lokalne datoteke\".",
                    $"Nato na desni strani kliknite gumb \"Brskaj\".",
                    $"Odprlo se bo okno Raziskovalca Windows. Kliknite na belo območje poti na vrhu.kopirajte zdaj izbrano pot in jo vnesite v program.",
                    $"Poleg tega ta mod ponuja nekaj manjših prilagoditev, ki jih lahko vnesete v igro.",
                    $"S seznama samo izberite, katere dodatne funkcije želite uporabiti:",
                    $"Razumljivo",
                    $"Brskanje po",
                    $"Nadaljuj",
                    $"Nadaljuj",
                    $"Zapri",
                    $"Izvleček besedil"
                };
            }
            else if (language == "ES")
            {
                translations = new string[]
                {
                    $"Bienvenido a {ModManager.Configuration.ApplicationName}.",
                    $"Este software es GRATUITO. Eso significa que si has pagado por él, te han estafado.",
                    $"La página oficial de descarga de este mod es https://www.potatoepet.de.",
                    $"Puedes encontrar el código fuente de este mod de código abierto en https://www.github.com/FluffyFishGames",
                    $"Este mod cambiará los archivos de tu juego. Por eso necesitas saber algunas cosas. No todos los fallos que encuentres en el juego se producirán necesariamente en un juego no modificado. Así que antes de informar de un fallo a {ModManager.Configuration.DeveloperName}, asegúrate de que el fallo también se produce en una versión no modificada del juego.",
                    $"Si se produce una actualización del juego, el mod dejará de funcionar. La mejor forma de hacer frente a una actualización es esperar a que se actualice el mod.",
                    $"Para modificar tu juego, se necesita la ruta a los archivos de tu juego.",
                    $"Si tienes problemas para encontrar los archivos de tu juego, puedes hacerlo en Steam.Sólo tienes que hacer clic con el botón derecho en tu juego y seleccionar \"Propiedades\".",
                    $"En la ventana que se abre, selecciona \"Archivos locales\" en la parte izquierda.",
                    $"A continuación, haz clic en el botón \"Examinar\" de la parte derecha.",
                    $"Se abrirá una ventana del Explorador de Windows. Haz clic en la zona blanca de la ruta en la parte superior.Copia la ruta ahora seleccionada e introdúcela en el programa.",
                    $"Además, este mod ofrece algunos pequeños ajustes que puedes hacer en el juego.",
                    $"Sólo tienes que elegir de la lista qué funciones adicionales quieres utilizar:",
                    $"Entendido",
                    $"Explorar",
                    $"Continuar",
                    $"Continuar",
                    $"Cerrar",
                    $"Extraer textos"
                };
            }
            else if (language == "SV")
            {
                translations = new string[]
                {
                    $"Välkommen till {ModManager.Configuration.ApplicationName}.",
                    $"Denna programvara är gratis. Det betyder att om du har betalat för den har du blivit lurad.",
                    $"Den officiella nedladdningssidan för denna mod är https://www.potatoepet.de",
                    $"Du kan hitta källkoden för denna mod med öppen källkod på https://www.github.com/FluffyFishGames.",
                    $"Denna mod kommer att ändra dina spelfiler. Därför måste du känna till vissa saker. Alla fel som du hittar i spelet kommer inte nödvändigtvis att förekomma i ett oförändrat spel. Så innan du rapporterar ett fel till {ModManager.Configuration.DeveloperName} ska du se till att felet också förekommer i en oförändrad version av spelet.",
                    $"Om det sker en uppdatering av spelet kommer modden inte längre att fungera. Det bästa sättet att hantera en uppdatering är att vänta tills modet har uppdaterats.",
                    $"För att modifiera ditt spel behövs sökvägen till dina spelfiler.",
                    $"Om du har problem med att hitta dina spelfiler kan du hitta dem i Steam: Högerklicka på ditt spel och välj \"Egenskaper\".",
                    $"I fönstret som öppnas väljer du \"Lokala filer\" på vänster sida.",
                    $"Klicka sedan på knappen \"Bläddra\" på den högra sidan.",
                    $"Ett fönster med Windows Explorer öppnas. Klicka på det vita området för sökvägen högst upp. kopiera den nu valda sökvägen och skriv in den i programmet.",
                    $"Dessutom erbjuder denna mod några små justeringar som du kan göra i spelet.",
                    $"Välj bara från listan vilka ytterligare funktioner du vill använda:",
                    $"Förstått",
                    $"Bläddra på",
                    $"Fortsätt",
                    $"Fortsätt",
                    $"Stäng",
                    $"Utdrag av texter"
                };
            }
            else if (language == "TR")
            {
                translations = new string[]
                {
                    $"{ModManager.Configuration.ApplicationName} a hoş geldiniz.",
                    $"Bu yazılım ÜCRETSİZDİR. Yani eğer bunun için para ödediyseniz, dolandırılmışsınız demektir.",
                    $"Bu mod için resmi indirme sayfası https://www.potatoepet.de",
                    $"Bu açık kaynak kodlu modun kaynak kodunu https://www.github.com/FluffyFishGames adresinde bulabilirsiniz.",
                    $"Bu mod oyun dosyalarınızı değiştirecek. Bu yüzden bazı şeyleri bilmeniz gerekiyor. Oyunda bulduğunuz her hatanın modifiye edilmemiş bir oyunda ortaya çıkması gerekmez. Bu yüzden bir hatayı {ModManager.Configuration.DeveloperName} a bildirmeden önce, hatanın oyunun değiştirilmemiş bir sürümünde de meydana geldiğinden emin olun.",
                    $"Eğer bir oyun güncellemesi varsa, mod artık çalışmayacaktır. Bir güncelleme ile başa çıkmanın en iyi yolu, mod güncellenene kadar beklemektir.",
                    $"Oyununuzu değiştirmek için oyun dosyalarınızın yolu gereklidir.",
                    $"Oyun dosyalarınızı bulmakta sorun yaşıyorsanız, Steam'de bulabilirsiniz. Oyununuza sağ tıklayın ve \"Özellikler\" i seçin.",
                    $"Açılan pencerede sol taraftan \"Yerel Dosyalar\" ı seçin.",
                    $"Ardından sağ taraftaki \"Gözat\" düğmesine tıklayın.",
                    $"Bir Windows Gezgini penceresi açılacaktır. Üstteki yolun beyaz alanına tıklayın, şimdi seçilen yolu kopyalayın ve programa girin.",
                    $"Ek olarak, bu mod oyunda yapabileceğiniz bazı küçük ayarlamalar sunar.",
                    $"Listeden hangi ek özellikleri kullanmak istediğinizi seçmeniz yeterlidir:",
                    $"Anlaşıldı",
                    $"Gözat",
                    $"Devam et",
                    $"Devam et",
                    $"Kapat",
                    $"Alıntı metinler"
                };
            }
            else if (language == "UK")
            {
                translations = new string[]
                {
                    $"Ласкаво просимо до {ModManager.Configuration.ApplicationName}.",
                    $"Це програмне забезпечення є БЕЗКОШТОВНИМ. Це означає, що якщо ви заплатили за нього, вас ошукали.",
                    $"Офіційна сторінка завантаження цього мода: https://www.potatoepet.de",
                    $"Ви можете знайти вихідний код цього мода з відкритим кодом на https://www.github.com/FluffyFishGames",
                    $"Цей мод змінить ваші ігрові файли. Ось чому вам потрібно знати деякі речі. Не кожна помилка, яку ви знайдете у грі, обов'язково трапиться у немодифікованій грі. Тому перед тим, як повідомити про ваду {ModManager.Configuration.DeveloperName}, переконайтеся, що вона трапляється і в немодифікованій версії гри.",
                    $"Якщо вийшло оновлення гри, мод більше не буде працювати. Найкращий спосіб вирішити проблему з оновленням - зачекати, поки мод оновиться.",
                    $"Щоб модифікувати вашу гру, потрібен шлях до ваших ігрових файлів.",
                    $"Якщо у вас виникли проблеми з пошуком файлів гри, ви можете знайти їх у Steam: просто клацніть правою кнопкою миші на вашій грі і виберіть \"Властивості\".",
                    $"У вікні, що відкриється, виберіть \"Локальні файли\" зліва.",
                    $"Потім натисніть на кнопку \"Огляд\" праворуч.",
                    $"Відкриється вікно Провідника Windows. Клацніть на білу область шляху вгорі. Скопіюйте вибраний шлях і введіть його у програму.",
                    $"Крім того, цей мод пропонує деякі невеликі зміни, які ви можете внести у гру.",
                    $"Просто виберіть зі списку, які додаткові функції ви хочете використовувати:",
                    $"Зрозуміло",
                    $"Переглянути",
                    $"Продовжити",
                    $"Продовжити",
                    $"Закрити",
                    $"Витягнути тексти"
                };
            }

            this.WelcomeText0.Text = translations[0];
            this.WelcomeText1.Text = translations[1];
            this.WelcomeText2.Text = translations[2];
            this.WelcomeText3.Text = translations[3];
            this.WelcomeText4.Text = translations[4];
            this.WelcomeText5.Text = translations[5];
            this.PathText0.Text = translations[6];
            this.PathText1.Text = translations[7];
            this.PathText2.Text = translations[8];
            this.PathText3.Text = translations[9];
            this.PathText4.Text = translations[10];
            this.ModsText0.Text = translations[11];
            this.ModsText1.Text = translations[12];
            this.UnderstoodText.Text = translations[13];
            this.BrowseText.Text = translations[14];
            this.Continue0Text.Text = translations[15];
            this.Continue1Text.Text = translations[16];
            this.FinishText.Text = translations[17];
            this.ExtractText.Text = translations[18];
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
                    ModManager.Mod.SetLanguage(Model.LanguageCodes[Model.CurrentLanguage]);
                    ModManager.Mod.Apply(Model.Directory, options);
                }
                catch (Exception ex)
                {
                    Log("Error: " + ex.ToString());
                }

                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.FinishButton.IsEnabled = true;
                });
            });
            //this.ApplyModsPanel.IsVisible = true;
        }

        public void ContinueToExtract(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.SelectModsPanel.IsVisible = false;
            this.ProgressPanel.IsVisible = true;
            Task.Run(() =>
            {
                try
                {
                    ModManager.Mod.SetLanguage(Model.LanguageCodes[Model.CurrentLanguage]);
                    ModManager.Mod.Extract(Model.Directory);
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
#if LINUX
            var ptr = tinyfd_openFileDialog("Please select game exe", this.Model.Directory, 0, new string[]{}, "Game EXE", 0);
#else
            var filter = new string[ModManager.Configuration.FileNames.Length];
            for (var i = 0; i < ModManager.Configuration.FileNames.Length; i++)
                filter[i] = ModManager.Configuration.FileNames[i] + ".exe";
            var ptr = tinyfd_openFileDialog("Please select game exe", this.Model.Directory, filter.Length, filter, "Game EXE", 0);
#endif
            if (ptr != IntPtr.Zero)
            {
                //var ptr = tinyfd_selectFolderDialog("Please select game path", this.Model.Directory);
                var newValue = StringFromANSI(ptr);
                if (newValue != null)
                    this.Model.Directory = System.IO.Path.GetDirectoryName(newValue);
            }
        }
    }
}
