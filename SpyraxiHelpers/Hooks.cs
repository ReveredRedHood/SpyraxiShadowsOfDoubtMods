using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UniverseLib;

// TODO
namespace SpyraxiHelpers
{
    public static class Hooks
    {
        internal static bool s_isFirstLoadingTip = true;
        internal static string s_loadGameFilePath;
        internal static string s_saveGameFilePath;
        public static ManagedEvent OnApplicationStarted { get; } = new();
        public static ManagedEvent OnMainMenuStart { get; } = new();
        public static ManagedEvent OnMurderDetected = new();
        public static ManagedEvent OnPlayerKnockedOut = new();
        public static ManagedEvent<bool> OnGamePauseChange = new();
        public static ManagedEvent<string> OnActionPressed = new();
        public static ManagedEvent<string> OnGameStart { get; } = new();
        public static ManagedEvent<string> OnPreLoad { get; } = new();
        public static ManagedEvent<string> OnPostLoad { get; } = new();
        public static ManagedEvent<string> OnPreSave = new();
        public static ManagedEvent<string> OnPostSave = new();

#pragma warning disable IDE1006

        [HarmonyPatch(typeof(ControlDetectController), nameof(ControlDetectController.Start))]
        internal static class ControlDetectController_Start
        {
            internal static void Postfix()
            {
                Plugin.Logger.LogInfo("Application start detected");
                OnApplicationStarted.Invoke();
            }
        }

        [HarmonyPatch(typeof(MainMenuController), nameof(MainMenuController.Start))]
        internal static class MainMenuController_Start
        {
            static bool s_hasInit = false;

            internal static void Postfix()
            {
                if (s_hasInit)
                    return;
                s_hasInit = true;

                // Turn off bug reporting so the devs don't get extraneous bug
                // reports
                Plugin.Logger.LogInfo("Turning off bug reporting");
                Game.Instance.enableBugReporting = false;

                OnMainMenuStart.Invoke();
            }
        }

        [HarmonyPatch(typeof(CityConstructor), nameof(CityConstructor.StartGame))]
        internal static class CityConstructor_StartGame
        {
            internal static void Postfix()
            {
                OnGameStart.Invoke(s_loadGameFilePath);
                s_isFirstLoadingTip = true;
            }
        }

        [HarmonyPatch(typeof(SaveStateController), nameof(SaveStateController.LoadSaveState))]
        internal static class SaveStateController_LoadSaveState
        {
            internal static void Prefix(StateSaveData load)
            {
                s_loadGameFilePath = RestartSafeController.Instance.saveStateFileInfo.FullName;
                OnPreLoad.Invoke(s_loadGameFilePath);
            }
            internal static void Postfix(StateSaveData load)
            {
                OnPostLoad.Invoke(s_loadGameFilePath);
            }
        }

        [HarmonyPatch(typeof(MurderController), nameof(MurderController.OnVictimDiscovery))]
        internal static class MurderController_OnVictimDiscovery
        {
            internal static void Postfix()
            {
                OnMurderDetected.Invoke();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TriggerPlayerKO))]
        internal static class Player_TriggerPlayerKO
        {
            internal static void Postfix()
            {
                OnPlayerKnockedOut.Invoke();
            }
        }

        [HarmonyPatch(typeof(SessionData), nameof(SessionData.PauseGame))]
        internal static class SessionData_PauseGame
        {
            internal static void Postfix()
            {
                OnGamePauseChange.Invoke(true);
            }
        }

        [HarmonyPatch(typeof(SessionData), nameof(SessionData.ResumeGame))]
        internal static class SessionData_ResumeGame
        {
            internal static void Postfix()
            {
                OnGamePauseChange.Invoke(false);
            }
        }

        [HarmonyPatch(typeof(SaveStateController), nameof(SaveStateController.CaptureSaveStateAsync))]
        internal static class SaveStateController_CaptureSaveStateAsync
        {
            internal static void Prefix(string path)
            {
                s_saveGameFilePath = path;
                OnPreSave.Invoke(s_saveGameFilePath);
            }
        }

        [HarmonyPatch(typeof(InterfaceController), nameof(InterfaceController.NewGameMessage))]
        internal static class InterfaceController_NewGameMessage
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
                OnPostSave.Invoke(s_saveGameFilePath);
            }
        }

        [HarmonyPatch]
        internal static class Rewired_Player_GetButton
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

                // if (actionName != GameConstants.ButtonActions.QUICKSAVE || !__result)
                // {
                //     return;
                // }

                // OnPreSave.Invoke($"{Helpers.SAVE_FILES_PATH}/Quick Save.sodb");
            }
        }
    }
}
