# PrintVmailBugFix - Fixes the bug where vmails sometimes don't print

A BepInEx Bleeding Edge (v667) plugin for Shadows of Doubt

## What is it?

Fixes the bug where V-Mails, surveillance files, etc. sometimes don't print. This is not a clean fix for the issue, just a workaround, but it should do the job.

## Installation

If you are not using r2modman or Thunderstore for installation, follow these steps:

1. Download BepInEx (build artifact 667 or higher) from the official repository.
2. Extract the downloaded files into the same folder as the "Shadows of Doubt.exe" executable.
3. Launch the game, load the main menu, and then exit the game.
4. Download the latest version of the plugin from the Releases page. Unzip the files and place them in corresponding directories within "Shadows of Doubt\BepInEx...".
5. Start the game.

## Note for Mod Authors

If your mod benefits from this bugfix, consider adding it as a HardDependency via BepInEx. You can also add to the affected preset names, if your mod introduces custom preset behavior.

```cs
// This attribute forces your mod to load after PrintBugfix, that's it.
// You may also need to add PrintBugfix to your manifest.json dependencies array
[BepInDependency(PrintBugfix.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin { // Or inheriting SOD.Common's PluginController instead of BasePlugin
    public override void Load() {
        // OPTIONAL: if your mod prints the preset named BirthCertificate
        PrintBugfix.Plugin.AffectedPresetNames.Add("BirthCertificate");

        // Whatever your plugin startup logic is...
    }

    public override bool Unload() {
        // OPTIONAL: if you registered a preset name in Load, unregister it here...
        PrintBugfix.Plugin.AffectedPresetNames.Remove("BirthCertificate");

        // Whatever your plugin unload logic is...
    }
}
```

## License

All code in this repo is distributed under the [MIT License](https://bitbucket.org/shadows-of-doubt-mods/mods/src/main/LICENSE). Feel free to use, modify, and distribute as needed.
