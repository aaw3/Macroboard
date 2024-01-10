using System;
using System.Collections.Generic;
using System.Text;

namespace MacroBoard
{
    class HandlerCommunications
    {
        /// <summary>
        /// Tells the Mod Handler how keys should be handled.
        /// </summary>
        public enum HandleType
        {
            /// <summary>
            /// Have the mod handler (Main Application) handle all of the keys. This means the mod won't recieve "Call" method calls.
            /// </summary>
            HandlerOnly,

            /// <summary>
            /// Have the mod handle all of the keys. This means the mod handler (Main Application) won't accept any [List&lt;KeyCombination&gt;, delegate] dicts.
            /// </summary>
            ModOnly,

            /// <summary>
            /// Have both the mod handler (Main Application) and mod handle the keys. This means the mod will recieve "Call" method calls, and can send [List&lt;KeyCombination&gt;, delegate] dicts to the mod handler.
            /// </summary>
            Both
        }
    }
}
