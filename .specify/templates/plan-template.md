# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [specs/[###-feature-name]/spec.md]

**Input**: Feature specification from `specs/[###-feature-name]/spec.md`

## Summary

[Extract from feature spec: primary requirement + technical approach, lifecycle hooks, and multiplayer authority]

## Technical Context

**Language/Version**: C# 12 / `.NET Framework 4.8` (`net48`)
**Primary Dependencies**:
- BepInEx 5.4.x / HarmonyX 2.12.x
- Unity 2022.3.x assemblies (`UnityEngine.CoreModule`, `UnityEngine.PhysicsModule`)
- `Assembly-CSharp.dll` (Game binary)
- `RepoAPI` (`external/RepoAPI` submodule, modular `<Compile Include>`)
**Target Platform**: Windows 64-bit / R.E.P.O.
**Project Type**: BepInEx Game Mod (`.dll`)
**Performance Goals**: Zero runtime GC allocations in per-frame/Update loops; lightweight lifecycle hooks.
**Constraints**:
- Host-only spawn authority (`SemiFunc.IsMasterClientOrSingleplayer()`) for networked state mutations.
- Strict modern C# standard (§6: zero Hungarian / underscore / `s_` prefixes).
- Zero compiler warnings (`TreatWarningsAsErrors = true`).
- Scoping: `internal` by default, only `BaseUnityPlugin` entry point is `public`.

## Constitution Check

*GATE: Must pass before proceeding to task generation.*

- [ ] **I. RepoKit Alignment**: Complies with `REPO_MODS_WORKSPACE.md` and `REPO_MODS_METHODOLOGY.md`.
- [ ] **II. Host-Only Authority**: Uses `SemiFunc.RunIsLevel()` and `SemiFunc.IsMasterClientOrSingleplayer()` for game-state mutations.
- [ ] **III. Modern C# & Zero Legacy Prefixes**:
  - No leading underscores (`_field`).
  - No static prefixes (`s_field`).
  - Scoping: `internal sealed` or `internal static` for patches and helpers.
- [ ] **IV. Shared Code Hygiene (RepoAPI)**: Reusable logic consumed from `external/RepoAPI` via `<Compile Include>` in `.csproj`. No duplicate generic code.
- [ ] **V. 3-Tier Testing**: Tier 3 smoke testing via Steam / r2modman Debug profile (debug hotkeys strictly optional and disabled by default).

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── spec.md              # Requirements and user scenarios
├── plan.md              # This technical plan
└── tasks.md             # Ordered task breakdown
```

### Source Code (mod repository)

```text
[ModName]/
├── [ModName].csproj                     # Configured with RepoAPI compilation items
├── [ModName]Plugin.cs                   # BepInPlugin entry point, initializes config & Harmony
├── ConfigurationController.cs          # BepInEx ConfigFile wrapper for mod settings
├── Patches/
│   └── [TargetType]_[Method]_Patch.cs  # Harmony patch files (one per target method)
├── ARCHITECTURE.md                     # Synchronized architectural documentation
├── README.md                           # User-facing features and configuration docs
└── CHANGELOG.md                        # Version release notes
```

**Structure Decision**: Standard BepInEx mod repository with submodule references under `external/`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., Non-host execution] | [current need] | [why host authority insufficient] |
