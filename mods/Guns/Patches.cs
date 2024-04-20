using System;
using HarmonyLib;

namespace Guns;
internal class Patches {
    [HarmonyPatch(typeof(BioScreenController), nameof(BioScreenController.SelectSlot))]
    internal class BioScreenController_SelectSlot {
        [HarmonyPostfix]
        internal static void Postfix() {
            Plugin.Instance.CurrentWeaponItemState = WeaponItemState.NotEquipped;
        }
    }
    [HarmonyPatch(typeof(FirstPersonItemController), nameof(FirstPersonItemController.FinishedDrawingNewItem))]
    internal class FirstPersonItemController_FinishedDrawingNewItem {
        [HarmonyPostfix]
        internal static void Postfix() {
            if (Plugin.Instance.CurrentWeaponItemState != WeaponItemState.NotEquipped) {
                return;
            }
            Plugin.Instance.CurrentWeaponItemState = WeaponItemState.Ready;
            Plugin.Instance.WeaponActionsUpdate();
        }
    }
    [HarmonyPatch(typeof(Citizen), nameof(Citizen.RecieveDamage))]
    internal class Citizen_RecieveDamage {
        [HarmonyPrefix]
        internal static void Prefix(Citizen __instance, Actor fromWho, ref bool alertSurrounding) {
            if (alertSurrounding) {
                return;
            }
            if (!fromWho.isPlayer || __instance.isPlayer) {
                return;
            }
            if (Plugin.Instance.CurrentWeaponInteractionState != WeaponInteractionState.Firing) {
                return;
            }
            if (Helpers.IsPlayerBeingPursuedByActor(__instance)) {
                return;
            }
            if (!Player.Instance.isSeenByOthers) {
                // being heard locally but not seen by anyone is OK, so not
                // checking if the player is heard
                return;
            }
            // Force an alert when a citizen is hit by the player's gunfire
            alertSurrounding = true;
        }
    }
}

