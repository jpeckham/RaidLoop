# Persisted Item Payload Cleanup Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove persisted authored-item `name` and `itemKey` metadata from player save and raid-session payloads so `itemDefId` is the only saved item identity for authored items.

**Architecture:** Keep legacy `name` and `itemKey` accepted on inbound compatibility paths, but stop persisting them in `public.game_saves.payload` and `public.raid_sessions.payload`. Update SQL normalization/build helpers so authored items are saved as `itemDefId` plus dynamic gameplay fields only, and add a forward migration that rewrites existing persisted payloads to the lean shape.

**Tech Stack:** PostgreSQL / Supabase SQL migrations, Deno Edge Functions, JavaScript tests, xUnit migration-shape tests

---

### Task 1: Prove persisted payloads still contain redundant item metadata

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/local-integration.test.mjs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/local-integration.test.mjs`

**Step 1: Write the failing test**

Add an integration test that seeds a user row in `public.game_saves` or `public.raid_sessions` with authored items containing `itemDefId`, `itemKey`, and `name`, runs a normal profile/raid path, and asserts the persisted row no longer contains `itemKey` or item-object `name`.

```javascript
test("persisted save payload strips itemKey and item name from authored items", async () => {
  // seed game_saves.payload.mainStash with itemDefId + itemKey + name
  // invoke game-action or bootstrap path that rewrites the save
  // fetch row and assert authored item object keeps itemDefId but not itemKey/name
});
```

**Step 2: Run test to verify it fails**

Run: `deno test supabase/functions/game-action/local-integration.test.mjs --filter "persisted save payload strips itemKey and item name from authored items"`

Expected: FAIL because SQL still writes item objects with `itemKey` and `name`.

**Step 3: Keep the test focused on persisted JSON**

- Assert on the actual stored payload row, not only the HTTP response.
- Cover at least one save payload path and one active raid/session path.

**Step 4: Re-run test and confirm it still fails for the expected reason**

Run: `deno test supabase/functions/game-action/local-integration.test.mjs --filter "persisted save payload strips itemKey and item name from authored items"`

Expected: FAIL with persisted payload still containing those fields.

**Step 5: Commit**

```bash
git add supabase/functions/game-action/local-integration.test.mjs
git commit -m "test: pin redundant item metadata in persisted payloads"
```

### Task 2: Stop SQL helper functions from emitting `itemKey` and `name` for authored items

**Files:**
- Create: `C:/users/james/source/repos/extractor-shooter-light/supabase/migrations/20260404xx_strip_persisted_item_name_and_key.sql`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing shape test**

Add or extend a migration-shape test that asserts the new migration removes `itemKey` and `name` from authored-item builders.

```csharp
[Fact]
public void PersistedItemCleanupMigration_StopsWritingAuthoredItemNameAndItemKey()
{
    var migration = File.ReadAllText(PersistedItemCleanupMigrationPath);

    Assert.Contains("create or replace function game.authored_item", migration);
    Assert.DoesNotContain("'itemKey'", migration);
    Assert.DoesNotContain("'name'", migration);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter PersistedItemCleanupMigration_StopsWritingAuthoredItemNameAndItemKey`

Expected: FAIL because no such migration exists yet.

**Step 3: Implement the minimal SQL helper cleanup**

In the new migration:
- replace `game.authored_item(...)` so authored items return only:
  - `itemDefId`
  - `type`
  - `value`
  - `slots`
  - `rarity`
  - `displayRarity`
  - `weight`
- replace `game.normalize_item(...)` so:
  - authored-item resolution by `itemDefId`/`itemKey`/legacy `name` still works
  - authored-item output no longer writes `itemKey` or `name`
  - unknown/non-authored fallback items may still preserve `name` because they have no canonical authored identity

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter PersistedItemCleanupMigration_StopsWritingAuthoredItemNameAndItemKey`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/migrations/20260404xx_strip_persisted_item_name_and_key.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "refactor: stop persisting authored item name metadata"
```

### Task 3: Rewrite existing persisted rows to the lean item shape

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/supabase/migrations/20260404xx_strip_persisted_item_name_and_key.sql`
- Test: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/local-integration.test.mjs`

**Step 1: Write the failing migration-backfill test**

Extend the local integration test to seed legacy rows and assert that after the migration-backed rewrite path runs, stored authored item objects keep `itemDefId` and drop `itemKey`/`name`.

```javascript
test("legacy persisted authored items are backfilled to itemDefId-only shape", async () => {
  // seed legacy game_saves and/or raid_sessions payload
  // invoke a profile or raid write path
  // fetch rows and assert authored items no longer store itemKey/name
});
```

**Step 2: Run test to verify it fails**

Run: `deno test supabase/functions/game-action/local-integration.test.mjs --filter "legacy persisted authored items are backfilled to itemDefId-only shape"`

Expected: FAIL because current rows are only normalized to a keyed/named shape.

**Step 3: Add the forward data migration**

In the migration:
- update every authored item object nested under:
  - `mainStash`
  - `onPersonItems[].item`
  - `activeRaid.equippedItems`
  - `activeRaid.carriedLoot`
  - `activeRaid.discoveredLoot`
  - corresponding arrays in `public.raid_sessions.payload`
- use the existing normalization helpers or new lean helpers so authored items are rewritten to `itemDefId`-only shape
- leave non-item text fields like `randomCharacter.name`, `enemyName`, `encounterDescription`, and log entries untouched

**Step 4: Run test to verify it passes**

Run: `deno test supabase/functions/game-action/local-integration.test.mjs --filter "legacy persisted authored items are backfilled to itemDefId-only shape"`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/migrations/20260404xx_strip_persisted_item_name_and_key.sql supabase/functions/game-action/local-integration.test.mjs
git commit -m "feat: backfill persisted item payloads to itemDefId-only shape"
```

### Task 4: Keep edge-function runtime normalization compatible while persistence becomes lean

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/profile-bootstrap/handler.mjs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/handler.mjs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/profile-bootstrap/handler.test.mjs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/handler.test.mjs`

**Step 1: Write the failing tests**

Add tests that feed persisted authored item objects containing only `itemDefId` and ensure the HTTP/runtime contracts still normalize correctly.

```javascript
test("profile-bootstrap accepts persisted authored items with itemDefId only", async () => {
  // bootstrap returns lean stored items
  // response still contains only runtime itemDefId shape
});

test("game-action accepts persisted authored raid items with itemDefId only", async () => {
  // game-action snapshot contains lean stored items
  // response still projects itemDefId-only runtime items
});
```

**Step 2: Run tests to verify they fail**

Run:
- `deno test supabase/functions/profile-bootstrap/handler.test.mjs --filter "itemDefId only"`
- `deno test supabase/functions/game-action/handler.test.mjs --filter "itemDefId only"`

Expected: FAIL if runtime normalization still assumes persisted `type/slots/value/name/itemKey` combinations too narrowly.

**Step 3: Implement minimal handler updates**

- Loosen the item-like detection helpers so lean persisted authored items with only `itemDefId` are recognized where appropriate.
- Keep response normalization itemDefId-only.
- Do not reintroduce `name` or `itemKey` into HTTP responses.

**Step 4: Run tests to verify they pass**

Run:
- `deno test supabase/functions/profile-bootstrap/handler.test.mjs --filter "itemDefId only"`
- `deno test supabase/functions/game-action/handler.test.mjs --filter "itemDefId only"`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/functions/profile-bootstrap/handler.mjs supabase/functions/game-action/handler.mjs supabase/functions/profile-bootstrap/handler.test.mjs supabase/functions/game-action/handler.test.mjs
git commit -m "fix: accept lean persisted authored item payloads"
```

### Task 5: Remove persisted-item metadata assumptions from SQL action paths

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/supabase/migrations/20260404xx_strip_persisted_item_name_and_key.sql`
- Test: `C:/users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/local-integration.test.mjs`

**Step 1: Write the failing test**

Add an integration test that performs a raid/item action against a lean persisted raid payload and proves the SQL can still find the selected item without stored `name` or `itemKey`.

```javascript
test("raid item actions work when persisted raid items only store itemDefId", async () => {
  // seed raid session with itemDefId-only equipped/carried/discovered items
  // call take-loot / equip-from-discovered / drop-carried
  // assert action succeeds and persisted payload stays lean
});
```

**Step 2: Run test to verify it fails**

Run: `deno test supabase/functions/game-action/local-integration.test.mjs --filter "persisted raid items only store itemDefId"`

Expected: FAIL if SQL still searches raid arrays strictly by stored `name`.

**Step 3: Refactor SQL action lookup minimally**

- In the migration, update raid/profile action helpers to resolve selected items by:
  - `itemDefId` when present
  - otherwise compatibility `itemName`
- Internal SQL may still translate payload item ids back to authored DB rows for weight/lookups.
- Persisted raid/save payloads must remain lean after the action completes.

**Step 4: Run test to verify it passes**

Run: `deno test supabase/functions/game-action/local-integration.test.mjs --filter "persisted raid items only store itemDefId"`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/migrations/20260404xx_strip_persisted_item_name_and_key.sql supabase/functions/game-action/local-integration.test.mjs
git commit -m "refactor: use itemDefId for persisted raid item actions"
```

### Task 6: Run the full verification suite and document the persistence boundary

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/README.md`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing doc/test check**

Add or extend a doc-oriented test to pin that authored persisted item payloads should not store `itemKey` or item-object `name`.

```csharp
[Fact]
public void Readme_StatesPersistedAuthoredItemsAreItemDefIdOnly()
{
    var readme = File.ReadAllText(ReadmePath);
    Assert.Contains("Persisted authored items should store itemDefId only", readme);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter Readme_StatesPersistedAuthoredItemsAreItemDefIdOnly`

Expected: FAIL until README is updated.

**Step 3: Update docs**

Document in `README.md`:
- runtime responses already scrub item labels/keys
- persisted authored item JSON should now store `itemDefId` only
- legacy `name`/`itemKey` are compatibility input only
- non-item display text fields are out of scope and remain persisted normally

**Step 4: Run full verification**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`
- `deno test supabase/functions/profile-bootstrap/handler.test.mjs`
- `deno test supabase/functions/game-action/handler.test.mjs`
- `deno test supabase/functions/game-action/local-integration.test.mjs`
- `rg -n "\"itemKey\"|'itemKey'|\"name\"|'name'" supabase/migrations/20260404xx_strip_persisted_item_name_and_key.sql`

Expected:
- all tests PASS
- any remaining `name` references in the new migration are only compatibility reads or non-item text handling, not persisted authored item writes

**Step 5: Commit**

```bash
git add README.md tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs supabase/migrations/20260404xx_strip_persisted_item_name_and_key.sql supabase/functions/profile-bootstrap/handler.test.mjs supabase/functions/game-action/handler.test.mjs supabase/functions/game-action/local-integration.test.mjs
git commit -m "docs: describe itemDefId-only persisted item payloads"
```
