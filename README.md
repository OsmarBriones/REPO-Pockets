# Pockets

[Short 1-2 sentence description of what the mod brings to the player's game. Example: "Adds customizable duck spawns across the facility to bring joy and chaos to your salvage runs."]

Only the host needs to have this mod installed â€” other players see the effects automatically without installing anything.

## Preview

<!-- Visual gameplay demo GIF (auto-configured by RepoKit tools or placed in assets/preview.gif) -->
![Gameplay Preview](https://raw.githubusercontent.com/OsmarBriones/Pockets/master/assets/preview.gif)

## How to Use

1. **Quick Start**: Explain how the player starts using or finds the mod content in-game.
2. **Controls / Triggers**: Explain the key bindings or player actions (e.g. Press `E` to toggle).
3. **Feedback**: Describe how the player knows the mod is active (HUD prompt, sound, or visual cue).

## Features

<!--
  PLAYER-FACING FEATURES GUIDELINE:
  This section is for players on Thunderstore and mod managers.
  - Describe gameplay mechanics, effects, audiovisual feedback, and player experience.
  - Highlight multiplayer behavior in plain language (e.g., host-only installation).
  - DO NOT include Unity engine terms, hook/patch target names (e.g., EnemyDirector, TruckSafetySpawnPoint),
    Harmony methods, or internal code architecture here (put those in ARCHITECTURE.md).
-->

- **Exciting Gameplay Mechanic**: Explain what players experience in-game.
- **Dynamic Facility Effects**: Describe the in-game behavior and balance.
- **Host-Only Multiplayer**: Only the host needs the mod installed; effects are fully synchronized for all players in the lobby.
- **Customizable Experience**: Adjust settings via the configuration file to tune the mod to your liking.

## Requirements
- [BepInEx Pack for R.E.P.O.](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)

## Installation
1. Install the latest [BepInEx Pack](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/).
2. Place `Pockets.dll` into your `BepInEx/plugins` folder (or install via r2modman / Thunderstore Mod Manager).
3. Launch the game once â€” the configuration file will be generated automatically inside `BepInEx/config`.

## Configuration
All settings are controlled through the generated file: `com.osmar.Pockets.cfg` located in `BepInEx/config`.

- `ExampleSetting` (default `3`): Describe the gameplay effect of this option in plain words.

## Issues & Bug Reports
The official way to report issues, suggest improvements, or submit feedback is by opening an issue on the official GitHub repository:
ðŸ‘‰ [GitHub Issues](https://github.com/OsmarBriones/Pockets/issues)

## Credits
Developed by **Osmar Briones**
