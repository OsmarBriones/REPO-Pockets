# Research: Technical Spikes

Outcomes of the five spikes listed in `plan.md`. The prototype code lives in `Spikes/` and is removed once this file is filled in (task T010).

## How to run the spikes

1. Build (`dotnet build`); the post-build step deploys `Pockets.dll` to the Steam plugins folder and to the r2modman `Debug` profile.
2. Start the game once so BepInEx creates `BepInEx/config/com.osmar.Pockets.cfg`, close it, and set `[Debug] EnableSpikes = true`. Hotkeys are developer-only and disabled by default.
3. Read results in the BepInEx console or `BepInEx/LogOutput.log`; every spike line starts with `[Spike ...]`.
4. For S2 and S3 you need a second game instance as a client (host in one, client in the other).

Hotkeys: **F6** hide the held valuable (or show the last hidden one in front of the camera), **F7** dump the state of the held or last hidden valuable, **F8** publish the hidden ids as a player property and list every player's property, **F9** (host) try to write another player's property.

## S1: hide technique on a valuable (singleplayer is enough)

Steps: start a level, grab a Tiny valuable, press F6, press F7, walk around, press F6 again.

Expected: after F6 the valuable disappears and the grab is released; F7 shows `hidden=True`, scale about 0.01, `colliders=0/n`, `kinematic=True`; after the second F6 it reappears in front of the camera with `hidden=False`, original scale and colliders, and the same `value`.

Result (2026-11-02, singleplayer, two valuables: Museum Tooth, Museum Fish): **works**. Hide and show each ran twice per valuable, the RPC component was reached, the valuable disappeared and came back, and the log has no exceptions. Not confirmed: the F7 state dump never appeared in the log (no `[Spike dump]` line at all), so scale, colliders and kinematic state were not measured; a re-run with key logging is queued. Findings:
- After showing, the valuable **fell to the floor instead of landing in the player's hands**. Vanilla items are re-grabbed automatically (`ItemEquippable.ForceGrab`, 1.5 s); Pockets must do the same (task T021).
- The first show position was `camera + forward * 1.5` with no raycast. Facing a wall closer than that, the valuable ended up inside or beyond the wall and looked like it vanished. Use the vanilla `RayHitTestNew` approach: raycast, then `hit.point + hit.normal * bounds extents` (now in the spike).

Second run (updated spike, singleplayer, Wizard Bird Skull, Tiny, value 500): **confirmed**.
- Hidden: `hidden=True scale=0.010 colliders=0/4 kinematic=True grabbers=0` (the grab was released).
- Shown: `hidden=False scale=1.000 colliders=4/4 kinematic=False grabbers=1` (the player was re-grabbing it, so the auto-grab works).
- The dollar value stayed at 500 through hide and show; the cycle also repeated cleanly a second time.

Result: S1 **confirmed** (singleplayer).

## S3: runtime component with RPC (host plus client)

Steps: host and client in a level; the host grabs a valuable and presses F6; then the client does the same with another valuable.

Expected: both consoles show `[Spike S3] hide RPC received on host` and `... on client` for the same view id, and the object is hidden on both machines. Showing it again behaves the same way.

Result so far: in singleplayer the RPC methods on the runtime-added component ran (`view 0`). Later, hosting a multiplayer lobby alone, the `AllBuffered` RPC reached the component through the server with a real view id: `hide RPC received on host for view 92 from borenfenix`. That confirms PUN finds `[PunRPC]` methods on a component added at runtime after `RefreshRpcMonoBehaviourCache`. **Delivery to a remote client is still untested**, so S3 stays open.

## S4: who simulates a hidden Tiny/Small valuable

Steps: hide a Tiny valuable from the host, press F7 on the host and on the client.

Expected: `kinematic=True` on both, and the hidden object does not drift or fall. Repeat after showing it: it falls and behaves like a normal valuable on both machines (`clientNonKinematic` is logged for reference).

Result: singleplayer only. `clientNonKinematic=True` for this Tiny valuable (as read in the code); forcing `kinematic=True` while hidden and restoring it to `False` afterwards worked on the host. The client side is not tested, so S4 is still open.

## S5: haul leak and other valuable iterators

Steps: carry a valuable into the extraction room, press F6 there, wait a few seconds, press F7; also let an extraction point run while it is hidden.

