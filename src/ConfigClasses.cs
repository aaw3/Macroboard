using System;
using System.Collections.Generic;

namespace MacroBoard
{
    public class DeviceConfig
    {
        public DeviceConfig(List<KeyboardConfig> keyboards)
        {
            Keyboards = keyboards;
        }

        //Need a blank constructor to allow for Xml Deserialization
        private DeviceConfig() { }


        public List<KeyboardConfig> Keyboards { get; set; }
    }

    public class KeyboardConfig
    {
        public KeyboardConfig(string keyboardAlias, bool isMacroBoard, bool hasAutoNumLock, string keyboardName, string keyboardPath, bool isDefaultMacroBoard)
        {
            KeyboardAlias = keyboardAlias;
            IsMacroBoard = isMacroBoard;
            HasAutoNumLock = hasAutoNumLock;
            KeyboardName = keyboardName;
            KeyboardPath = keyboardPath;
            IsDefaultMacroBoard = isDefaultMacroBoard;
        }

        //Need a blank constructor to allow for Xml Deserialization
        private KeyboardConfig() { }

        public string KeyboardAlias { get; set; }
        public bool IsMacroBoard { get; set; }
        public bool HasAutoNumLock { get; set; }
        public string KeyboardName { get; set; } 
        public string KeyboardPath { get; set; }

        public bool IsDefaultMacroBoard { get; set; }
    }
}
