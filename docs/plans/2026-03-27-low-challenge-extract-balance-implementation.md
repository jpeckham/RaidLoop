# Low Challenge Extract Balance Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Restore neutral and low-loot outcomes to travel and extract-approach encounters so low-challenge extraction is not forced combat every step.

**Architecture:** Keep the existing encounter-generation code and fix the authored SQL data instead. Add/update encounter rows in the travel and extract families, then lock the presence of those rows in migration-binding tests.

**Tech Stack:** Supabase SQL migrations, C#, xUnit

---

### Task 1: Add failing migration-binding coverage for non-combat travel and extract rows

**Files:**
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add assertions that the authored surprise encounter migration includes non-combat entries in `default_raid_travel` and `extract_approach`.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "AuthoredSurpriseEncounterStylesMigrationPinsContactStatesAndFamilies"`
Expected: FAIL because the current migration only inserts combat rows for those tables.

**Step 3: Write minimal implementation**

Update the SQL migration data to include neutral and loot rows for both families.

**Step 4: Run test to verify it passes**

Run the same filtered command and expect PASS.

### Task 2: Verify no binding regressions

**Files:**
- No new files

**Step 1: Run focused tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`
Expected: PASS

**Step 2: Run solution tests**

Run: `dotnet test RaidLoop.sln`
Expected: PASS
