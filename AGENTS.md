# Pockets — Local Agent Context

This is an independent BepInEx 5.x mod repository for R.E.P.O. Its entry
point is `PocketsPlugin.cs`, and it builds `Pockets.dll`
for .NET Framework 4.8.

## Working rules

- This repository must remain usable after a normal clone. Do not require its
  parent workspace for normal builds or documentation.
- Build with `dotnet build Pockets.csproj`. The post-build deployment
  target is conditioned to skip execution when `AssemblyName` is `Pockets`
  to prevent deploying the template into game installations; mods scaffolded from
  the template will deploy to Steam and r2modman Debug profile normally.
- Configuration and paths:
  - Game paths and target framework are configured in `Directory.Build.props`.
  - Override the default Steam path if needed with the `REPO_GAME_DIR` environment variable.
- Shared code and guidance:
  - Add `RepoKit` for canonical workspace guidance:
    `git submodule add https://github.com/OsmarBriones/RepoKit.git external/RepoKit`
  - If consuming `RepoAPI` for reusable game logic (item spawning, keys, etc.):
    `git submodule add https://github.com/OsmarBriones/RepoAPI.git external/RepoAPI`
    and include only the required modules via `<Compile Include>` in `.csproj`.
- Preserve existing local modifications. Do not reset, discard, or overwrite
  unrelated work.
- Testing: Follow the 3-tier testing strategy in `external/RepoKit/REPO_MODS_METHODOLOGY.md` §8 (Tier 1 unit tests, Tier 2 Harmony reflection verification, Tier 3 in-game smoke tests; debug triggers are optional and opt-in).
- Coding standards: Follow `external/RepoKit/REPO_MODS_METHODOLOGY.md` §6 (clean PascalCase/camelCase, no `_` or `s_` prefixes, scope classes to `internal` by default, BepInEx logger).
- Author vs. Technical Identifier: Human developer author is **always** `Osmar Briones` (credits in `README.md`, `<Authors>` tag in `.csproj`). Technical reverse-DNS `com.osmar` is strictly reserved for GUIDs (`PluginGuid`), config file names (`BepInEx/config/com.osmar.<ModName>.cfg`), and namespaces.
- Player-friendly README (`## Features`): Thunderstore documentation is for players. The `## Features` section must describe gameplay mechanics and player experience in accessible language. **Never** leak internal Unity engine hooks (`EnemyDirector.Start`, `TruckSafetySpawnPoint`, etc.), Harmony patches, class/method names, or code architecture into `README.md`; those belong strictly in `ARCHITECTURE.md`.
- Spec Kit (SDD): Spec Kit is pre-configured in `.specify/`. Its constitution at `.specify/memory/constitution.md` automatically enforces RepoKit, host authority, and coding standards. Use `/speckit-specify`, `/speckit-plan`, and `/speckit-tasks` when developing features.
- Maintain documentation with implementation: update this file, `README.md`,
  `CHANGELOG.md`, and `ARCHITECTURE.md` when runtime or design facts change.

Before the first edit of every task, synchronize
`external/RepoKit` once and follow its synchronization gate. Read
its `VERSION.md`, `REPO_MODS_WORKSPACE.md`, and
`REPO_MODS_METHODOLOGY.md`; do not repeat that check during the same task
unless shared guidance changes.
