# Weapon-Specific Ammo and Knife Behavior Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add weapon-specific magazine capacities and make Rusty Knife attacks infinite with no ammo/reload requirements.

**Architecture:** Extend `CombatBalance` with ammo policy helpers and migrate raid combat UI logic to use current equipped weapon capacity and ammo usage flags. Keep malfunction and damage balance unchanged.

**Tech Stack:** C#/.NET 10, Blazor, xUnit

---

### Task 1: Add failing tests for ammo policy

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write failing tests**
- Assert magazine capacities for Makarov/PPSH/AK74/AK47/Rusty Knife and ammo-usage behavior for knife vs firearms.

**Step 2: Run test to verify it fails**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~Ammo"`
Expected: FAIL due to missing APIs.

**Step 3: Implement minimal API**
- Add ammo policy methods in `CombatBalance`.

**Step 4: Run targeted tests**
Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~Ammo"`
Expected: PASS.

### Task 2: Wire client combat to dynamic ammo and knife no-reload

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Implement behavior**
- Replace global max ammo usage with weapon-capacity helper.
- Knife bypasses ammo checks/decrement and reload requirements.
- Reload is no-op for knife with a clear log message.

**Step 2: Verify full suite**
Run: `dotnet test RaidLoop.sln`
Expected: PASS.
