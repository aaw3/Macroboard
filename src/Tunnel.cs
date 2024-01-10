using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;

namespace MacroBoard
{
    public class Tunnel
    {

        public Tunnel()
        {

        }


        public void UpdateKeyCombinations(dynamic modType, dynamic modKeyCombinations)
        {
            ModKeyCombination.SetModCombinations(modType, (List<ModKeyCombination>)modKeyCombinations);
        }


        public readonly MainWindow HostWindow;

        public void WriteDebugLine(string str)
        {
            Debug.WriteLine(str);
        }

        public void ShowMessageBox(string messageBoxText, string caption, int button, int icon)
        {
            new Thread(() => MessageBox.Show(messageBoxText, caption, (MessageBoxButton)button, (MessageBoxImage)icon)).Start();
        }
    }
}
