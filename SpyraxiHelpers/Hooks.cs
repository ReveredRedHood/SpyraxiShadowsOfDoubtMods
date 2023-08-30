using System.Linq;
using System.Reflection;
using HarmonyLib;

// TODO
namespace SpyraxiHelpers
{
    public static class Hooks
    {
        private static bool s_isFirstLoadingTip = true;

        public static ManagedEvent OnGameTransitionToLoading { get; } = new();
        public static ManagedEvent OnGameStart { get; } = new();
        public static ManagedEvent OnMainMenuStart { get; } = new();
        public static ManagedEvent OnApplicationStarted { get; } = new();
        public static ManagedEvent OnAddAwarenessIcon { get; } = new();
        public static ManagedEvent OnSetAwarenessOutlineActive { get; } = new();
        public static ManagedEvent OnPreSave = new();
        public static ManagedEvent OnPostSave = new();
        public static ManagedEvent OnMurderDetected = new();
        public static ManagedEvent OnPlayerKnockedOut = new();
        public static ManagedEvent<bool> OnGamePauseChange = new();
        public static ManagedEvent<string> OnActionPressed = new();
        public static ManagedEvent<Interactable> OnInteractableSpawnOrRespawn = new();
        private const float SAVE_WAIT_TIME = 3.0f;

#pragma warning disable IDE1006

        [HarmonyPatch(typeof(ControlDetectController), nameof(ControlDetectController.Start))]
        internal static class ControlDetectController_Start
        {
            internal static bool Prefix()
            {
                return !OnApplicationStarted.ShouldSkip();
            }
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

            internal static bool Prefix()
            {
                return !OnMainMenuStart.ShouldSkip();
            }
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

        [HarmonyPatch(typeof(MurderController), nameof(MurderController.OnStartGame))]
        internal static class MurderController_OnStartGame
        {
            internal static bool Prefix()
            {
                return !OnGameStart.ShouldSkip();
            }
            internal static void Postfix()
            {
                OnGameStart.Invoke();
                s_isFirstLoadingTip = true;
            }
        }

        [HarmonyPatch(typeof(MainMenuController), nameof(MainMenuController.LoadTip))]
        internal static class MainMenuController_LoadTip
        {
            internal static bool Prefix()
            {
                return !OnGameTransitionToLoading.ShouldSkip();
            }
            internal static void Postfix()
            {
                if (s_isFirstLoadingTip)
                {
                    s_isFirstLoadingTip = false;
                    OnGameTransitionToLoading.Invoke();
                }
            }
        }
        
        [HarmonyPatch(typeof(InterfaceController), nameof(InterfaceController.AddAwarenessIcon))]
        internal static class InterfaceController_AddAwarenessIcon
        {
            internal static bool Prefix()
            {
                return !OnAddAwarenessIcon.ShouldSkip();
            }
            internal static void Postfix()
            {
                OnAddAwarenessIcon.Invoke();
            }
        }

        [HarmonyPatch(typeof(OutlineController), nameof(OutlineController.SetOutlineActive))]
        internal static class OutlineController_SetOutlineActive
        {
            internal static bool Prefix()
            {
                return !OnSetAwarenessOutlineActive.ShouldSkip();
            }
            internal static void Postfix()
            {
                OnSetAwarenessOutlineActive.Invoke();
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
            internal static bool Prefix()
            {
                return !OnGamePauseChange.ShouldSkip(true);
            }
            internal static void Postfix()
            {
                OnGamePauseChange.Invoke(true);
            }
        }

        [HarmonyPatch(typeof(SessionData), nameof(SessionData.ResumeGame))]
        internal static class SessionData_ResumeGame
        {
            internal static bool Prefix()
            {
                return !OnGamePauseChange.ShouldSkip(false);
            }
            internal static void Postfix()
            {
                OnGamePauseChange.Invoke(false);
            }
        }

        [HarmonyPatch(typeof(MainMenuController), nameof(MainMenuController.StartSave))]
        internal static class MainMenuController_StartSave
        {
            internal static void Prefix()
            {
                OnPreSave.Invoke();
            }
            internal static void Postfix()
            {
                // RuntimeHelper.StartCoroutine();
                // new WaitInstruction(
                //     SAVE_WAIT_TIME,
                //     new Action(() => OnPostSave.Invoke()),
                //     0
                // );
                OnPostSave.Invoke();
            }
        }

        [HarmonyPatch(typeof(Interactable), nameof(Interactable.OnSpawn))]
        internal static class Interactable_OnSpawn
        {
            internal static void Postfix(Il2CppSystem.Object __instance)
            {
                var castResult = __instance.TryCast<Interactable>();
                if (castResult.preset != null)
                {
                    OnInteractableSpawnOrRespawn.Invoke(castResult);
                }
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
                if (actionName != GameConstants.ButtonActions.QUICKSAVE || !__result)
                {
                    return;
                }

                OnPreSave.Invoke();
                // _ = new WaitInstruction(
                //     SAVE_WAIT_TIME,
                //     new Action(() => OnPostSave.Invoke()),
                //     0);
            }
        }
    }
}
