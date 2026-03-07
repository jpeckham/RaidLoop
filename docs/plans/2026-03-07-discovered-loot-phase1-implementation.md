# Raid Discovered Loot Unification (Phase 1) Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Unify all raid loot discovery sources so search, combat drops, and event loot are presented through the same per-encounter discovered-loot area.

**Architecture:** Introduce a small shared loot-encounter helper in core for clear-vs-append semantics and route client encounter flows to it. Keep current per-encounter abandonment behavior unchanged.

**Tech Stack:** C#/.NET 10, Blazor, xUnit

---

### Task 1: Add failing tests for discovered loot clear/append semantics

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Create: `src/RaidLoop.Core/EncounterLoot.cs`

**Step 1: Write failing tests**
- Add tests proving `StartLootEncounter` clears prior discovered items and `AppendDiscoveredLoot` accumulates.

**Step 2: Run tests to verify failure**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~EncounterLoot"`
Expected: FAIL due to missing helper APIs.

**Step 3: Implement minimal helper**
- Add `EncounterLoot` static helper in core.

**Step 4: Run tests to verify pass**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~EncounterLoot"`
Expected: PASS.

### Task 2: Route raid loot sources to shared discovered-loot flow

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Implement minimal behavior**
- Search/container loot starts a loot encounter list.
- Combat victory starts a loot encounter list.
- Neutral hidden loot appends into discovered loot encounter instead of auto-looting to backpack.

**Step 2: Verify full suite**
Run: `dotnet test RaidLoop.sln`
Expected: PASS.
