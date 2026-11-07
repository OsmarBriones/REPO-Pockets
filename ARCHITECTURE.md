# Pockets Architecture

Runtime structure, data flow and design decisions of the mod.

---

## High-Level Concept

Pockets lets a player store a held valuable in a free vanilla inventory slot and take it out again with the same slot key.

1. **Trigger:** the slot keys (Inventory1-3). `InventorySpot.HandleInput` is intercepted before vanilla.
2. **Authority / networking:** **Category 2, every player installs the mod.** Storing is triggered by each player's own key and drawn on their own HUD, so a vanilla client would lose functionality (an invisible occupied slot, silent conflicts). The master validates every request and broadcasts the result.
3. **Outcome:** the valuable is hidden, kept out of the extraction haul list and shown on the slot (picture and value) until it is taken out, dropped or lost.

Why a parallel layer: the vanilla inventory only accepts `ItemEquippable` items; valuables are not items. Pockets reuses the three vanilla slots but keeps its own state.

---

## Layers (see `specs/001-pockets-storage/plan.md` for the full rationale)

| Folder | Role |
|--------|------|
| `Domain/` | Pure logic without game types, linked into the unit tests: `PocketState` (three slots), `SizePolicy` and `PocketSize` (size limit, same order as the game's volume enum). |
| `Net/` | `PocketedValuable` (runtime component on every valuable: RPCs, hide and show), `PocketSync` (host validation), `PlayerKeys`, `HostSettings` (host's settings as room properties), `PocketsRoomEvents` (join, leave, master switch). |
| `Ui/` | `PocketSlotView` (picture and value on the slot), `ValuableIconRenderer` (packaged or rendered icons), `IconBaker` (developer tool). |
| `Api/` | The public surface for other mods: `PocketsApi`, `PocketEvent`, `PlayerDyingEventArgs`, plus the internal `PlayerPockets` registry. |
| `Patches/` | Thin Harmony patches that delegate to the layers above. |

---

## Main Data Flow

### Store
1. The local player holds a valuable and presses a free slot key. `InventorySpot_HandleInput_Patch` checks the vanilla slot is free, the valuable is acceptable (`PocketSync.IsSizeAccepted`) and rate-limits the key.
2. `PocketedValuable.RequestStore` sends `RequestStoreRpc` to the master (direct call in singleplayer).
3. The master runs `PocketSync.CanStore` (mod enabled, inside a level, player alive, slot free, size allowed, not committed to an extraction, held only by the requester) and broadcasts `StoredRpc`.
4. Every client runs `ApplyStored`: registers the valuable in `PocketsApi`'s registry, renders its slot picture (owner only, before hiding), hides it (grabs released, haul list entry removed, colliders off, kinematic, scale 0.01, deactivation re-asserted every frame), raises `PocketsApi.Stored` and refreshes the slot.

### Release
1. The owner presses the key of a pocketed slot. The patch never lets vanilla run on it.
2. `RequestRelease` picks a spot in front of the camera with a raycast (stays out of walls) and sends `RequestReleaseRpc`.
3. The master checks ownership and that the spot is near the player, then broadcasts `ReleasedRpc`.
4. Every client restores the valuable; the master teleports it to the spot; the owner auto-grabs it for 1.5 s (like `ItemEquippable.ForceGrab`).

### Switching the mod off
- `Enabled` is only a gate for storing. When it turns off (config menu or reload), `PocketReleaser` drops the local player's stored valuables side by side in front of them (not grabbed), through the normal release path, so nothing is left hidden. Taking out, the occupied-slot display and the slot picture keep working regardless, as a safety net.

### Death, disconnect, level change
- **Death:** after vanilla accepts `PlayerDeathRPC`, the master raises `PocketsApi.PlayerDying`. A subscriber can cancel it to handle the valuables itself (the Phone talisman); otherwise, with `DropOnDeath`, they are released where the player fell.
- **Disconnect:** the master releases the leaver's valuables where they were hidden.
- **Late join:** the master resends the stored state to the new player (`SendStateTo`).
- **Level change:** `EnemyDirector.Start` clears the registry. Valuables are scene objects, so nothing persists.

---

## Settings (`com.osmar.Pockets.cfg`)

`MaxValuableSize` and `DropOnDeath` belong to the host: it publishes them as room properties on level start, on join and when the master changes, and clients read them on demand (`HostSettings`). `ShowSlotValue` is local; `PocketsApi.RequireSlotValue` lets a dependent mod (TalismanCore) force it on. `Debug.EnableSpikes` and `Debug.EnableIconBake` are developer-only and off by default.

---

## Icons

Valuables have no vanilla icon. `ValuableIconRenderer` resolves, in order: a PNG packaged in the DLL (`assets/Icons/<Valuable Name>.png`, `Pockets.Icons.*` resources), a picture rendered at runtime with a throwaway camera and studio lighting, or a generic gem. `IconBaker` (F10, developer tool) renders every valuable prefab to `BepInEx/PocketIcons`; the PNGs are cropped and copied into `Assets/Icons`. Re-run it after a game update adds valuables.

---

## Tests

- **Tier 1:** `PocketState` and `SizePolicy` (`Pockets.Tests`).
- **Tier 2:** `GameContractTests` reads the game's `Assembly-CSharp.dll` as metadata and checks every member the mod patches or reads still exists; skipped when the game is not installed.
- **Tier 3:** in-game checklist, see `specs/001-pockets-storage/research.md` and `tasks.md`.

---

## Known gaps

- Multiplayer was verified with two real machines (host and client): stores by either side appear on the other, and the host releases a leaver's valuables where they were hidden. Not yet verified with two machines: taking a valuable out from the client, late join, the client changing its own settings, and the client dying.
- A valuable held by a monster is not specifically detected; the host only checks that the requesting player is the only grabber.
- Resending the stored state to a late joiner assumes the joiner already has the valuables loaded.

## RepoAPI reuse decision

Nothing here is generic enough for `RepoAPI` yet. Promotion candidate once another mod needs it: the per-player state sync helper (`HostSettings` plus `PocketsRoomEvents`).
