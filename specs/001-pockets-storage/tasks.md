---
description: "Task list for the Pockets storage feature"
---

# Tasks: Pockets Storage

**Input**: Design documents from `specs/001-pockets-storage/` (`spec.md`, `plan.md`)
**Prerequisites**: `spec.md`, `plan.md`, `constitution.md`

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 store, US2 take out, US3 see the value, US4 configuration and death behavior
- Spike tasks (S1-S5) gate the rest: if one fails, stop and revise `plan.md` before continuing.

## Phase 1: Setup

**Purpose**: Clean the template and prepare references

- [x] T001 Remove the template placeholder `Patches/EnemyDirector_Start_Patch.cs` and any reference to it in `PocketsPlugin.cs`
- [x] T002 Add the Unity UI, TextMeshPro and Photon assembly references (`Private=false`) to `Pockets.csproj`; keep `Compile Remove="external\**"` and do not include RepoAPI modules
- [ ] T003 [P] Create the folder skeleton from the plan: `Configuration/`, `Domain/`, `Net/`, `Ui/`, `Api/`, `Patches/`
- [x] T004 [P] Create `Pockets.Tests/` (MSTest) following the layout used by the other mods; Tier 1 and Tier 2 live here

---

## Phase 2: Technical spikes (gate)

**Purpose**: Resolve the five risks in `plan.md` with throwaway prototypes behind an opt-in debug config (disabled by default)

- [ ] T005 S1 (code written in Spikes/, needs the game): prototype hiding a Tiny valuable (shrink, colliders off, `OverrideDeactivate`, release grabbers) and restoring it in front of the camera; log rigidbody, room and haul-list state
- [ ] T006 S3 (code written in Spikes/, needs host and client): prove a runtime-added `MonoBehaviour` with `[PunRPC]` plus `RefreshRpcMonoBehaviourCache` works on a scene-spawned valuable on host and client; if not, switch to `PhotonNetwork.RaiseEvent` with a custom event code
- [ ] T007 S4 (code written in Spikes/, needs host and client): check who simulates a hidden Tiny/Small valuable (`clientNonKinematic`) and that forcing kinematic on store and restoring on release is stable on a client
- [ ] T008 S5 (code written in Spikes/, needs the game): verify a pocketed valuable inside an extraction room is not added to `RoundDirector.dollarHaulList` and that nothing iterating valuables throws
- [ ] T009 S2 (code written in Spikes/, needs three instances): decide late-join sync (Photon player custom properties vs buffered RPC) and whether the master can write another player's state
- [ ] T010 Record the outcomes in `specs/001-pockets-storage/research.md` and update `plan.md` where a spike changed the design

---

## Phase 3: Foundations (config, domain, API)

**Purpose**: Pure logic and settings that every story needs

- [ ] T011 [US4] Implement `Configuration/ConfigurationController.cs` (`Enabled`, `MaxValuableSize`, `DropOnDeath`, `ShowSlotValue`) and wire it in `PocketsPlugin.cs`; the host's values are authoritative and sent to clients on join
- [ ] T012 [P] [US4] Implement `Domain/SizePolicy.cs` (accept by `ValuableVolume.Type` enum order up to `MaxValuableSize`) with Tier 1 tests in `Pockets.Tests/`
- [ ] T013 [P] Implement `Domain/PocketState.cs` (3 slots, `TryStore`, `TryRelease`, `Clear`, no Unity types) with Tier 1 tests
- [ ] T014 [P] Implement the public `Api/PocketsApi.cs` surface (`GetStored`, `IsPocketed`, `Stored`/`Removed` events, `PlayerDying`, `TryRemove`) over `PocketState`

---

## Phase 4: Store a valuable (Priority: P1) 🎯 MVP

**Goal**: User Story 1, storing a held valuable in a free slot

