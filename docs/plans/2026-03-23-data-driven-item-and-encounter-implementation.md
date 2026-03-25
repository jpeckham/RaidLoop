# Data-Driven Item And Encounter Authoring Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Move hard-coded item, weapon, armor, loot, and encounter authoring rules into Supabase tables, implementing `game.item_defs` first and loot/encounter authoring tables second.

**Architecture:** Keep the raid state machine and RPC surface intact while replacing hard-coded lookup logic with seeded authoring tables. Phase 1 establishes `game.item_defs` as the canonical source for all static item facts; phase 2 adds weighted authoring tables for enemy loadouts, loot containers, and encounter selection.

**Tech Stack:** Supabase SQL migrations, PostgreSQL PL/pgSQL, SQL functions, JSONB payload helpers

---

### Task 1: Inventory Current Hard-Coded Authoring Rules

**Files:**
- Modify: `C:/Users/james/source/repos/extractor-shooter-light/docs/plans/2026-03-23-data-driven-item-and-encounter-implementation.md`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032010_rebalance_sell_prices.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026031807_game_raid_start_functions.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026031809_game_raid_action_functions.sql`

**Audit Checklist**

Track every current authored source that must be replaced or preserved:

- `game.authored_item`
- `game.shop_item`
- `game.default_save_payload`
- `game.random_luck_run_loadout`
- `game.random_enemy_loadout`
- `game.random_loot_items_for_container`
- `game.weapon_magazine_capacity`
- `game.backpack_capacity`
- `game.weapon_armor_penetration`
- `game.armor_damage_reduction`
- `game.weapon_supports_*`
- `game.weapon_burst_attack_penalty`
- `game.roll_weapon_damage_d20`
- `game.generate_raid_encounter`

**Latest Effective Definitions**

Use the latest runtime definitions, not historical duplicates:

- `game.authored_item` lives in [2026032010_rebalance_sell_prices.sql](C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032010_rebalance_sell_prices.sql)
- `game.weapon_magazine_capacity` and `game.backpack_capacity` live in [2026031807_game_raid_start_functions.sql](C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026031807_game_raid_start_functions.sql)
- `game.random_enemy_loadout` and `game.random_loot_items_for_container` live in [2026032111_fix_loot_rarity_weights.sql](C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032111_fix_loot_rarity_weights.sql)
- `game.generate_raid_encounter` lives in [2026031809_game_raid_action_functions.sql](C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026031809_game_raid_action_functions.sql)
- `game.weapon_armor_penetration`, `game.armor_damage_reduction`, `game.weapon_supports_*`, `game.weapon_burst_attack_penalty`, and `game.roll_weapon_damage_d20` live in [2026032205_remove_gun_malfunctions_and_clear_jams.sql](C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql)

**Compact Current Inventory**

Current authored item rows and values to seed into `game.item_defs`:

- Weapons: Rusty Knife `(type 0, value 1, slots 1, rarity 0, displayRarity 1)`, Makarov `(60, 1, 0, 1)`, PPSH `(160, 1, 1, 2)`, AK74 `(320, 1, 2, 3)`, AK47 `(375, 1, 2, 3)`, SVDS `(550, 1, 3, 4)`, PKP `(800, 1, 4, 5)`
- Armor: 6B2 body armor `(95, 1, 0, 1)`, 6B13 assault armor `(225, 1, 2, 3)`, FORT Defender-2 `(375, 1, 3, 4)`, 6B43 Zabralo-Sh body armor `(450, 1, 4, 5)`, NFM THOR `(650, 1, 4, 5)`
- Backpacks: Small Backpack `(25, 1, 1, 2)`, Tactical Backpack `(75, 2, 2, 3)`, Tasmanian Tiger Trooper 35 `(400, 3, 3, 4)`, 6Sh118 `(600, 4, 4, 5)`
- Consumables and loot: Medkit `(30, 1, 0, 1)`, Bandage `(15, 1, 0, 0)`, Ammo Box `(20, 1, 0, 0)`, Scrap Metal `(18, 1, 0, 0)`, Rare Scope `(80, 1, 2, 0)`, Legendary Trigger Group `(150, 1, 4, 0)`

Current non-item rule values to preserve:

- `game.weapon_magazine_capacity`: PPSH `35`, AK74 `30`, SVDS `20`, AK47 `30`, PKP `100`, Rusty Knife `0`, default `8`
- `game.backpack_capacity`: 6Sh118 `10`, Tasmanian Tiger Trooper 35 `8`, Tactical Backpack `6`, Small Backpack `3`, default `2`
- `game.weapon_armor_penetration`: Makarov `1`, PPSH `1`, AK74 `2`, AK47 `2`, SVDS `3`, default `0`
- `game.armor_damage_reduction`: NFM THOR `6`, 6B43 Zabralo-Sh body armor `5`, FORT Defender-2 `4`, 6B13 assault armor `3`, 6B2 body armor `1`, default `0`
- `game.weapon_supports_single_shot`: only PKP is blocked
- `game.weapon_supports_burst_fire`: Makarov, PPSH, AK74, AK47, SVDS, PKP are allowed
- `game.weapon_supports_full_auto`: PPSH, AK74, AK47, PKP are allowed
- `game.weapon_burst_attack_penalty`: Makarov `3`, PPSH `2`, AK74 `2`, AK47 `2`, SVDS `2`, PKP `2`, default `3`
- `game.roll_weapon_damage_d20`: die sizes are PPSH `4`, AK74 `8`, AK47 `10`, SVDS `12`, PKP `12`, Makarov `6`, default `6`; attack-mode die counts are `attack=2`, `burst-fire=3`, `full-auto=4`

Current content-generation weights/outcomes to preserve:

- `game.random_enemy_loadout`: weights are equivalent to `110` Makarov, `110` Bandage + 6B2 body armor, `110` 6B2 body armor, `60` PPSH + Bandage, `6` AK74 + 6B2 body armor, `6` AK47 + Bandage, `6` 6B13 assault armor, `3` SVDS, `3` FORT Defender-2, `3` PKP, `3` NFM THOR
- `game.random_loot_items_for_container('Weapons Crate')`: weights are equivalent to `40` Makarov + Ammo Box, `12` PPSH, `3` AK74, `3` AK47, `3` SVDS, `2` PKP
- `game.random_loot_items_for_container('Medical Container')`: weights are `4` Medkit + Bandage, `3` Bandage + Ammo Box, `2` Medkit, `1` Medkit + Ammo Box
- `game.random_loot_items_for_container('Dead Body')`: delegates to `game.random_enemy_loadout()`
- `game.random_loot_items_for_container(other)`: weights are equivalent to `40` split evenly across Bandage + Ammo Box, Scrap Metal, and Medkit, then `36` PPSH, `9` Rare Scope, `9` AK74, `9` SVDS, `6` Legendary Trigger Group
- `game.generate_raid_encounter`: extraction-route check first, then `roll < 50` combat, `roll < 80` loot, else neutral
- `game.generate_raid_encounter` combat enemy name weighting: Scav `65%`, Patrol Guard `35%`
- `game.generate_raid_encounter` combat HP range: `12 + floor(random() * 9)` so `12` through `20`
- `game.generate_raid_encounter` extraction container selection: uniform over Filing Cabinet, Weapons Crate, Medical Container, Dead Body
- `game.generate_raid_encounter` extraction trigger: `extractProgress >= extractRequired` and `random() < 0.35`

This phase does not change the raid state machine. It changes where the state machine gets its static item facts.

**Step 2: Confirm the latest effective function definitions**

Run: `rg -n "create or replace function (game\\.authored_item|game\\.weapon_magazine_capacity|game\\.backpack_capacity|game\\.weapon_armor_penetration|game\\.armor_damage_reduction|game\\.weapon_supports_single_shot|game\\.weapon_supports_burst_fire|game\\.weapon_supports_full_auto|game\\.weapon_burst_attack_penalty|game\\.roll_weapon_damage_d20|game\\.random_enemy_loadout|game\\.random_loot_items_for_container|game\\.generate_raid_encounter)" supabase/migrations`

Expected: A list of migrations showing where the latest replacements live so implementation touches only the final runtime definitions.

**Step 3: Record the extracted current values in the plan while implementing**

Document the item rows and weighted encounter/loot outcomes you find so parity checks have an explicit source.

**Step 4: Commit**

```bash
git add docs/plans/2026-03-23-data-driven-item-and-encounter-implementation.md
git commit -m "docs: inventory hard-coded game authoring rules"
```

### Task 2: Add `game.item_defs` Schema And Seed Data

**Files:**
- Create: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/20260323xx_add_item_defs_table.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032010_rebalance_sell_prices.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026031807_game_raid_start_functions.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql`

