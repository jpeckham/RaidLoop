# Hard Cut ItemDefId Schema Migration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make `itemDefId` the only authored item identity everywhere by promoting `game.item_defs.item_def_id` to the primary key, migrating every dependent table and SQL path to it, and removing `item_key` and `itemKey` support completely.

**Architecture:** Treat this as a hard schema cut, not a compatibility migration. First pin the remaining schema/runtime dependencies with failing tests, then introduce a forward-only migration that moves authored-content tables and SQL joins to `item_def_id`, promotes `item_def_id` to the `game.item_defs` primary key, and drops `item_key` from schema. After that, remove all `itemKey` support from .NET and edge-function readers, then run the full verification suite.

**Tech Stack:** PostgreSQL / Supabase SQL migrations, Deno Edge Functions, .NET 10 / xUnit, Blazor WebAssembly

---

### Task 1: Pin the remaining `item_key` schema dependencies with failing tests

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing schema-shape tests**

Add tests that assert the new migration will:
- promote `game.item_defs.item_def_id` to primary key
- remove `item_key` columns from authored-content tables
- reference `item_def_id` in `enemy_loadout_variant_items` and `loot_table_variant_items`

```csharp
[Fact]
public void HardCutItemDefIdMigration_PromotesItemDefIdToPrimaryKey()
{
    var migration = File.ReadAllText(HardCutItemDefIdMigrationPath);

    Assert.Contains("alter table game.item_defs drop constraint", migration);
    Assert.Contains("primary key (item_def_id)", migration);
    Assert.DoesNotContain("item_key text primary key", migration);
}

[Fact]
public void HardCutItemDefIdMigration_ReplacesAuthoredContentItemKeyColumns()
{
    var migration = File.ReadAllText(HardCutItemDefIdMigrationPath);

    Assert.Contains("enemy_loadout_variant_items", migration);
    Assert.Contains("loot_table_variant_items", migration);
    Assert.Contains("item_def_id", migration);
    Assert.DoesNotContain("item_key text not null references game.item_defs(item_key)", migration);
}
```

**Step 2: Run the narrow test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HardCutItemDefIdMigration_"`

Expected: FAIL because the migration file does not exist yet.

**Step 3: Keep the assertions focused on the new migration**

- Add a `HardCutItemDefIdMigrationPath` constant pointing to `supabase/migrations/2026040501_hard_cut_itemdefid_schema_migration.sql`
- Do not mutate older migration tests; this pass should prove the new forward-only migration.

**Step 4: Re-run the same test to confirm failure is still for missing migration content**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HardCutItemDefIdMigration_"`

Expected: FAIL with missing file/content assertions.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "test: pin hard-cut itemDefId schema migration"
```

### Task 2: Move authored-content tables from `item_key` to `item_def_id`

**Files:**
- Create: `C:/users/james/source/repos/extractor-shooter-light/supabase/migrations/2026040501_hard_cut_itemdefid_schema_migration.sql`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Create the new migration with the authored-content table rewrite**

In the new migration:
- add `item_def_id int` to `game.enemy_loadout_variant_items`
- add `item_def_id int` to `game.loot_table_variant_items`
- backfill each from `game.item_defs.item_def_id` via the existing `item_key`
- make the new columns `not null`
- add foreign keys to `game.item_defs(item_def_id)`
- replace table primary keys from `(variant_key, item_key, item_order)` to `(variant_key, item_def_id, item_order)`
- drop old `item_key` columns from those two tables

Example migration fragment:

```sql
alter table game.enemy_loadout_variant_items
    add column if not exists item_def_id int;

update game.enemy_loadout_variant_items items
set item_def_id = defs.item_def_id
from game.item_defs defs
where defs.item_key = items.item_key
  and items.item_def_id is null;
```

**Step 2: Rewrite the seed inserts in the same migration**

- delete/reinsert authored-content rows using `item_def_id`
- use subqueries from stable authored names during the one-time migration seed statements
- avoid reintroducing `item_key` into the final schema

Example:

```sql
insert into game.enemy_loadout_variant_items (variant_key, item_def_id, item_order)
values
    ('enemy_makarov', (select item_def_id from game.item_defs where name = 'Makarov'), 10);
```

**Step 3: Run the migration-shape tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HardCutItemDefIdMigration_"`

Expected: PASS

**Step 4: Rebuild local Supabase from scratch**

Run: `npx supabase db reset`

Expected: PASS with the new migration applied cleanly.

**Step 5: Commit**

```bash
git add supabase/migrations/2026040501_hard_cut_itemdefid_schema_migration.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "refactor: move authored content tables to itemDefId"
```

