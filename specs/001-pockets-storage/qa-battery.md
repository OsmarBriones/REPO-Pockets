# QA Battery: Pockets Storage

Generated from `spec.md`, `plan.md`, `ARCHITECTURE.md`, the configuration and the Harmony patches. Log lines are in `%APPDATA%\r2modmanPlus-local\REPO\profiles\Debug\BepInEx\LogOutput.log`. Set `[Debug] EnableSpikes = false` and keep the game closed while building.

## Tier 1: unit tests (automated)

- [x] `dotnet test Pockets.Tests/Pockets.Tests.csproj` passes (31 tests: 20 Tier 1 plus 11 Tier 2 contract tests).

## Tier 2: build and contract (automated)

- [x] `dotnet build -c Debug` and `dotnet build -c Release`: 0 errors, 0 warnings.
- [x] The deployed `Pockets.dll` in the r2modman Debug profile matches the build and contains the packaged icons.
- [x] `GameContractTests` confirms every patched or read game member exists (re-run after each game update).

## Tier 3: in-game (needs you)

### A. Baseline (mod off)
- [ ] Set `Enabled = false`. Hold a valuable and press a slot key: nothing happens, and equipping and unequipping items works as vanilla.

- [x] Known pitfall P-007 (log-confirmed 2026-11-04): with valuables in two or three slots, set `Enabled = false` (in the config menu, or edit the file and change level): the valuables drop side by side in front of you (not in your hands), the slots read as free, and a vanilla item can be equipped in them. Then set `Enabled = true` again and store one: it works.

### B. Happy path (singleplayer is enough)
- [x] Store a Tiny valuable in each of the three slots (log: `Stored ... in slot N`).
- [x] Take one out: it appears in front of you, in your hands (log: `Released ...`).
- [x] The slot shows the valuable's picture; the value reads on one line (`$600`, `$2.8K`).
- [ ] Optional (spike S5 already confirmed the haul list behavior with the prototype code): store a valuable inside the extraction zone and let the extraction finish: it is not counted. Take it out inside the zone: it is counted again. Cover it in a normal run rather than as a separate test.
- [x] Level change: end a level with a valuable in a pocket; in the next level the slot is empty and the valuable is gone.

### C. Configuration
- [ ] `ShowSlotValue = false`: the slot shows only the picture.
- [ ] `MaxValuableSize = Small` (then `VeryTall`): bigger valuables now store; a larger one than the limit does not (log: `Store refused ... over the limit`).
- [ ] `DropOnDeath = false`: after dying, the valuables stay hidden and are lost at the end of the level.
- [ ] Settings reload on the next level without restarting the game.

### D. Edge cases
- [ ] Hold a valuable against a wall, store it, take it out: it appears in front of the wall, not inside.
- [ ] All three slots full: a fourth valuable is not stored.
- [ ] Equip a vanilla item in a free slot, then store valuables in the others; both keep working.
- [ ] Known pitfall P-002: equip a vanilla item that has a battery bar in a slot, unequip it, then store a valuable in that same slot: the slot shows no battery bars.
- [ ] With a valuable pocketed in slot 1, hold an item and press key 1: the item is not equipped there.
- [x] Die with valuables in your pockets (`DropOnDeath = true`): they drop where you fell and can be picked up.
- [ ] Drop a valuable into a death pit, a cart or the extraction machine, then try to store another: no errors.

### E. Multiplayer (needs a second player; closes spikes S2, S3, S4)
- [ ] Host and client both have the mod. The client stores a valuable: it disappears for the host too, and the host cannot grab it.
- [ ] The host stores one: the client sees it disappear, and it does not drift or fall (hidden physics, spike S4).
- [ ] The client takes it out: it appears for both in front of the client's camera, and the client holds it.
- [ ] The client changes `MaxValuableSize`/`DropOnDeath` locally: the host's values still apply.
- [ ] The client disconnects with a valuable pocketed: it drops where it was hidden.
- [ ] A player joins after another has valuables pocketed: they see them hidden (spike S2).
- [ ] The client dies with valuables: they drop where the client fell.

### F. Log audit
- [x] `Pockets 1.0.0 loaded!` at startup.
- [x] `Icon for <name>: packaged` for every stored valuable type (never `rendered` or `generic` for the base game's valuables).
- [x] No `Exception`, `NullReference` or `Could not register ... ignoring the store` lines after a full session.
