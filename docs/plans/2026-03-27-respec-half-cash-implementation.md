# Respec Half Cash Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the fixed `$5000` stat reallocation fee with a dynamic `50% of current cash`, rounded to the nearest whole dollar.

**Architecture:** Keep the rule in one small client helper for UI/gating and mirror it in a single Supabase migration for authoritative mutation behavior. Reuse existing tests and migration binding coverage instead of introducing new systems.

**Tech Stack:** C#, xUnit, Blazor, Supabase SQL migrations

---

### Task 1: Lock in client-facing dynamic respec pricing

**Files:**
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\ProfileMutationFlowTests.cs`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Update the existing respec tests to require a dynamic affordability gate and dynamic button label rather than a fixed `$5000`.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "CanReallocateStats|HomeShowsCompactStatStripInPreparingForRaidHeader|ReallocateStats"`
Expected: FAIL because the current code still hardcodes `$5000`.

**Step 3: Write minimal implementation**

Add the client helper and update the button label and gate.

**Step 4: Run test to verify it passes**

Run the same filtered command and expect PASS.

### Task 2: Lock in backend dynamic respec pricing

**Files:**
- Create: `C:\Users\james\source\repos\extractor-shooter-light\supabase\migrations\2026032706_half_cash_respec_cost.sql`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a migration binding test that requires a rounded half-cash respec cost instead of fixed `5000`.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HalfCashRespec"`
Expected: FAIL because the migration does not exist yet.

**Step 3: Write minimal implementation**

Create the migration and update only the `reallocate-stats` pricing logic.

**Step 4: Run test to verify it passes**

Run the same filtered command and expect PASS.

### Task 3: Verify regression safety

**Files:**
- No new files

**Step 1: Run focused tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileMutationFlowTests|HomeMarkupBindingTests"`
Expected: PASS

**Step 2: Run solution tests**

Run: `dotnet test RaidLoop.sln`
Expected: PASS
