using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MinecraftManager.CrossCutting;

namespace MinecraftManager.JavaExe
{
    class InPathBehavior
    {
        internal static string TryLocate()
        {
            string java = "java.exe";
            try
            {
                var p = Process.Start(java+" -version");
                p.WaitForExit();
                var output = p.StandardOutput.ReadToEnd();
                return java;
            }
            catch (Win32Exception ex)
            {
                Logging.Log(ex.Message, "JavaInPathBehavior");
                return null;
            }
        }
    }
}
