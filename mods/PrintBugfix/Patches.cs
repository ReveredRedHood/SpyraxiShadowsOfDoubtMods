using HarmonyLib;

namespace PrintBugfix {
    internal static class Patches {
        internal static bool IsBuggedVec3;

        [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.FindClosestValidNodeToWorldPosition))]
        internal static class Toolbox_FindClosestValidNodeToWorldPosition {

            [HarmonyPostfix]
            internal static void Postfix(
                    int safety,
                    ref NewNode __result) {
                if (safety != 2000 || __result != null) {
                    return;
                }
                Plugin.Logger.LogDebug($"Detected bugged print...");
                IsBuggedVec3 = true;
            }
        }

        [HarmonyPatch(typeof(Interactable), nameof(Interactable.SafeDelete))]
        internal static class Interactable_SafeDelete {
            internal static bool Prefix(Interactable __instance) {
                if (IsBuggedVec3) {
                    IsBuggedVec3 = false;
                    if (Plugin.affectedPresetNames.Contains(__instance.preset.presetName)) {
                        Plugin.Logger.LogDebug($"Squashing bug... skipping deletion for {__instance.GetName()}");
                        return false;
                    }
                }
                return true;
            }
        }
    }
}