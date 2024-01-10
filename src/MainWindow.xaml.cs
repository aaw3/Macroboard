using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;
using System.Windows.Interop;
//using RawInput_dll;
using Linearstar.Windows.RawInput;
using System.Threading;
using System.ComponentModel;
using MacroBoard.Native;
using MacroBoard.Hook;
using MacroBoard.Inject;
using System.Runtime.InteropServices;

using System.IO;
using static MacroBoard.Hook.KeyboardHook;
using MacroBoard.Volume;
using System.Reflection;
using Linearstar.Windows.RawInput.Native;
using static MacroBoard.KeyHandling;
using static MacroBoard.HandlerCommunications;
using AutoMapper;
//using static MacroBoard.KeyHandling;


namespace MacroBoard
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Tunnel between mods and the host process.
        Tunnel tunnel;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);


            tunnel = new Tunnel();
        }

        IntPtr hwnd;

        Dictionary<string, RawInputKeyboard> NameToRIK = new Dictionary<string, RawInputKeyboard>();
        Dictionary<string, RawInputKeyboard> HIDToRIK = new Dictionary<string, RawInputKeyboard>();
        List<KeyboardConfig> KeyboardConfig = new List<KeyboardConfig>();
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            GetKeyboardDevices();

            hwnd = new WindowInteropHelper(this).Handle;

            int installed = DllInject.InstallHook(hwnd);
            Debug.WriteLine("Installed? : " + installed);


            RawInputDevice.RegisterDevice(new RawInputDeviceRegistration(HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DevNotify, hwnd));
            
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(new HwndSourceHook(WndProc));

            LoadConfigs();
            LoadMods();

            ReloadAllModConfigs.Click += (s, eargs) => ReloadModConfigs();
            ReloadAllModFiles.Click += (s, eargs) => ReloadModFiles();
        }

        public void LoadConfigs()
        {
            if (!Directory.Exists(Config.AppConfigDir))
                Directory.CreateDirectory(Config.AppConfigDir);

            if (!Directory.Exists(Config.ModConfigDir))
                Directory.CreateDirectory(Config.ModConfigDir);




            Config.HandleDeviceConfig(KeyboardConfigs);
        }


        // long must be used to prevent issues when converting to IntPtr
        public static event Action<long, long, dynamic[]> MacroKeyEvent;

        public List<dynamic> Mods = new List<dynamic>();
        public void LoadMods()
        {
            

            string ModsFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");
            if (!Directory.Exists(ModsFolder))
                Directory.CreateDirectory(ModsFolder);

            string[] DLLs = Directory.GetFiles(ModsFolder);
            for (int i = 0; i < DLLs.Length; i++)
            {
                Debug.WriteLine(FileVersionInfo.GetVersionInfo(DLLs[i]).FileDescription);

                Assembly asm = Assembly.LoadFile(DLLs[i]);
                Type type = asm.GetType($"{System.IO.Path.GetFileNameWithoutExtension(DLLs[i])}.Mod");

                dynamic Mod = Activator.CreateInstance(type);

                

                //Initialize the Mod, send it the HostProcess to access the Mods
                
                HandleType handleType = (HandleType)Mod.Init(tunnel);

                Mods.Add(Mod);

                if (handleType == HandleType.HandlerOnly || handleType == HandleType.Both)
                {
                    Handler.SetHandlerType(type, true);

                    //requests combinations so that the mod knows when it is okay to send them and not have conflict with the SetHandlerType restricting it
                    
                    List<ModKeyCombination> data = ModKeyCombination.Convert(Mod.ReturnKeyCombinations());

                    ModKeyCombination.SetModCombinations(type, data);
                    
                    ModData modData = new ModData(asm, type, Mod);
                    ModData.ModDataList.Add(modData);
                    MacroKeyEvent += (a, b, c) =>
                    {
                        SendKeyToMod(modData, a, b, c);
                    };
                }
                else
                {
                    Handler.SetHandlerType(type, false);
                }
            }
        }


        public void SendKeyToMod(ModData modData, long a, long b, dynamic[] c)
        {
            try
            {
                modData.Mod.Call(a, b, c);
            }
            catch (Exception ex)
            {
                if (IgnoreCallExceptions.IsChecked ?? false)
                    return;


                int inc = 0;
                foreach (var debug in new StackTrace(ex).GetFrames())
                {
                    Debug.WriteLine(inc + ": " + debug.GetMethod().Name);
                    inc++;
                }

                var s = new StackTrace(ex);
                var ModMethodNames = s.GetFrames().Select(f => f.GetMethod()).Where(m => m.Module.Assembly == modData.Assembly);

                string StraceTraceMethods = "Stack Trace of Mod Methods:";
                foreach (var methodBase in ModMethodNames)
                    StraceTraceMethods += "\r\n" + methodBase.Name;


                new Thread(() =>
                MessageBox.Show("Please notify the mod developer of the following error:\r\n\r\nException while or after passing data to \"" + modData.Type.Namespace + "\" (Call Method)\r\n\r\n" + ex.GetType().ToString() + ": " + ex.Message + "\r\n\r\n" + StraceTraceMethods,
                "MacroBoard - Exception")).Start();




            }
        }

        // Didn't finish implementing
        public void UnloadMods()
        {
            //foreach (var Mod in Mods)
            //{
            //    Debug.WriteLine("Output Of Releasal: " + Marshal.ReleaseComObject(Mod));
            //}
        }

        public void ReloadModConfigs()
        {
            Mods.ForEach(Mod => Mod.Reload());
        }

        public void NotifyModsOfClosing()
        {
            Mods.ForEach(Mod => Mod.Closing());
        }

        public void ReloadModFiles()
        {
            MessageBox.Show("Not possible at the moment.");
            return;
            UnloadMods();
            LoadMods();
        }

        DeviceConfig LoadedDeviceConfig;

        public static readonly string BlankDeviceName = "None";
        public void GetKeyboardDevices()
        {
            var devices = RawInputDevice.GetDevices();
            var keyboards = devices.OfType<RawInputKeyboard>();

            List<string> keyboardList = new List<string>();


            Devices.ItemsSource = KeyboardConfigs;


            if (File.Exists(Config.AppConfigDevicesPath))
            {
                LoadedDeviceConfig = Config.ReadDevices(Config.AppConfigDevicesPath);

                DeviceConfig dc = LoadedDeviceConfig;

                foreach (var keyboard in keyboards)
                {
                    string keyboardName = keyboard.ProductName;

                    int i = 0;
                    while (true)
                    {
                        if (i == 0)
                        {
                            if (!NameToRIK.ContainsKey(keyboardName))
                            {
                                keyboardList.Add(keyboardName);
                                NameToRIK.Add(keyboardName, keyboard);
                                HIDToRIK.Add(keyboard.DevicePath, keyboard);

                                foreach (var dcKeyboard in dc.Keyboards)
                                {
                                    if (dcKeyboard.KeyboardPath == keyboard.DevicePath)
                                    {
                                        KeyboardConfigs.Add(new KeyboardConfig(dcKeyboard.KeyboardAlias, dcKeyboard.IsMacroBoard, dcKeyboard.HasAutoNumLock, keyboardName, keyboard.DevicePath, dcKeyboard.IsDefaultMacroBoard));
                                        DefaultKeyboardAlias = dcKeyboard.KeyboardAlias;
                                    }
                                }

                                break;
                            }
                            i++;
                        }
                        else
                        {
                            if (!NameToRIK.ContainsKey($"{keyboardName} ({i})"))
                            {
                                keyboardList.Add($"{keyboardName} ({i})");
                                NameToRIK.Add($"{keyboardName} ({i})", keyboard);
                                HIDToRIK.Add(keyboard.DevicePath, keyboard);

                                foreach (var dcKeyboard in dc.Keyboards)
                                {
                                    if (dcKeyboard.KeyboardPath == keyboard.DevicePath)
                                    {
                                        KeyboardConfigs.Add(new KeyboardConfig(dcKeyboard.KeyboardAlias, dcKeyboard.IsMacroBoard, dcKeyboard.HasAutoNumLock, $"{keyboardName} ({i})", keyboard.DevicePath, dcKeyboard.IsDefaultMacroBoard));
                                        DefaultKeyboardAlias = dcKeyboard.KeyboardAlias;
                                    }
                                }
                                break;
                            }
                            i++;
                        }
                    }
                }
            }
            else
            {
                //Set LoadedDeviceConfig.UsingMultipleKeyboards to true by default, can change this later maybe or have an option to change this later
                if (LoadedDeviceConfig == null)
                    LoadedDeviceConfig = new DeviceConfig(/*true,*/ null);

                foreach (var keyboard in keyboards)
                {
                    string keyboardName = keyboard.ProductName;

                    int i = 0;
                    while (true)
                    {
                        if (i == 0)
                        {
                            if (!NameToRIK.ContainsKey(keyboardName))
                            {
                                keyboardList.Add(keyboardName);
                                NameToRIK.Add(keyboardName, keyboard);

                                KeyboardConfigs.Add(new KeyboardConfig("---", false, false, keyboardName, keyboard.DevicePath, false));
                                break;
                            }
                            i++;
                        }
                        else
                        {
                            if (!NameToRIK.ContainsKey($"{keyboardName} ({i})"))
                            {
                                keyboardList.Add($"{keyboardName} ({i})");
                                NameToRIK.Add($"{keyboardName} ({i})", keyboard);

                                KeyboardConfigs.Add(new KeyboardConfig("---", false, false, $"{keyboardName} ({i})", keyboard.DevicePath, false));
                                break;
                            }
                            i++;
                        }

                        //Just set the first KeyboardConfig's IsDefaultMacroBoard to true
                        DefaultKeyboardAlias = KeyboardConfigs[0].KeyboardAlias;
                        KeyboardConfigs[0].IsDefaultMacroBoard = true;

                    }

                }
            }
            KeyboardConfigs = KeyboardConfigs.OrderBy(c => c.KeyboardName).ToList();

            NameToRIK.Add(BlankDeviceName, null);

            foreach (var keyboardName in keyboardList.OrderBy(c => c).ToArray())
            {
                //DevicesComboBox.Items.Add(keyboardName);

            }

            //Add keys to dictionary for key modifiers
            foreach (var KeyboardConfig in KeyboardConfig)
            {
                KeyModifierDict.Add(KeyboardConfig, (KeyModifiers)0);
            }

            //Update the selected keyboard highlight
            try
            {
                Devices.Items.Refresh();
            }
            catch (Exception ex) { }
        }

        //Using KeyboardConfig instead of RawInputKeyboard (even though RIK has more information) because we don't want to store all of the information RIK has because it is unnecessary!
        /*RawInputKeyboard*/ KeyboardConfig macroKeyboard;
        public void DevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Retrieve the KeyboardConfig object instead of getting a string to pass to a dictionary
            macroKeyboard = (KeyboardConfig)((ComboBox)sender).SelectedValue;
            //macroKeyboard = NameToRIK[comboBoxText];

        }


        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            DllInject.UninstallHook();
            RawInputDevice.UnregisterDevice(HidUsageAndPage.Keyboard);
            //RawInputDevice.UnregisterDevice(HidUsageAndPage.GamePad);

            using (var f = File.Open(Config.AppConfigDevicesPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                    //LoadedDeviceConfig.UsingMultipleKeyboards = true;

                Config.SaveDevices(new DeviceConfig(KeyboardConfig), f);
            }

            NotifyModsOfClosing();

        }

        private void EnumDevices_Click(object sender, RoutedEventArgs e)
        {
        }

        Dictionary<IntPtr, string> HID_DeviceNames = new Dictionary<IntPtr, string>();

        RawInputKeyboardData mostRecentData;


        bool blockNextScrollLock;
        bool receivedBlockNextScrollLock;

        protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
        
            switch (msg)
            {

                case (int)WindowMessage.WM_INPUT:
                    {
                        RawInputData data = null;

                        try
                        {
                            // Create an RawInputData from the handle stored in lParam.
                            data = RawInputData.FromHandle(lParam);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.GetType() + ": " + ex.Message);
                        }

                        // You can identify the source device using Header.DeviceHandle or just Device.
                        var sourceDeviceHandle = data?.Header.DeviceHandle;
                        var sourceDevice = data?.Device;


                        //Complete Product Logic Here
                        if (sourceDevice != null && sourceDevice is RawInputKeyboard) //Null check because not able to retreive device obj when media keys pressed
                        {
                            mostRecentData = (RawInputKeyboardData)data;
                        }

                        // The data will be an instance of either RawInputMouseData, RawInputKeyboardData, or RawInputHidData.
                        // They contain the raw input data in their properties.

                        switch (data)
                        {
                            case RawInputMouseData mouse:
                                Debug.WriteLine(mouse.Mouse);
                                break;
                            case RawInputKeyboardData keyboard:

                                //Need to communicate between WM_HOOK to prevent an infinite loop and pressing the key twice
                                if ((VKeys)keyboard.Keyboard.VirutalKey == VKeys.NUMLOCK && keyboard.Device != null)
                                {
                                    foreach (var keybd in KeyboardConfig)
                                    {
                                        if (keybd.HasAutoNumLock && keybd.KeyboardPath == keyboard.Device.DevicePath)
                                        {
                                            if (receivedBlockNextScrollLock)
                                            {
                                                receivedBlockNextScrollLock = false;
                                                break;
                                            }

                                            blockNextScrollLock = true;
                                            break;
                                        }
                                    }
                                }
                                break;

                            case RawInputHidData hid:
                                break;

                        }

                        break;
                        Debug.WriteLine(data.Device.DeviceType);
                    }

                case (int)CustomMessages.WM_HOOK:
                    {
                        if ((VKeys)wParam == VKeys.NUMLOCK)
                        {
                            foreach (var keybd in KeyboardConfig)
                            {
                                if (blockNextScrollLock && mostRecentData.Device.DevicePath == keybd.KeyboardPath)
                                {
                                    blockNextScrollLock = false;
                                    receivedBlockNextScrollLock = true;

                                    Thread.Sleep(10);

                                    Keyboards.keybd_event((byte)VKeys.NUMLOCK, 0x45, Keyboards.KEYEVENTF_EXTENDEDKEY | 0, 0);

                                    Keyboards.keybd_event((byte)VKeys.NUMLOCK, 0x45, Keyboards.KEYEVENTF_EXTENDEDKEY | Keyboards.KEYEVENTF_KEYUP, 0);
                                    
                                    return (IntPtr)1;
                                }
                            }
                        }

                        if (HandleKey(wParam, lParam, mostRecentData))
                        {
                            handled = true;
                            return (IntPtr)1;
                        }
                        break;
                    }

                    //This message can be used for detecting when a device disconnects and reconnects so it can be reprocessed if it is moved and be reconnected to its keyboard alias
                case (int)CustomMessages.WM_INPUT_DEVICE_CHANGE: //WindowMessage.WM_DEVICECHANGE is more compatible with XP era
                    {
#warning this seems to be a race condition between this and HIDToRIK being assigned!
                        string name = User32.GetRawInputDeviceName((RawInputDeviceHandle)lParam);

                        //Should Have the user handle it later, setting as it null currently
                        if (!HID_DeviceNames.ContainsKey(lParam))
                            HID_DeviceNames.Add(lParam, name == null ? HID_DeviceNames[lParam] : name);
                        else
                        {
                            HID_DeviceNames[lParam] = name == null ? HID_DeviceNames[lParam] : name; 

                        }

                        // You can identify the source device using Header.DeviceHandle or just Device. //???
                        //var sourceDeviceHandle = data.Header.DeviceHandle;
                        //var sourceDevice = data.Device;


                        //if (sourceDevice != null)
                        //{
                        //    keybdData = (RawInputKeyboardData)data;
                        //}

                        if (wParam == (IntPtr)WM_INPUT_DEVICE_CHANGE_WPARAM.GIDC_ARRIVAL)
                        {
                            Debug.WriteLine("Device Arrived: " + name + " : " + lParam);

                            Debug.WriteLine("In DeviceNames Dict? : " + HID_DeviceNames.ContainsKey(lParam) + (HID_DeviceNames.ContainsKey(lParam) ? " : DevicePath: " + HID_DeviceNames[lParam] + " : ProductName: " + (HIDToRIK.ContainsKey(HID_DeviceNames[lParam]) ? HIDToRIK[HID_DeviceNames[lParam]].ProductName : "") : ""));

                        } else if (wParam == (IntPtr)WM_INPUT_DEVICE_CHANGE_WPARAM.GIDC_REMOVAL)
                        {
                            Debug.WriteLine("Device Removed: " + name + " : " + lParam);

                            //Debug.WriteLine("Keyboard Removed: " + HIDToRIK[HID_DeviceNames[lParam]]);

                            Debug.WriteLine("In DeviceNames Dict? : " + HID_DeviceNames.ContainsKey(lParam) + (HID_DeviceNames.ContainsKey(lParam) ? " : DevicePath: " + HID_DeviceNames[lParam] + " : ProductName: " + (HIDToRIK.ContainsKey(HID_DeviceNames[lParam]) ? HIDToRIK[HID_DeviceNames[lParam]].ProductName : "") : ""));

                            //remove device if disconnected.
                        }




                        break;
                    }
            }


            return IntPtr.Zero;
        }


        Dictionary<KeyboardConfig, KeyModifiers> KeyModifierDict = new Dictionary<KeyboardConfig, KeyModifiers>();
        public bool HandleKey(IntPtr wParam, IntPtr lParam, RawInputKeyboardData keyboardDeviceData)
        {
            foreach (var KeyboardConfig in KeyboardConfig)
            {
                if (keyboardDeviceData?.Device?.DevicePath == KeyboardConfig.KeyboardPath && KeyboardConfig.IsMacroBoard)
                {
                    string KeyboardAlias = "";
                    foreach (var KeyBoardConfig in KeyboardConfig)
                    {
                        if (keyboardDeviceData?.Device?.DevicePath == KeyboardConfig.KeyboardPath)
                        {
                            KeyboardAlias = KeyboardConfig.KeyboardAlias;
                        }
                    }

                    if (ShowCurrentKeyData.IsChecked != null ? (bool)ShowCurrentKeyData.IsChecked : false)
                    {
                        new Thread(() => MessageBox.Show($"Keyboard Name: {keyboardDeviceData.Device.ProductName}\r\nKeyboardAlias: {KeyboardAlias}\r\nVirtual Key: {(VKeys)keyboardDeviceData.Keyboard.VirutalKey} ({keyboardDeviceData.Keyboard.VirutalKey})\r\nScanCode: {keyboardDeviceData.Keyboard.ScanCode}\r\nFlags: {keyboardDeviceData.Keyboard.Flags} ({(int)keyboardDeviceData.Keyboard.Flags})\r\nWindows Message: {(WindowMessage)keyboardDeviceData.Keyboard.WindowMessage} ({keyboardDeviceData.Keyboard.WindowMessage})")).Start();
                    }

                    KeyData.ProcessKey(KeyData.Format(KeyboardAlias, keyboardDeviceData), keyboardDeviceData.Keyboard.WindowMessage);




                    MacroKeyEvent?.Invoke((long)wParam, (long)lParam, new dynamic[]
                    {
                    new KeyData(KeyboardAlias, (int)keyboardDeviceData.Keyboard.Flags, keyboardDeviceData.Keyboard.ScanCode, keyboardDeviceData.Keyboard.VirutalKey)
                    });

                    return true;
                }
            }

            return false;
        }

        public static string DefaultKeyboardAlias;
        private void Devices_MouseDoubleClick(object sender, MouseButtonEventArgs eargs)
        {
            for (int i = 0; i < KeyboardConfig.Count; i++)
            {
                KeyboardConfig[i].IsDefaultMacroBoard = false;
            }
            var kc = ((KeyboardConfig)Devices.SelectedItem);
            string Alias = kc.KeyboardAlias;
            kc.IsDefaultMacroBoard = true;
            DefaultKeyboardAlias = kc.KeyboardAlias;

            Debug.WriteLine(Alias);

            try
            {
                Devices.Items.Refresh();
            }
            catch (Exception ex) { }
        }

    }

    #region Redundant
    class KeyboardInfo
    {
        public KeyboardInfo(string caption, string configManagerErrorCode, string installDate, string configManagerUserConfig, string creationClassName,
            string description, string deviceID, string errorCleared, string errorDescription, string layout, string numberOfFunctionKeys, 
            string lastErrorCode, string name, string pNPDeviceID, string powerManagementSupported, 
            string status, string systemCreationClassName, string systemName)
        {
            Caption = caption;
            ConfigManagerErrorCode = configManagerErrorCode;
            InstallDate = installDate;
            ConfigManagerUserConfig = configManagerUserConfig;
            CreationClassName = creationClassName;
            Description = description;
            DeviceID = deviceID;
            ErrorCleared = errorCleared;
            ErrorDescription = errorDescription;
            Layout = layout;
            NumberOfFunctionKeys = numberOfFunctionKeys;
            LastErrorCode = lastErrorCode;
            Name = name;
            PNPDeviceID = pNPDeviceID;
            PowerManagementSupported = powerManagementSupported;
            Status = status;
            SystemCreationClassName = systemCreationClassName;
            SystemName = systemName;
        }

        public string Caption { get; private set; }
        public string ConfigManagerErrorCode { get; private set; }
        public string InstallDate { get; private set; }
        public string ConfigManagerUserConfig { get; private set; }
        public string CreationClassName { get; private set; }
        public string Description { get; private set; }
        public string DeviceID { get; private set; }
        public string ErrorCleared { get; private set; }
        public string ErrorDescription { get; private set; }
        public string Layout { get; private set; }
        public string NumberOfFunctionKeys { get; private set; }
        public string LastErrorCode { get; private set; }
        public string Name { get; private set; }
        public string PNPDeviceID { get; private set; }
        public string PowerManagementSupported { get; private set; }
        public string Status { get; private set; }
        public string SystemCreationClassName { get; private set; }
        public string SystemName { get; private set; }

    }

    //class USBDeviceInfo
    //{
    //    public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
    //    {
    //        this.DeviceID = deviceID;
    //        this.PnpDeviceID = pnpDeviceID;
    //        this.Description = description;
    //    }
    //    public string DeviceID { get; private set; }
    //    public string PnpDeviceID { get; private set; }
    //    public string Description { get; private set; }
    //}

    #endregion
}
