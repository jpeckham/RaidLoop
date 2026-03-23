# Raid Action Button Consistency Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Keep raid combat actions visible but disabled when ammo is insufficient, and align `Attempt Extraction` to the right so it matches the `Move to Extract` position.

**Architecture:** The Blazor HUD currently hides combat buttons by conditionally rendering them, while the authoritative SQL raid action path already enforces ammo checks server-side. This change should move the client to always render supported combat actions with disabled states based on ammo thresholds, reorder the extraction actions in the HUD, and leave the backend ammo guards intact.

**Tech Stack:** Blazor/Razor, C#, xUnit, .NET test runner, PostgreSQL migration text assertions

---

### Task 1: Add failing UI regression tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing tests**

- Assert the combat action buttons are always rendered and use `disabled` bindings instead of `@if`.
- Assert `Continue Searching` appears before `Attempt Extraction` in the extraction action row.
- Assert the home page still passes the combat availability bindings needed for disabled state.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`

Expected: FAIL because the HUD still conditionally hides the combat buttons and extraction order is unchanged.

**Step 3: Write minimal implementation**

- Update the HUD markup and parameters to render supported actions with disabled states.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`

Expected: PASS

### Task 2: Wire client availability state for disabled combat actions

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Test: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing test**

- Assert supported fire modes stay available in the HUD model even when ammo is below their required thresholds, while their enabled-state flags go false.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileMutationFlowTests"`

Expected: FAIL because current `CanAttack` / `CanBurstFire` / `CanFullAuto` are used as render gates.

**Step 3: Write minimal implementation**

- Split support/visibility checks from enabled-state checks in the home page.
- Keep attack disabled at `0` ammo, burst disabled below `3`, and full auto disabled below `10`.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileMutationFlowTests"`

Expected: PASS

### Task 3: Verify backend guardrails remain intact

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Run targeted verification**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|ProfileMutationFlowTests"`

Expected: PASS

**Step 2: Commit**

```bash
git add docs/plans/2026-03-23-raid-action-button-consistency.md tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Components/RaidHUD.razor
git commit -m "fix: keep raid actions visible when disabled"
```
