using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using DeTESTive;
using FluentAssertions;
using UniverseLib;

namespace SpyraxiHelpersTests
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    [BepInDependency("DeTESTive", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("SpyraxiHelpers", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BasePlugin
    {
        internal static ManualLogSource Logger;

        public override void Load()
        {
            Logger = Log;

            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Tests
            TestRunner.AddTest(TestEx.Test, MyPluginInfo.PLUGIN_NAME);

            TestRunner.RunTests(false);
        }
    }
}