# Implementation Plan: Pockets Storage

**Branch**: `001-pockets-storage` | **Date**: 2026-11-02 | **Spec**: [specs/001-pockets-storage/spec.md](spec.md)

**Input**: Feature specification from `specs/001-pockets-storage/spec.md`

## Summary

Pockets lets a player store a held valuable in a free vanilla inventory slot and take it out again with the same slot key.

The vanilla inventory only accepts `ItemEquippable` items, and valuables are not items, so Pockets adds a **parallel pocket layer that reuses the three vanilla slots**:

- A per-valuable runtime component carries the store and release RPCs and hides the object with the same technique the game uses for equipped items (shrink, colliders off, grab released).
- Harmony patches on `InventorySpot` make a pocketed slot read as occupied, intercept the slot key, and draw the pocket on the slot.
- The host validates every request (size policy, free slot, ownership) and broadcasts the result, mirroring `ItemEquippable.RPC_RequestEquip`.
- A small public API (events and queries) lets `TalismanCore` consume the pockets without knowing how they are stored.

Multiplayer scope is Category 2: every player installs the mod.

## Technical Context

**Language/Version**: C# 12 / `.NET Framework 4.8` (`net48`)
**Primary Dependencies**:
- BepInEx 5.4.x / HarmonyX 2.12.x
- Unity 2022.3.x assemblies (`UnityEngine.CoreModule`, `UnityEngine.PhysicsModule`, `UnityEngine.UI`, `Unity.TextMeshPro`)
- `Assembly-CSharp.dll` (publicized, so private game members are reachable directly)
- Photon PUN 2 (`PhotonView`, `PunRPC`, custom properties)
- `RepoAPI`: not needed (see RepoAPI reuse decision)
**Target Platform**: Windows 64-bit / R.E.P.O.
**Project Type**: BepInEx game mod (`Pockets.dll`), also a **referenced dependency** of `TalismanCore`
**Performance Goals**: No per-frame allocations; no `FindObjectsOfType` in `Update`; the slot value text updates at most a few times per second.
**Constraints**:
- Strict modern C# (§6): no `_`/`s_` prefixes, `internal` by default; only the plugin class and the API surface are `public`.
- Zero compiler warnings (`TreatWarningsAsErrors = true`).
- The public API must not expose RepoAPI types (none are used anyway).

## Research Findings (decompiled game source)

| Area | Finding | Consequence |
|------|---------|-------------|
| Inventory | `Inventory.inventorySpots` has exactly 3 `InventorySpot`. `InventorySpot.AttemptEquipItem` only accepts `PhysGrabber.grabbedPhysGrabObject.GetComponent<ItemEquippable>()`. | A held valuable is ignored by vanilla; a patch must handle it. |
| Slot input | `InventorySpot.Update` calls private `HandleInput()` on `InputKey.Inventory1/2/3`. If `IsOccupied()` it calls `CurrentItem.RequestUnequip()`. | Prefix `HandleInput`; never let vanilla run on a pocketed slot (`CurrentItem` is null there). |
| Occupied state | `IsOccupied()` is `currentState == Occupied`. `Inventory.GetFirstFreeInventorySpotIndex` and `InventorySpotsOccupied` use it. | Postfix `IsOccupied()` so vanilla never equips into a pocketed slot. |
| CurrentItem null-safety | All other dereferences of `CurrentItem` (`UpdateUI`, `StateOccupied`, `Inventory.ForceUnequip`, `GetBatteryStateFromInventorySpot`, `ItemManager`) are null-guarded; the only unguarded one is `HandleInput` line 184. | A pocketed slot with `CurrentItem == null` is safe once `HandleInput` is intercepted. |
| Slot drawing | `UpdateUI` draws `CurrentItem`'s icon via `SetEmoji(sprite)`; `noItem` (TMP) is the empty label. | Postfix `UpdateUI`: set our own sprite and show the value in our own TMP clone. |
| Hiding technique | `ItemEquippable.AnimateEquip`: shrink to 0.01 scale, `collider.enabled = false`, `physGrabObject.OverrideDeactivate()`; released grabbers via `PhysGrabber.OverrideGrabRelease(viewId)`. `AnimateUnequip` restores scale, colliders, mass and teleports in front of the camera (`RayHitTestNew`). | Reuse the same steps on the valuable. |
| Valuable | `ValuableObject` has `volumeType` (Tiny, Small, Medium, Big, Wide, Tall, VeryTall), `dollarValueOriginal/Current`. Tiny and Small set `physGrabObject.clientNonKinematic = true`. | Size policy reads `volumeType`; physics ownership of Tiny/Small needs a spike. |
| **Haul leak** | `ValuableObject.Update` (master) adds the object to `RoundDirector.dollarHaulList` when any of `roomVolumeCheck.CurrentRooms` is an Extraction room. A hidden valuable left inside an extraction room would be extracted. | Patch private `AddToDollarHaulList` to skip pocketed objects; remove from the list on store; refuse to store when `committedToExtraction`. |
| Death | `PlayerAvatar.PlayerDeath(int)` / `PlayerDeathRPC` is the death entry point. | Drop pockets there, after raising a cancelable event for `TalismanCore` (Phone rescue). |
| RPC on runtime components | PUN finds RPC methods on `MonoBehaviour`s of the object that has the `PhotonView`; the cache must be refreshed after adding a component (`PhotonView.RefreshRpcMonoBehaviourCache`). | Add the component at runtime on every client and refresh. Needs a spike (see S2). |

