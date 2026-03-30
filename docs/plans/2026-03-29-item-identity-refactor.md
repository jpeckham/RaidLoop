# Item Identity Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Finish the move from name-based item identity to `itemDefId`-based contracts with a bootstrap-only rules catalog and a client-only localization boundary so future label renames become client localization changes instead of gameplay or persistence rewrites.

**Architecture:** Introduce `item_def_id` in SQL, migrate contracts and persisted payloads to `itemDefId`, return a lightweight rules catalog for client UX, keep localization and other presentation text entirely client-owned, and keep legacy readers only for migration compatibility. Use forward-only migrations and runtime SQL verification throughout.

**Tech Stack:** .NET 10, Blazor WebAssembly, Supabase Postgres migrations, Deno/Node edge-function tests, xUnit.

---

### Task 1: Add Red Tests For `itemDefId` Identity In C#

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write failing contract tests**

Add tests that expect:

- item snapshots can carry `itemDefId`
- round-trip serialization preserves `itemDefId`
- legacy payloads without `itemDefId` still deserialize through compatibility paths

**Step 2: Write failing catalog tests**

Add tests that expect:

- runtime item references resolve by `itemDefId`
- item display labels are not required as lookup identity

**Step 3: Write failing gameplay tests**

Add tests that expect combat and backpack rules to resolve by `itemDefId` rather than label or textual key.

**Step 4: Run the narrow tests and verify RED**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ContractsTests|ItemCatalogTests|RaidEngineTests" -m:1
```

Expected: failing tests due to missing `itemDefId` support and name-based gameplay lookups.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "test: add red coverage for itemDefId identity"
```

### Task 2: Implement C# Domain And Catalog `itemDefId` Support

**Files:**
- Modify: `src/RaidLoop.Core/Models.cs`
- Modify: `src/RaidLoop.Core/ItemCatalog.cs`
- Modify: `src/RaidLoop.Core/CombatBalance.cs`
- Modify: `src/RaidLoop.Core/LootTables.cs`
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`
- Modify: `src/RaidLoop.Client/Services/ProfileApiClient.cs`

**Step 1: Add `ItemDefId` to the item model**

Update the item model so authored items include a stable `ItemDefId` field.

**Step 2: Keep authored lookup compatibility, but make runtime resolution `itemDefId`-first**

Change the catalog to:

- store authored items keyed by `item_def_id` for runtime use
- preserve internal authored lookup by `item_key`
- support legacy lookup by old `Name` only as temporary compatibility if necessary

**Step 3: Move gameplay rules to `itemDefId`**

Update combat, backpack, armor, and loot rule lookups to switch on `itemDefId` rather than labels or textual keys.

**Step 4: Keep legacy save compatibility**

Teach storage and API normalization to hydrate old name-based items into `itemDefId`-based authored items.

**Step 5: Run the narrow tests and verify GREEN**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ContractsTests|ItemCatalogTests|RaidEngineTests" -m:1
```

Expected: passing.

**Step 6: Commit**

```bash
git add src/RaidLoop.Core/Models.cs src/RaidLoop.Core/ItemCatalog.cs src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Core/LootTables.cs src/RaidLoop.Client/Services/StashStorage.cs src/RaidLoop.Client/Services/ProfileApiClient.cs tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: add itemDefId-based item identity in csharp"
```

### Task 3: Add Red SQL Tests For Surrogate Key, Rules Catalog, And Payload Migration

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Modify: `supabase/functions/game-action/local-integration.test.mjs`
- Modify: `supabase/functions/profile-bootstrap/handler.test.mjs`

**Step 1: Add static migration assertions**

Assert that the new migration:

- adds `item_def_id int generated always as identity`
- preserves unique `item_key`
- backfills payload `itemDefId`
- adds or updates bootstrap support for the lightweight rules catalog

**Step 2: Add runtime integration tests**

Seed legacy payloads that only have item `name`, then call real local RPC paths and assert:

- upgraded responses include `itemDefId`
- bootstrap includes the item rules catalog
- gameplay still works for migrated items

**Step 3: Run narrow tests and verify RED**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests" -m:1
node --test supabase/functions/profile-bootstrap/handler.test.mjs
deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs
```

Expected: failures because the migration and function outputs do not exist yet.

**Step 4: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs supabase/functions/profile-bootstrap/handler.test.mjs supabase/functions/game-action/local-integration.test.mjs
git commit -m "test: add red sql coverage for itemDefId migration"
```

### Task 4: Add Forward Migration For `item_def_id` And Payload `itemDefId`

**Files:**
- Create: `supabase/migrations/2026032904_add_item_identity_keys.sql`

**Step 1: Add surrogate key migration**

The migration should:

- add `item_def_id int generated always as identity` to `game.item_defs`
- backfill existing rows deterministically
- add a primary key or unique index on `item_def_id`
- preserve `item_key` uniqueness

**Step 2: Rewrite payload identity**

The migration should upgrade existing `public.game_saves.payload` and `public.raid_sessions.payload` item entries to include `itemDefId`.

**Step 3: Rewrite SQL functions as needed**

Patch the live functions that emit or depend on item identity so they emit `itemDefId`, provide the bootstrap rules catalog, and stop requiring label identity.

**Step 4: Run fresh DB rebuild**

Run:

```bash
. .\env.local.ps1
npx supabase start
npx supabase db reset
```

Expected: the migration chain applies cleanly.

