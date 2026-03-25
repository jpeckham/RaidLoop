# Item Key Lookups And Shared Selection Helpers Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Reduce duplicate database lookup logic by introducing canonical item-definition helpers, shared weighted-selection helpers, and incremental internal use of `item_key` instead of mutable item names.

**Architecture:** Keep the current table-driven design and RPC surface intact while adding a thin helper layer in SQL. The first half of the work centralizes repeated weighted-pick SQL and repeated item-definition lookups; the second half incrementally switches authored generator paths to prefer `item_key` internally while preserving current JSON payload shapes for callers.

**Tech Stack:** Supabase SQL migrations, PostgreSQL PL/pgSQL, SQL helper functions, JSONB payload helpers, local Supabase CLI verification

## Task 1 Inventory Notes

### Duplicate-Pattern Checklist

- repeated `select ... from game.item_defs where name = ... and enabled` in `game.authored_item` and the item stat helpers currently redefined in `supabase/migrations/2026032301_add_item_defs_table.sql`
- repeated weighted running-sum selection CTEs in `game.random_enemy_loadout_from_table`, `game.random_loot_items_from_table`, and both extraction/default encounter branches in `game.generate_raid_encounter` from `supabase/migrations/2026032302_add_authored_loot_and_encounter_tables.sql`
- repeated joins from authored variant item tables back to `game.item_defs` in enemy-loadout and loot-table JSON generation in `supabase/migrations/2026032302_add_authored_loot_and_encounter_tables.sql`
- repeated name-based stat lookups in combat/action code, with the latest effective `game.perform_raid_action` definition still in `supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql`

### Latest Effective Runtime Files

`rg -n "create or replace function game\.(authored_item|weapon_magazine_capacity|backpack_capacity|weapon_armor_penetration|armor_damage_reduction|weapon_supports_single_shot|weapon_supports_burst_fire|weapon_supports_full_auto|weapon_burst_attack_penalty|roll_weapon_damage_d20|random_enemy_loadout_from_table|random_loot_items_from_table|generate_raid_encounter|perform_raid_action)" supabase/migrations` currently resolves the latest definitions to:

- `supabase/migrations/2026032301_add_item_defs_table.sql` for `game.authored_item` and the item stat helper layer
- `supabase/migrations/2026032302_add_authored_loot_and_encounter_tables.sql` for authored weighted selection and encounter generation
- `supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql` for the latest effective `game.perform_raid_action`

### Intended Helper Boundaries

- one canonical item-definition fetch path for enabled item rows by `name` and by `item_key`
- one narrowly shared weighted-entry selection layer for authored enemy loadouts, authored loot variants, and encounter entries
- no public RPC payload shape changes in this increment; helper refactors stay internal unless a payload adds a backward-compatible `itemKey`
---

### Task 1: Inventory Current Duplicate Lookup Patterns

**Files:**
- Modify: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/docs/plans/2026-03-23-item-key-lookups-and-shared-selection-helpers.md`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032301_add_item_defs_table.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032302_add_authored_loot_and_encounter_tables.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql`

**Step 1: Write the duplicate-pattern checklist**

Document the specific duplication to remove:

- repeated `select ... from game.item_defs where name = ... and enabled`
- repeated weighted-running-sum selection CTEs
- repeated joins from variant tables to `game.item_defs`
- repeated name-based stat lookups in combat/action code

**Step 2: Confirm the latest runtime files to change**

Run: `rg -n "create or replace function game\\.(authored_item|weapon_magazine_capacity|backpack_capacity|weapon_armor_penetration|armor_damage_reduction|weapon_supports_single_shot|weapon_supports_burst_fire|weapon_supports_full_auto|weapon_burst_attack_penalty|roll_weapon_damage_d20|random_enemy_loadout_from_table|random_loot_items_from_table|generate_raid_encounter|perform_raid_action)" supabase/migrations`

Expected: Identify the latest effective migration files containing the duplicated patterns.

**Step 3: Record intended helper boundaries**

Capture the target helper surface in the doc:

- one canonical item-definition fetch path
- one generic or narrowly shared weighted-entry selection path
- no behavior changes to public RPC interfaces in this increment

**Step 4: Commit**

```bash
git add docs/plans/2026-03-23-item-key-lookups-and-shared-selection-helpers.md
git commit -m "docs: inventory duplicate lookup patterns"
```

### Task 2: Add Canonical Item Definition Helpers

