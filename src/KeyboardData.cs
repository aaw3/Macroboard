using Linearstar.Windows.RawInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static MacroBoard.Hook.KeyboardHook;

namespace MacroBoard
{


    class KeyboardData
    {
        private static List<KeyboardData> ActiveData = new List<KeyboardData>();

        public static void ProcessKey(KeyboardData data)
        {
            KeyState state = KeyState.Unknown;

            if ((KeyMessages)data.WindowMessage == KeyMessages.WM_KEYDOWN || (KeyMessages)data.WindowMessage == KeyMessages.WM_SYSKEYDOWN)
            {
                state = KeyState.Down;
            } else if ((KeyMessages)data.WindowMessage == KeyMessages.WM_KEYUP || (KeyMessages)data.WindowMessage == KeyMessages.WM_SYSKEYUP)
            {
                state = KeyState.Up;
            }
            else
            {
                //state is Unknown, this shouldn't happen
                throw new Exception("KeyState Unknown");
            }

            if (state == KeyState.Down)
            {
                ActiveData.Add(data);
            }

            ActiveData.ForEach(kbd => Debug.WriteLine(kbd.KeyboardAlias + " : " + (VKeys)kbd.VirtualKey + " : " + kbd.Flags + " : " + kbd.ScanCode));
            Debug.WriteLine("\r\n");

            if (state == KeyState.Up)
            {
                for (int i = 0; i < ActiveData.Count; i++)
                {
                    KeyboardData kbd = ActiveData[i];


                    RawKeyboardFlags kbdFlags = (RawKeyboardFlags)kbd.Flags;
                    RawKeyboardFlags dataFlags = (RawKeyboardFlags)data.Flags;

                    if (kbd.KeyboardAlias == data.KeyboardAlias &&
                    kbdFlags.HasFlag(RawKeyboardFlags.KeyE0) == dataFlags.HasFlag(RawKeyboardFlags.KeyE0) && //could technically just remove the None/KeyUp flag from both and it would solve the problem simpler.
                    kbdFlags.HasFlag(RawKeyboardFlags.KeyE1) == dataFlags.HasFlag(RawKeyboardFlags.KeyE1) &&
                    kbd.ScanCode == data.ScanCode &&
                    kbd.VirtualKey == data.VirtualKey)
                    {
                        ActiveData.Remove(kbd);
                    }
                }
            }
        }

        public static KeyboardData Format(string KeyboardAlias, RawInputKeyboardData data)
        {
            // Using WindowMessage to determine key state
            return new KeyboardData(
                KeyboardAlias,
                data.Keyboard.ExtraInformation,
                (int)data.Keyboard.Flags,
                data.Keyboard.ScanCode,
                data.Keyboard.VirutalKey,
                data.Keyboard.WindowMessage
                );
        }

        //Passed data from "RawInputKeyboardData"

        public readonly uint ExtraInformation;
        public readonly /*RawKeyboardFlags*/ int Flags;
        public readonly int ScanCode;
        public readonly int VirtualKey;
        public readonly uint WindowMessage;
        
        //Custom made data
        
        //The string passed when the key is pressed to tell the mod what keyboard it was sent from.
        public readonly string KeyboardAlias;
        
        //Determine if any key modifiers are presed.
        //public readonly int KeyModifiers;


        public KeyboardData(string keyboardAlias, uint extraInformation, int flags, int scanCode, int virtualKey, uint windowMessage)
        {
            KeyboardAlias = keyboardAlias;
            ExtraInformation = extraInformation;
            Flags = flags;
            ScanCode = scanCode;
            VirtualKey = virtualKey;
            WindowMessage = windowMessage;
        }
    }

    public enum KeyState
    {
        Unknown,
        Down,
        Up,
    }

    [Flags]
    public enum RawKeyboardFlags : ushort
    {
        None = 0,
        Up = 1 << 0,
        KeyE0 = 1 << 1,
        KeyE1 = 1 << 2,
    }
}
