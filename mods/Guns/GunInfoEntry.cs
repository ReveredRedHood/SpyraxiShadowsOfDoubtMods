using UnityEngine;

namespace Guns;

public class GunInfoEntry {
    public GunInfoEntry(string itemPresetName, Vector3 rotation, float delayBetweenShotsFactor, float damageFactor, bool isFullAuto, Vector3 recoilPatternFactors, float recoilAmplitude, float zoomInOnAimPct, float baseGameMinBuyPrice, float accuracyOverride = -1.0f, int projectilesPerShot = 1) {
        ItemPresetName = itemPresetName;
        Rotation = rotation;
        DelayBetweenShotsFactor = delayBetweenShotsFactor;
        DamageFactor = damageFactor;
        IsFullAuto = isFullAuto;
        RecoilPatternFactors = recoilPatternFactors;
        RecoilAmplitude = recoilAmplitude;
        ZoomInOnAimPct = zoomInOnAimPct;
        BaseGameMinBuyPrice = baseGameMinBuyPrice;
        AccuracyOverride = accuracyOverride;
        ProjectilesPerShot = projectilesPerShot;
    }

    public InteractablePreset InteractablePreset { get; internal set; }
    public Vector3 RecoilPatternFactors { get; }
    public Vector3 Rotation { get; }
    public bool IsFullAuto { get; }
    public float AccuracyOverride { get; internal set; }
    public float BaseGameMinBuyPrice { get; }
    public float DamageFactor { get; internal set; }
    public float DelayBetweenShotsFactor { get; }
    public float RecoilAmplitude { get; internal set; }
    public float ZoomInOnAimPct { get; }
    public int ProjectilesPerShot { get; internal set; }
    public string ItemPresetName { get; }

    public override string ToString() {
        return $@"GunInfoEntry for {ItemPresetName}: 
            AccuracyOverride = {AccuracyOverride},
            BaseGameMinBuyPrice = {BaseGameMinBuyPrice},
            DamageFactor = {DamageFactor},
            DelayBetweenShotsFactor = {DelayBetweenShotsFactor},
            InteractablePreset = {InteractablePreset},
            IsFullAuto = {IsFullAuto},
            ProjectilesPerShot = {ProjectilesPerShot},
            RecoilAmplitude = {RecoilAmplitude},
            RecoilPatternFactors = {RecoilPatternFactors},
            Rotation = {Rotation},
            ZoomInOnAimPct = {ZoomInOnAimPct},
        ";
    }
}