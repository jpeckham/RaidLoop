# Consolidated Stash and On-Person Inventory Design

**Date:** 2026-03-06
**Status:** Approved

## Goal
Consolidate pre-raid equipped items and post-raid loot into one `On Person` area with per-item `Equipped` state, while keeping only two inventory areas total: `Stash` and `On Person`.

## Core Rules
- Only two inventory areas exist in base UI:
  - `Stash`
  - `On Person`
- `On Person` items have an `IsEquipped` flag.
- Raid loadout is built only from `On Person` items where `IsEquipped = true`.

## Enter Raid Gate (Recommended UX)
`Enter Raid` button is disabled until both conditions pass:
1. No unequipped items remain in `On Person`.
2. At least one equipped weapon exists.

Reason text shown under disabled button:
- `You need to move your unequipped items to stash or sell them.`
- `You don't have a weapon equipped.`

## Item Flow
- Shop purchases go to `On Person` as unequipped.
- Successful extraction returns items to `On Person` as unequipped.
- `On Person` item actions:
  - toggle `Equipped`
  - `Move to Stash` (respect stash cap)
  - `Sell`

## Data Model
- Replace plain `CharacterInventory` items with `OnPerson` entries:
  - `{ Item, IsEquipped }`
- Migration behavior:
  - existing character inventory items become `IsEquipped = false`.

## Constraints
- Stash cap remains enforced.
- Existing weapon fallback behavior remains, with exploit guard for fallback knife selling.

## Verification
- Build and tests pass.
- Manual checks:
  - enter raid disabled when on-person contains unequipped items.
  - enter raid disabled when no equipped weapon.
  - reason text matches the blocking condition.
