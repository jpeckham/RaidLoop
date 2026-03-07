# Weapon Damage, Armor Quality, and Pricing Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add deterministic, weapon-specific damage ranges, armor-based damage reduction tiers, and updated pricing for the new weapon/armor set.

**Architecture:** Move combat balance data and calculations into `RaidLoop.Core` via a small gameplay catalog and combat resolver that accept an injectable RNG abstraction. Update the Blazor page to call core helpers instead of hardcoded ranges and item names. Preserve save compatibility by normalizing legacy item names during load.

**Tech Stack:** C#/.NET 10, xUnit, Blazor

---

### Task 1: Add failing tests for catalog, deterministic RNG combat, and armor reduction

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing tests**
- Add tests for weapon damage ranges (Makarov/PPSH/AK74/AK47), armor reduction tiers (6B2/6B13/6B43), deterministic damage roll with a fake RNG, and armor floor behavior.

**Step 2: Run test to verify it fails**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~CombatBalance"`
Expected: FAIL because new combat balance APIs do not exist yet.

**Step 3: Write minimal implementation**
- Add core types and methods required by tests only.

**Step 4: Run test to verify it passes**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~CombatBalance"`
Expected: PASS.

**Step 5: Commit**
```bash
git add tests/RaidLoop.Core.Tests/RaidEngineTests.cs src/RaidLoop.Core/*.cs
git commit -m "feat: add deterministic combat balance catalog"
```

### Task 2: Wire client to new catalog data and replace item set

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`

**Step 1: Write failing tests (indirect via core tests + build)**
- Build should fail if client still references removed names/old pricing assumptions.

**Step 2: Run build to verify failure if APIs mismatch**
Run: `dotnet build RaidLoop.sln`
Expected: compile errors until client methods are migrated.

**Step 3: Write minimal implementation**
- Replace hardcoded weapon/armor names, price switches, and damage ranges with core catalog/resolver calls.
- Add legacy item-name migration in save normalization.

**Step 4: Run build and tests**
Run: `dotnet test RaidLoop.sln`
Expected: PASS.

**Step 5: Commit**
```bash
git add src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Services/StashStorage.cs
git commit -m "feat: use weapon and armor balance catalog in client"
```

### Task 3: Verify end-to-end behavior and regression coverage

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs` (if additional edge coverage needed)

**Step 1: Add any missing failing regression test**
- Cover unknown item fallback behavior if needed.

**Step 2: Run test to verify it fails**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~Unknown"`
Expected: FAIL before code adjustment.

**Step 3: Implement minimal fix**
- Add fallback defaults for unknown names.

**Step 4: Run full verification**
Run: `dotnet test RaidLoop.sln`
Expected: all tests pass cleanly.

**Step 5: Commit**
```bash
git add -A
git commit -m "test: cover combat balance fallback behavior"
```
