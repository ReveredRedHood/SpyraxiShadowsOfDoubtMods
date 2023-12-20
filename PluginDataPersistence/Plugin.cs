using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Text.Json;
using CLSS;
using AsmResolver;

namespace PluginDataPersistence
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    [BepInDependency("SpyraxiHelpers", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BasePlugin
    {
        internal const uint MAX_JSON_SIZE = 1_000_000;
        internal const string DATA_TO_RESTORE_KEY = "__dataToRestore";
        internal static ManualLogSource Logger;
        internal static Dictionary<string, Dictionary<string, object>> queuedDataDictionary = new();
        internal static string s_tempOriginalPropertyData;

        internal static string s_OriginalProperty
        {
            get
            {
                return Game.Instance.playerFirstName;
            }
            set
            {
                Game.Instance.playerFirstName = value;
            }
        }

        public override void Load()
        {
            Logger = Log;

            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            SpyraxiHelpers.Hooks.OnPreSave.AddListener(_ => StoreModDataInSave());
            SpyraxiHelpers.Hooks.OnPostSave.AddListener(_ => UnstoreModDataInSave());

            SpyraxiHelpers.Hooks.OnPostLoad.AddListener(_ => UnpackStoredModData());
        }

        /// <summary>
        /// Called just after saving a game. Reverts the changes made by StoreModDataInSave.
        /// </summary>
        private void UnstoreModDataInSave()
        {
            s_OriginalProperty = s_tempOriginalPropertyData;
            Log.LogInfo("Removed stored mod plugin data post-save.");
        }

        /// <summary>
        /// Called just after loading a game. Reverts the changes made to game properties by StoreModDataInSave, and stores the saved information for mod access.
        /// </summary>
        private void UnpackStoredModData()
        {
            Log.LogInfo("Unpacking stored mod data.");
            string jsonStr = s_OriginalProperty;
            if (!jsonStr.StartsWith('{'))
            {
                Log.LogInfo("No mod data found in savegame.");
                return;
            }
            var fullData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonStr);

            s_OriginalProperty = fullData[DATA_TO_RESTORE_KEY];
            Log.LogInfo("Restored property storing mod data to original value.");
            fullData.Remove(DATA_TO_RESTORE_KEY);

            queuedDataDictionary = new();
            foreach (var key in fullData.Keys)
            {
                jsonStr = fullData[key];
                var modData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
                queuedDataDictionary[key] = modData;
                Log.LogInfo($"Loaded {key} mod plugin data into dictionary.");
            }
        }

        /// <summary>
        /// Called just before saving a game. Saves loaded plugin data dictionaries across all plugins in json format in a property that gets saved by the game.
        /// </summary>
        private void StoreModDataInSave()
        {
            if (queuedDataDictionary.Keys.Count == 0)
            {
                Log.LogInfo("No mod data found, saving game normally.");
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
                    Log.LogError($"Skipping writing {key} mod plugin data: size of {size} bytes exceeds {MAX_JSON_SIZE} byte limit.");
                    continue;
                }
                fullData.Add(key, jsonStr);
                Log.LogInfo($"Wrote {key} mod plugin data to the saved data ({size} bytes).");
            }
            fullData.Add(DATA_TO_RESTORE_KEY, s_OriginalProperty);
            jsonStr = JsonSerializer.Serialize(fullData);
            s_tempOriginalPropertyData = s_OriginalProperty;
            s_OriginalProperty = jsonStr;
            Log.LogInfo("Finished storing mod plugin data in save.");
        }

        public static Dictionary<string, object> LoadOrGetSaveGameData(BasePlugin plugin)
        {
            var metadata = MetadataHelper.GetMetadata(plugin);
            var key = $"{metadata.Name}";
            return queuedDataDictionary.GetOrAdd(key, _ => new());
        }
    }
}