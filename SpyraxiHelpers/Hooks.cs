using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UniverseLib;

namespace SpyraxiHelpers
{
    public static class Hooks
    {
        // internal static bool isFirstLoadingTip = true;
        internal static string loadGameFilePath;
        internal static string saveGameFilePath;
        public static ManagedEvent OnApplicationStarted { get; } = new();
        public static ManagedEvent OnMainMenuStart { get; } = new();
        public static ManagedEvent OnMurderDetected { get; } = new();
        public static ManagedEvent OnPlayerKnockedOut { get; } = new();
        public static ManagedEvent<bool> OnGamePauseChange { get; } = new();
        public static ManagedEvent<string> OnActionPressed { get; } = new();
        public static ManagedEvent<string> OnGameStart { get; } = new();
        public static ManagedEvent<string> OnPreLoad { get; } = new();
        public static ManagedEvent<string> OnPostLoad { get; } = new();
        public static ManagedEvent<string> OnPreSave { get; } = new();
        public static ManagedEvent<string> OnPostSave { get; } = new();

#pragma warning disable IDE1006

        [HarmonyPatch(typeof(ControlDetectController), nameof(ControlDetectController.Start))]
        internal static class ControlDetectControllerStart
        {
            internal static void Postfix()
            {
                Plugin.Logger.LogInfo("Application start detected");
                OnApplicationStarted.Invoke();
            }
        }

        [HarmonyPatch(typeof(MainMenuController), nameof(MainMenuController.Start))]
        internal static class MainMenuControllerStart
        {
            static bool hasInit = false;

            internal static void Postfix()
            {
                if (hasInit)
                    return;
                hasInit = true;

                // Turn off bug reporting so the devs don't get extraneous bug
                // reports
                Plugin.Logger.LogInfo("Turning off bug reporting");
                Game.Instance.enableBugReporting = false;

                OnMainMenuStart.Invoke();
            }
        }

        [HarmonyPatch(typeof(CityConstructor), nameof(CityConstructor.StartGame))]
        internal static class CityConstructorStartGame
        {
            internal static void Postfix()
            {
                OnGameStart.Invoke(loadGameFilePath);
                // isFirstLoadingTip = true;
            }
        }

        [HarmonyPatch(typeof(SaveStateController), nameof(SaveStateController.LoadSaveState))]
        internal static class SaveStateControllerLoadSaveState
        {
            internal static void Prefix(StateSaveData load)
            {
                loadGameFilePath = RestartSafeController.Instance.saveStateFileInfo.FullName;
                OnPreLoad.Invoke(loadGameFilePath);
            }
            internal static void Postfix(StateSaveData load)
            {
                OnPostLoad.Invoke(loadGameFilePath);
            }
        }

        [HarmonyPatch(typeof(MurderController), nameof(MurderController.OnVictimDiscovery))]
        internal static class MurderControllerOnVictimDiscovery
        {
            internal static void Postfix()
            {
                OnMurderDetected.Invoke();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TriggerPlayerKO))]
        internal static class PlayerTriggerPlayerKO
        {
            internal static void Postfix()
            {
                OnPlayerKnockedOut.Invoke();
            }
        }

        [HarmonyPatch(typeof(SessionData), nameof(SessionData.PauseGame))]
        internal static class SessionDataPauseGame
        {
            internal static void Postfix()
            {
                OnGamePauseChange.Invoke(true);
            }
        }

        [HarmonyPatch(typeof(SessionData), nameof(SessionData.ResumeGame))]
        internal static class SessionDataResumeGame
        {
            internal static void Postfix()
            {
                OnGamePauseChange.Invoke(false);
            }
        }

        [HarmonyPatch(typeof(SaveStateController), nameof(SaveStateController.CaptureSaveStateAsync))]
        internal static class SaveStateControllerCaptureSaveStateAsync
        {
            internal static void Prefix(string path)
            {
                saveGameFilePath = path;
                OnPreSave.Invoke(saveGameFilePath);
            }
        }

        [HarmonyPatch(typeof(InterfaceController), nameof(InterfaceController.NewGameMessage))]
        internal static class InterfaceControllerNewGameMessage
        {
            internal static void Postfix(InterfaceController.GameMessageType newType, int newNumerical, string newMessage, InterfaceControls.Icon newIcon, AudioEvent additionalSFX, bool colourOverride, UnityEngine.Color col, int newMergeType, float newMessageDelay, UnityEngine.RectTransform moveToOnDestroy, GameMessageController.PingOnComplete ping, Evidence keyMergeEvidence, Il2CppSystem.Collections.Generic.List<Evidence.DataKey> keyMergeKeys, UnityEngine.Sprite iconOverride)
            {
                if (newType != InterfaceController.GameMessageType.notification)
                {
                    return;
                }
                if (!newMessage.Contains("Game saved"))
                {
                    return;
                }
                OnPostSave.Invoke(saveGameFilePath);
            }
        }

        [HarmonyPatch]
        internal static class RewiredPlayerGetButton
        {
            [HarmonyTargetMethod]
            internal static MethodBase CalculateMethod()
            {
                return typeof(Rewired.InputManager_Base).Assembly.ExportedTypes
                    .Single(t => t.FullName == "Rewired.Player")
                    .GetMethods()
                    .Where(m => m.Name == "GetButtonDown")
                    .Single(m => m.GetParameters().Single().ParameterType == typeof(string));
            }

            internal static void Postfix(string actionName, ref bool __result)
            {
                OnActionPressed.Invoke(actionName);
            }
        }
    }
}
