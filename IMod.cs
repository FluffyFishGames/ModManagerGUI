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
        public abstract bool Verify(string directory);
        public abstract void Apply(string gameDirectory, HashSet<int> options);
    }
}