**Step 1: Write the failing verification query in the migration notes**

Define the expected row count and key columns for the current item set:

- one row per currently authored item
- unique stable `item_key`
- unique current `name`

Expected current authored items:

- Rusty Knife
- Makarov
- PPSH
- AK74
- SVDS
- AK47
- PKP
- 6B2 body armor
- 6B13 assault armor
- FORT Defender-2
- 6B43 Zabralo-Sh body armor
- NFM THOR
- Small Backpack
- Tactical Backpack
- Tasmanian Tiger Trooper 35
- 6Sh118
- Medkit
- Bandage
- Ammo Box
- Scrap Metal
- Rare Scope
- Legendary Trigger Group

**Step 2: Create the table**

Implement a migration that creates `game.item_defs` with columns like:

```sql
item_key text primary key,
name text not null unique,
item_type int not null,
value int not null,
slots int not null,
rarity int not null,
display_rarity int not null,
magazine_capacity int not null default 0,
backpack_capacity int not null default 0,
armor_damage_reduction int not null default 0,
armor_penetration int not null default 0,
supports_single_shot boolean not null default true,
supports_burst_fire boolean not null default false,
supports_full_auto boolean not null default false,
burst_attack_penalty int not null default 3,
damage_die_size int not null default 6,
enabled boolean not null default true,
sort_order int not null default 0,
notes text null
```

