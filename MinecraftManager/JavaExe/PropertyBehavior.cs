using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MinecraftManager.JavaExe
{
    static class PropertyBehavior
    {
        public static string TryLocate(Func<String> getter,Action useUserInput)
        {
            var java = getter();
            if(java.IsNullOrEmpty()||!File.Exists(java))
            {
                useUserInput();
            }
            if (getter().IsNullOrEmpty())
                return null;
            if (!File.Exists(java))
                return null;
            return java;

        }
    }
}
