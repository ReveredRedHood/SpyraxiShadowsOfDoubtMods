using UnityEngine;

namespace Guns;

public class GunInfoEntry {
    public GunInfoEntry(string itemPresetName, Vector3 rotation, float delayBetweenShotsFactor, float damageFactor, bool isFullAuto, Vector3 recoilPatternFactors, float recoilAmplitude, float zoomInOnAimPct, float baseGameMinBuyPrice, int projectilesPerShot = 1) {
        ItemPresetName = itemPresetName;
        Rotation = rotation;
        DelayBetweenShotsFactor = delayBetweenShotsFactor;
        DamageFactor = damageFactor;
        IsFullAuto = isFullAuto;
        RecoilPatternFactors = recoilPatternFactors;
        RecoilAmplitude = recoilAmplitude;
        ZoomInOnAimPct = zoomInOnAimPct;
        BaseGameMinBuyPrice = baseGameMinBuyPrice;
        ProjectilesPerShot = projectilesPerShot;
    }

    public string ItemPresetName { get; }
    public Vector3 Rotation { get; }
    public float DelayBetweenShotsFactor { get; }
    public float DamageFactor { get; }
    public bool IsFullAuto { get; }
    public Vector3 RecoilPatternFactors { get; }
    public float RecoilAmplitude { get; }
    public float ZoomInOnAimPct { get; }
    public float BaseGameMinBuyPrice { get; }
    public int ProjectilesPerShot { get; }
    public InteractablePreset InteractablePreset { get; internal set; }
}