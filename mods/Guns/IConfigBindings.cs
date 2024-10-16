using SOD.Common.BepInEx.Configuration;

namespace Guns;

public interface IConfigBindings {
    [Binding(true, "If true, eject bullet casings.")]
    bool EjectBrass { get; set; }

    [Binding(1.0f, "The factor applied on top of recoil amplitude. Set to 0 to disable recoil completely.")]
    float RecoilAmplitudeFactor { get; set; }

    [Binding(0.2f, "The chance (from 0.0 to 1.0 = 100%) that citizens will warn you to put a gun away while you have one drawn.")]
    float GunDrawBarkChance { get; set; }

    [Binding(3.0f, "The number of seconds after a citizen is hit by the player's gunfire that the player has illegal status.")]
    float IllegalStatusDurationOnHit { get; set; }

    [Binding(0.5f, "How much of a citizen's nerve is lost per second that the player is aiming directly at them with a gun within the citizen's sight range. Citizen's tend to have maximum nerve values of around 0.4 to 0.5, and start running away around 0.1 (and may attempt to sound the alarm).")]
    float CitizenNerveLostPerSecondAimedAt { get; set; }

    [Binding(true, "Account for citizen alertness and blindness when calculating the actual nerve lost per second while being aimed at. The amount of nerve lost will be reduced by some factor depending on the citizen's traits while this setting is enabled.")]
    bool CitizenNerveLossAccountsForAwareness { get; set; }

    [Binding(false, "If true, the shotgun fires a singular slug round instead of buckshot.")]
    bool ShotgunUsesSingularRound { get; set; }

    [Binding(-1.0f, "The accuracy of the shotgun's buckshot spread as a value from 0.0 to 1.0, with 1.0 being perfectly accurate. Set to -1.0 to use the game's built-in accuracy value.")]
    float ShotgunAccuracyOverride { get; set; }


    [Binding(0.7f, "Per-projectile damage multiplier for the Faucon Rifle.")]
    float GunDamageFactorFauconRifle { get; set; }

    [Binding(1.2f, "Per-projectile damage multiplier for the Revolver.")]
    float GunDamageFactorRevolver { get; set; }

    [Binding(1.3f, "Per-projectile damage multiplier for the Semi-Auto Pistol.")]
    float GunDamageFactorSemiAuto { get; set; }

    [Binding(1.3f, "Per-projectile damage multiplier for the Semi-Auto Pistol (Silenced).")]
    float GunDamageFactorSemiAutoSilenced { get; set; }

    [Binding(1.0f, "Per-projectile damage multiplier for the Shotgun.")]
    float GunDamageFactorShotgun { get; set; }

    [Binding(1.4f, "Per-projectile damage multiplier for the Hamilton Rifle.")]
    float GunDamageFactorHamiltonRifle { get; set; }

    [Binding(false, "If true, add all guns and ammo to your inventory. Works immediately. Save the setting as false prior to setting it back to true to repeat the command.")]
    bool GunTestingMode { get; set; }
}
