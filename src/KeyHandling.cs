using System;
using System.Collections.Generic;
using System.Text;

namespace MacroBoard
{
    public class KeyHandling
    {
        [Flags]
        public enum KeyModifiers
        {
            None = 0,
            LShift = 1 << 0,
            RShift = 1 << 1,
            LControl = 1 << 2,
            RControl = 1 << 3,
            LAlt = 1 << 4,
            RAlt = 1 << 5,
            LWin = 1 << 6,
            RWin = 1 << 7
        }


    }
}
