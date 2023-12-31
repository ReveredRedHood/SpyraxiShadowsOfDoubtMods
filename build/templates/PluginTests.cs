using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace {{ tests_namespace }}
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    // [BepInDependency(SOD.Common.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("{{ plugin_name }}", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BasePlugin
    {
        internal static Harmony Harmony;

        public override void Load()
        {
            // Plugin startup logic
            LogUtils.Load();
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Harmony.PatchAll();
            // Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");
        }

        public override bool Unload()
        {
            // Harmony?.UnpatchSelf();

            LogUtils.Unload();
            return base.Unload();
        }
    }
}
