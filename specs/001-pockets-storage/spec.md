# Feature Specification: Pockets Storage

**Feature Branch**: `001-pockets-storage`
**Status**: Draft
**Source concept**: `sandbox/PocketCharms_spec.md` (Pockets is a standalone mod; other mods, such as CharmCore, depend on it)

## Summary

Pockets is a standalone mod that lets a player store a held valuable in a free inventory slot and take it out again.
It is useful on its own and is the base dependency of CharmCore. It knows nothing about effects or wear.

## User Scenarios & Testing

### User Story 1 - Store a held valuable (Priority: P1)

As a player, I want to put the valuable I am holding into a free inventory slot so that I can carry it hands-free.

**Acceptance Scenarios**:

1. **Given** I hold a Tiny valuable and have a free slot, **When** I press that slot's key, **Then** the valuable is stored and the slot shows it.
2. **Given** I hold a valuable larger than the configured `MaxValuableSize`, **When** I press a free slot key, **Then** nothing is stored.
3. **Given** all slots are occupied, **When** I press a slot key while holding a valuable, **Then** nothing happens.

### User Story 2 - Take a valuable out (Priority: P1)

As a player, I want to press the key of an occupied slot to drop its valuable in front of me.

**Acceptance Scenarios**:

1. **Given** a stored valuable, **When** I press that slot's key, **Then** the valuable appears in front of me (never inside a wall), I am holding it, and the slot is free.

### User Story 3 - See the stored value (Priority: P2)

As a player with the mod, I want to see the stored valuable's current value on the slot; players without the mod read it by taking the valuable out.

### User Story 4 - Configure size and death behavior (Priority: P2)

As a player, I want to configure the largest storable size (default Tiny, up to all sizes) and whether valuables drop when I die.

## Requirements

- **FR-001**: Valuables up to `MaxValuableSize` (default Tiny) can be stored in free vanilla inventory slots.
- **FR-002**: Storing and taking out use the existing slot keys; no new keys.
- **FR-003**: Stored valuables are removed from the world and cannot count toward the haul until taken out.
- **FR-004**: Stored valuables are lost on level change.
- **FR-005**: On death, stored valuables are dropped where the player fell (`DropOnDeath`, default true).
- **FR-006**: The mod exposes events for stored and removed valuables for other mods (consumed by CharmCore).
- **FR-007**: Multiplayer scope is Category 2: all players install the mod. A player without it would lose functional things, not cosmetic ones (cannot store or release a valuable, cannot see which slot is occupied, and could try to equip items into a slot that holds a pocket), so the minimum install footprint rule does not allow a lighter scope.
- **FR-008**: Config file `BepInEx/config/com.osmar.Pockets.cfg` with `Enabled`, `MaxValuableSize`, `DropOnDeath`.

## Edge Cases

- Dead or downed player: stored valuables drop where the player fell.
- Level change: stored valuables are lost.
- Late joiner: the stored state of other players must be synchronized.
- A held valuable that is being grabbed by a monster cannot be stored.

## Success Criteria

- **SC-001**: A player can store and retrieve a valuable in a full level without desync between host and clients.
- **SC-002**: Players without the mod are not required to understand the slot display.
