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

Result: _pending_

## S3: runtime component with RPC (host plus client)

Steps: host and client in a level; the host grabs a valuable and presses F6; then the client does the same with another valuable.

Expected: both consoles show `[Spike S3] hide RPC received on host` and `... on client` for the same view id, and the object is hidden on both machines. Showing it again behaves the same way.

Result: _pending_

## S4: who simulates a hidden Tiny/Small valuable

Steps: hide a Tiny valuable from the host, press F7 on the host and on the client.

Expected: `kinematic=True` on both, and the hidden object does not drift or fall. Repeat after showing it: it falls and behaves like a normal valuable on both machines (`clientNonKinematic` is logged for reference).

Result: _pending_

## S5: haul leak and other valuable iterators

Steps: carry a valuable into the extraction room, press F6 there, wait a few seconds, press F7; also let an extraction point run while it is hidden.

Expected: F7 shows `inExtractionRoom=True` but `inHaulList=False`, the log shows `[Spike S5] blocked haul list add ...`, and no exceptions appear in the log from other valuable code.

Result: _pending_

## S2: late-join sync

Steps: the host hides a valuable (F6) and presses F8; a third instance joins late and presses F8.

Expected for the buffered-RPC path: the late joiner sees the valuable already hidden. Expected for the property path: the late joiner's F8 list shows the host's `pockets.spike` value. Then the host presses F9 on a client to learn whether the master can write another player's property (`SetCustomProperties returned True` and the value arrives).

Decision: _pending_ (buffered RPC vs player custom properties)