**Files:**
- Create: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032303_add_item_lookup_helpers.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032301_add_item_defs_table.sql`

**Step 1: Write the failing verification queries in migration notes**

Add example verification queries for:

- `game.item_def_by_name('AK74')`
- `game.item_def_by_key('ak74')`
- `game.item_key_for_name('AK74')`

Expected results:

- row exists and is enabled
- key lookup and name lookup resolve the same item

**Step 2: Add minimal helper functions**

Create canonical helpers such as:

```sql
game.item_def_by_name(item_name text)
game.item_def_by_key(def_item_key text)
game.item_key_for_name(item_name text)
```

Prefer SQL functions returning either `game.item_defs` row type or a stable JSON object. Keep the surface small and concrete.

**Step 3: Preserve compatibility semantics**

If current callers rely on unknown-name defaults, keep those defaults in the old public helper functions. The new canonical helpers may return `null` for unknown items.

**Step 4: Verify helper definitions exist**

Run: `rg -n "create or replace function game\\.(item_def_by_name|item_def_by_key|item_key_for_name)" supabase/migrations/2026032303_add_item_lookup_helpers.sql`

Expected: All canonical item lookup helpers are defined in the new migration.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032303_add_item_lookup_helpers.sql
git commit -m "feat: add canonical item lookup helpers"
```

### Task 3: Refactor Existing Item Stat Helpers To Use Canonical Lookups

**Files:**
- Modify: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032303_add_item_lookup_helpers.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032301_add_item_defs_table.sql`

**Step 1: Write parity checks**

Document expected results for:

- `game.weapon_magazine_capacity('PKP') = 100`
- `game.backpack_capacity('6Sh118') = 10`
- `game.weapon_supports_single_shot('PKP') = false`
- `game.weapon_burst_attack_penalty('Makarov') = 3`

**Step 2: Recreate the small stat helpers using the canonical item helpers**

Reduce duplicate direct table lookups by routing through `game.item_def_by_name(...)` or equivalent helper logic.

**Step 3: Keep fallback defaults in the public helper layer**

Continue returning the current defaults:

- unknown magazine capacity `8`
- unknown backpack capacity `2`
- unknown armor reduction `0`
- unknown burst penalty `3`

**Step 4: Verify duplicate direct lookups are reduced**

Run: `rg -n "from game\\.item_defs|where item_defs\\.name =" supabase/migrations/2026032303_add_item_lookup_helpers.sql`

Expected: direct item-def lookup duplication is confined to the canonical helper functions or meaningfully reduced.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032303_add_item_lookup_helpers.sql
git commit -m "refactor: route item stat helpers through canonical lookups"
```

### Task 4: Add Shared Weighted Selection Helpers

**Files:**
- Create: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032304_add_weighted_selection_helpers.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032302_add_authored_loot_and_encounter_tables.sql`

**Step 1: Write the target helper list**

Keep the helper layer narrow. Recommended helpers:

- `game.pick_enemy_loadout_variant(loadout_table_key text)`
- `game.pick_loot_variant(loot_table_key text)`
- `game.pick_encounter_entry(encounter_table_key text)`

Prefer this over an over-generalized dynamic SQL helper.

**Step 2: Implement the helpers**

Move the repeated running-weight/target-roll logic into those shared functions. Return either keys or row types, whichever keeps downstream functions simplest.

**Step 3: Add verification queries in comments or plan notes**

Example checks:

- `select game.pick_enemy_loadout_variant('default_enemy_loadout')`
- `select game.pick_loot_variant('weapons_crate')`
- `select (game.pick_encounter_entry('default_raid')).encounter_type`

**Step 4: Verify helper definitions exist**

Run: `rg -n "create or replace function game\\.(pick_enemy_loadout_variant|pick_loot_variant|pick_encounter_entry)" supabase/migrations/2026032304_add_weighted_selection_helpers.sql`

Expected: all three weighted-pick helpers exist.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032304_add_weighted_selection_helpers.sql
git commit -m "feat: add shared weighted selection helpers"
```

### Task 5: Refactor Authored Generators To Use Shared Helpers

**Files:**
- Modify: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032304_add_weighted_selection_helpers.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032302_add_authored_loot_and_encounter_tables.sql`

**Step 1: Write the exact refactor targets**

Refactor these functions:

- `game.random_enemy_loadout_from_table`
- `game.random_loot_items_from_table`
- `game.generate_raid_encounter`

**Step 2: Replace inline weighted-selection CTEs with the shared helpers**

Keep return shapes unchanged. Only the selection mechanism should change.

**Step 3: Verify the old repeated CTE pattern is removed**

Run: `rg -n "running_weight|target_roll|sum\\(.*weight\\) over" supabase/migrations/2026032304_add_weighted_selection_helpers.sql`

Expected: only the new weighted helper functions contain the running-weight implementation.

**Step 4: Commit**

```bash
git add supabase/migrations/2026032304_add_weighted_selection_helpers.sql
git commit -m "refactor: share weighted selection logic across authored generators"
```

### Task 6: Add Internal `item_key` Use To Authored Generator Paths

**Files:**
- Create: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032305_add_internal_item_key_usage.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032301_add_item_defs_table.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032302_add_authored_loot_and_encounter_tables.sql`