**Step 5: Re-run SQL tests and verify GREEN**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests" -m:1
node --test supabase/functions/profile-bootstrap/handler.test.mjs
deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs
```

Expected: passing.

**Step 6: Commit**

```bash
git add supabase/migrations/2026032904_add_item_identity_keys.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs supabase/functions/profile-bootstrap/handler.test.mjs supabase/functions/game-action/local-integration.test.mjs
git commit -m "feat: add forward migration for itemDefId identity"
```

### Task 5: Update Edge Functions And Client Contracts To Emit `itemDefId`

**Files:**
- Modify: `src/RaidLoop.Core/Contracts/*.cs`
- Modify: `supabase/functions/profile-bootstrap/handler.mjs`
- Modify: `supabase/functions/profile-save/handler.mjs`
- Modify: `supabase/functions/game-action/handler.mjs`
- Modify: `src/RaidLoop.Client/Services/ProfileApiClient.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `tests/RaidLoop.Core.Tests/ProfileApiClientTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Modify: `supabase/functions/game-action/handler.test.mjs`
- Modify: `supabase/functions/profile-save/handler.test.mjs`
- Modify: `supabase/functions/profile-bootstrap/handler.test.mjs`

**Step 1: Add failing tests for handler and contract shape**

Expect `itemDefId` in bootstrap, save, and raid-action payloads, and expect `name` and `itemKey` to be absent from runtime item contracts.

**Step 2: Run narrow tests and verify RED**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileApiClientTests|RaidStartApiTests|RaidActionApiTests" -m:1
node --test supabase/functions/game-action/handler.test.mjs
node --test supabase/functions/profile-save/handler.test.mjs
node --test supabase/functions/profile-bootstrap/handler.test.mjs
```

Expected: failures for missing `itemDefId` and legacy fields still present.

**Step 3: Implement minimal handler and contract changes**

Emit `itemDefId`, deserialize it, consume the bootstrap rules catalog, and keep legacy compatibility only on inbound migration reads where needed.

**Step 4: Re-run tests and verify GREEN**

Run the same commands and expect passing.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Contracts src/RaidLoop.Client/Services/ProfileApiClient.cs src/RaidLoop.Client/Pages/Home.razor.cs supabase/functions/profile-bootstrap/handler.mjs supabase/functions/profile-save/handler.mjs supabase/functions/game-action/handler.mjs tests/RaidLoop.Core.Tests/ProfileApiClientTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs supabase/functions/game-action/handler.test.mjs supabase/functions/profile-save/handler.test.mjs supabase/functions/profile-bootstrap/handler.test.mjs
git commit -m "feat: emit itemDefId contracts and rules catalog"
```

### Task 6: Move Runtime Client Logic To Downloaded Rules And Local Assets

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/ShopStock.cs`
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`
- Modify: `src/RaidLoop.Core/Models.cs`
- Modify: `src/RaidLoop.Core/ItemJsonConverter.cs`
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/StashStorageTests.cs`

**Step 1: Add failing client tests**

Add or update tests that prove:

- live client item actions and inventory state rely on `itemDefId`
- downloaded `itemRules` supply static UX facts
- runtime server payloads do not need item labels to keep the UI usable
- legacy item names are only read through compatibility paths

**Step 2: Run the narrow tests and verify RED**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileMutationFlowTests|StashStorageTests" -m:1
```

Expected: failures in remaining name-driven client/runtime paths.

**Step 3: Implement the minimal client/runtime cleanup**

- keep `itemDefId` as the live runtime identity
- make `_itemRulesById` the live source of item UX facts for downloaded items
- narrow `Item.Name` and `itemKey` usage to compatibility and display composition only
- keep the client ready for a later dedicated localization asset layer

**Step 4: Re-run tests and verify GREEN**

Run the same command and expect passing.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/ShopStock.cs src/RaidLoop.Client/Services/StashStorage.cs src/RaidLoop.Core/Models.cs src/RaidLoop.Core/ItemJsonConverter.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/StashStorageTests.cs
git commit -m "refactor: move client runtime item handling to itemDefId rules"
```

### Task 7: Add Documentation For `itemDefId` Identity

**Files:**
- Modify: `README.md`

**Step 1: Document the new identity rule**

Add a short contributing note:

- `itemDefId` is canonical runtime identity
- `item_key` is an internal authored lookup key
- labels are client-only display data
- bootstrap returns the full non-localized rules catalog once
- action payloads stay lean and do not repeat the rules catalog
- forward-only migrations for payload identity changes

**Step 2: Verify docs are clear**

Read the changed section and ensure it matches the SQL testing guidance already in the README.

**Step 3: Commit**

```bash
git add README.md
git commit -m "docs: describe itemDefId identity model"
```

### Task 8: Final Verification

**Files:**
- Verify only

**Step 1: Run solution tests**

```bash
dotnet test RaidLoop.sln -m:1
```

**Step 2: Run edge-function tests**

```bash
node --test supabase/functions/game-action/handler.test.mjs
node --test supabase/functions/profile-save/handler.test.mjs
node --test supabase/functions/profile-bootstrap/handler.test.mjs
```

**Step 3: Run runtime SQL integration**

```bash
. .\env.local.ps1
npx supabase db reset
deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs
```

**Step 4: Commit any final fixes**

```bash
git add .
git commit -m "test: finalize itemDefId identity verification"
```
