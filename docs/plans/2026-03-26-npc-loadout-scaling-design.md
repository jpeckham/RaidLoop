# NPC Loadout Scaling Design

**Date:** 2026-03-26

**Goal:** Make combat NPCs scale by challenge through authored health, stats, and loadouts, while ensuring dropped loot exactly matches the NPC gear they entered combat with.

## Current State

The authoritative raid flow lives in Supabase SQL migrations and runtime functions, especially `game.generate_raid_encounter` and `game.perform_raid_action` in `supabase/migrations/2026032601_fix_challenge_distance_prod_upgrade.sql`.

NPCs already support:

- `enemyHealth`
- `enemyDexterity`
- `enemyLoadout`
- weapon-based outgoing damage
- armor-based incoming damage reduction
- loot drops sourced from `enemyLoadout` on death

NPCs do not yet support:

- challenge-driven authored stat scaling beyond health bands
- challenge-driven loadout table selection
- a fuller combat stat block such as constitution or strength
- active medkit use in combat

## Approved Direction

NPCs should:

- have challenge-based hit points
- have challenge-based combat stats
- have challenge-based gear
- drop the exact equipped/carried gear they spawned with

NPCs should not:

- actively consume medkits during combat

This keeps loot expectations stable and makes scaling easier to tune.

## Design

### Authoritative Data Model

Extend encounter authoring so combat entries can specify challenge-aware enemy stats and loadout selection. The encounter system should remain data-driven rather than adding hardcoded challenge cases inside combat resolution.

For combat encounters, authored data should include:

- a health range
- dexterity
- constitution
- optional strength for future melee/damage hooks
- an enemy loadout table key
- challenge band information so different tiers can be selected cleanly

The active raid payload should persist the realized NPC state for the current fight, including:

- `enemyHealth`
- `enemyDexterity`
- `enemyConstitution`
- `enemyStrength`
- `enemyLoadout`

This ensures combat and loot both use the same spawned enemy snapshot.

### Encounter Generation

`game.generate_raid_encounter` should use the current raid `challenge` value to choose an authored combat enemy profile appropriate to that tier. Challenge `0` should bias toward low-end loadouts such as:

- common-grade weapon
- optional common-grade armor
- optional medkit carried as loot only

Higher challenge values should progressively unlock stronger health ranges, better stats, and better loadout tables.

The selected enemy’s realized stats and loadout should be written directly into the raid payload when the encounter is created.

### Combat Resolution

`game.perform_raid_action` should read enemy stats from the raid payload rather than relying on partial defaults.

Initial implementation should wire in:

- `enemyDexterity` for hit/evasion as today
- `enemyConstitution` to support challenge-based max/current health generation
- `enemyStrength` persisted but unused unless there is already a clear combat hook

YAGNI applies here: if strength is not used by any current mechanic, persist it now but avoid inventing a new damage formula in the same change.

### Loot Rules

Enemy loot after death should continue to come from the enemy’s actual spawned inventory snapshot:

- equipped weapon
- equipped armor
- other carried loot
- medkits if they spawned carrying them

No fallback random drop generation should be needed when an enemy has a realized `enemyLoadout`. If a combat NPC somehow has an empty loadout, that should be treated as an authoring/data problem, not masked with unrelated random gear.

### Testing

Add coverage for:

- encounter generation producing challenge-appropriate enemy stats/loadout fields
- challenge `0` enemies drawing from low-tier loadouts
- enemy death converting actual `enemyLoadout` into discovered loot
- medkits remaining loot-only for NPCs
- raid payload projections preserving new enemy stat fields

## Risks

- Migration drift: the repo’s latest authoritative SQL lives in a large forward migration file, so edits must target the newest definitions rather than older superseded ones.
- Data tuning: challenge scaling can become too steep if health, dexterity, and gear all ramp at once.
- Backward compatibility: the client currently ignores most enemy stat fields, so new fields must not break existing snapshot parsing.

## Out of Scope

- NPC medkit consumption behavior
- new UI for displaying full enemy stats
- introducing new item categories or combat systems beyond the authored stat/loadout scaling
