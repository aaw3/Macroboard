using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MacroBoard.Inject
{
#warning Need to get proper unmangled names in the future.
    class DllInject
    {
        [DllImport("KeyboardHook.dll")]
        public static extern int InstallHook(IntPtr hwndParent);

        [DllImport("KeyboardHook.dll")]
        public static extern int UninstallHook();
    }
}