## Architecture & Data Flow

```text
slot key pressed (local player)
  InventorySpot.HandleInput [Prefix]
    ├─ spot pocketed          -> PocketController.RequestRelease(spot)        -> consume key
    ├─ spot free + holding an eligible valuable
    │                          -> PocketController.RequestStore(spot, obj)    -> consume key
    └─ otherwise              -> vanilla HandleInput

RequestStore / RequestRelease -> RPC to MasterClient (or direct in singleplayer)
Master validates               -> broadcasts RPC_SetStored(playerViewId, spot, valuableViewId, stored)
Every client                   -> PocketedValuable applies hide/show, PocketState updates,
                                  InventorySpot UI refreshes, PocketsApi events fire
```

State per player: `PocketState` is a plain class (no Unity types) with 3 entries (valuable view id or none) and pure methods (`TryStore`, `TryRelease`, `Clear`), so it is unit-testable (Tier 1).

## Harmony Hooks

| Target | Kind | Purpose |
|--------|------|---------|
| `InventorySpot.HandleInput()` (private) | Prefix, `bool` | Intercept store, release; return `false` when handled. |
| `InventorySpot.IsOccupied()` | Postfix | Return `true` when a pocket holds this spot. |
| `InventorySpot.UpdateUI()` | Postfix | Draw the pocket icon and hide the empty label. |
| `InventorySpot.Update()` | Postfix | Refresh the value text (throttled). |
| `ValuableObject.Start()` | Postfix | Add `PocketedValuable` and refresh the RPC cache (all clients). |
| `ValuableObject.AddToDollarHaulList()` (private) | Prefix | Skip when pocketed (haul leak). |
| `PlayerAvatar.PlayerDeath(int)` | Prefix | Raise cancelable `PlayerDying`, then drop pockets if configured. |
| Level teardown (`LevelGenerator` start / `Inventory.Awake`) | Postfix | Clear all pocket state (nothing persists across levels). |

Exact signatures are confirmed in Tier 2 reflection tests; if a target changes with a game update the contract test fails with a clear message.

## Public API (consumed by TalismanCore)

`Pockets.Api.PocketsApi` (public, no Unity-game types beyond `ValuableObject`):

- `IReadOnlyList<ValuableObject?> GetStored(int playerViewId)`
- `event Action<PocketEvent> Stored`, `event Action<PocketEvent> Removed`
- `event Action<PlayerDyingEventArgs> PlayerDying` (cancelable: when cancelled, Pockets does not drop; the subscriber moves the valuables)
- `bool IsPocketed(ValuableObject valuable)`
- `bool TryRemove(int playerViewId, int spot, Vector3? at)` (used by Phone)

`Pockets.dll` ships as a real DLL referenced by `TalismanCore` (not source-included), as decided for the family.

## Configuration (`BepInEx/config/com.osmar.Pockets.cfg`)

| Key | Type | Default | Notes |
|-----|------|---------|-------|
| `Enabled` | bool | true | Master switch. |
| `MaxValuableSize` | enum | Tiny | Largest accepted `ValuableVolume.Type` by the game's enum order; the **host's value is authoritative**. |
| `DropOnDeath` | bool | true | Host-authoritative. |
| `ShowSlotValue` | bool | true | Local only: draw the value text on the slot. |

## Edge Cases and Safety

- Refuse to store: object not a `ValuableObject`; over `MaxValuableSize`; `committedToExtraction`; held by another player, a monster or a cart; player dead or downed; arena or shop run; all slots occupied.
- Store only while a level is running (`SemiFunc.RunIsLevel()`); pockets are not available in lobby or shop.
- Player disconnects or dies: pockets drop where the player stood (or are cancelled and handled by `PlayerDying` subscribers).
- Late joiner: receives the full pocket state of every player (mechanism chosen in S2).
- Level change: valuables are scene objects and are destroyed with the level; pocket state is cleared on level start. Nothing is written to `StatsManager`, so nothing persists.
- Release position: raycast from the camera as `ItemEquippable.RayHitTestNew`; fall back to in front of the player if blocked.

## Risks and Spikes (to resolve before or while implementing)

