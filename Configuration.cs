using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManagerGUI
{
    public class Configuration
    {
        public string ApplicationName = "Little Witch Translator";
        public string GameName = "Little Witch in the Woods";
        public string[] FileNames = new string[] { "LWIW", "Little Witch in the Woods" };
        public string DeveloperName = "SUNNY SIDE UP";
        public string SteamAppID = "1594940";
        public string[][] AdditionalMods = new string[][] {
            new string[] { "Jederzeit schlafen gehen", "Sleep anytime" },
            new string[] { "Immer neue Dialoage", "Always new dialogues"}
        };
    }
}
