using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using FluentAssertions;
using UniverseLib;

namespace {{ tests_namespace }}
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    // [BepInDependency(SOD.Common.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("{{ plugin_name }}", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : PluginController<Plugin, IConfigBindings>
    {
        internal static Harmony Harmony;
        internal RateLimitedAction<string> ThrottledLog;

        public override void Load()
        {
            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            ThrottledLog = Throttler.Throttle<string>(str => Log.LogInfo(str), TimeSpan.FromSeconds(0.5));

            // Harmony.PatchAll();
            // Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");
        }

        public override bool Unload()
        {
            // Harmony?.UnpatchSelf();

            return base.Unload();
        }
    }
}
