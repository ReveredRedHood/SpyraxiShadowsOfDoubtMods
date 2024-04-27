using System;
using System.Collections;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using SOD.Common;
using SOD.Common.Extensions;
using SOD.Common.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TestHelper {
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    [BepInDependency(SOD.Common.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    // [BepInDependency("PresetEdit", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BasePlugin {
        internal static Harmony Harmony;

        public override void Load() {
            // Plugin startup logic
            LogUtils.Load(Log);
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Harmony.PatchAll();
            // Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");

            // Lib.PluginDetection.OnAllPluginsFinishedLoading += DetectPlugins;
            // Lib.PluginDetection.OnAllPluginsFinishedLoading += DetectPluginsFails;
            UniverseLib.RuntimeHelper.StartCoroutine(SkipLoadCoroutine());
        }

        private void DetectPlugins(object sender, EventArgs e) {
            var guid = Lib.PluginDetection.GetPluginGuidFromPartialGuid("Dialog");
            if (guid == null) {
                return;
            }

            Log.LogInfo(Lib.PluginDetection.IsPluginLoaded(guid));
            Log.LogInfo(Lib.PluginDetection.AllPluginsFinishedLoading);

            BepInPlugin metadata = Lib.PluginDetection.GetPluginInfo(guid).Metadata;
            Log.LogInfo(metadata.GUID);
            Log.LogInfo(metadata.Name);
            Log.LogInfo(metadata.Version);

            var value = Lib.PluginDetection.GetPluginConfigEntryValue<bool>(guid, "Talk to Partner", "Can asking for the partner fail?");
            Log.LogInfo(value);

            // To respond to in-game changes in plugin config
            Lib.PluginDetection.AddPluginConfigEntryChangedListener(guid, DialogAdditionsConfigSettingChanged);
        }

        private void DetectPluginsFails(object sender, EventArgs e) {
            var guid = Lib.PluginDetection.GetPluginGuidFromPartialGuid("Dialog");
            if (guid == null) {
                return;
            }

            var value = Lib.PluginDetection.GetPluginConfigEntryValue<bool>("lol", "Talk to Partner", "Can asking for the partner fail?");
            Log.LogInfo(value);
        }

        private void DialogAdditionsConfigSettingChanged(SettingChangedEventArgs args) {
            Log.LogInfo(args.ChangedSetting.Definition.Section);
            Log.LogInfo(args.ChangedSetting.Definition.Key);
            Log.LogInfo(args.ChangedSetting.Description.Description);
            if (args.ChangedSetting.Definition.Key == "Example") {
                var value = (float)args.ChangedSetting.BoxedValue;
                // ...
            }
        }

        private void OnButton(object sender, InputDetectionEventArgs e) {
            if (e.ActionName == "LeanLeft" && e.IsDown) {
                // Lib.PlayerStatus.SetIllegalStatusModifier("on demand", true, 5.0f);
                Log.LogDebug("Hello");
                Player.Instance.illegalActionActive = true;
                Player.Instance.illegalActionTimer = float.MaxValue;
            }
            // if (e.Key == InteractablePreset.InteractionKey.LeanRight && e.IsDown) {
            // Lib.PlayerStatus.SetIllegalStatusModifier("on demand but doesn't overwrite", false, 5.0f);
            // }
            // if (e.Key == InteractablePreset.InteractionKey.flashlight) {
            // Lib.PlayerStatus.ToggleIllegalStatusModifier("toggled");
            // }
        }

        // Skip the intro and just load into a game
        // Run it from Plugin.Load() with: UniverseLib.RuntimeHelper.StartCoroutine(SkipLoadCoroutine());
        private IEnumerator SkipLoadCoroutine() {
            while (ControlDetectController.Instance == null || !ControlDetectController.Instance.enabled) {
                yield return new WaitForEndOfFrame();
            }
            ControlDetectController.Instance.loadSceneTriggered = true;
            yield return new WaitForEndOfFrame();
            while (SceneManager.GetActiveScene().name != "Main") {
                yield return new WaitForEndOfFrame();
            }
            while (!MainMenuController.Instance.mainMenuActive) {
                yield return new WaitForEndOfFrame();
            }
            // Ready to load the game
            MainMenuController.Instance.SetMenuComponent(MainMenuController.Component.loadGame);

            yield return new WaitForEndOfFrame();
            MainMenuController.Instance.RefreshSaveEntries();

            yield return new WaitForEndOfFrame();
            var savegames = MainMenuController.Instance.spawnedLoadGames.ToList();
            if (savegames.Count == 0) {
                Log.LogWarning("You don't have any savegames.");
                yield break;
            }
            MainMenuController.Instance.SelectNewSave(savegames.First());

            yield return new WaitForEndOfFrame();
            MainMenuController.Instance.LoadGame();

            yield return new WaitForEndOfFrame();
            MainMenuController.Instance.SetMenuComponent(MainMenuController.Component.loadingCity);
        }

        internal static void AddExceptMsg(System.Action action, string addMsgOnException) {
            try {
                action();
            }
            catch (System.Exception except) {
                throw new System.InvalidOperationException(innerException: except, message: addMsgOnException);
            }
        }

        public override bool Unload() {
            // Harmony?.UnpatchSelf();

            LogUtils.Unload();
            return base.Unload();
        }
    }
}