**Step 3: Seed the table with the current authored values**

Insert one row per current item, carrying over:

- economy and inventory values from `game.authored_item`
- magazine and backpack capacities from current helper functions
- armor and weapon combat stats from the current combat helper functions

**Step 4: Add basic constraints**

Include checks that prevent obviously invalid authored data, for example:

- non-negative value and slots
- non-negative capacities
- non-negative armor and penetration
- positive damage die size

**Step 5: Verify the migration shape**

Run: `rg -n "create table .*item_defs|insert into game\\.item_defs" supabase/migrations/20260323xx_add_item_defs_table.sql`

Expected: The migration defines the table and seed rows.

**Step 6: Commit**

```bash
git add supabase/migrations/20260323xx_add_item_defs_table.sql
git commit -m "feat: add canonical item definitions table"
```

### Task 3: Refactor Item Lookup Functions To Read From `game.item_defs`

**Files:**
- Modify: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/20260323xx_add_item_defs_table.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032010_rebalance_sell_prices.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026031807_game_raid_start_functions.sql`

**Step 1: Write parity expectations for each helper**

Document the expected lookups:

- `game.authored_item('AK74')` returns the same JSON shape as before
- `game.weapon_magazine_capacity('PKP')` returns `100`
- `game.backpack_capacity('6Sh118')` returns `10`
- `game.weapon_armor_penetration('SVDS')` returns `3`
- `game.armor_damage_reduction('NFM THOR')` returns `6`
- `game.weapon_supports_single_shot('PKP')` returns `false`
- `game.weapon_supports_full_auto('AK47')` returns `true`
- `game.weapon_burst_attack_penalty('Makarov')` returns `3`

**Step 2: Replace hard-coded `case` logic with table lookups**

Recreate the latest function definitions inside the new migration so they query `game.item_defs` by `name`.

Preferred shape:

```sql
select coalesce(column_name, default_value)
from game.item_defs
where name = lookup_name
  and enabled
