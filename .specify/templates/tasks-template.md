---
description: "Task list template for R.E.P.O. mod feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `specs/[###-feature-name]/` (`spec.md`, `plan.md`)
**Prerequisites**: `spec.md`, `plan.md`, `constitution.md`

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Phase 1: Setup & Foundational

**Purpose**: Build configuration, submodules, and mod base setup

- [ ] T001 Configure `[ModName].csproj` with required `RepoAPI` submodule items (`external/RepoAPI/...`)
- [ ] T002 [P] Clean or configure template placeholder patch `Patches/EnemyDirector_Start_Patch.cs`

---

## Phase 2: Configuration & Settings (Priority: P2)

**Goal**: Expose user-configurable options via BepInEx ConfigFile

- [ ] T003 [US2] Implement `ConfigurationController.cs` following §6 Modern C# standards (no `_`/`s_` prefixes, `internal sealed class`)
- [ ] T004 [US2] Wire configuration initialization into `[ModName]Plugin.cs`

---

## Phase 3: Gameplay Features & Patches (Priority: P1) 🎯 MVP

**Goal**: Implement core mod functionality with host authority and multiplayer safety

- [ ] T005 [US1] Implement `Patches/[TargetType]_[Method]_Patch.cs` with host authority checks (`SemiFunc.RunIsLevel()`, `SemiFunc.IsMasterClientOrSingleplayer()`) and appropriate lifecycle hook
- [ ] T006 [US1] Connect feature to `RepoAPI` helpers or game managers
- [ ] T007 [US1] Add structured logging via `[ModName]Plugin.Logger`

---

## Phase 4: Verification, Documentation & Polish

**Purpose**: Quality assurance, zero-warning compilation, and documentation updates per `RULE[user_global]`

- [ ] T008 Compile mod via `dotnet build [ModName].csproj` and ensure **0 warnings and 0 errors**
- [ ] T009 [P] Update `ARCHITECTURE.md` to reflect runtime flow and lifecycle hooks
- [ ] T010 [P] Update `README.md` and `CHANGELOG.md` with new features and config keys
- [ ] T011 Perform Tier 3 smoke testing via Steam or r2modman Debug profile
