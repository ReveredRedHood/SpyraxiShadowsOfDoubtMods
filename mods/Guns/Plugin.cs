using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using SOD.Common;
using SOD.Common.BepInEx;
using SOD.Common.Extensions;
using SOD.Common.Helpers;
using UnityEngine;
using UniverseLib;

namespace Guns;

/// <summary>
/// Guns BepInEx BE plugin.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Shadows of Doubt.exe")]
[BepInDependency(SOD.Common.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : PluginController<Plugin, IConfigBindings> {
    // TODO: I really don't know how accurate the descriptions for these recoil
    // patterns are
    public static Vector3 DiagonalRecoilPattern = new Vector3(1.0f, 0.5f, 1.0f).normalized;
    public static Vector3 VerticalRecoilPattern = new Vector3(0.2f, 1.0f, 0.2f).normalized;
    public static Vector3 ShortOvalRecoilPattern = new Vector3(1.0f, 1.0f, 0.5f).normalized;
    public static Vector3 CircularRecoilPattern = Vector3.one.normalized;
    private const string ACTION_DDS_DICTIONARY_NAME = "ui.interaction";
    private const string ACTION_NAME_AIM = "Aim";
    private const string ACTION_NAME_FIRE = "Fire";
    private const string ACTION_NAME_THROW = "Throw";
    private const string ACTION_NAME_ATTACK = "Attack";
    private const string PRIMARY = "Primary";
    private const string SECONDARY = "Secondary";
    private const float DEFAULT_RECOIL_SPEED = 1.0f;
    private const float EJECT_POINT_PROJECTION_FACTOR = 6.0f;
    private const int SHOTGUN_PROJECTILES_PER_SHELL = 5;
    private const string SHOTGUN_PRESET_NAME = "Shotgun";
    private const string LACK_AMMO_MESSAGE_START = "You lack the ammo needed to fire this weapon: ";

    /* For those who want to use this as a modding resource, you can add
     * additional weapons that shoot or edit the info for existing ones in
     * Guns.Plugin.GunInfoEntries. If you do this while in-game (new game or
     * loaded save), then call Guns.Plugin.InGameSetup() afterwards.
    */
    public List<GunInfoEntry> GunInfoEntries = new List<GunInfoEntry> {
        new(itemPresetName: "BattleRifle",
            rotation: new Vector3(0f, 0f, 0f),
            delayBetweenShots: 0.25f,
            isFullAuto: true,
            recoilPatternFactors: DiagonalRecoilPattern,
            recoilAmplitude: 2.0f,
            zoomInOnAimPct: 0.2f),
        new(itemPresetName: "Revolver",
            rotation: new Vector3(0f, -90f, 0f),
            delayBetweenShots: 1.0f,
            isFullAuto: false,
            recoilPatternFactors: ShortOvalRecoilPattern,
            recoilAmplitude: 15.0f,
            zoomInOnAimPct: 0.2f),
        new(itemPresetName: "SemiAutomaticPistol",
            rotation: new Vector3(0f, -90f, 0f),
            delayBetweenShots: 1.0f,
            isFullAuto: false,
            recoilPatternFactors: ShortOvalRecoilPattern,
            recoilAmplitude: 1.0f,
            zoomInOnAimPct: 0.2f),
        new(itemPresetName: "SemiAutomaticPistolSilenced",
            rotation: new Vector3(0f, -90f, 0f),
            delayBetweenShots: 1.0f,
            isFullAuto: false,
            recoilPatternFactors: ShortOvalRecoilPattern,
            recoilAmplitude: 1.0f,
            zoomInOnAimPct: 0.2f),
        new(itemPresetName: "Shotgun",
            rotation: new Vector3(0f, 0f, 0f),
            delayBetweenShots: 1.0f,
            isFullAuto: false,
            recoilPatternFactors: ShortOvalRecoilPattern,
            recoilAmplitude: 20.0f,
            zoomInOnAimPct: 0.1f),
        new(itemPresetName: "SniperRifle",
            rotation: new Vector3(270f, 0f, 0f),
            delayBetweenShots: 2.0f,
            isFullAuto: false,
            recoilPatternFactors: CircularRecoilPattern,
            recoilAmplitude: 30.0f,
            zoomInOnAimPct: 0.6f),
    };
    public Dictionary<string, GunInfoEntry> GunEntriesByPresetName = new();
    public HashSet<int> WeaponsOnCooldown { get; internal set; } = new();
    private WeaponPrimaryState currentWeaponPrimaryState;
    public WeaponPrimaryState CurrentWeaponPrimaryState {
        get => currentWeaponPrimaryState;
        internal set {
            currentWeaponPrimaryState = value;
            switch (currentWeaponPrimaryState) {
                case WeaponPrimaryState.Ready:
                case WeaponPrimaryState.Firing:
                    SetPrimaryActionFire(true);
                    break;
                default:
                    SetPrimaryActionFire(false);
                    break;
            }
        }
    }
    private WeaponSecondaryState currentWeaponSecondaryState;
    public WeaponSecondaryState CurrentWeaponSecondaryState {
        get => currentWeaponSecondaryState;
        internal set {
            currentWeaponSecondaryState = value;
            switch (currentWeaponSecondaryState) {
                case WeaponSecondaryState.Ready:
                case WeaponSecondaryState.Aiming:
                case WeaponSecondaryState.Busy:
                    SetSecondaryActionAim(true);
                    break;
                default:
                    SetSecondaryActionAim(false);
                    break;
            }
        }
    }
    private bool IsFireHeldDown { get; set; }
    private bool IsGamePaused { get; set; } = false;
    private bool InGameSetupCompleted { get; set; } = false;
    private Coroutine CameraFovTweenCoroutine { get; set; }

    internal InteractablePreset CurrentInteractablePresetHeld => BioScreenController.Instance.selectedSlot?.GetInteractable()?.preset;
    internal FirstPersonItem.FPSInteractionAction AttackFpsAction { get; private set; }
    internal FirstPersonItem.FPSInteractionAction ThrowFpsAction { get; private set; }


    public override void Load() {
        // Plugin startup logic
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Harmony.PatchAll();
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");

        Lib.SaveGame.OnAfterLoad += OnAfterLoad;
        Lib.SaveGame.OnAfterNewGame += OnAfterNewGame;
        Lib.Time.OnGamePaused += OnGamePaused;
        Lib.Time.OnGameResumed += OnGameResumed;
        Lib.InputDetection.OnButtonStateChanged += OnButtonStateChanged;

        ConfigFile.SettingChanged += ProcessSettingChange;
    }

    public override bool Unload() {
        Harmony?.UnpatchSelf();

        Lib.SaveGame.OnAfterLoad -= OnAfterLoad;
        Lib.SaveGame.OnAfterNewGame -= OnAfterNewGame;
        Lib.Time.OnGamePaused -= OnGamePaused;
        Lib.Time.OnGameResumed -= OnGameResumed;
        Lib.InputDetection.OnButtonStateChanged -= OnButtonStateChanged;

        ConfigFile.SettingChanged -= ProcessSettingChange;

        return base.Unload();
    }

    private void ProcessSettingChange(object sender, SettingChangedEventArgs e) {
        if (!InGameSetupCompleted) {
            return;
        }
        if (Config.GunTestingMode) {
            FirstPersonItemController.Instance.SetSlotSize(12);
            GunEntriesByPresetName.Values.Select(x => x.InteractablePreset).ForEach(x => x.SpawnIntoInventory());
            GunEntriesByPresetName.Values.Select(x => x.InteractablePreset.weapon.ammunition[0]).ForEach(x => x.SpawnIntoInventory());
        }
    }

    private void OnAfterNewGame(object sender, EventArgs e) {
        InGameSetup();
    }

    private void OnAfterLoad(object sender, SaveGameArgs e) {
        InGameSetup();
    }

    public void InGameSetup() {
        // Reset state in case we are loading a game after already being
        // in-game
        currentWeaponPrimaryState = WeaponPrimaryState.NotEquipped;
        currentWeaponSecondaryState = WeaponSecondaryState.NotEquipped;
        GunEntriesByPresetName.Clear();
        IsFireHeldDown = false;
        WeaponsOnCooldown.Clear();

        // Add DDS strings for Aim and Fire interactionNames
        Lib.DdsStrings.AddOrUpdateEntries(ACTION_DDS_DICTIONARY_NAME, (ACTION_NAME_AIM, ACTION_NAME_AIM), (ACTION_NAME_FIRE, ACTION_NAME_FIRE));

        // Assign interactable presets to corresponding gun info entries and
        // add the entries to a dictionary for quick lookup using
        // CurrentInteractablePresetHeld.presetName as the key
        var interactablePresets = Helpers.GetPresetInstances<InteractablePreset>();
        for (int i = 0; i < GunInfoEntries.Count; i++) {
            var entry = GunInfoEntries[i];
            entry.InteractablePreset = interactablePresets.First(x => x.presetName == entry.ItemPresetName);
            GunEntriesByPresetName.Add(entry.ItemPresetName, entry);
        }

        foreach (var (key, gunEntry) in GunEntriesByPresetName) {
            // Set the fps item offset and rotation so the guns are aiming
            // forward
            gunEntry.InteractablePreset.fpsItemOffset = Vector3.zero;
            gunEntry.InteractablePreset.fpsItemRotation = gunEntry.Rotation;
        }

        InGameSetupCompleted = true;
    }

    private void OnButtonStateChanged(object sender, InputDetectionEventArgs eventArgs) {
        if (!InGameSetupCompleted) {
            return;
        }

        if (eventArgs.ActionName != PRIMARY && eventArgs.ActionName != SECONDARY) {
            return;
        }

        if (!IsPlayerHoldingAGun()) {
            CurrentWeaponSecondaryState = WeaponSecondaryState.NotEquipped;
            RestartCameraAimModeCoroutine(false);
            return;
        }

        if (eventArgs.ActionName == PRIMARY) {
            IsFireHeldDown = eventArgs.IsDown;
        }

        switch (CurrentWeaponSecondaryState) {
            case WeaponSecondaryState.Ready:
                if (eventArgs.ActionName == SECONDARY && eventArgs.IsDown) {
                    CurrentWeaponSecondaryState = WeaponSecondaryState.Aiming;
                    CurrentWeaponPrimaryState = WeaponPrimaryState.Ready;
                    RestartCameraAimModeCoroutine(true);
                }
                break;
            case WeaponSecondaryState.Aiming:
                if (eventArgs.ActionName == SECONDARY && !eventArgs.IsDown) {
                    CurrentWeaponSecondaryState = WeaponSecondaryState.Ready;
                    CurrentWeaponPrimaryState = WeaponPrimaryState.NotEquipped;
                    RestartCameraAimModeCoroutine(false);
                }
                else if (IsFireHeldDown) {
                    FireWeapon();
                }
                break;
            default:
                break;
        }
    }

    public bool IsPlayerHoldingAGun() => CurrentInteractablePresetHeld != null && GunEntriesByPresetName.Keys.Contains(CurrentInteractablePresetHeld.presetName);

    private void RestartCameraAimModeCoroutine(bool enter) {
        if (CameraFovTweenCoroutine != null) {
            RuntimeHelper.StopCoroutine(CameraFovTweenCoroutine);
        }
        if (enter) {
            CameraFovTweenCoroutine = RuntimeHelper.StartCoroutine(InGameTween(0.25f, AdjustCameraEnterAimMode));
            return;
        }
        CameraFovTweenCoroutine = RuntimeHelper.StartCoroutine(InGameTween(0.25f, AdjustCameraExitAimMode));
    }

    internal void AdjustCameraEnterAimMode(float progressPct) {
        var fovReductionPct = GunEntriesByPresetName[CurrentInteractablePresetHeld.presetName].ZoomInOnAimPct;
        var currentFov = CameraController.Instance.cam.fieldOfView;
        var defaultFov = Game.Instance.fov;
        var targetFov = defaultFov * (1.0f - fovReductionPct);
        CameraController.Instance.cam.fieldOfView = currentFov - (currentFov - targetFov) * progressPct;
    }

    internal void AdjustCameraExitAimMode(float progressPct) {
        var currentFov = CameraController.Instance.cam.fieldOfView;
        var defaultFov = Game.Instance.fov;
        CameraController.Instance.cam.fieldOfView = currentFov - (currentFov - defaultFov) * progressPct;
    }

    public void FireWeapon() {
        if (!InGameSetupCompleted || IsGamePaused) {
            return;
        }
        // FIXME
        // if (Config.IsAmmoRequired && !HasAmmoInInventory()) {
        //     return;
        // }
        var weapon = CurrentInteractablePresetHeld.weapon;
        var weaponHash = weapon.GetHashCode();
        if (WeaponsOnCooldown.Contains(weaponHash)) {
            return;
        }
        if (CurrentWeaponSecondaryState != WeaponSecondaryState.Aiming || CurrentWeaponPrimaryState != WeaponPrimaryState.Ready) {
            return;
        }

        CurrentWeaponPrimaryState = WeaponPrimaryState.Firing;

        var gunInfoEntry = GunEntriesByPresetName[CurrentInteractablePresetHeld.presetName];

        // Largely taken from NewAIController.RecalculateWeaponStats
        var weaponRangeMax = weapon.GetAttackValue(MurderWeaponPreset.AttackValue.range, Player.Instance);
        var weaponRefire = weapon.GetAttackValue(MurderWeaponPreset.AttackValue.fireDelay, Player.Instance) * gunInfoEntry.DelayBetweenShotsFactor;
        var weaponAccuracy = weapon.GetAttackValue(MurderWeaponPreset.AttackValue.accuracy, Player.Instance);
        var weaponDamage = weapon.GetAttackValue(MurderWeaponPreset.AttackValue.damage, Player.Instance);

        // Largely taken from Game.ShootFromPlayer
        var muzzlePoint = FirstPersonItemController.Instance.rightHandObjectParent.TransformPoint(weapon.itemRightLocalPos);
        var ejectPoint = FirstPersonItemController.Instance.rightHandObjectParent.TransformPoint(weapon.itemRightLocalPos + Vector3.back * EJECT_POINT_PROJECTION_FACTOR);
        var aimPoint = CameraController.Instance.cam.transform.position;
        var aimForwardPoint = CameraController.Instance.cam.transform.TransformPoint(Vector3.forward);

        // Custom muzzle flash handling, so we don't get a flash in the middle
        // of our camera view
        CreateMuzzleFlash(weapon, muzzlePoint, aimForwardPoint);
        // remove the muzzle flash prior to calling Shoot, and restore it
        // afterwards
        var muzzleFlash = weapon.muzzleFlash;
        weapon.muzzleFlash = null;
        Toolbox.Instance.Shoot(Player.Instance, aimPoint, aimForwardPoint, weaponRangeMax, weaponAccuracy, weaponDamage, weapon, Config.EjectBrass, ejectPoint, false);

        // TODO: change this to be extensible to potentially support more
        // weapons in the future
        if (gunInfoEntry.ItemPresetName == SHOTGUN_PRESET_NAME) {
            var fireEvent = weapon.fireEvent;
            weapon.fireEvent = null;
            var impactEvent = weapon.impactEvent;
            weapon.impactEvent = null;
            var impactEventPlayer = weapon.impactEventPlayer;
            weapon.impactEventPlayer = null;
            for (int i = 1; i < SHOTGUN_PROJECTILES_PER_SHELL; i++) {
                // Do not eject brass for additional shots
                Toolbox.Instance.Shoot(Player.Instance, aimPoint, aimForwardPoint, weaponRangeMax, weaponAccuracy, weaponDamage, weapon, false, ejectPoint, false);
            }
            weapon.fireEvent = fireEvent;
            weapon.impactEvent = impactEvent;
            weapon.impactEventPlayer = impactEventPlayer;
        }

        weapon.muzzleFlash = muzzleFlash;

        var recoilVector = new Vector3(Toolbox.Instance.Rand(-1.0f, 1.0f), Toolbox.Instance.Rand(-1.0f, 1.0f), Toolbox.Instance.Rand(-1.0f, 1.0f));
        recoilVector.Scale(gunInfoEntry.RecoilPatternFactors);
        Player.Instance.fps.JoltCamera(recoilVector, gunInfoEntry.RecoilAmplitude * Config.RecoilAmplitudeFactor, speed: DEFAULT_RECOIL_SPEED);

        ApplyPlayerIllegalActionStatus();

        // Start the cooldown period
        WeaponsOnCooldown.Add(weaponHash);
        CurrentWeaponPrimaryState = WeaponPrimaryState.Ready;
        RuntimeHelper.StartCoroutine(
            InGameTimer(weaponRefire, () => {
                if (WeaponsOnCooldown.Contains(weaponHash)) {
                    WeaponsOnCooldown.Remove(weaponHash);
                }
                if (!gunInfoEntry.IsFullAuto || CurrentInteractablePresetHeld == null || CurrentInteractablePresetHeld.weapon != weapon || !IsFireHeldDown) {
                    return;
                }
                FireWeapon();
            })
        );
    }

    private void CreateMuzzleFlash(MurderWeaponPreset weapon, Vector3 muzzlePoint, Vector3 forward) {
        if (weapon.muzzleFlash == null) {
            return;
        }
        // Taken from game code
        GameObject gameObject = UnityEngine.Object.Instantiate(weapon.muzzleFlash, PrefabControls.Instance.mapContainer);
        gameObject.transform.position = muzzlePoint;
        gameObject.transform.rotation = Quaternion.LookRotation(forward, gameObject.transform.up);
    }

    private bool HasAmmoInInventory() {
        // FIXME
        var ammo = CurrentInteractablePresetHeld.weapon.ammunition.Select(x => x);
        var result = ammo.Any(Helpers.IsPresentInPlayerInventory);
        if (!result) {
            Lib.GameMessage.Broadcast($"{LACK_AMMO_MESSAGE_START}{string.Join(", ", ammo.Select(x => x.name))}", InterfaceController.GameMessageType.notification, InterfaceControls.Icon.cross);
        }
        return result;
    }

    private void ApplyPlayerIllegalActionStatus() {
        // Applied on top of the alert caused when someone is hit by the player's gunfire
        if (Player.Instance.isHome || Helpers.IsPlayerBeingPursuedByMurderer() || (!Player.Instance.isSeenByOthers && !Helpers.IsPlayerHeardByOthers())) {
            return;
        }
        // TODO: use a safer approach that doesn't potentially invalidate other illegal actions
        if (Player.Instance.illegalActionActive) {
            // don't call SetIllegalActionActive, which may affect the illegal action timer
            return;
        }
        InteractionController.Instance.SetIllegalActionActive(true);
    }

    private System.Collections.IEnumerator InGameTimer(float durationSec, Action actionOnCompletion) {
        var timePassed = 0.0f;
        while (timePassed < durationSec) {
            yield return new WaitForEndOfFrame();
            if (IsGamePaused) {
                continue;
            }
            timePassed += UnityEngine.Time.deltaTime;
        }
        actionOnCompletion();
    }

    private System.Collections.IEnumerator InGameTween(float durationSec, Action<float> actionPerTick) {
        var timePassed = 0.0f;
        while (timePassed < durationSec) {
            yield return new WaitForEndOfFrame();
            if (IsGamePaused) {
                continue;
            }
            timePassed += UnityEngine.Time.deltaTime;
            actionPerTick(timePassed / durationSec);
        }
    }

    private void SetPrimaryActionFire(bool showAsFire) {
        if (!InGameSetupCompleted || CurrentInteractablePresetHeld == null) {
            return;
        }

        AttackFpsAction = CurrentInteractablePresetHeld.fpsItem.actions.ToList().First(x => x.action.presetName == "FPSPunch");

        if (!showAsFire) {
            AttackFpsAction.interactionName = ACTION_NAME_ATTACK;
            AttackFpsAction.useCameraJolt = true;
            AttackFpsAction.mainSpecialAction = FirstPersonItem.SpecialAction.punch;
            InteractionController.Instance.UpdateInteractionText();
            return;
        }

        AttackFpsAction.interactionName = ACTION_NAME_FIRE;
        AttackFpsAction.useCameraJolt = false;
        AttackFpsAction.mainSpecialAction = FirstPersonItem.SpecialAction.none;
        InteractionController.Instance.UpdateInteractionText();
    }

    private void SetSecondaryActionAim(bool showAsAim) {
        if (!InGameSetupCompleted || CurrentInteractablePresetHeld == null) {
            return;
        }

        ThrowFpsAction = CurrentInteractablePresetHeld.fpsItem.actions.ToList().First(x => x.action.presetName == "FPSThrow");

        if (!showAsAim) {
            ThrowFpsAction.interactionName = ACTION_NAME_THROW;
            ThrowFpsAction.action.throwObjectsAtTarget = true;
            ThrowFpsAction.mainSpecialAction = FirstPersonItem.SpecialAction.attack;
            InteractionController.Instance.UpdateInteractionText();
            return;
        }

        ThrowFpsAction.interactionName = ACTION_NAME_AIM;
        ThrowFpsAction.action.throwObjectsAtTarget = false;
        ThrowFpsAction.mainSpecialAction = FirstPersonItem.SpecialAction.none;
        InteractionController.Instance.UpdateInteractionText();
    }

    private void OnGameResumed(object sender, EventArgs e) {
        IsGamePaused = false;
    }

    private void OnGamePaused(object sender, EventArgs e) {
        IsGamePaused = true;
    }
}