limit 1
```

For `game.authored_item`, return a `jsonb_build_object(...)` assembled from the row.

**Step 3: Keep compatibility defaults**

Preserve current fallback behavior for unknown names where the existing functions rely on it, for example:

- unknown weapon magazine defaults
- unknown backpack capacity defaults
- unknown armor reduction defaults

Do not silently change gameplay semantics in this phase.

**Step 4: Run targeted verification queries**

Run commands such as:

```bash
rg -n "create or replace function game\\.(authored_item|weapon_magazine_capacity|backpack_capacity|weapon_armor_penetration|armor_damage_reduction|weapon_supports_single_shot|weapon_supports_burst_fire|weapon_supports_full_auto|weapon_burst_attack_penalty|roll_weapon_damage_d20)" supabase/migrations/20260323xx_add_item_defs_table.sql
```

Expected: Updated function definitions exist in the new migration and no longer depend on embedded item-specific `case` blocks.

**Step 5: Commit**

```bash
git add supabase/migrations/20260323xx_add_item_defs_table.sql
git commit -m "refactor: read item stats from item definitions"
```

### Task 4: Verify Phase 1 Gameplay Parity

**Files:**
- Modify: `C:/Users/james/source/repos/extractor-shooter-light/docs/plans/2026-03-23-data-driven-item-and-encounter-implementation.md`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/20260323xx_add_item_defs_table.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032202_add_dexterity_stats.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql`

**Step 1: Identify phase 1 callers that should remain untouched**

Callers relying on the refactored helper functions include:

- `game.default_save_payload`
- `game.shop_item`
- `game.random_luck_run_loadout`
- `game.build_raid_snapshot`
- `game.perform_raid_action`

**Step 2: Run repo-level validation searches**

Run: `rg -n "authored_item\\(|weapon_magazine_capacity\\(|backpack_capacity\\(|weapon_armor_penetration\\(|armor_damage_reduction\\(|weapon_supports_|weapon_burst_attack_penalty\\(|roll_weapon_damage_d20\\(" supabase/migrations`

Expected: Existing callers still use the same function names, meaning call sites stay stable.

**Step 3: Record verification outcomes**

Add a short verification note to this plan or a follow-up handoff doc describing whether:

- any default behaviors changed
- any values required special fallback handling
- any callers should later be simplified to query `item_defs` directly

**Verification Notes**

- `game.item_defs` was added in [20260323015_add_item_defs_table.sql](C:/Users/james/source/repos/extractor-shooter-light/.worktrees/data-driven-authoring/supabase/migrations/20260323015_add_item_defs_table.sql) with 22 seeded rows and parity-oriented fallback defaults.
- `game.authored_item`, `game.weapon_magazine_capacity`, `game.backpack_capacity`, `game.weapon_armor_penetration`, `game.armor_damage_reduction`, `game.weapon_supports_*`, `game.weapon_burst_attack_penalty`, and `game.roll_weapon_damage_d20` were redefined in the same migration to read from `game.item_defs`.
- Table-backed lookup functions that were previously marked `immutable` were downgraded to `stable` because they now depend on persisted row data.
- Current callers remain untouched and continue to use the same function names. This kept phase 1 isolated to schema and lookup logic.
- SQL text verification was completed with `rg` and `git diff`; no local Supabase SQL execution or migration application was run in this branch, so runtime validation is still pending.

**Step 4: Commit**

```bash
git add docs/plans/2026-03-23-data-driven-item-and-encounter-implementation.md supabase/migrations/20260323xx_add_item_defs_table.sql
git commit -m "test: verify item definition parity"
```

### Task 5: Add Loot, Enemy Loadout, And Encounter Authoring Tables