- [ ] T015 [US1] Implement `Net/PocketedValuable.cs` (runtime component: store/hide RPCs, kinematic handling per S4)
- [ ] T016 [US1] Implement `Patches/ValuableObject_Start_Patch.cs` (add `PocketedValuable` and refresh the RPC cache on every client)
- [ ] T017 [US1] Implement `Net/PocketSync.cs` request, host validation and broadcast; refuse non-valuable, oversize, `committedToExtraction`, held by another player or a monster or a cart, dead or downed player, arena or shop run, no free slot; active only when `SemiFunc.RunIsLevel()`
- [ ] T018 [US1] Implement the store branch of `Patches/InventorySpot_HandleInput_Patch.cs` (prefix, consume the key)
- [ ] T019 [US1] Implement `Patches/InventorySpot_Occupied_Patch.cs` so vanilla never equips into a pocketed slot
- [ ] T020 [US1] Implement `Patches/ValuableObject_AddToDollarHaulList_Patch.cs` and remove an already listed valuable from the haul list on store (haul leak)

---

## Phase 5: Take a valuable out (Priority: P1)

**Goal**: User Story 2

- [ ] T021 [US2] Implement the release flow in `Net/PocketSync.cs` and `Net/PocketedValuable.cs`: raycast position as in `ItemEquippable.RayHitTestNew`, restore scale, colliders, mass and physics, free the slot
- [ ] T022 [US2] Add the release branch to `Patches/InventorySpot_HandleInput_Patch.cs`

---

## Phase 6: Slot display (Priority: P2)

**Goal**: User Story 3

- [ ] T023 [US3] Implement `Ui/PocketSlotView.cs` (generated icon sprite and a cloned TMP label for the value)
- [ ] T024 [US3] Implement `Patches/InventorySpot_UI_Patch.cs` (`UpdateUI` and throttled `Update` postfixes, honoring `ShowSlotValue`)

---

## Phase 7: Death, level change and joining (Priority: P2)

**Goal**: User Story 4 and the lifecycle edge cases

- [ ] T025 [US4] Implement `Patches/PlayerAvatar_PlayerDeath_Patch.cs`: raise the cancelable `PlayerDying` event, then drop pockets at the player's position when `DropOnDeath` is on
- [ ] T026 [US4] Clear all pocket state on level start so nothing persists across levels
- [ ] T027 [US4] Sync the full pocket state to late joiners using the mechanism chosen in T009
- [ ] T028 [US4] Drop a player's pockets when they disconnect

---

## Phase 8: Verification, Documentation & Polish

**Purpose**: Quality assurance, zero-warning compilation and documentation

- [ ] T029 Tier 2: reflection contract tests for every Harmony target in the plan (`InventorySpot.HandleInput`, `IsOccupied`, `UpdateUI`, `Update`, `ValuableObject.Start`, `AddToDollarHaulList`, `PlayerAvatar.PlayerDeath`)
- [ ] T030 Build `dotnet build -c Debug` and `-c Release` with **0 warnings and 0 errors**
- [ ] T031 [P] Create `ARCHITECTURE.md` (pocket layer, RPC flow, hooks, API) and update `README.md` and `CHANGELOG.md` under `[Unreleased]` (no version bump)
- [ ] T032 [P] Record the RepoAPI reuse decision (nothing generic yet; S2 helper as a promotion candidate) in the completion summary and, if kept in the mod, in `RepoAPI/README.md`
- [ ] T033 Tier 3 smoke testing in the r2modman Debug profile with a second client (use the `generate-test-battery` skill): store, release, extraction with a pocketed valuable, death, late join, level change

---

## Dependencies

- Phase 2 gates Phases 3 to 7. T010 depends on T005-T009.
- Phase 3 (T011-T014) before Phases 4-7; T012, T013 and T014 are parallel.
- T015-T017 before T018-T020; T018 needs T019 to be safe to test.
- Phase 5 depends on Phase 4. Phase 6 depends on T019. Phase 7 depends on Phases 4 and 5.
- Phase 8 closes the feature; T029 can start as soon as the patches exist.
