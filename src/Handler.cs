using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace MacroBoard
{
    class Handler
    {
        //Contains list of ModKeyCombination objects accessible by the mod's Type
        public static Dictionary<dynamic, List<ModKeyCombination>> ModsKeyCombinations = new Dictionary<dynamic, List<ModKeyCombination>>();

        /// <summary>
        /// This is a boolean mapping a ModType to a boolean saying whether or not it is allowed to set key combinations determined by the HandlerCommunications.HandleType
        /// </summary>
        private static Dictionary<dynamic, bool> SupportedByHandler = new Dictionary<dynamic, bool>();

        public static void SetHandlerType(dynamic modType, bool b)
        {
            SupportedByHandler.Add(modType, b);
        }

        public static void HandleCombination(List<KeyData> Keys)
        {

            var modDataList = ModData.ModDataList;

            for (int i = 0; i < modDataList.Count; i++)
            {
                //Contains list of KeyCombinations under a specific mod
                var modKeyCombinationsList = ModsKeyCombinations[modDataList[i].Type];
                
                for (int j = 0; j < modKeyCombinationsList.Count; j++)
                {

                    int keysMatching = 0;
                    var ModComboKeys = modKeyCombinationsList[j].Keys;

                    for (int k = 0; k < ModComboKeys.Count; k++)
                    {

                        for (int l = 0; l < Keys.Count; l++)
                        {

                            if (KeyData.CompareToModKeybind(ModComboKeys[k], Keys[l], MainWindow.DefaultKeyboardAlias))
                            {
                                keysMatching++;
                            }
                        }

                    }

                    if (keysMatching == ModComboKeys.Count)
                    {
                        Debug.WriteLine("Calling Method: " + modKeyCombinationsList[j].CallbackMethod.Method.Name);
                        modKeyCombinationsList[j].CallbackMethod.Invoke();
                    }
                }
            }
        }
    }

    public class ModKeyCombination
    {
        //Convert List<[Mod Name].ModKeyComination> to List<MacroBoard.ModKeyCombination> Not sure how to cast different types, so copying each invidivual primitive at a time.
        public static List<ModKeyCombination> Convert(dynamic data) 
        {
            List<ModKeyCombination> ComboList = new List<ModKeyCombination>();
            Debug.WriteLine(data.Count as object);
            for (int i = 0; i < data.Count; i++)
            {
                ModKeyCombination Combo = new ModKeyCombination();
                foreach (PropertyInfo prop in data[i].GetType().GetProperties())
                {
                    if (prop.Name == "Keys")
                    {
                        List<KeyData> keyListData = new List<KeyData>();
                        var modKeys = prop.GetValue(data[i]);
                        for (int j = 0; j < modKeys.Count; j++)
                        {
                            KeyData keyData = new KeyData();

                            foreach (PropertyInfo propKey in modKeys[j].GetType().GetProperties())
                            {

                                typeof(KeyData)
                                    .GetProperty(propKey.Name, BindingFlags.Instance | BindingFlags.Public)
                                    .SetValue(keyData, propKey.GetValue(modKeys[j]));
                            }

                            keyListData.Add(keyData);
                        }

                        Combo.Keys = keyListData;
                    }
                    else
                    {
                        typeof(ModKeyCombination)
                            .GetProperty(prop.Name, BindingFlags.Instance | BindingFlags.Public)
                            .SetValue(Combo, prop.GetValue(data[i]));

                    }


                }

                ComboList.Add(Combo);
            }

            return ComboList;
        }

        public static void SetModCombinations(dynamic modType, List<ModKeyCombination> Combinations)
        {
            if (!Handler.ModsKeyCombinations.ContainsKey(modType))
            {
                Handler.ModsKeyCombinations.Add(modType, Combinations);
            }
            else
            {
                Handler.ModsKeyCombinations[modType] = Combinations;
            }
        }

        public ModKeyCombination()
        {

        }

        public ModKeyCombination(List<KeyData> keys, Action callbackMethod)
        {
            Keys = keys;
            CallbackMethod = callbackMethod;

        }


        public List<KeyData> Keys { get; set; }
        public Action CallbackMethod { get; set; }
    }
