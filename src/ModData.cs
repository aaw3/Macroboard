using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MacroBoard
{
    public class ModData
    {
        public static List<ModData> ModDataList = new List<ModData>();
        //Type is kinda unnecessary as you can get the type through other methods, but this makes it easier
        public ModData(Assembly asm, Type type, dynamic mod)
        {
            Assembly = asm;
            Type = type;
            Mod = mod;
        }

        public Assembly Assembly;
        public Type Type;
        public dynamic Mod;
    }
}
