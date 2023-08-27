using HarmonyLib;

namespace DeTESTive
{
    public static class Hooks
    {
        internal static ManagedEvent OnApplicationStarted = new();
        internal static ManagedEvent OnMainMenuStart = new();
        internal static ManagedEvent OnGameStart = new();
        internal static ManagedEvent OnGameTransitionToLoading = new();
        private static bool s_isFirstLoadingTip = true;

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

        [HarmonyPatch(typeof(MurderController), nameof(MurderController.OnStartGame))]
        internal static class MurderController_OnStartGame
        {
            internal static void Postfix()
            {
                OnGameStart.Invoke();
                s_isFirstLoadingTip = true;
            }
        }

        [HarmonyPatch(typeof(MainMenuController), nameof(MainMenuController.LoadGame))]
        internal static class MainMenuController_LoadGame
        {
            internal static void Postfix()
            {
                if (s_isFirstLoadingTip)
                {
                    s_isFirstLoadingTip = false;
                    OnGameTransitionToLoading.Invoke();
                }
            }
        }
    }
}
