using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace PrintBugfix {
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    public class Plugin : BasePlugin {
        internal static Harmony Harmony;
        internal static ManualLogSource Logger;
        public const string PLUGIN_GUID = MyPluginInfo.PLUGIN_GUID;

        public static List<string> AffectedPresetNames => [
            "PrintedSurveillance",
            "PrintedEmployeeRecord",
            "PrintedVMail",
            "PrintedCitizenFile",
            "PrintedResidentsFile",
            "PrintedReceipt",
        ];

        public override void Load() {
            Logger = Log;

            // Plugin startup logic
            Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            Harmony.PatchAll();
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");
        }

        public override bool Unload() {
            Harmony?.UnpatchSelf();

            return base.Unload();
        }
    }
}