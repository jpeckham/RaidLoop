# Data-Driven Item And Encounter Authoring Design

## Goal

Replace hard-coded item, weapon, armor, loot, and encounter authoring logic in Supabase PL/pgSQL with table-driven definitions so designers can expand and rebalance content without rewriting procedural SQL.

## Current State

The current backend already has a partial authored item catalog through `game.authored_item(item_name text)`, but runtime gameplay rules still depend on many separate `case` expressions and embedded constants:

- item metadata lives in `game.authored_item(...)`
- weapon magazine size lives in `game.weapon_magazine_capacity(...)`
- backpack size lives in `game.backpack_capacity(...)`
- weapon fire-mode support, armor penetration, burst penalties, and damage dice live in separate functions
- random enemy loadouts, container loot, encounter weights, and enemy name weighting live in procedural functions

This creates multiple sources of truth for the same item and balancing concepts. Adding a new weapon or armor currently requires touching several functions and knowing which constants must stay in sync.

## Design Summary

The refactor will happen in two phases.

### Phase 1: Canonical Item Definitions

Introduce a real `game.item_defs` table as the authoritative source for all static item data. Existing helper functions remain part of the gameplay API, but they become thin wrappers over table lookups instead of hard-coded `case` expressions.

The table should cover:

- identity and authoring: `item_key`, `name`, `item_type`
- inventory and economy: `value`, `slots`, `rarity`, `display_rarity`
- weapon tuning: `magazine_capacity`, `armor_penetration`, `supports_single_shot`, `supports_burst_fire`, `supports_full_auto`, `burst_attack_penalty`, `damage_die_size`
- armor tuning: `armor_damage_reduction`
- backpack tuning: `backpack_capacity`
- lifecycle metadata useful for designers later: `enabled`, `sort_order`, `notes`

The current functions that should read from `game.item_defs` after the migration:

- `game.authored_item(item_name text)`
- `game.weapon_magazine_capacity(weapon_name text)`
- `game.backpack_capacity(backpack_name text)`
- `game.weapon_armor_penetration(weapon_name text)`
- `game.armor_damage_reduction(armor_name text)`
- `game.weapon_supports_single_shot(weapon_name text)`
- `game.weapon_supports_burst_fire(weapon_name text)`
- `game.weapon_supports_full_auto(weapon_name text)`
- `game.weapon_burst_attack_penalty(weapon_name text)`
- `game.roll_weapon_damage_d20(weapon_name text, attack_mode text)`

This phase does not change the raid state machine. It changes where the state machine gets its static item facts.

### Phase 2: Loot And Encounter Authoring Tables

Once item definitions are table-backed, add authored content tables for random selection.

Recommended schema shape:

- `game.loot_tables`
- `game.loot_table_entries`
- `game.enemy_loadout_tables`
- `game.enemy_loadout_entries`
- `game.encounter_tables`
- `game.encounter_table_entries`

This separation keeps content normalized:

- loot tables define container drops and weighted outcomes
- enemy loadout tables define weighted enemy gear sets
- encounter tables define weighted encounter types by context

The procedural functions still own raid flow, raid payload mutation, and action branching, but random selection becomes table-driven. Designers can then add new containers, enemy archetypes, and encounter weights through row inserts and updates.

## Recommended Boundaries

Some logic should become data, and some should stay procedural.

Move to data:

- item stats and authored item metadata
- enemy loadout definitions
- loot container contents and weights
- encounter weights and contextual selection rules

Keep in code:

- `public.game_action(...)` action dispatch
- raid action control flow in `game.perform_raid_action(...)`
- session lifecycle in `game.start_raid_action(...)` and `game.finish_raid_session(...)`
- JSON payload normalization and persistence rules

This boundary keeps the system maintainable. Content becomes editable data; the gameplay engine remains explicit and reviewable code.

## Migration Strategy

### Step 1: Add `game.item_defs`

Create the table, seed it with the current items, and update helper functions to read from it. Preserve existing function names and argument contracts so callers do not need to change.

### Step 2: Verify Item Lookup Stability

Confirm that:

- default save payload still builds correctly
- shop items still resolve correctly
- raid snapshots still derive ammo and backpack capacity correctly
- combat functions still calculate weapon damage and armor reduction as before

### Step 3: Add Loot And Encounter Tables

Create the new authoring tables and seed them with the current random content definitions.

### Step 4: Refactor Random Generation Functions

Update:

- `game.random_enemy_loadout()`
- `game.random_loot_items_for_container(container_name text)`
- `game.generate_raid_encounter(...)`

These functions should query authored tables and preserve current probability behavior unless intentionally rebalanced.

## Risks

- The existing system uses item names as lookup keys. If names remain mutable designer-facing labels, runtime lookups become fragile. A stable internal `item_key` should back the table even if current functions still accept names for compatibility.
- Historical migrations duplicate older function versions. The refactor must target the latest effective definitions only.
- Random-content table design can become over-generalized. Phase 2 should model only the current needs first and avoid building a generic content engine prematurely.

## Testing Strategy

Phase 1 should verify parity of all item-derived helper functions against the current live item set.

Phase 2 should verify:

- weighted selection functions return only valid authored items and encounters
- empty or disabled tables fail predictably
- current gameplay flows still produce valid raid payloads

## Recommendation

Implement the table-backed item catalog first and stop there until parity is verified. Then layer loot and encounter authoring tables on top of that stable foundation. This keeps the refactor incremental, reduces regression risk, and creates the clean authoring model needed for future designer-driven content expansion.
