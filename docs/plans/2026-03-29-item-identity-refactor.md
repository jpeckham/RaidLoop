# Item Identity Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Move the game from name-based item identity to `item_key`-based identity with an `int` surrogate key in SQL so future label renames do not require gameplay or persistence rewrites.

**Architecture:** Introduce `item_def_id` in SQL, add `itemKey` to contracts and client models, migrate server and client logic to use keys as canonical identity, keep legacy name readers during transition, and defer label renames until identity is stable. Use forward-only migrations and runtime SQL verification throughout.

**Tech Stack:** .NET 10, Blazor WebAssembly, Supabase Postgres migrations, Deno/Node edge-function tests, xUnit.

---

### Task 1: Return Workspace To Clean Main

**Files:**
- Modify: working tree only

**Step 1: Discard the abandoned rename pass**

Run:

```bash
git restore README.md src tests supabase/functions
git clean -f supabase/migrations/2026032904_forward_rename_risky_item_names.sql
```

Expected: the worktree no longer includes the partial rename attempt.

**Step 2: Verify clean status**

Run:

```bash
git status --short
```

Expected: no output.

**Step 3: Commit if needed**

No commit expected if the workspace returns to the current checked-in state cleanly.

### Task 2: Add Red Tests For Key-Based Identity In C#

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write failing contract tests**

Add tests that expect:

- item snapshots can carry `itemKey`
- round-trip serialization preserves `itemKey`
- legacy payloads without `itemKey` still deserialize

**Step 2: Write failing catalog tests**

Add tests that expect:

- `ItemCatalog` lookups succeed by `itemKey`
- item display labels are not required as lookup identity

**Step 3: Write failing gameplay tests**

Add tests that expect combat and backpack rules to resolve by key rather than label.

**Step 4: Run the narrow tests and verify RED**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ContractsTests|ItemCatalogTests|RaidEngineTests" -m:1
```

Expected: failing tests due to missing `itemKey` support and name-based gameplay lookups.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "test: add red coverage for item key identity"
```

### Task 3: Implement C# Domain And Catalog Key Support

**Files:**
- Modify: `src/RaidLoop.Core/Item.cs`
- Modify: `src/RaidLoop.Core/ItemCatalog.cs`
- Modify: `src/RaidLoop.Core/CombatBalance.cs`
- Modify: `src/RaidLoop.Core/LootTables.cs`
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`

**Step 1: Add `Key` to the item model**

Update the item model so authored items include a stable `Key` field.

**Step 2: Key the catalog by `item_key`**

Change the catalog to:

- store authored items keyed by `item_key`
- resolve localized/display label from the item data
- support legacy lookup by old `Name` only as temporary compatibility if necessary

**Step 3: Move gameplay rules to keys**

Update combat, backpack, armor, and loot rule lookups to switch on item keys rather than labels.

**Step 4: Keep legacy save compatibility**

Teach `StashStorage` normalization to hydrate old name-based items into keyed authored items.

**Step 5: Run the narrow tests and verify GREEN**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ContractsTests|ItemCatalogTests|RaidEngineTests" -m:1
```

Expected: passing.

**Step 6: Commit**

```bash
git add src/RaidLoop.Core/Item.cs src/RaidLoop.Core/ItemCatalog.cs src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Core/LootTables.cs src/RaidLoop.Client/Services/StashStorage.cs tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: add key-based item identity in csharp"
```

### Task 4: Add Red SQL Tests For Surrogate Key And Payload Migration

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Modify: `supabase/functions/game-action/local-integration.test.mjs`
- Modify: `supabase/functions/profile-bootstrap/handler.test.mjs`

**Step 1: Add static migration assertions**

Assert that the new migration:

- adds `item_def_id int generated always as identity`
- preserves unique `item_key`
- backfills payload `itemKey`

**Step 2: Add runtime integration tests**

Seed legacy payloads that only have item `name`, then call real local RPC paths and assert:

- upgraded responses include `itemKey`
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
git commit -m "test: add red sql coverage for item key migration"
```

### Task 5: Add Forward Migration For `item_def_id` And Payload Keys

**Files:**
- Create: `supabase/migrations/2026032904_add_item_identity_keys.sql`

**Step 1: Add surrogate key migration**

The migration should:

- add `item_def_id int generated always as identity` to `game.item_defs`
- backfill existing rows deterministically
- add a primary key or unique index on `item_def_id`
- preserve `item_key` uniqueness

**Step 2: Rewrite payload identity**

The migration should upgrade existing `public.game_saves.payload` and `public.raid_sessions.payload` item entries to include `itemKey`.

**Step 3: Rewrite SQL functions as needed**

Patch the live functions that emit or depend on item identity so they emit keyed items and stop requiring label identity.

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
git commit -m "feat: add forward migration for keyed item identity"
```

### Task 6: Update Edge Functions And Client Contracts To Emit `itemKey`

**Files:**
- Modify: `src/RaidLoop.Core/Contracts/*.cs`
- Modify: `supabase/functions/profile-bootstrap/handler.mjs`
- Modify: `supabase/functions/profile-save/handler.mjs`
- Modify: `supabase/functions/game-action/handler.mjs`
- Modify: `tests/RaidLoop.Core.Tests/ProfileApiClientTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Modify: `supabase/functions/game-action/handler.test.mjs`
- Modify: `supabase/functions/profile-save/handler.test.mjs`

**Step 1: Add failing tests for handler and contract shape**

Expect `itemKey` in bootstrap, save, and raid-action payloads.

**Step 2: Run narrow tests and verify RED**

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileApiClientTests|RaidStartApiTests|RaidActionApiTests" -m:1
node --test supabase/functions/game-action/handler.test.mjs
node --test supabase/functions/profile-save/handler.test.mjs
```

Expected: failures for missing `itemKey`.

**Step 3: Implement minimal handler and contract changes**

Emit `itemKey`, deserialize it, and keep legacy `name` compatibility only where needed.

**Step 4: Re-run tests and verify GREEN**

Run the same commands and expect passing.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Contracts supabase/functions/profile-bootstrap/handler.mjs supabase/functions/profile-save/handler.mjs supabase/functions/game-action/handler.mjs tests/RaidLoop.Core.Tests/ProfileApiClientTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs supabase/functions/game-action/handler.test.mjs supabase/functions/profile-save/handler.test.mjs
git commit -m "feat: emit keyed item identity in contracts and handlers"
```

### Task 7: Add Documentation For Key-Based Item Identity

**Files:**
- Modify: `README.md`

**Step 1: Document the new identity rule**

Add a short contributing note:

- `item_key` is canonical gameplay identity
- labels are client-only display data
- forward-only migrations for payload identity changes

**Step 2: Verify docs are clear**

Read the changed section and ensure it matches the SQL testing guidance already in the README.

**Step 3: Commit**

```bash
git add README.md
git commit -m "docs: describe keyed item identity model"
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
git commit -m "test: finalize keyed item identity verification"
```
