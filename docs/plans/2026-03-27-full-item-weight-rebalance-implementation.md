# Full Item Weight Rebalance Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Apply the requested full item weight rebalance across the current authoritative client/core catalog and the backend `game.item_defs` table.

**Architecture:** Keep weights integer-based and current-authority only. Update `src/RaidLoop.Core/ItemCatalog.cs` for shared behavior and add a new forward Supabase migration that updates `game.item_defs.weight` for the same items. Lock the change with focused tests first, then verify broader encumbrance behavior.

**Tech Stack:** C#, xUnit, Supabase SQL migrations

---

### Task 1: Lock the requested weights in tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**
- Update exact weight assertions to the new requested values.
- Point the migration contract test at a new forward migration and assert representative updated weights.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemCatalogTests|HomeMarkupBindingTests"`
Expected: FAIL because the catalog and migration do not yet match the new table.

### Task 2: Apply the minimal implementation

**Files:**
- Modify: `src/RaidLoop.Core/ItemCatalog.cs`
- Create: `supabase/migrations/2026032708_rebalance_item_weights.sql`

**Step 1: Write minimal implementation**
- Update the canonical weights in `ItemCatalog`.
- Add a forward migration updating `game.item_defs.weight` for the same items.

**Step 2: Run focused tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemCatalogTests|HomeMarkupBindingTests"`
Expected: PASS

### Task 3: Verify downstream encumbrance behavior

**Files:**
- Modify as needed: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Modify as needed: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify as needed: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`

**Step 1: Run broader tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests|ItemCatalogTests|HomeMarkupBindingTests|ProfileMutationFlowTests|RaidActionApiTests"`
Expected: PASS after legitimate expectation updates for the new weights.

**Step 2: Run full verification**

Run: `dotnet test RaidLoop.sln`
Expected: PASS

### Task 4: Apply the migration locally

**Files:**
- Create: `supabase/migrations/2026032708_rebalance_item_weights.sql`

**Step 1: Replay against local Supabase**

Run:
```powershell
docker cp supabase/migrations/2026032708_rebalance_item_weights.sql supabase_db_supabase-authoritative-backend:/tmp/2026032708_rebalance_item_weights.sql
docker exec supabase_db_supabase-authoritative-backend psql -U postgres -d postgres -f /tmp/2026032708_rebalance_item_weights.sql
```

Expected: SQL applies cleanly and `game.item_defs` reflects the new weights.
