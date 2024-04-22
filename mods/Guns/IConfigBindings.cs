using SOD.Common.BepInEx.Configuration;

namespace Guns;

public interface IConfigBindings {
    [Binding(true, "If true, eject bullet casings.")]
    bool EjectBrass { get; set; }

    [Binding(1.0f, "The factor applied on top of recoil amplitude. Set to 0 to disable recoil completely.")]
    float RecoilAmplitudeFactor { get; set; }

    [Binding(0.2f, "The chance (from 0.0 to 1.0 = 100%) that citizens will warn you to put a gun away while you have one drawn.")]
    float GunDrawBarkChance { get; set; }

    // FIXME
    // [Binding(true, "If true, guns require their ammo to be in the player's inventory to fire.")]
    // bool IsAmmoRequired { get; set; }

    [Binding(false, "If true, add all guns and ammo to your inventory. Works immediately. Save the setting as false prior to setting it back to true to repeat the command.")]
    bool GunTestingMode { get; set; }
}
