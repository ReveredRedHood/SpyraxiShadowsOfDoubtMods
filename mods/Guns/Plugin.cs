using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
[BepInDependency(LifeAndLivingIntegration.LIFE_AND_LIVING_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(SOD.Common.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public partial class Plugin : PluginController<Plugin, IConfigBindings> {
    // TODO: I really don't know how accurate the descriptions for these recoil
    // patterns are
    public static Vector3 CircularRecoilPattern = Vector3.one.normalized;
    public static Vector3 DiagonalRecoilPattern = new Vector3(1.0f, 0.5f, 1.0f).normalized;
    public static Vector3 ShortOvalRecoilPattern = new Vector3(1.0f, 1.0f, 0.5f).normalized;
    public static Vector3 VerticalRecoilPattern = new Vector3(0.2f, 1.0f, 0.2f).normalized;
    internal const float DEFAULT_RECOIL_SPEED = 1.0f;
    internal const float EJECT_POINT_PROJECTION_FACTOR = 6.0f;
    internal const string ACTION_DDS_DICTIONARY_NAME = "ui.interaction";
    internal const string ACTION_NAME_AIM = "Aim";
    internal const string ACTION_NAME_ATTACK = "Attack";
    internal const string ACTION_NAME_FIRE = "Fire";
    internal const string ACTION_NAME_THROW = "Throw";
    internal const string LACK_AMMO_MESSAGE_START = "You lack the ammo needed to fire this weapon: ";
    internal const string PRIMARY = "Primary";
    internal const string SECONDARY = "Secondary";
    internal const float TIMER_DURATION = 0.25f;

    /* For those who want to use this as a modding resource, you can add
     * additional weapons that shoot or edit the info for existing ones in
     * Guns.Plugin.GunInfoEntries. If you do this while in-game (new game or
     * loaded save), then call Guns.Plugin.InGameSetup() afterwards.
    */
    public List<GunInfoEntry> GunInfoEntries = new List<GunInfoEntry> {
        new(itemPresetName: "BattleRifle",
            rotation: new Vector3(0f, 0f, 0f),
            delayBetweenShotsFactor: 0.2f,
            damageFactor: 0.7f,
            isFullAuto: true,
            recoilPatternFactors: DiagonalRecoilPattern,
            recoilAmplitude: 2.25f,
            zoomInOnAimPct: 0.2f,
            baseGameMinBuyPrice: 1000.0f
            ),
        new(itemPresetName: "Revolver",
            rotation: new Vector3(0f, -90f, 0f),
            delayBetweenShotsFactor: 1.0f,
            damageFactor: 1.2f,
            isFullAuto: false,
            recoilPatternFactors: ShortOvalRecoilPattern,
            recoilAmplitude: 15.0f,
            zoomInOnAimPct: 0.2f,
            baseGameMinBuyPrice: 400.0f
            ),
        new(itemPresetName: "SemiAutomaticPistol",
            rotation: new Vector3(0f, -90f, 0f),
            delayBetweenShotsFactor: 0.8f,
            damageFactor: 1.3f,
            isFullAuto: false,
            recoilPatternFactors: ShortOvalRecoilPattern,
            recoilAmplitude: 1.0f,
            zoomInOnAimPct: 0.2f,
            baseGameMinBuyPrice: 150.0f
            ),
        new(itemPresetName: "SemiAutomaticPistolSilenced",
            rotation: new Vector3(0f, -90f, 0f),
            delayBetweenShotsFactor: 0.8f,
            damageFactor: 1.3f,
            isFullAuto: false,
            recoilPatternFactors: ShortOvalRecoilPattern,
            recoilAmplitude: 1.0f,
            zoomInOnAimPct: 0.2f,
            baseGameMinBuyPrice: 200.0f
            ),
        new(itemPresetName: "Shotgun",
            rotation: new Vector3(0f, 0f, 0f),
            delayBetweenShotsFactor: 1.0f,
            damageFactor: 1.0f,
            isFullAuto: false,
            recoilPatternFactors: ShortOvalRecoilPattern,
            recoilAmplitude: 20.0f,
            zoomInOnAimPct: 0.1f,
            baseGameMinBuyPrice: 500.0f,
            projectilesPerShot: 8
            ),
        new(itemPresetName: "SniperRifle",
            rotation: new Vector3(270f, 0f, 0f),
            delayBetweenShotsFactor: 1.5f,
            damageFactor: 1.4f,
            isFullAuto: false,
            recoilPatternFactors: CircularRecoilPattern,
            recoilAmplitude: 30.0f,
            zoomInOnAimPct: 0.6f,
            baseGameMinBuyPrice: 700.0f
            ),
    };
    public Dictionary<string, GunInfoEntry> GunEntriesByPresetName = new();
    public HashSet<int> WeaponsOnCooldown { get; internal set; } = new();
    private WeaponPrimaryState currentWeaponPrimaryState;
    public WeaponPrimaryState CurrentWeaponPrimaryState {
        get => currentWeaponPrimaryState;
        internal set {
            currentWeaponPrimaryState = value;
            switch (currentWeaponPrimaryState) {
                case WeaponPrimaryState.Aiming:
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
                case WeaponSecondaryState.NotAiming:
                case WeaponSecondaryState.Aiming:
                    RestartCitizenNerveUnderAimCoroutine();
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
    private Coroutine CitizenNerveUnderAimCoroutine { get; set; }

    internal InteractablePreset CurrentInteractablePresetHeld => BioScreenController.Instance.selectedSlot?.GetInteractable()?.preset;
    internal LifeAndLivingIntegration LifeAndLivingIntegration { get; } = new LifeAndLivingIntegration();


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

        LifeAndLivingIntegration.Setup();
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

    private Human GetHumanThatPlayerIsAimingAt() {
        var aimPoint = CameraController.Instance.cam.transform.position;
        var aimForwardPoint = CameraController.Instance.cam.transform.TransformPoint(Vector3.forward);
        if (!Physics.Raycast(aimPoint, (aimForwardPoint - aimPoint).normalized, out var hitInfo, GameplayControls.Instance.citizenSightRange, Toolbox.Instance.physicalObjectsLayerMask)) {
            return null;
        }
        var hitHuman = hitInfo.transform.gameObject.GetComponent<Human>();
        if (hitHuman == null) {
            hitHuman = hitInfo.transform.gameObject.GetComponentInParent<Human>();
        }
        if (hitHuman == null || hitHuman.isPlayer || hitHuman.isMachine) {
            return null;
        }
        return hitHuman;
    }

    public void InGameSetup() {
        // Reset state in case we are loading a game after already being
        // in-game
        currentWeaponPrimaryState = WeaponPrimaryState.NotAiming;
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
            CalculateBuyPrice(entry);
        }

        foreach (var (key, gunEntry) in GunEntriesByPresetName) {
            // Set the fps item offset and rotation so the guns are aiming
            // forward
            gunEntry.InteractablePreset.fpsItemOffset = Vector3.zero;
            gunEntry.InteractablePreset.fpsItemRotation = gunEntry.Rotation;
        }

        InGameSetupCompleted = true;
    }

    internal void CalculateBuyPrice(GunInfoEntry entry) {
        var minItemValue = 100;
        var percentageValueIncrease = 0;
        if (LifeAndLivingIntegration.IsActive) {
            LifeAndLivingIntegration.OverwriteVars(ref minItemValue, ref percentageValueIncrease);
        }
        var preset = entry.InteractablePreset;
        var multiplier = entry.BaseGameMinBuyPrice / 100.0f;
        var minValue = Math.Max(100.0f, minItemValue) * (1.0f + percentageValueIncrease / 100.0f) * multiplier;
        var maxValue = Math.Max(100.0f, minItemValue) * (1.0f + percentageValueIncrease / 100.0f) * multiplier;
        preset.value = new UnityEngine.Vector2((int)minValue, (int)maxValue);
        Log.LogInfo($"Adjusted item purchase price of {preset.presetName} to ({preset.value.x} - {preset.value.y}).");

        var companies = CityData.Instance.companyDirectory.ToList().Where(c => c.prices.ContainsKey(preset));
        foreach (var c in companies) {
            if (!c.prices.ContainsKey(preset)) {
                continue;
            }
            var price = System.Random.Shared.NextSingle() * (preset.value.y - preset.value.x) + preset.value.x;
            price = (float)Math.Floor(price);
            c.prices[preset] = (int)price;
            Log.LogInfo($"  Applied change to {c.name} ({price})");
        }
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
            case WeaponSecondaryState.NotAiming:
                if (eventArgs.ActionName == SECONDARY && eventArgs.IsDown) {
                    CurrentWeaponSecondaryState = WeaponSecondaryState.Aiming;
                    CurrentWeaponPrimaryState = WeaponPrimaryState.Aiming;
                    RestartCameraAimModeCoroutine(true);
                }
                break;
            case WeaponSecondaryState.Aiming:
                if (eventArgs.ActionName == SECONDARY && !eventArgs.IsDown) {
                    CurrentWeaponSecondaryState = WeaponSecondaryState.NotAiming;
                    CurrentWeaponPrimaryState = WeaponPrimaryState.NotAiming;
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

    private void RestartCitizenNerveUnderAimCoroutine() {
        if (CitizenNerveUnderAimCoroutine != null) {
            RuntimeHelper.StopCoroutine(CitizenNerveUnderAimCoroutine);
        }
        if (Config.CitizenNerveLostPerSecondAimedAt <= float.Epsilon) {
            return;
        }
        CitizenNerveUnderAimCoroutine = RuntimeHelper.StartCoroutine(InGameTimer(TIMER_DURATION, ReduceCitizenNerveUnderAim));
    }

    private void ReduceCitizenNerveUnderAim() {
        if (!IsPlayerHoldingAGun()) {
            return;
        }
        var human = GetHumanThatPlayerIsAimingAt();
        if (human == null || human.isAsleep || human.isDead || human.isStunned) {
            RestartCitizenNerveUnderAimCoroutine();
            return;
        }
        // cancel if the citizen is not facing/tracking you
        var rotationToFacePlayerDirectly = Quaternion.FromToRotation(human.ai.facingDirection, Player.Instance.aimTransform.position - human.aimTransform.position);
        var angle = Quaternion.Angle(Quaternion.identity, rotationToFacePlayerDirectly);
        if (angle > (GameplayControls.Instance.citizenFOV / 2.0f)) {
            // the player is not in the citizen's fov
            RestartCitizenNerveUnderAimCoroutine();
            return;
        }

        // account for player visibility
        var awarenessFactor = Player.Instance.visibilityLag;
        // optionally, account for citizen traits and certain status effects
        if (Config.CitizenNerveLossAccountsForAwareness) {
            awarenessFactor = human.blinded > 0.0f ? (human.alertness - human.blinded) : human.alertness;
            awarenessFactor = Math.Clamp(awarenessFactor, 0.0f, 1.0f);
        }
        // account for player proximity
        var proximityFactor = 1.0f - (Vector3.Distance(Player.Instance.transform.position, human.transform.position) / GameplayControls.Instance.citizenSightRange);

        var nerveToRemove = Config.CitizenNerveLostPerSecondAimedAt * TIMER_DURATION * awarenessFactor * proximityFactor;
        human.AddNerve(-1.0f * nerveToRemove, Player.Instance);
        RestartCitizenNerveUnderAimCoroutine();
    }

    private void RestartCameraAimModeCoroutine(bool enter) {
        if (CameraFovTweenCoroutine != null) {
            RuntimeHelper.StopCoroutine(CameraFovTweenCoroutine);
        }
        if (enter) {
            CameraFovTweenCoroutine = RuntimeHelper.StartCoroutine(InGameTween(TIMER_DURATION, AdjustCameraEnterAimMode));
            return;
        }
        CameraFovTweenCoroutine = RuntimeHelper.StartCoroutine(InGameTween(TIMER_DURATION, AdjustCameraExitAimMode));
    }

    internal void AdjustCameraEnterAimMode(float progressPct) {
        if (!IsPlayerHoldingAGun()) {
            RestartCameraAimModeCoroutine(false);
            return;
        }
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
        var weapon = CurrentInteractablePresetHeld.weapon;
        var weaponHash = weapon.GetHashCode();
        if (WeaponsOnCooldown.Contains(weaponHash)) {
            return;
        }
        if (CurrentWeaponSecondaryState != WeaponSecondaryState.Aiming || CurrentWeaponPrimaryState != WeaponPrimaryState.Aiming) {
            return;
        }

        CurrentWeaponPrimaryState = WeaponPrimaryState.Firing;

        var gunInfoEntry = GunEntriesByPresetName[CurrentInteractablePresetHeld.presetName];

        // Largely taken from NewAIController.RecalculateWeaponStats
        var weaponRangeMax = weapon.GetAttackValue(MurderWeaponPreset.AttackValue.range, Player.Instance);
        var weaponRefire = weapon.GetAttackValue(MurderWeaponPreset.AttackValue.fireDelay, Player.Instance) * gunInfoEntry.DelayBetweenShotsFactor;
        var weaponAccuracy = weapon.GetAttackValue(MurderWeaponPreset.AttackValue.accuracy, Player.Instance);
        var weaponDamage = weapon.GetAttackValue(MurderWeaponPreset.AttackValue.damage, Player.Instance) * gunInfoEntry.DamageFactor;

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
        Toolbox.Instance.Shoot(Player.Instance, aimPoint, aimForwardPoint, weaponRangeMax, weaponAccuracy, weaponDamage, weapon, Config.EjectBrass, ejectPoint, false, true);

        if (gunInfoEntry.ProjectilesPerShot > 1) {
            var fireEvent = weapon.fireEvent;
            weapon.fireEvent = null;
            var impactEvent = weapon.impactEvent;
            weapon.impactEvent = null;
            var impactEventPlayer = weapon.impactEventPlayer;
            weapon.impactEventPlayer = null;
            for (int i = 1, length = gunInfoEntry.ProjectilesPerShot; i < length; i++) {
                // Do not eject brass for additional shots,
                // and do not play the firing sound
                Toolbox.Instance.Shoot(Player.Instance, aimPoint, aimForwardPoint, weaponRangeMax, weaponAccuracy, weaponDamage, weapon, false, ejectPoint, false, false);
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
        CurrentWeaponPrimaryState = WeaponPrimaryState.Aiming;
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

    private void ApplyPlayerIllegalActionStatus() {
        // Applied on top of the alert caused when someone is hit by the player's gunfire
        if (Player.Instance.isHome || Helpers.IsPlayerBeingPursuedByMurderer() || (!Player.Instance.isSeenByOthers && !Helpers.IsPlayerHeardByOthers())) {
            Lib.PlayerStatus.RemoveIllegalStatusModifier(MyPluginInfo.PLUGIN_GUID);
            return;
        }
        Lib.PlayerStatus.SetIllegalStatusModifier(MyPluginInfo.PLUGIN_GUID, TimeSpan.FromSeconds(Config.IllegalStatusDurationOnHit), true);
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

        FirstPersonItem.FPSInteractionAction attackFpsAction;
        try {
            attackFpsAction = CurrentInteractablePresetHeld.fpsItem.actions.ToList().First(x => x.action.presetName == "FPSPunch");
        }
        catch {
            return;
        }

        if (!showAsFire) {
            attackFpsAction.interactionName = ACTION_NAME_ATTACK;
            attackFpsAction.useCameraJolt = true;
            attackFpsAction.mainSpecialAction = FirstPersonItem.SpecialAction.punch;
            InteractionController.Instance.UpdateInteractionText();
            return;
        }

        attackFpsAction.interactionName = ACTION_NAME_FIRE;
        attackFpsAction.useCameraJolt = false;
        attackFpsAction.mainSpecialAction = FirstPersonItem.SpecialAction.none;
        InteractionController.Instance.UpdateInteractionText();
    }

    private void SetSecondaryActionAim(bool showAsAim) {
        if (!InGameSetupCompleted || CurrentInteractablePresetHeld == null) {
            return;
        }

        FirstPersonItem.FPSInteractionAction throwFpsAction;
        try {
            throwFpsAction = CurrentInteractablePresetHeld.fpsItem.actions.ToList().First(x => x.action.presetName == "FPSThrow");
        }
        catch {
            return;
        }

        if (!showAsAim) {
            throwFpsAction.interactionName = ACTION_NAME_THROW;
            throwFpsAction.action.throwObjectsAtTarget = true;
            throwFpsAction.mainSpecialAction = FirstPersonItem.SpecialAction.attack;
            InteractionController.Instance.UpdateInteractionText();
            return;
        }

        throwFpsAction.interactionName = ACTION_NAME_AIM;
        throwFpsAction.action.throwObjectsAtTarget = false;
        throwFpsAction.mainSpecialAction = FirstPersonItem.SpecialAction.none;
        InteractionController.Instance.UpdateInteractionText();
    }

    private void OnGameResumed(object sender, EventArgs e) {
        IsGamePaused = false;
    }

    private void OnGamePaused(object sender, EventArgs e) {
        IsGamePaused = true;
    }
}