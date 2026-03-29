# Shop Restriction and Sellable Type Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Restrict shopkeeper inventory, introduce `Sellable` item type, and make advanced guns/armor rare loot-only while keeping medkits consumable.

**Architecture:** Add `Sellable` to shared `ItemType`, migrate item typing and save normalization, and adjust client shop/loot generation to enforce rarity and loot-only progression. Preserve existing combat balance and medkit usage.

**Tech Stack:** C#/.NET 10, Blazor, xUnit

---

### Task 1: Add failing tests for Sellable type usage

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing test**
- Add a test using `ItemType.Sellable` in loot capacity flow.

**Step 2: Run test to verify it fails**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~Sellable"`
Expected: FAIL because enum value is missing.

**Step 3: Write minimal implementation**
- Add `Sellable` enum value.

**Step 4: Run test to verify it passes**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~Sellable"`
Expected: PASS.

### Task 2: Enforce shop and loot rules

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`

**Step 1: Implement minimal behavior**
- Shop sells only Light Pistol and Medkit.
- Advanced guns/armor removed from shop/random starter loadouts.
- Add weighted rare loot entries for advanced guns/armor.
- Convert Bandage/Ammo Box to `Sellable`.

**Step 2: Verify build and tests**
Run: `dotnet test RaidLoop.sln`
Expected: PASS.
