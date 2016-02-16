using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinecraftManager.CrossCutting
{
    static class Logging
    {
        public static void Log(string message, string category)
        {
            Debug.WriteLine(message, category);
        }
    }
}
