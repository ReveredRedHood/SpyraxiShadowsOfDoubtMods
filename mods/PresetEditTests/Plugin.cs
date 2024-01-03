using System;
using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using SOD.Common.Extensions;
using SOD.Common.Helpers;

namespace PresetEditTests {
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    // [BepInDependency(SOD.Common.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("PresetEdit", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BasePlugin {
        internal static Harmony Harmony;

        public override void Load() {
            // Plugin startup logic
            LogUtils.Load(Log);
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Harmony.PatchAll();
            // Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");

            SOD.Common.Lib.SaveGame.OnAfterLoad += OnAfterLoad;
        }

        private void OnAfterLoad(object sender, SaveGameArgs e) {
            Toolbox.Instance.allArt.ToList().QuickLog();
            Log.LogInfo("Exporting...");
            PresetEdit.Serializer.ExportSaveGamePresetData();
            Log.LogInfo("Apply for InteractablePreset.json");
            PresetEdit.Serializer.TryApplyOverwritesFromJson(
                Path.Combine(
                    Path.GetDirectoryName(this.GetType().Assembly.Location),
                    "InteractablePreset.json"
                )
            );
            Log.LogInfo("Apply for MenuPreset.json");
            PresetEdit.Serializer.TryApplyOverwritesFromJson(
                Path.Combine(
                    Path.GetDirectoryName(this.GetType().Assembly.Location),
                    "MenuPreset.json"
                )
            );
        }

        public override bool Unload() {
            // Harmony?.UnpatchSelf();

            LogUtils.Unload();
            return base.Unload();
        }
    }
}