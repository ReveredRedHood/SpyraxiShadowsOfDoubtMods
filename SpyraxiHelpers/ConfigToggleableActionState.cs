// TODO
namespace SpyraxiHelpers;

public enum ConfigToggleableActionState {
    EffectsInactive,
    // Inactive Substates:
    WaitingForStartGame,
    SuspendedPostStartGame,

    EffectsActive,
    // Active Substates:
    EnabledPostStartGame,
    Continued,
}