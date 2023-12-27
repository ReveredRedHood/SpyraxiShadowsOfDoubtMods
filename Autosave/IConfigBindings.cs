using SOD.Common.BepInEx.Configuration;

namespace Autosave;

public interface IConfigBindings
{
    [Binding(120, "The amount of time between autosaves in seconds. The minimum is 15 seconds.")]
    int AutosaveDelay { get; set; }

    [Binding(true, "If true, warn of upcoming autosaves 5 seconds and 30 seconds beforehand.")]
    bool ShowWarnings { get; set; }

    [Binding(
        true,
        "If true, then do not start the next autosave timer if the player was AFK during an autosave. The timer will start when the player returns from being AFK."
    )]
    bool AvoidConsecutiveAfkAutosaves { get; set; }

    [Binding(5, "The number of autosaves to keep. Old autosaves will be overwritten.")]
    int NumberOfAutosavesToKeep { get; set; }

    [Binding(
        true,
        "If true, autosaves will be named \"<current save name> - AUTO #\". Otherwise, autosaves will be named \"Autosave - AUTO #\" instead."
    )]
    bool UseLastSaveName { get; set; }
}
