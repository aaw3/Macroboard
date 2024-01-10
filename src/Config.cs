using Linearstar.Windows.RawInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;

namespace MacroBoard
{
    class Config
    {
        public static readonly string ConfigDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");
        public static readonly string AppConfigDir = Path.Combine(ConfigDir, "Application");
        public static readonly string ModConfigDir = Path.Combine(ConfigDir, "Mods");

        public static readonly string AppConfigDevicesPath = Path.Combine(AppConfigDir, "Devices.cfg");

        /// <summary>
        /// Saves and updates the device config if a new device is found
        /// </summary>
        /// <param name="deviceConfig">The DeviceConfig object being stored</param>
        /// <param name="saveStream">Path where the configuration file will be saved</param>
        public static void SaveDevices(DeviceConfig deviceConfig, Stream saveStream)
        {
            XmlSerializer xs = new XmlSerializer(typeof(DeviceConfig));

            xs.Serialize(saveStream, deviceConfig);
        }

        public static DeviceConfig ReadDevices(string savePath)
        {
            XmlSerializer xs = new XmlSerializer(typeof(DeviceConfig));

            using (TextReader tw = new StreamReader(savePath))
            {
                return (DeviceConfig)xs.Deserialize(tw);
            }
        }

        //???
        public static DeviceConfig GetDevices()
        {
            return new DeviceConfig(/*new bool(),*/ new List<KeyboardConfig>());
        }

        //???
        public static bool NewDevicesFound()
        {
            return new bool();
        }

        public static void /*DeviceConfig*/ HandleDeviceConfig(List<KeyboardConfig> keyboardList)
        {
            if (File.Exists(AppConfigDevicesPath))
            {
                DeviceConfig dc = ReadDevices(AppConfigDevicesPath);

                //New Keyboard(s) Found
                if (dc.Keyboards.Count < keyboardList.Count)
                {
                    List<KeyboardConfig> AddedKeyboardsList = new List<KeyboardConfig>();

                    AddedKeyboardsList = keyboardList.Where(x => !dc.Keyboards.Any(y => y.KeyboardName == x.KeyboardName)).ToList();
                  
                    
                    foreach (var keyboard in dc.Keyboards)
                        System.Diagnostics.Debug.WriteLine("dc.Keyboards: " + keyboard.KeyboardName);

                    foreach (var keyboard in keyboardList)
                    {
                        System.Diagnostics.Debug.WriteLine("keyboardList: " + keyboard.KeyboardName);
                    }

                    string addedKeyboards = "";
                    foreach (var keyboard in AddedKeyboardsList)
                        addedKeyboards += $"\r\n{keyboard.KeyboardName}\r\n";

                    MessageBox.Show($"New devices have been found.\r\nPress \"Yes\" if you would like to configure these devices.\r\n\r\nAdded Devices:\r\n{addedKeyboards}", "New Devices Found", MessageBoxButton.YesNo, MessageBoxImage.Question);
                }
                //Old Keyboard(s) Removed
                else if (dc.Keyboards.Count > keyboardList.Count)
                {
                    List<KeyboardConfig> RemovedKeyboardsList = new List<KeyboardConfig>();

                    RemovedKeyboardsList = dc.Keyboards.Where(x => !keyboardList.Any(y => x.KeyboardName == y.KeyboardName)).ToList();

                    string removedKeyboards = "";
                    foreach (var keyboard in RemovedKeyboardsList)
                        removedKeyboards += $"\r\n{keyboard.KeyboardName}\r\n";

                    MessageBox.Show($"Old devices have been removed.\r\nRemoved Devices:\r\n{removedKeyboards}", "Old Devices Removed", MessageBoxButton.YesNo, MessageBoxImage.Question);
                }
            }
            else
            {
                using (var f = File.Create(AppConfigDevicesPath))
                {
                    DeviceConfig dc = new DeviceConfig(keyboardList);
                    SaveDevices(dc, f);
                }
            }
        }
    }
}