| ID | Question | Why it matters | Plan |
|----|----------|----------------|------|
| S1 | Does the vanilla hide technique (shrink, colliders off, `OverrideDeactivate`) leave a valuable inert and recoverable? | Core of the feature. | Prototype on a Tiny valuable; log rigidbody, haul list and room state. |
| S2 | Best way to sync pocket state to late joiners: Photon player custom properties vs buffered RPC? And can the master write another player's properties? | Desync is the main failure mode. | Try custom properties first; fall back to buffered RPC with explicit removal. |
| S3 | Runtime `AddComponent` with `[PunRPC]` plus `RefreshRpcMonoBehaviourCache` on a scene-spawned valuable. | RPC transport. | Fall back to `PhotonNetwork.RaiseEvent` with a custom event code if it misbehaves. |
| S4 | `clientNonKinematic` on Tiny/Small: who simulates the object while hidden? | Prevents the hidden object from drifting or colliding. | Force kinematic on store, restore on release; verify on a client. |
| S5 | Does anything else iterate valuables and dereference colliders or renderers (e.g. `ValuableDirector`, `RoundDirector`, extraction tracking)? | Hidden objects might throw. | Grep and smoke test with a pocketed valuable during extraction. |

## Known Pitfalls Check

Against `external/RepoKit/references/known-pitfalls.md`.

| Entry | Relevant? | Safeguard or reason |
|-------|-----------|---------------------|
| P-001 Mod-spawned items grow the item lists | No | Pockets spawns nothing and never writes to `StatsManager`; valuables are scene objects that disappear with the level, so there is no persistent list to grow. |
| P-002 Shop items inherit a battery | Partly | No items or batteries are created. The only touchpoint is the slot's battery display: a pocketed slot stays in vanilla's `Empty` state, so no battery bars show. Verify in game that a slot that held a battery item and then a pocketed valuable shows no bars (QA battery, section D). |
| P-003 Singleplayer view ids are 0 | Yes | `PocketedValuable.Key` uses the view id only in multiplayer and the Unity instance id in singleplayer; unit tests accept 0 and negative keys. |
| P-004 A running game locks the deployed DLL | Yes (workflow) | Close the game before building and compare the deployed DLL with the build output before asking for an in-game test. |
| P-005 Hidden objects and extraction rooms | Yes | Haul list entry removed on store, `AddToDollarHaulList` guarded, store refused when `committedToExtraction`; confirmed with the spike tools (research S5). |

## Constitution Check

- [x] **I. RepoKit Alignment**: complies with `REPO_MODS_WORKSPACE.md` and `REPO_MODS_METHODOLOGY.md`; repository named `REPO-Pockets`, package `Pockets`.
- [ ] **II. Host-Only Authority**: not applicable as written; see Complexity Tracking. State mutations are still validated by the host.
- [x] **III. Modern C# & Zero Legacy Prefixes**: no `_`/`s_` prefixes; `internal sealed`/`internal static` except the plugin and API.
- [x] **IV. Shared Code Hygiene (RepoAPI)**: no duplicated generic code; no RepoAPI modules needed.
- [x] **V. 3-Tier Testing**: Tier 1 on `PocketState` and the size policy; Tier 2 reflection contract on every Harmony target; Tier 3 in-game checklist (store, release, extraction, death, late join).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Client-side input and UI (Category 2, all players install) | Storing is triggered by each player's own slot key and drawn on their own HUD; those are local by nature. | A host-only mod cannot intercept a client's key press nor draw on its HUD (fails the 4-Question Network Feasibility Gate, questions 1 and 2). A hybrid was also rejected: a client without the mod would see its slot as empty, could equip items into a slot that holds a pocket (the host can veto the request, but the player gets no feedback), and could not see the stored value or wear. Under the minimum install footprint rule those losses are functional, not cosmetic. |

## RepoAPI Reuse Decision

Nothing in this feature is generic enough to belong in `RepoAPI` yet. Candidate for later promotion once a second mod needs it: the per-player state sync helper (S2). Record it under "Promotion candidates" in `RepoAPI/README.md` if it stays in Pockets.

## Project Structure

```text
REPO-Pockets/
├── Pockets.csproj
├── PocketsPlugin.cs                    # BepInPlugin entry point, config, Harmony
├── Configuration/ConfigurationController.cs
├── Domain/
│   ├── PocketState.cs                  # pure state, Tier 1 testable
│   └── SizePolicy.cs                   # MaxValuableSize rule
├── Net/
│   ├── PocketedValuable.cs             # runtime component: RPCs + hide/show
│   └── PocketSync.cs                   # request/validate/broadcast, late join
├── Ui/PocketSlotView.cs                # icon sprite + value text on InventorySpot
├── Api/PocketsApi.cs                   # public surface for TalismanCore
├── Patches/
│   ├── InventorySpot_HandleInput_Patch.cs
│   ├── InventorySpot_Occupied_Patch.cs
│   ├── InventorySpot_UI_Patch.cs
│   ├── ValuableObject_Start_Patch.cs
│   ├── ValuableObject_AddToDollarHaulList_Patch.cs
│   └── PlayerAvatar_PlayerDeath_Patch.cs
├── Pockets.Tests/                      # Tier 1 and Tier 2
├── ARCHITECTURE.md  README.md  CHANGELOG.md
```

The template's dummy `Patches/EnemyDirector_Start_Patch.cs` is removed during implementation.
