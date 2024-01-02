using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using SOD.Common;
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

            Lib.SaveGame.OnAfterLoad += OnAfterLoad;
            // Harmony.PatchAll();
            // Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");
        }

        private void OnAfterLoad(object sender, SaveGameArgs e) {
            Toolbox.Instance.allArt.ToList().QuickLog();
        }

        public override bool Unload() {
            // Harmony?.UnpatchSelf();

            LogUtils.Unload(Log);
            return base.Unload();
        }
    }
}