# Changelog

## [Unreleased]

### Fixed
- A slot whose valuable was destroyed while pocketed no longer stays stuck: pressing its key frees it.

## [1.0.0] - 2026-11-06
- Store a held valuable in a free inventory slot with the slot key, and take it out with the same key (it ends up in your hands).
- A picture of each valuable (198 packaged icons) and its current value on the slot; `ShowSlotValue` turns the value off.
- Valuables in a pocket are kept out of the extraction haul list and are lost when the level ends.
- `MaxValuableSize` (default Tiny) and `DropOnDeath` are decided by the host; pocketed valuables drop when their owner dies or leaves.
- Public API for other mods (`PocketsApi`): read pockets, store and remove events, a cancelable death event and a way to require the slot value.
- Turning the mod off drops whatever is in your pockets (side by side in front of you); taking valuables out always works, only storing needs the mod enabled.
- Tier 1 and Tier 2 tests, and a developer-only icon baker that is off by default.