**Files:**
- Create: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/20260323yy_add_loot_and_encounter_tables.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032010_rebalance_sell_prices.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026031809_game_raid_action_functions.sql`

**Step 1: Write the desired authored table model**

Implement tables with the smallest useful scope:

- `game.enemy_loadout_tables`
- `game.enemy_loadout_entries`
- `game.loot_tables`
- `game.loot_table_entries`
- `game.encounter_tables`
- `game.encounter_table_entries`

**Step 2: Model weighted authoring**

Every entry table should support weighted selection using explicit numeric weights. Keep the schema concrete rather than generic.

Examples:

- enemy loadout entry points to one `item_key` plus quantity/order context
- loot table entry points to one `item_key` plus quantity/order context
- encounter table entry carries `encounter_type`, `weight`, and contextual settings like enemy name or container table

**Step 3: Seed current content into the new tables**

Seed the current authored outcomes from:

- `game.random_enemy_loadout()`
- `game.random_loot_items_for_container(...)`
- `game.generate_raid_encounter(...)`

Preserve current weights and current item combinations first. Rebalancing is out of scope for this migration.

**Step 4: Verify the authored rows exist**

Run: `rg -n "create table .*loot_tables|create table .*encounter_tables|insert into game\\.(loot_tables|loot_table_entries|enemy_loadout_tables|enemy_loadout_entries|encounter_tables|encounter_table_entries)" supabase/migrations/20260323yy_add_loot_and_encounter_tables.sql`

Expected: The migration contains both schema and seed rows.

**Step 5: Commit**

```bash
git add supabase/migrations/20260323yy_add_loot_and_encounter_tables.sql
git commit -m "feat: add authored loot and encounter tables"
```

### Task 6: Refactor Random Content Functions To Query Authored Tables

**Files:**
- Modify: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/20260323yy_add_loot_and_encounter_tables.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026032010_rebalance_sell_prices.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026031809_game_raid_action_functions.sql`

**Step 1: Write the selection helpers**

Add small helper functions if needed for weighted selection, for example:

- `game.pick_weighted_encounter(...)`
- `game.pick_weighted_loot_table(...)`
- `game.pick_weighted_enemy_loadout(...)`

Keep them narrowly focused on current schema needs.

**Step 2: Refactor the content generators**

Recreate the latest definitions for:

- `game.random_enemy_loadout()`
- `game.random_loot_items_for_container(container_name text)`
- `game.generate_raid_encounter(raid_payload jsonb, moving_to_extract boolean default false)`

Update them to query the new authoring tables while preserving current return shapes.

**Step 3: Preserve gameplay flow**

Do not move raid payload mutation or action branching into data tables. Only replace selection of:

- enemy archetypes
- enemy equipment
- loot container outcomes
- encounter probabilities and content

**Step 4: Verify no legacy hard-coded content tables remain in those functions**

Run: `rg -n "array\\['Filing Cabinet'|'Weapons Crate'|case floor\\(random\\(\\) \\*|case container_name|case roll <|case when random\\(\\) < 0\\.65" supabase/migrations/20260323yy_add_loot_and_encounter_tables.sql`

Expected: No item-specific authored content remains embedded in those refactored functions except unavoidable state-machine branching.

**Step 5: Commit**

```bash
git add supabase/migrations/20260323yy_add_loot_and_encounter_tables.sql
git commit -m "refactor: drive loot and encounters from authored tables"
```

### Task 7: Run End-To-End Verification And Write Handoff Notes

**Files:**
- Modify: `C:/Users/james/source/repos/extractor-shooter-light/docs/plans/2026-03-23-data-driven-item-and-encounter-implementation.md`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/20260323xx_add_item_defs_table.sql`
- Reference: `C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/20260323yy_add_loot_and_encounter_tables.sql`

**Step 1: Run migration-focused verification commands**

Run:

```bash
rg -n "item_defs|loot_tables|enemy_loadout_tables|encounter_tables" supabase/migrations
```

Expected: The new schema objects and replacement function definitions exist in the latest migrations.

**Step 2: Run repo status**

Run: `git status --short`

Expected: Only intended docs and migration files are modified.

**Step 3: Write handoff notes**

Document:

- the exact seeded item list
- any defaults retained for backwards compatibility
- any future cleanup candidates such as moving callers from name-based lookup to `item_key`
- any follow-up tests or Supabase local verification still needed

**Step 4: Commit**

```bash
git add docs/plans/2026-03-23-data-driven-item-and-encounter-implementation.md supabase/migrations/20260323xx_add_item_defs_table.sql supabase/migrations/20260323yy_add_loot_and_encounter_tables.sql
git commit -m "docs: hand off data-driven item and encounter refactor"
```
