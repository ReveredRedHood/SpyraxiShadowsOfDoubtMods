# CHANGELOG

**0.2.1**

- Adjustment to LifeAndLiving integration to be compatible with the newest version.

**0.2.0**

- Added behavior where citizens will lose nerve and run away if you point a gun at them for a long enough period of time. If indoors, citizens will attempt to sound the alarm. The rate at which this happens varies depending on your distance from the citizen, their alertness level, and your visibility. Added configuration settings related to this behavior.
- Added damage adjustment factors, and did a first pass to balance weapon damage.
- Increased number of shotgun projectiles per shot to 8 in total.
- Added weapon buy price adjustments, and did a first pass to balance weapon prices.
  - If Venomaus' Life and Living mod is installed, this mod will take its item price configuration settings into account.
- Fixed console errors popping up while barging through a door while aiming a gun (and other events that forcefully unequip items).
- Fixed bug where melee weapons could get stuck showing "Fire" instead of "Attack" as their primary action, disabling the melee attack (caused by unequipping a gun while aiming it).

**0.1.3**

- Removed SOD.Common and Castle assemblies that could become outdated.

**0.1.2**

- The GunTestingMode config setting has been changed to work as soon as it is set (no need to load a game or start a new game).
- Cleaned up the code, refactored in various places to simplify the logic.
- Fixed bug where the player would sometimes get stuck in a state where "throw" is their secondary action (during the first game started with the mod enabled).
- Fixed bug where citizens would warn the player ("bark" at them) about the weapon they are holding using the bark chance config value, even when the weapon was not a gun.
- Updated IL2CPP game dependencies

**0.1.1**

- Fixed issue where certain non-gun weapons would show an "Aim" action and disable throwing.
- Fixed issue where swapping items while zoomed in with the "Aim" action would cause you to get stuck in "zoomed-in mode".
