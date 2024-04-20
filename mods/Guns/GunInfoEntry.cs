using UnityEngine;

namespace Guns;

public class GunInfoEntry {
    public GunInfoEntry(string itemPresetName, Vector3 rotation, float delayBetweenShots, bool isFullAuto, Vector3 recoilPatternFactors, float recoilAmplitude, float zoomInOnAimPct) {
        ItemPresetName = itemPresetName;
        Rotation = rotation;
        DelayBetweenShotsFactor = delayBetweenShots;
        IsFullAuto = isFullAuto;
        RecoilPatternFactors = recoilPatternFactors;
        RecoilAmplitude = recoilAmplitude;
        ZoomInOnAimPct = zoomInOnAimPct;
    }

    public string ItemPresetName { get; }
    public Vector3 Rotation { get; }
    public float DelayBetweenShotsFactor { get; }
    public bool IsFullAuto { get; }
    public Vector3 RecoilPatternFactors { get; }
    public float RecoilAmplitude { get; }
    public float ZoomInOnAimPct { get; }
    public InteractablePreset InteractablePreset { get; internal set; }
}