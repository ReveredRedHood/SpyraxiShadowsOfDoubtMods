# Guns - Implements player-shootable guns into the game

A BepInEx Bleeding Edge (v667) plugin for Shadows of Doubt (IL2CPP).

## What is it?

This plugin implements player-shootable guns into the game.

## Installation

### r2modman or Thunderstore Mod Manager installation

If you are using [r2modman](https://thunderstore.io/c/shadows-of-doubt/p/ebkr/r2modman/) or [Thunderstore Mod Manager](https://www.overwolf.com/oneapp/Thunderstore-Thunderstore_Mod_Manager) for installation, simply download the mod from the Online tab.

### Manual installation

Follow these steps:

1. Download BepInEx (build artifact 667 or higher) from the official repository.
2. Extract the downloaded files into the same folder as the "Shadows of Doubt.exe" executable.
3. Launch the game, load the main menu, and then exit the game.
4. Download the latest version of the plugin from the Releases page. Unzip the files and place them in corresponding directories within "Shadows of Doubt\BepInEx...". Also, download the [SOD.Common](https://thunderstore.io/c/shadows-of-doubt/p/Venomaus/SODCommon/) dependency.
5. Start the game.

## Usage and features

This mod changes the guns that are already in game so that they can be fired while the player is using them. The way that citizens use guns is not changed.

- You must hold down the Secondary Action button to aim and then press the Primary Action button to fire, otherwise you will bash with the gun (like normal except for the direction that the gun is pointing).
- In general, you will need to be careful when you use your gun if you don't want to end up being chased and shot at by an angry crowd. Firing guns makes a lot of noise, which causes citizens to either investigate or flee (and sometimes sound the alarm).
- Includes recoil and a cooldown period between shots for all guns.
- Shotguns are adjusted to have a multiple bullet holes and a greater spread than other weapons.
- Includes "hold to fire" for automatic weapons (just the Faucon Rifle for now).

Note: I interpreted the Prop Gun as being fake, so it is unchanged from the base game.

### Configuration

Config settings include:

| Name | Description |
|------|-------------|
| EjectBrass | If true, eject bullet casings (default: true). |
| GunTestingMode | If true, add all guns and ammo to your inventory. Works immediately. Save the setting as false prior to setting it back to true to repeat the command (default: false). |
| RecoilAmplitudeFactor | The factor applied on top of recoil amplitude. Set to 0.0 to disable recoil completely (default: 1.0). |
| GunDrawBarkChance | The chance (from 0.0 to 1.0 = 100%) that citizens will warn you to put a gun away while you have one drawn (default: 0.2). |

You can use r2modman or Thunderstore Mod Manager to easily change the plugin's config settings (via the Config Editor tab). You can also manually edit the config file in the BepInEx directory (I believe that changes you make will take effect while the game is running, but I'm not 100% positive about that).

If you have [BepInExConfigManager](https://thunderstore.io/c/shadows-of-doubt/p/TeamSpyraxi/BepInExConfigManager/) installed, you can use the in-game menu to edit the config settings.

### Known Issues

Report any other issues in the "Spyraxi's Mods & Utilities" thread on the official game Discord ("mod-releases" channel).

### Future Plans

Roughly in order of highest to lowest priority. Lower items on the list are more ambitious ideas that might be implemented in separate plugins.

- Add a configuration option that makes aiming your gun in a citizen's face for too long an illegal action.
- Add a configuration option where ammo must be in the player's inventory for the gun to fire.
- Add temporarily shooting out lights, disabling cameras, breaking TVs, etc.
- Add a configuration option that causes ammo in the inventory to be used up while firing.
- Add a configuration option that adds clip sizes and makes reloading required (that works with or without the ammo setting).
- Allow the Pistol Silencer to be attached to a pistol as a contextual action, or detached by putting it down.

## License

All code in this repo is distributed under the MIT License. Feel free to use, modify, and distribute as needed.