**Step 1: Define the compatibility rule**

Internal generated payloads may start carrying `itemKey`, but they must still keep the current display fields:

- `name`
- `type`
- `value`
- `slots`
- `rarity`
- `displayRarity`

Do not break existing JSON consumers in this increment.

**Step 2: Add a normalized item JSON helper if needed**

If it simplifies the change, add one helper such as:

```sql
game.authored_item_by_key(def_item_key text)
```

or a helper that returns the existing authored JSON shape plus `itemKey`.

**Step 3: Refactor authored generator output to use `item_key` internally**

Prefer table joins on `item_key` and emit `itemKey` in generated item payloads from:

- enemy loadout generation
- loot table generation

Avoid touching unrelated persisted save payload structures unless needed for parity.

**Step 4: Verify generated payload shape**

Run: `rg -n "itemKey|authored_item_by_key|jsonb_build_object\\(" supabase/migrations/2026032305_add_internal_item_key_usage.sql`

Expected: the migration explicitly adds internal key usage without removing current display fields.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032305_add_internal_item_key_usage.sql
git commit -m "refactor: add internal item keys to authored generator payloads"
```

### Task 7: Simplify Combat And Action Callers Incrementally

**Files:**
- Create: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032306_reduce_name_based_action_lookups.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032303_add_item_lookup_helpers.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032305_add_internal_item_key_usage.sql`

**Step 1: Identify the minimum safe cleanup in `game.perform_raid_action`**

Do not rewrite the whole function. Only reduce repeated name-based stat lookups where it clearly lowers duplication.

Examples:

- resolve equipped weapon item def once
- resolve enemy weapon item def once
- resolve equipped armor item def once

**Step 2: Implement minimal helper-backed cleanup**

Refactor repeated stat lookups to use the new canonical item helpers or resolved local variables. Preserve all current behavior and text output.

**Step 3: Verify behavior-critical values still match**

Run direct SQL checks against the local DB after migration application for:

- `PKP` single-shot restriction
- `full-auto` and `burst-fire` support values
- armor reduction and penetration values used during combat

**Step 4: Commit**

```bash
git add supabase/migrations/2026032306_reduce_name_based_action_lookups.sql
git commit -m "refactor: reduce repeated action lookups with canonical item helpers"
```

### Task 8: Local Supabase Verification And Handoff Notes

**Files:**
- Modify: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/docs/plans/2026-03-23-item-key-lookups-and-shared-selection-helpers.md`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032303_add_item_lookup_helpers.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032304_add_weighted_selection_helpers.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032305_add_internal_item_key_usage.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/2026032306_reduce_name_based_action_lookups.sql`

**Step 1: Run migration application against local Supabase**

Run:

```bash
. .\env.local.ps1
npx supabase db reset --local --no-seed
```

Expected: migrations apply successfully. If the command times out on a non-database service after migration application, verify the DB state directly before retrying.

**Step 2: Run focused SQL verification**

Run direct SQL checks through the local DB container or CLI-connected database for:

- canonical item lookup helper results
- weighted helper execution
- generated enemy and loot payloads
- `game.generate_raid_encounter(...)`

Expected: helpers execute and return valid rows/payloads.

**Step 3: Run local lint**

Run:

```bash
. .\env.local.ps1
npx supabase db lint
```

Expected: no new SQL errors introduced by this increment. Existing unrelated warnings may remain.

**Step 4: Write handoff notes**

Document:

- which functions now centralize item lookup
- which functions now centralize weighted selection
- whether generated JSON now includes `itemKey`
- any remaining name-based runtime lookups left intentionally untouched
- any follow-up step for broader `item_key` adoption in persisted gameplay payloads

**Step 5: Commit**

```bash
git add docs/plans/2026-03-23-item-key-lookups-and-shared-selection-helpers.md supabase/migrations/2026032303_add_item_lookup_helpers.sql supabase/migrations/2026032304_add_weighted_selection_helpers.sql supabase/migrations/2026032305_add_internal_item_key_usage.sql supabase/migrations/2026032306_reduce_name_based_action_lookups.sql
git commit -m "docs: hand off item key lookup and selection helper refactor"
```
