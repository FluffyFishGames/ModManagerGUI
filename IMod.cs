using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManagerGUI
{
    public abstract class IMod
    {
        public delegate void Log(string message);
        public Log OnLog;
        public delegate void Progress(float percent, string message);
        public Progress OnProgress;
        public abstract bool Verify(string directory);
        public abstract void Apply(string gameDirectory, HashSet<int> options);
        public abstract void Extract(string gameDirectory);
        public abstract void SetLanguage(string languageCode);
        public abstract string[] GetSupportedLanguages();

        public abstract (string, Action)[] GetDebugActions();
    }
}
