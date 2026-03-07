# Slot-Locked Equip Buttons Design

**Date:** 2026-03-06
**Status:** Approved

## Goal
Replace unclear equip checkbox flow with explicit equip/unequip buttons and faster stash-to-on-person gearing while enforcing one equipped item per slot.

## Slot Rules
- Slots with strict equip limit: `Weapon`, `Armor`, `Backpack`.
- Exactly one equipped item per slot at a time.
- Equipping an on-person item auto-unequips the currently equipped item in that slot.

## On Person UX
- Remove checkbox.
- For slotted items show explicit button:
  - `Equip` if item is not equipped
  - `Unequip` if item is equipped
- Non-slotted items have no equip toggle.

## Stash UX
- Slotted stash items:
  - if no equipped item in that slot: button label `Equip` (move to on-person and equip immediately)
  - if slot already has equipped item: button label `On Person` (move only, unequipped)
- Non-slotted stash items always show `On Person`.

## Raid Gate
- Existing gate remains:
  - blocked when any on-person item is unequipped
  - blocked when no weapon is equipped

## Persistence
- Keep existing on-person item model with `IsEquipped`.
- Persist equip state after every relevant action.

## Verification
- Build/tests pass.
- Manual checks:
  1. One equipped per slot.
  2. Equip auto-swaps in same slot.
  3. Stash label logic updates by slot state.
  4. Checkbox removed, button labels explicit.
