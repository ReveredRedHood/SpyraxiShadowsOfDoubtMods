namespace PresetEdit {
    using BepInEx;
    using SOD.Common.BepInEx;

    /// <summary>
    /// PresetEdit BepInEx BE plugin.
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    [BepInDependency(SOD.Common.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : PluginController<Plugin> {

        public override void Load() {
            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            Harmony.PatchAll();
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");
        }


        public override bool Unload() {
            Harmony?.UnpatchSelf();

            return base.Unload();
        }
    }
}
