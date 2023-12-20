# Plugin Data Persistence - Save your mod's data in savegames

A BepInEx Bleeding Edge (v667) plugin for Shadows of Doubt

## What is it?

A mod utility to persist data between game save sessions.

## Installation

If you are not using r2modman or Thunderstore for installation, follow these steps:

1. Download BepInEx (build artifact 667 or higher) from the official repository.
2. Extract the downloaded files into the same folder as the "Shadows of Doubt.exe" executable.
3. Launch the game, load the main menu, and then exit the game.
4. Download the latest version of the plugin from the Releases page. Unzip the files and place them in corresponding directories within "Shadows of Doubt\BepInEx...".
5. Start the game.

## Usage

Random example: imagine you have a mod where the player collects rings like in Sonic and redeems them for coffee at vending machines, or something.

```cs
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Shadows of Doubt.exe")]
[BepInDependency("SpyraxiHelpers", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    internal const string KEY_NUM_RINGS = "numberOfRings";
    internal const string KEY_NUM_COFFEES_PURCHASED = "numberOfCoffeesPurchased";
    internal const string KEY_MOD_VERSION = "modVersion";

    private int nRings = 0;
    private int nCoffees = 0;

    public override void Load()
    {
        // Plugin startup...

        SpyraxiHelpers.Hooks.OnPostLoad.AddListener(_ => this.OnGameStarted());
    }
    
    private void OnGameStarted() {
        Dictionary<string, object> data = PluginDataPersistence.Plugin.LoadOrGetSaveGameData(this);

        if(data.Keys.Count == 0)) {
            Logger.LogWarning("No SonicRingsForCoffee data found in the savegame.");
            return;
        }
        
        // Mod data version and compatibility checks should probably go here...
        // e.g. check data[KEY_MOD_VERSION] against MyPluginInfo.PLUGIN_VERSION
        // or some constant you change manually.

        nRings = data[KEY_NUM_RINGS]; // load the value from the savegame into your instance var
        nCoffees = data[KEY_NUM_COFFEES_PURCHASED]; // ditto
        Logger.LogInfo($"Loaded data: nRings = {nRings}, nCoffees = {nCoffees}.");
    }
    
    // Call this whenver you change nRings or nCoffees... you could even make
    // them both properties and put this in the setter logic. It doesn't cause
    // any slowdown to change the dictionary, the data only gets saved when the
    // game is saved.
    private void SyncDataWithPersistence() {
        Dictionary<string, object> data = PluginDataPersistence.Plugin.LoadOrGetSaveGameData(this);
        data[KEY_NUM_RINGS] = nRings;
        data[KEY_NUM_COFFEES_PURCHASED] = nCoffees;
        Logger.LogInfo("Data synced!");
    }
}
```

Nothing else is needed; the plugin will save all of the data your plugin added to the dictionary with the player's game. The data is included in the player's save game and syncs with Steam Cloud etc. It does not break the save game in a way that would cause any issues with loading after mods are uninstalled or updated or whatever.

### Limitations

- IL2CPP only... aka it won't work on the mono build. I may create a mono branch for this repo if enough modders want it.
- I intentionally limited the scope to savegame-specific mod data.
- To keep things simple, I did not include things like version checking, compatibility checks etc. between data that was saved with different versions of your mods. I'd suggest that you just include a version entry in the dictionary and do custom checks based off that.
- I also didn't add encryption or obfuscation... you can pre-treat your data to accomplish that if you need to.
- The plugin enforces a limit of ~1MB of data post-JSON-serialization (pre-brotli-compression) per plugin. Obviously, you can get around this if you are determined, but I put the restriction in as a hard reminder that you are probably doing something wrong. Most mods won't get close to this limit. The reason I added the limit is that there is a risk of exceeding Steam Cloud save file size limits if mods write too much data to game save files. I'd recommend that you create your own custom solution if you absolutely need to store >1MB of persistent data for your mod.

## License

All code in this repo is distributed under the [MIT License](https://bitbucket.org/shadows-of-doubt-mods/mods/src/main/LICENSE). Feel free to use, modify, and distribute as needed.
