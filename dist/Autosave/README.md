# Autosave - Adds in-game autosave functionality

A BepInEx Bleeding Edge (v667) plugin for Shadows of Doubt

## What is it?

This plugin adds in-game autosave functionality.

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

This mod keeps track of the time since you last saved your game and saves automatically after a certain amount of time. Here are the basic settings, but you can change them:

- The mod saves your game every 5 minutes, but it doesn't count time spent with the game paused.
- Before each automatic save, it gives you two warnings, one 30 seconds before and another 5 seconds before.
- It can tell if you're away from your keyboard/mouse while the game is active. If you are, it stops the timer until you're back. The first save after you're back still happens on time.
- It keeps up to 5 autosaves for each character, adding "- AUTO #" onto the end of the save name. After the last autosave, it will replace the oldest save as it makes each new one.
- The autosaves are named using the format "{current save name} - AUTO {#}" if you manually save your game at least once. If not, or if you turn off this setting, they're named "Autosave - AUTO {#}" instead.

Note: The setting controlling the max number of autosaves works off of the save name, so using "Autosave - AUTO {#}" will limit the number of autosaves across all of your characters to the setting.

### Configuration

Config settings include:

| Name | Description |
|------|-------------|
AutosaveDelay | The amount of time between autosaves in seconds. The minimum is 15 seconds.
ShowWarnings | If true, warn of upcoming autosaves 5 seconds and 30 seconds beforehand.
AvoidConsecutiveAfkAutosaves | If true, then do not start the next autosave timer if the player was AFK during an autosave. The timer will start when the player returns from being AFK.
NumberOfAutosavesToKeep | The number of autosaves to keep. Old autosaves will be overwritten.
UseLastSaveName | If true, autosaves will be named \"{current save name} - AUTO #\". Otherwise, autosaves will be named \"Autosave - AUTO #\" instead.

You can use r2modman or Thunderstore Mod Manager to easily change the plugin's config settings (via the Config Editor tab). You can also manually edit the config file in the BepInEx directory (I believe that changes you make will take effect while the game is running, but I'm not 100% positive about that).

If you have [BepInExConfigManager](https://thunderstore.io/c/shadows-of-doubt/p/TeamSpyraxi/BepInExConfigManager/) installed, you can use the in-game menu to edit the config settings.

## License

All code in this repo is distributed under the MIT License. Feel free to use, modify, and distribute as needed.
