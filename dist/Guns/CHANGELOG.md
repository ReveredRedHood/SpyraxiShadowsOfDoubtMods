# CHANGELOG

**0.1.2**

- The GunTestingMode config setting has been changed to work as soon as it is set (no need to load a game or start a new game).
- Cleaned up the code, refactored in various places to simplify the logic.
- Fixed bug where the player would sometimes get stuck in a state where "throw" is their secondary action (during the first game started with the mod enabled).
- Fixed bug where citizens would warn the player ("bark" at them) about the weapon they are holding using the bark chance config value, even when the weapon was not a gun.
- Updated IL2CPP game dependencies

**0.1.1**

- Fixed issue where certain non-gun weapons would show an "Aim" action and disable throwing.
- Fixed issue where swapping items while zoomed in with the "Aim" action would cause you to get stuck in "zoomed-in mode".