Expected: F7 shows `inExtractionRoom=True` but `inHaulList=False`, the log shows `[Spike S5] blocked haul list add ...`, and no exceptions appear in the log from other valuable code.

Result (2026-11-02): **inconclusive**. No exceptions, but no F7 dump and no `[Spike S5] blocked` line. Likely cause: the valuable had already been added to the haul list (it polls every 0.5 s) before being hidden, so the add guard never fired. From reading `ValuableObject.Update`:
- Hiding must also **remove the valuable from the haul list** (now in the spike, master only, skipped when `committedToExtraction`).
- `inStartRoom` stays `true` after the guard blocks the add, so a valuable shown again inside the extraction room would never be re-added until it leaves and re-enters. Reset `inStartRoom = false` on release (now in the spike).
- A second run is needed with the updated spike: hide it inside the room, press F7, press F6 to show it still inside the room, press F7, wait about a second and check `inHaulList` becomes true again.

Second run (2026-11-02): still **inconclusive**. Every dump reads `inExtractionRoom=False` and `inHaulList=False`, so the valuable was not inside an extraction room (or its room list did not update, which is itself unknown for a hidden object). Next time confirm first, with F7 while the valuable is shown inside the zone, that `inExtractionRoom=True` before hiding it.

Third run (2026-11-02, host of a multiplayer lobby, Desert Golden Coin, Tiny, value 600): **confirmed, and the leak is smaller than feared**.
- Shown inside the zone: `inHaulList=True inExtractionRoom=True rooms=[Room Volume(extraction), Room Volume (1)] inStartRoom=True`.
- Hidden: `inHaulList=False rooms=[] inStartRoom=False`. With its colliders disabled the valuable has no room, so vanilla `ValuableObject.Update` itself removes it from the haul list.
- Shown again in the zone: `inHaulList=True`, `inExtractionRoom=True`, `inStartRoom=True` again, so vanilla adds it back by itself.
- No exceptions anywhere in the log.
Consequences: keep the explicit removal on store and the `AddToDollarHaulList` guard as cheap safety for the up to 0.5 s polling window, and refuse to store when `committedToExtraction`; the `inStartRoom` reset on release is harmless but probably unnecessary. A full extraction completing while a valuable is hidden was not run.

Result: S5 **confirmed**.

## S2: late-join sync

Steps: the host hides a valuable (F6) and presses F8; a third instance joins late and presses F8.

Expected for the buffered-RPC path: the late joiner sees the valuable already hidden. Expected for the property path: the late joiner's F8 list shows the host's `pockets.spike` value. Then the host presses F9 on a client to learn whether the master can write another player's property (`SetCustomProperties returned True` and the value arrives).

Result so far: in a lobby hosted alone, the local player property was written and read back (`pockets.spike=` after a second press; the first read returned `<none>` because the write is asynchronous). **Late joiner, remote client and master-writes-other-player were not tested.**

Decision: _pending_ (buffered RPC vs player custom properties)

## Other findings

- **Singleplayer view ids:** every valuable reports `PhotonView.ViewID = 0` in singleplayer (seen for Museum Tooth, Museum Fish and Wizard Bird Skull), while in a multiplayer lobby they have real ids (for example 92). The pocket layer therefore identifies a valuable by an opaque key supplied by the integration layer (view id in multiplayer, `GetInstanceID()` in singleplayer); the pure `PocketState` only requires it to differ from `NoValuable`.

## Pitfall candidates

Noted as they happened, with the verdict of the filter in `external/RepoKit/references/known-pitfalls.md`.

| Candidate | Verdict |
|-----------|---------|
| Singleplayer view ids are all 0 (found while reading logs, would have mixed valuables up) | Added as P-003 |
| A running game blocks the DLL copy, so tests used stale code | Added as P-004 |
| A hidden valuable inside an extraction room can reach the haul list | Added as P-005 |
| The Sunstone update added 6 Tiny valuables that the concept's 29-item table did not know | Added as P-006 |
| Unity's `ImageConversion.LoadImage` overloads need `ReadOnlySpan` on net48, and the image module needs netstandard 2.1 | Skip: the compiler reports it immediately, and the fix is in the project file |
| The value label wrapped onto two lines in a narrow text box | Skip: cosmetic, caught on first sight |
| Runtime-added `[PunRPC]` components need `RefreshRpcMonoBehaviourCache` | Skip for now: the call works (research S3), but missing it was never observed to fail, so the cause is unverified |
