using SOD.Common.BepInEx.Configuration;

namespace PresetEdit;

public interface IConfigBindings
{
    [Binding(
        false,
        "Whether to use the first savegame loaded to write all preset data for the current game version."
    )]
    bool WriteGamePresetData { get; set; }
}
