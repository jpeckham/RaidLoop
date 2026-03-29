# Light Gear Weight Adjustments Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Apply the new current authoritative weights for Medkit, Makarov, 6B2 body armor, and Small Backpack across shared code and authoritative SQL.

**Architecture:** Keep weights as integers. Update the shared `ItemCatalog` for client/core behavior and add a new forward migration that patches `game.item_defs` so the backend authoritative weight lookup stays aligned. Lock the change with focused tests before and after implementation.

**Tech Stack:** C#, xUnit, Supabase SQL migrations

---

### Task 1: Lock the new weights in tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing tests**
- Change the canonical item weight assertions to the new requested values.
- Add a migration-contract assertion for a new forward weight-adjustment migration.
- Update one encumbrance total assertion to reflect the new medkit weight.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemCatalogTests|HomeMarkupBindingTests"`
Expected: FAIL because the current catalog and migration do not yet match the new weights.

### Task 2: Apply the minimal implementation

**Files:**
- Modify: `src/RaidLoop.Core/ItemCatalog.cs`
- Create: `supabase/migrations/2026032707_adjust_light_gear_weights.sql`

**Step 1: Write minimal implementation**
- Update the four item weights in `ItemCatalog`.
- Add a migration that updates `game.item_defs.weight` for the same four item names.

**Step 2: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemCatalogTests|HomeMarkupBindingTests"`
Expected: PASS

### Task 3: Verify broader behavior

**Files:**
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`

**Step 1: Run targeted behavior tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests|ItemCatalogTests|HomeMarkupBindingTests"`
Expected: PASS

**Step 2: Run full solution verification**

Run: `dotnet test RaidLoop.sln`
Expected: PASS

### Task 4: Apply the forward migration locally

**Files:**
- Create: `supabase/migrations/2026032707_adjust_light_gear_weights.sql`

**Step 1: Replay the migration against local Supabase**

Run:
```powershell
docker cp supabase/migrations/2026032707_adjust_light_gear_weights.sql supabase_db_supabase-authoritative-backend:/tmp/2026032707_adjust_light_gear_weights.sql
docker exec supabase_db_supabase-authoritative-backend psql -U postgres -d postgres -f /tmp/2026032707_adjust_light_gear_weights.sql
```

Expected: SQL applies cleanly.
