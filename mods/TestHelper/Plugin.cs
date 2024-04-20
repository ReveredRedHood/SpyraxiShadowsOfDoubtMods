using System.Collections;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PresetEdit;
using SOD.Common.Extensions;
using SOD.Common.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TestHelper {
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

            // SOD.Common.Lib.SaveGame.OnAfterLoad += OnAfterLoad;
            UniverseLib.RuntimeHelper.StartCoroutine(SkipLoadCoroutine());
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

            OnAfterLoad(this, null);
        }

        internal static void AddExceptMsg(System.Action action, string addMsgOnException) {
            try {
                action();
            }
            catch (System.Exception except) {
                throw new System.InvalidOperationException(innerException: except, message: addMsgOnException);
            }
        }

        private void OnAfterLoad(object sender, SaveGameArgs e) {
            // Log.LogInfo("Exporting...");
            // var pluginsDir = SOD.Common.Lib.SaveGame.GetSavestoreDirectoryPath(this.GetType().Assembly);
            // Directory.CreateDirectory(pluginsDir);
            // var menuInstances = Helpers.GetPresetInstances(typeof(MenuPreset)).TryCastAll<MenuPreset>();
            // using (var stream = File.Create(Path.Join(pluginsDir, "menues.json"))) {
            //     PresetEdit.PresetSerializer.WriteJsonToFileStream(stream, menuInstances, null);
            // }
        }

        public override bool Unload() {
            // Harmony?.UnpatchSelf();

            LogUtils.Unload();
            return base.Unload();
        }
    }
}