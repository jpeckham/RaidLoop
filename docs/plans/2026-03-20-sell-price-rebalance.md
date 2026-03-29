# Sell Price Rebalance Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Raise authored item sell prices to roughly 25% of their buy prices so successful raids can finance healing and replacement costs again.

**Architecture:** Keep buy prices unchanged in `CombatBalance`, but rebalance authored item `Value` fields in both the C# item catalog and the server-authoritative SQL item definitions. Use test-first checks for representative items and the migration text so the client and Supabase economy stay aligned.

**Tech Stack:** C#, xUnit, PostgreSQL SQL migrations

---

### Task 1: Lock the target sell-price ratios with failing tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing tests**

Add tests that assert:
- representative items sell for roughly 25% of their buy price
- the new SQL migration defines updated authored values for key items like `Medkit`, `Field Carbine`, and `Assault Plate Carrier`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ItemCatalogTests|FullyQualifiedName~HomeMarkupBindingTests"`
Expected: FAIL because the current values are still too low and the new migration does not exist yet.

### Task 2: Rebalance authored values in C#

**Files:**
- Modify: `src/RaidLoop.Core/ItemCatalog.cs`

**Step 1: Write minimal implementation**

Raise item `Value` fields to approximately 25% of their buy price, with sensible rounding for readability.

**Step 2: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ItemCatalogTests"`
Expected: PASS

### Task 3: Rebalance canonical server item values

**Files:**
- Add: `supabase/migrations/2026032010_rebalance_sell_prices.sql`

**Step 1: Write minimal implementation**

Add a migration that introduces a canonical SQL item helper and redefines the relevant economy/loot functions to use the updated authored values.

**Step 2: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests"`
Expected: PASS

### Task 4: Verify the combined economy behavior

**Files:**
- No new files required

**Step 1: Run the relevant suite**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ItemCatalogTests|FullyQualifiedName~HomeMarkupBindingTests"`
Expected: PASS
