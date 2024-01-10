using Linearstar.Windows.RawInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static MacroBoard.Hook.KeyboardHook;

namespace MacroBoard
{


    public class KeyData
    {
        public KeyData() { }

        private static List<KeyData> Keys = new List<KeyData>();

        public static void ProcessKey(KeyData data, uint WindowMessage)
        {
            KeyState state = KeyState.Unknown;

            if ((KeyMessages)WindowMessage == KeyMessages.WM_KEYDOWN || (KeyMessages)WindowMessage == KeyMessages.WM_SYSKEYDOWN)
            {
                state = KeyState.Down;
            } else if ((KeyMessages)WindowMessage == KeyMessages.WM_KEYUP || (KeyMessages)WindowMessage == KeyMessages.WM_SYSKEYUP)
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
                //boolean determining if this key is already down
                bool keyAlreadyExists = false;
                for (int i = 0; i < Keys.Count; i++)
                {
                    KeyData key = Keys[i];
                    if (Compare(data, key))
                    {
                        keyAlreadyExists = true;
                    }

                if (!keyAlreadyExists)
                    Keys.Add(data);



            }

            Debug.WriteLine("Keys count: " + Keys.Count);

            Keys.ForEach(kbd => Debug.WriteLine(kbd.KeyboardAlias + " : (" + (VKeys)kbd.VirtualKey + ") " + kbd.VirtualKey + " : (" + (RawKeyboardFlags)kbd.Flags + ") " + kbd.Flags + " : " + kbd.ScanCode));
            Debug.WriteLine("\r\n");

            if (state == KeyState.Up)
            {
                for (int i = 0; i < Keys.Count; i++)
                {
                    KeyData key = Keys[i];

                    if (Compare(data, key))
                    {
                        Keys.Remove(key);
                    }
                    
                }
            }
            else
            {
                //Check all Key Combinations
                Handler.HandleCombination(Keys);
            }
        }

        public static KeyData Format(string KeyboardAlias, RawInputKeyboardData data)
        {

            return new KeyData(
                KeyboardAlias,
                (int)data.Keyboard.Flags,
                data.Keyboard.ScanCode,
                data.Keyboard.VirutalKey
                );
        }

        public static bool Compare(KeyData key1, KeyData key2)
        {
            RawKeyboardFlags key1Flags = (RawKeyboardFlags)key1.Flags;
            RawKeyboardFlags key2Flags = (RawKeyboardFlags)key2.Flags;

            if (key1.KeyboardAlias == key2.KeyboardAlias &&
            key1Flags.HasFlag(RawKeyboardFlags.KeyE0) == key2Flags.HasFlag(RawKeyboardFlags.KeyE0) && //could technically just remove the None/KeyUp flag from both and it would solve the problem simpler.
            key1Flags.HasFlag(RawKeyboardFlags.KeyE1) == key2Flags.HasFlag(RawKeyboardFlags.KeyE1) &&
            key1.ScanCode == key2.ScanCode &&
            key1.VirtualKey == key2.VirtualKey)
            {
                return true;
            }
            return false;
        }

        //Same as Compare, but is used specifically for comparing Keys and ModKeyCombinations specifically for KeyAlias being [Default]
        public static bool CompareToModKeybind(KeyData modData, KeyData keyData, string ReplaceDefault)
        {
            RawKeyboardFlags key1Flags = (RawKeyboardFlags)modData.Flags;
            RawKeyboardFlags key2Flags = (RawKeyboardFlags)keyData.Flags;

            if (((modData.KeyboardAlias == keyData.KeyboardAlias) || (modData.KeyboardAlias == "[Default]" && ReplaceDefault == keyData.KeyboardAlias)) &&
            key1Flags.HasFlag(RawKeyboardFlags.KeyE0) == key2Flags.HasFlag(RawKeyboardFlags.KeyE0) && //could technically just remove the None/KeyUp flag from both and it would solve the problem simpler.
            key1Flags.HasFlag(RawKeyboardFlags.KeyE1) == key2Flags.HasFlag(RawKeyboardFlags.KeyE1) &&
            modData.ScanCode == keyData.ScanCode &&
            modData.VirtualKey == keyData.VirtualKey)
            {
                return true;
            }
            return false;
        }

        public int Flags { get; set; }
        public int ScanCode { get; set; }
        public int VirtualKey { get; set; }

        //Custom made data

        //The string passed when the key is pressed to tell the mod what keyboard it was sent from.
        public /*readonly*/ string KeyboardAlias { get; set; }


        public KeyData(string keyboardAlias, int flags, int scanCode, int virtualKey)
        {
            KeyboardAlias = keyboardAlias;
            Flags = flags;
            ScanCode = scanCode;
            VirtualKey = virtualKey;
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