### Task 3: Promote `game.item_defs.item_def_id` to the actual primary key and drop `item_key`

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/supabase/migrations/2026040501_hard_cut_itemdefid_schema_migration.sql`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Extend the failing schema-shape test**

Add assertions that the new migration:
- drops the old `game.item_defs` primary key on `item_key`
- adds a primary key on `item_def_id`
- drops the `item_key` column entirely

```csharp
[Fact]
public void HardCutItemDefIdMigration_DropsItemKeyFromItemDefs()
{
    var migration = File.ReadAllText(HardCutItemDefIdMigrationPath);

    Assert.Contains("drop column item_key", migration);
    Assert.Contains("primary key (item_def_id)", migration);
    Assert.DoesNotContain("unique (item_key)", migration);
}
```

**Step 2: Implement the PK swap in the migration**

In the new migration:
- drop dependent constraints already moved off `item_key`
- drop the old primary key on `game.item_defs`
- add `primary key (item_def_id)`
- preserve `name` uniqueness
- drop `item_key`
- drop helper functions that depend on `item_key`, or rewrite them away in later steps

Use system catalog-safe DDL if needed:

```sql
alter table game.item_defs drop constraint item_defs_pkey;
alter table game.item_defs add constraint item_defs_pkey primary key (item_def_id);
alter table game.item_defs drop column item_key;
```

**Step 3: Run migration-shape tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HardCutItemDefIdMigration_"`

Expected: PASS

**Step 4: Rebuild local Supabase again**

Run: `npx supabase db reset`

Expected: PASS with no PK/FK errors.

**Step 5: Commit**

```bash
git add supabase/migrations/2026040501_hard_cut_itemdefid_schema_migration.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "refactor: promote itemDefId to item_defs primary key"
```

### Task 4: Rewrite authored SQL functions to use `item_def_id` only

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/supabase/migrations/2026040501_hard_cut_itemdefid_schema_migration.sql`
- Test: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/local-integration.test.mjs`

**Step 1: Add failing integration coverage for authored content generation**

Add tests that exercise runtime generation from authored tables after the schema cut:
- enemy loadouts still generate authored items
- loot tables still generate authored items
- encounter generation still works

```javascript
test("authored enemy loadout generation still works after itemDefId FK migration", async () => {
  // invoke start-main-raid enough to hit authored enemy/loot generation
  // assert returned enemy/discovered items contain itemDefId identities
});
```

**Step 2: Run the narrow test to verify it fails**

Run: `deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs --filter "FK migration"`

Expected: FAIL if SQL still joins authored tables by `item_key`.

**Step 3: Rewrite the authored SQL helpers**

In the migration:
- remove `game.item_def_by_key`
- remove `game.item_key_for_name`
- rewrite authored item builders and joins to use:
  - `item_def_id` for relational joins
  - `name` only where a player-facing authored label must be resolved internally during migration-time seed statements
- update:
  - `game.random_enemy_loadout_from_table`
  - `game.random_loot_items_from_table`
  - any helper queries still joining `items.item_key = item_defs.item_key`

Example:

```sql
join game.item_defs
  on game.item_defs.item_def_id = items.item_def_id
```

**Step 4: Run the narrow integration test and reset**

Run:
- `npx supabase db reset`
- `deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs --filter "FK migration"`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/migrations/2026040501_hard_cut_itemdefid_schema_migration.sql supabase/functions/game-action/local-integration.test.mjs
git commit -m "refactor: use itemDefId in authored SQL lookups"
```

### Task 5: Remove `itemKey` support from .NET item models and readers

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/Models.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/ItemCatalog.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/ItemJsonConverter.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Pages/Home.razor.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/StashStorageTests.cs`

**Step 1: Write the failing tests**

Update tests to assert:
- authored item deserialization no longer accepts `itemKey`
- authored runtime JSON never emits `itemKey`
- client-side hydration does not branch on `itemKey`

```csharp
[Fact]
public void ItemJsonConverter_DoesNotAcceptLegacyItemKeyPayloads()
{
    const string json = """{"itemKey":"ak74"}""";
    Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<Item>(json, SerializerOptions));
}
```

