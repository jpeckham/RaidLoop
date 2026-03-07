# Raid Inventory Architecture (Phase 2) Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Move raid loot/equip/drop behavior into a core `RaidInventory` architecture with discovered-loot transfers, medkit resource handling, and backpack spill rules.

**Architecture:** Extend core models and `RaidEngine` with inventory operations, then migrate `Home.razor` to consume only core raid-inventory state for equip/carry/discovered behavior. Preserve encounter-local discovered-loot abandonment semantics.

**Tech Stack:** C#/.NET 10, Blazor, xUnit

---

### Task 1: Add failing core tests for raid inventory operations

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write failing tests**
- Medkit looting increments medkit count and does not consume carried slots.
- Equipping slot item from discovered swaps old equipped item into discovered.
- Dropping equipped backpack spills carried items to discovered.

**Step 2: Run targeted tests and verify failure**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidInventory"`
Expected: FAIL due to missing APIs.

### Task 2: Implement core raid inventory model and operations

**Files:**
- Modify: `src/RaidLoop.Core/Models.cs`
- Modify: `src/RaidLoop.Core/RaidEngine.cs`
- Modify: `src/RaidLoop.Core/CombatBalance.cs`

**Step 1: Implement minimal core model/ops**
- Add `RaidInventory` to raid state.
- Add raid inventory operations for discovered/carry/equip/drop.
- Add backpack capacity lookup in core.

**Step 2: Run targeted tests**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidInventory"`
Expected: PASS.

### Task 3: Wire client to core raid inventory state

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Migrate UI/actions**
- Render discovered/equipped/carried from `RaidState.Inventory`.
- Route loot/equip/drop actions to core operations.
- Medkits remain resource-only and never shown as carried.

**Step 2: Verify full suite**
Run: `dotnet test RaidLoop.sln`
Expected: PASS.
