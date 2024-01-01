using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AsmResolver;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CLSS;
using SOD.Common;
using SOD.Common.Extensions;

namespace PluginDataPersistence
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    // [BepInDependency("SOD.Common", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BasePlugin
    {
        internal const uint MAX_JSON_SIZE = 1_000_000;
        internal static ManualLogSource Logger;
        internal static Dictionary<string, Dictionary<string, object>> queuedDataDictionary = new();

        internal static void WriteToOriginalProperty(string value)
        {
            RestoreOriginalProperty();
            GameplayController.Instance.companiesSabotaged.Add(value);
            Logger.LogInfo("Wrote mod plugin data to saved object.");
        }

        internal static bool TryRetrieveModDataFromSaveData(out string modDataJson)
        {
            var list = GameplayController.Instance.companiesSabotaged.ToList();
            if (list == null || !list.Any())
            {
                modDataJson = String.Empty;
                return false;
            }
            var filteredList = list.Where(element => element.StartsWith('{'));
            if (!filteredList.Any())
            {
                modDataJson = String.Empty;
                return false;
            }
            modDataJson = filteredList.First();
            return true;
        }

        internal static void RestoreOriginalProperty()
        {
            var list = GameplayController.Instance.companiesSabotaged.ToList();
            if (list == null || !list.Any())
            {
                return;
            }
            var filteredList = list.Where(element => element.StartsWith('{'));
            if (!filteredList.Any())
            {
                return;
            }
            foreach (var i in filteredList)
            {
                GameplayController.Instance.companiesSabotaged.Remove(i);
            }
            Logger.LogInfo("Removed mod plugin data from saved object.");
        }

        public override void Load()
        {
            Logger = Log;

            // Plugin startup logic
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            Lib.SaveGame.OnBeforeSave += (_, _) => StoreModDataInSave();
            Lib.SaveGame.OnAfterSave += (_, _) => RestoreOriginalProperty();
            Lib.SaveGame.OnAfterLoad += (_, _) => UnpackStoredModData();
        }

        /// <summary>
        /// Called just after loading a game. Reverts the changes made to game properties by StoreModDataInSave, and stores the saved information for mod access.
        /// </summary>
        private static void UnpackStoredModData()
        {
            Logger.LogInfo("Unpacking stored mod data.");
            if (!TryRetrieveModDataFromSaveData(out string jsonStr))
            {
                Logger.LogInfo("No mod data found in savegame.");
                return;
            }
            var fullData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonStr);

            RestoreOriginalProperty();

            queuedDataDictionary = new();
            foreach (var key in fullData.Keys)
            {
                jsonStr = fullData[key];
                var modData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
                queuedDataDictionary[key] = modData;
                Logger.LogInfo($"Loaded {key} mod plugin data into dictionary.");
            }
        }

        /// <summary>
        /// Called just before saving a game. Saves loaded plugin data dictionaries across all plugins in json format in a property that gets saved by the game.
        /// </summary>
        private void StoreModDataInSave()
        {
            if (queuedDataDictionary.Keys.Count == 0)
            {
                Logger.LogInfo("No mod data found, saving game normally.");
                return;
            }
            Dictionary<string, string> fullData = new();
            string jsonStr;
            foreach (var key in queuedDataDictionary.Keys)
            {
                var modData = queuedDataDictionary[key];
                if (modData.Count == 0)
                {
                    continue;
                }
                jsonStr = JsonSerializer.Serialize(modData);
                var size = jsonStr.GetBinaryFormatterSize();
                if (size > MAX_JSON_SIZE)
                {
                    Logger.LogError(
                        $"Skipping writing {key} mod plugin data: size of {size} bytes exceeds {MAX_JSON_SIZE} byte limit."
                    );
                    continue;
                }
                fullData.Add(key, jsonStr);
                Logger.LogInfo($"Wrote {key} mod plugin data to the saved data ({size} bytes).");
            }
            jsonStr = JsonSerializer.Serialize(fullData);
            WriteToOriginalProperty(jsonStr);
            Logger.LogInfo("Finished storing mod plugin data in save.");
        }

        public static Dictionary<string, object> LoadOrGetSaveGameData(BasePlugin plugin)
        {
            var metadata = MetadataHelper.GetMetadata(plugin);
            var key = $"{metadata.Name}";
            return queuedDataDictionary.GetOrAdd(key, _ => new());
        }
    }
}
