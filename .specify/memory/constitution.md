# Pockets Constitution

This constitution establishes the governing principles for all specifications, architectural plans, and implementations in this repository.

## Core Principles

### I. RepoKit Alignment (NON-NEGOTIABLE)
All development must strictly adhere to the canonical workspace guidance in `external/RepoKit/`:
- `external/RepoKit/REPO_MODS_WORKSPACE.md` (Multi-agent coordination protocol and workspace layout)
- `external/RepoKit/REPO_MODS_METHODOLOGY.md` (Engineering standards, §6 Coding standards, §8 Testing)
- Local `ARCHITECTURE.md` and `AGENTS.md`

### II. Host-Only Authority & Multiplayer Safety
R.E.P.O. is a multiplayer game running on Photon PUN 2. All game-state mutations, item spawning, reward grants, and drop calculations must be executed with host authority:
```csharp
if (!SemiFunc.RunIsLevel()) return;
if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
```
Clients must receive state via Photon synchronization rather than local execution to prevent desynchronization.

### III. Modern C# & Zero Legacy Prefixes
Follow §6 of `REPO_MODS_METHODOLOGY.md`:
- **No leading underscores (`_`):** Use `logger`, `config`, `itemTable` (never `_logger` or `_config`).
- **No static prefixes (`s_`):** Use `dropsThisLevel`, `cachedInstance` (never `s_dropsThisLevel` or `sInstance`).
- **No type prefixes:** Avoid `strName`, `bEnabled`, `iCount`.
- **Scoping:** All classes, structs, helpers, and patches must be `internal` or `private`. Only the `[BepInPlugin]` entry point is `public`.
- **C# Idioms:** Target `.NET Framework 4.8` via `Directory.Build.props` with `LangVersion latest` and `Nullable enable`. Use expression-bodied members and modern pattern matching.

### IV. Shared Code Hygiene (RepoAPI)
Reusable logic belongs in `RepoAPI`, not duplicated across mods. Consume modules via git submodule in `external/RepoAPI` with `<Compile Include>` in `.csproj`. Never fork or copy generic helper classes locally.

### V. 3-Tier Testing
Follow §8 of `REPO_MODS_METHODOLOGY.md`:
1. **Tier 1 (Unit tests):** Decoupled unit tests via `dotnet test` for pure logic and math.
2. **Tier 2 (Harmony verification):** Reflection tests to verify that patch targets exist in `Assembly-CSharp.dll` before deploying.
3. **Tier 3 (In-game smoke test):** Normal gameplay observation via r2modman debug profile. Custom debug hotkeys or triggers are strictly optional, opt-in, and disabled by default.

### VI. Audience Separation & Author Identity
- **Author vs. Technical ID:** Visible author credits in `README.md` and `<Authors>` in `.csproj` must always be `Osmar Briones`. Reverse-DNS `com.osmar` is strictly reserved for technical identifiers (`PluginGuid`, config files `com.osmar.<ModName>.cfg`, and namespaces).
- **Player-Friendly Features:** `README.md` is Thunderstore player-facing documentation. The `## Features` section must be written from the player's perspective, focusing on gameplay experience, balance, and mechanics in accessible language. Internal Unity hooks (`EnemyDirector.Start`, `TruckSafetySpawnPoint`, etc.), Harmony patches, and code architecture are strictly forbidden in `README.md` and belong in `ARCHITECTURE.md`.

## Governance
This constitution supersedes ad-hoc prompt instructions. Any planned feature, specification, or code change must verify compliance with these principles.

**Version**: 1.0.0 | **Ratified**: 2026-09-25
