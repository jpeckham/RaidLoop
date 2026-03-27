# Strength Encumbrance Design

## Goal

Add a strength-based encumbrance system that applies everywhere items are carried or equipped, while preserving existing backpack slot limits for raid storage volume.

## Approved Direction

Keep the current backpack slot model and add a separate weight model:

- `Slots` continue to represent storage volume inside carried raid loot.
- `Weight` becomes a new authored property on every item.
- `Strength` determines `MaxEncumbrance`.
- Current encumbrance is checked in pre-raid loadout management, shop purchases, raid looting, raid equipping, and luck run generation.

This keeps backpacks meaningful without overloading them as a substitute for character strength.

## Core Rules

- Every authored item has a `Weight`.
- `MaxEncumbrance` is derived from strength.
- Encumbrance includes:
  - equipped weapon
  - equipped armor
  - equipped backpack
  - carried raid loot
  - medkits
  - all pre-raid `On Person` items
- Actions that would exceed max encumbrance are blocked instead of partially applied.
- Backpack slot checks remain in place for raid carried loot volume.
- Weight checks are layered on top of existing slot checks.

## UI Behavior

- If an item cannot be looted due to weight, the `Loot` button is disabled the same way it is when the backpack is full.
- If an item cannot be equipped due to weight, the `Equip` button is disabled even if the slot is otherwise open.
- Pre-raid actions that move or add items into `On Person` are blocked when they would exceed max encumbrance.
- Weight display in the UI stays minimal:
  - show a compact readout like `40/100 lbs`
  - place it near the `On Person` inventory and raid inventory HUD rather than adding a new panel
- Existing backpack-full messaging stays, with weight-specific text added only where needed:
  - `Too heavy to carry.`
  - `Too heavy to equip.`

## Backend and Client Enforcement

- Client UI must gray out blocked actions before the player clicks them.
- Server-side raid action validation must enforce the same weight rules so client bypasses do not succeed.
- Shared encumbrance calculations should live in core logic so both client and backend can use the same rules.

## Luck Run Character Rules

- Encumbrance also applies to luck run characters.
- Luck run characters must never spawn overweight.
- Luck run generation should produce randomized stats, including strength, before finalizing inventory.
- If the initially rolled strength is too low for the generated legal loadout, the generator may raise strength until the loadout is valid rather than emitting an overweight character.

## Testing Requirements

- Strength to max-encumbrance scaling
- Item weight lookups
- Medkits contributing to encumbrance
- Raid loot blocked by weight
- Raid equip blocked by weight with an open slot
- Pre-raid move or buy blocked by weight
- Luck run generation always returning legal strength/loadout combinations
- Backend validation matching client gating
