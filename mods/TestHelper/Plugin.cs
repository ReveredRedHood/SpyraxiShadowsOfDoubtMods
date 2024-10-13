using System.Collections;
using System.Linq;
using BepInEx;
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
    public class Plugin : BasePlugin {
        internal static Harmony Harmony;

        public override void Load() {
            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Harmony.PatchAll();
            // Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");

            // Lib.InputDetection.OnButtonStateChanged += OnButtonStateChanged;
            // Lib.InputDetection.OnAxisStateChanged += OnAxisStateChanged;
            // Lib.InputDetection.OnSuppressedButtonStateChanged += OnSuppressedButtonStateChanged;
            // Lib.InputDetection.OnSuppressedAxisStateChanged += OnSuppressedAxisStateChanged;
            Lib.SaveGame.OnAfterLoad += Test;

            UniverseLib.RuntimeHelper.StartCoroutine(SkipLoadCoroutine());
        }

        // private void OnSuppressedAxisStateChanged(object sender, SuppressedAxisInputDetectionEventArgs e) {
        //     if (e.Key != InteractablePreset.InteractionKey.moveVertical && e.Key != InteractablePreset.InteractionKey.moveHorizontal) {
        //         return;
        //     }
        //     Log.LogInfo($"SuppressedAxisStateChanged {e.ActionName} {e.AxisValue}");
        // }

        // private void OnSuppressedButtonStateChanged(object sender, SuppressedInputDetectionEventArgs e) {
        //     if (e.Key != InteractablePreset.InteractionKey.primary && e.Key != InteractablePreset.InteractionKey.secondary) {
        //         return;
        //     }
        //     Log.LogInfo($"SuppressedButtonStateChanged {e.ActionName}");
        // }

        // private void OnAxisStateChanged(object sender, AxisInputDetectionEventArgs e) {
        //     if (e.Key != InteractablePreset.InteractionKey.moveVertical && e.Key != InteractablePreset.InteractionKey.moveHorizontal) {
        //         return;
        //     }
        //     Log.LogInfo($"AxisStateChanged {e.ActionName} {e.AxisValue}");
        // }

        // private void OnButtonStateChanged(object sender, InputDetectionEventArgs e) {
        //     if (e.Key != InteractablePreset.InteractionKey.primary && e.Key != InteractablePreset.InteractionKey.secondary) {
        //         return;
        //     }
        //     Log.LogInfo($"ButtonStateChanged {e.ActionName}");
        // }

        private void Test(object sender, SaveGameArgs e) {
            UniverseLib.RuntimeHelper.StartCoroutine(TestCoroutine());
        }

        private IEnumerator TestCoroutine() {
            yield return new WaitForEndOfFrame();
            // var guid = MyPluginInfo.PLUGIN_GUID;

            // Log.LogInfo(Lib.InputDetection.RewiredPlayer.descriptiveName);

            // Log.LogInfo(Lib.InputDetection.GetBinding(InteractablePreset.InteractionKey.primary));
            // Log.LogInfo(Lib.InputDetection.GetRewiredAction(InteractablePreset.InteractionKey.crouch).name);
            // Log.LogInfo(Lib.InputDetection.GetRewiredActionName(InteractablePreset.InteractionKey.crouch));

            // // Suppressed keycode and suppressed action
            // Lib.InputDetection.SetInputSuppressed(guid, KeyCode.Mouse2);
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.flashlight);
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, KeyCode.Mouse2, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.flashlight, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.flashlight, out _));
            // // Unsuppressed keycode, but suppressed action
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.LeanLeft);
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, KeyCode.Q, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.LeanLeft, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.LeanLeft, out _));
            // // Suppressed keycode, indirectly suppressed action
            // Lib.InputDetection.SetInputSuppressed(guid, KeyCode.E);
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, KeyCode.E, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.LeanRight, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.LeanRight, out _));
            // // Remove suppression
            // Lib.InputDetection.RemoveInputSuppression(InteractablePreset.InteractionKey.flashlight);
            // Lib.InputDetection.RemoveInputSuppression(KeyCode.E);
            // // Check
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, KeyCode.Mouse2, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.flashlight, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.flashlight, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, KeyCode.Q, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.LeanLeft, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.LeanLeft, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, KeyCode.E, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.LeanRight, out _));
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.LeanRight, out _));

            // Lib.InputDetection.RemoveInputSuppression(InteractablePreset.InteractionKey.flashlight);
            // Log.LogInfo(Lib.InputDetection.IsInputSuppressed(guid, KeyCode.Mouse2, out _));

            // Log.LogInfo(Lib.InputDetection.GetBinding(InteractablePreset.InteractionKey.jump));

            // Log.LogInfo("Suppressed sprint for 5 sec");
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.sprint, TimeSpan.FromSeconds(5.0f), true);
            // Log.LogInfo("Suppressed LeanLeft for 5 sec");
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.LeanLeft, TimeSpan.FromSeconds(5.0f), true);
            // Log.LogInfo("Suppressed caseBoard for 10 sec");
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.caseBoard, TimeSpan.FromSeconds(10.0f), true);
            // Log.LogInfo("Suppressed moveHorizontal for 5 sec");
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.moveHorizontal, TimeSpan.FromSeconds(15.0f), true);
            // Log.LogInfo("Just kidding, suppressed moveHorizontal for 15 sec");
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.moveHorizontal, TimeSpan.FromSeconds(15.0f), true);
            // Log.LogInfo("Unsuccessfully suppressing moveHorizontal for 60 sec");
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.moveHorizontal, TimeSpan.FromSeconds(60.0f), false);
            // Log.LogInfo("Suppressed flashlight indefinitely");
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.flashlight);
            // Log.LogInfo("Do I see the LeanRight suppression entry under a different guid?");
            // yield return new WaitForSeconds(1.0f);
            // if (!Lib.InputDetection.IsInputSuppressedByAnyPlugin(InteractablePreset.InteractionKey.LeanRight, out var suppressedBy, out _)) {
            //     Log.LogInfo("I don't see any suppression entry (expected on first run prior to save)");
            //     // Log.LogInfo($"Troubleshoot: {Lib.InputDetection.FindInputSuppressionEntries(_ => true).ToStringEnumerable()}");
            // }
            // else {
            //     Log.LogInfo("I see these entries for LeanRight");
            //     Log.LogInfo(suppressedBy.First());
            //     Log.LogInfo(suppressedBy.Last());
            // }
            // if (!Lib.InputDetection.IsInputSuppressed(guid, InteractablePreset.InteractionKey.LeanRight, out _)) {
            //     Log.LogInfo("I don't see any suppression entry for the actual guid");
            //     // Log.LogInfo($"Troubleshoot: {Lib.InputDetection.FindInputSuppressionEntries(_ => true).ToStringEnumerable()}");
            // }
            // else {
            //     Log.LogInfo("LeanRight is suppressed by this plugin's actual guid");
            // }
            // yield return new WaitForSeconds(5.0f);
            // Log.LogInfo("Suppressed LeanRight under this guid");
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.LeanRight);
            // Log.LogInfo("Suppressed LeanRight indefinitely under a different guid");
            // Lib.InputDetection.SetInputSuppressed("helloworld", InteractablePreset.InteractionKey.LeanRight);
            // // suppress moveHorizontal, secondary
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.moveHorizontal);
            // Lib.InputDetection.SetInputSuppressed(guid, InteractablePreset.InteractionKey.secondary);
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

        public override bool Unload() {
            // Harmony?.UnpatchSelf();

            return base.Unload();
        }
    }
}