using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UniverseLib;
using SpyraxiHelpers;

namespace DeTESTive
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    public class Plugin : BasePlugin
    {
        internal static ManualLogSource Logger;
        internal static Harmony Harmony;

        public override void Load()
        {
            Logger = Log;

            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            Harmony.PatchAll();

            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");

            Hooks.OnApplicationStarted.AddListener(() => TestRunner.ApplicationStarted = true);
            Hooks.OnMainMenuStart.AddListener(() => TestRunner.MainMenuLoaded = true);
        }

        public override bool Unload()
        {
            Harmony?.UnpatchSelf();

            return base.Unload();
        }
    }
}
