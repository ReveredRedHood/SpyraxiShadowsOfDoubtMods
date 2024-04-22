using System;
using HarmonyLib;

namespace Guns;
internal class Patches {
    [HarmonyPatch(typeof(FirstPersonItemController), nameof(FirstPersonItemController.FinishedDrawingNewItem))]
    internal class FirstPersonItemController_FinishedDrawingNewItem {
        [HarmonyPrefix]
        internal static void Prefix() {
            if (Plugin.Instance.IsPlayerHoldingAGun()) {
                // we are about to switch off our weapon
                FirstPersonItem currentFpsItem = Plugin.Instance.CurrentInteractablePresetHeld.fpsItem;
                currentFpsItem.barkTriggerChance = 0.0f;
            }
        }
        [HarmonyPostfix]
        internal static void Postfix() {
            if (!Plugin.Instance.IsPlayerHoldingAGun()) {
                Plugin.Instance.CurrentWeaponSecondaryState = WeaponSecondaryState.NotEquipped;
            }
            else {
                Plugin.Instance.CurrentWeaponSecondaryState = WeaponSecondaryState.Ready;
                FirstPersonItem currentFpsItem = Plugin.Instance.CurrentInteractablePresetHeld.fpsItem;
                currentFpsItem.bark = SpeechController.Bark.threatenByItem;
                currentFpsItem.barkTriggerChance = Plugin.Instance.Config.GunDrawBarkChance;
            }
        }
    }
    [HarmonyPatch(typeof(Citizen), nameof(Citizen.RecieveDamage))]
    internal class Citizen_RecieveDamage {
        [HarmonyPrefix]
        internal static void Prefix(Citizen __instance, Actor fromWho, ref bool alertSurrounding) {
            if (alertSurrounding) {
                return;
            }
            if ((fromWho != null && !fromWho.isPlayer) || __instance.isPlayer) {
                return;
            }
            if (Plugin.Instance.CurrentWeaponPrimaryState != WeaponPrimaryState.Firing) {
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