**Step 2: Run the narrow tests to verify failure**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "itemKey|LegacyItemKey"`

Expected: FAIL because compatibility branches still exist.

**Step 3: Remove the compatibility code**

- remove `Item.Key` from `Models.cs` if it is no longer required
- remove `TryGetByKey`, `TryGetItemDefIdByKey`, and `TryResolveAuthoredItem(... itemKey ...)` from `ItemCatalog.cs`
- change `ItemJsonConverter` to resolve authored items only from `itemDefId`
- remove `itemKey` branches from `Home.razor.cs` hydration/parsing

Keep fallback behavior for truly unknown non-authored items only if still required, but not via `itemKey`.

**Step 4: Re-run the narrow .NET tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "itemKey|LegacyItemKey|StashStorage"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Models.cs src/RaidLoop.Core/ItemCatalog.cs src/RaidLoop.Core/ItemJsonConverter.cs src/RaidLoop.Client/Pages/Home.razor.cs tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/StashStorageTests.cs
git commit -m "refactor: remove itemKey runtime compatibility"
```

### Task 6: Remove `itemKey` support from edge functions and handler tests

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/profile-bootstrap/handler.mjs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/handler.mjs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/_shared/profile-rpc.mjs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/profile-bootstrap/handler.test.mjs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/handler.test.mjs`

**Step 1: Write the failing tests**

Add/adjust tests to assert:
- handlers normalize only `itemDefId`
- `itemKey` is not accepted as a request or snapshot identity path
- no map of `ITEM_DEF_ID_BY_KEY` remains

```javascript
test("game-action rejects itemKey-based authored item payloads", async () => {
  // send itemKey-only payload
  // expect failure or ignored action, depending on contract you choose in implementation
});
```

**Step 2: Run the narrow handler tests**

Run:
- `deno test --allow-read --allow-env --allow-net supabase/functions/game-action/handler.test.mjs --filter "itemKey"`
- `deno test --allow-env --allow-net supabase/functions/profile-bootstrap/handler.test.mjs --filter "itemKey"`

Expected: FAIL until compatibility code is removed.

**Step 3: Remove key-based maps and readers**

- delete `ITEM_DEF_ID_BY_KEY` maps from both handlers
- make `resolveItemDefId` use `itemDefId` only, plus legacy `name` only if that is still intentionally supported elsewhere in this pass
- ensure outbound normalization still keeps runtime payloads lean

**Step 4: Re-run the handler tests**

Run:
- `deno test --allow-read --allow-env --allow-net supabase/functions/game-action/handler.test.mjs`
- `deno test --allow-env --allow-net supabase/functions/profile-bootstrap/handler.test.mjs`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/functions/profile-bootstrap/handler.mjs supabase/functions/game-action/handler.mjs supabase/functions/_shared/profile-rpc.mjs supabase/functions/profile-bootstrap/handler.test.mjs supabase/functions/game-action/handler.test.mjs
git commit -m "refactor: remove itemKey edge-function compatibility"
```

### Task 7: Remove `item_key`/`itemKey` references from docs and migration tests, then run full verification

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/README.md`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Add the final failing doc/test check**

Add assertions that:
- README states `itemDefId` is the only authored item identity
- README does not describe `item_key` as an internal bridge anymore

```csharp
[Fact]
public void Readme_StatesItemDefIdIsTheOnlyAuthoredItemIdentity()
{
    var readme = File.ReadAllText(ReadmePath);

    Assert.Contains("itemDefId is the only authored item identity", readme);
    Assert.DoesNotContain("item_key", readme);
}
```

**Step 2: Update documentation**

In `README.md`, document:
- `game.item_defs.item_def_id` is the primary key and only authored identity
- authored content tables reference `item_def_id`
- `item_key` and `itemKey` no longer exist

**Step 3: Run the full verification suite**

Run:
- `npx supabase db reset`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`
- `deno test --allow-env --allow-net supabase/functions/profile-bootstrap/handler.test.mjs`
- `deno test --allow-read --allow-env --allow-net supabase/functions/game-action/handler.test.mjs`
- `deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs`
- `rg -n "item_key|itemKey" supabase/migrations src tests README.md`

Expected:
- all tests PASS
- `rg` finds no remaining authored identity references to `item_key` / `itemKey`
- if any hits remain, they must be in historical migration files only, not current runtime/docs/tests

**Step 4: Review the migration blast radius**

Run:
- `git diff --stat`
- `git diff -- supabase/migrations/2026040501_hard_cut_itemdefid_schema_migration.sql`

Expected: clear evidence that the hard cut touched schema, SQL helpers, runtime compatibility, and docs together.

**Step 5: Commit**

```bash
git add README.md tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs supabase/migrations/2026040501_hard_cut_itemdefid_schema_migration.sql src/RaidLoop.Core src/RaidLoop.Client supabase/functions
git commit -m "refactor: hard cut authored item identity to itemDefId"
```
