# Home.razor Component Decomposition Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Extract five focused Razor components from `Home.razor` while preserving all existing gameplay behavior and keeping shell-owned state in `Home.razor`.

**Architecture:** Keep all state, derived values, and mutation handlers in the `Home.razor` shell. Move only presentation markup and callback wiring into `StashPanel`, `LoadoutPanel`, `ShopPanel`, `PreRaidPanel`, and `RaidHUD`, and promote any client-only shared types needed by those components into accessible client files.

**Tech Stack:** .NET 10, Blazor WebAssembly, xUnit for existing core tests, PowerShell verification commands

---

### Task 1: Add pre-implementation verification checks and shared client types

**Files:**
- Create: `src/RaidLoop.Client/EncounterType.cs`
- Create: `src/RaidLoop.Client/Components/`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Write the failing test**

Run a repo-state check that expects the decomposition artifacts to exist and `Home.razor` to be shell-sized.

**Step 2: Run test to verify it fails**

Run: `@(Test-Path src/RaidLoop.Client/Components/StashPanel.razor), ((Get-Content src/RaidLoop.Client/Pages/Home.razor).Count -le 150)`
Expected: `False False`

**Step 3: Write minimal implementation**

Create `EncounterType.cs` and prepare the shell for component extraction by removing the private enum dependency.

**Step 4: Run test to verify progress**

Run: `dotnet build RaidLoop.sln`
Expected: build succeeds after the shared type move.

### Task 2: Extract pre-raid panels

**Files:**
- Create: `src/RaidLoop.Client/Components/StashPanel.razor`
- Create: `src/RaidLoop.Client/Components/LoadoutPanel.razor`
- Create: `src/RaidLoop.Client/Components/ShopPanel.razor`
- Create: `src/RaidLoop.Client/Components/PreRaidPanel.razor`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Write the failing test**

Run a repo-state check that expects all four panel files to exist before extraction.

**Step 2: Run test to verify it fails**

Run: `@(Test-Path src/RaidLoop.Client/Components/StashPanel.razor), (Test-Path src/RaidLoop.Client/Components/LoadoutPanel.razor), (Test-Path src/RaidLoop.Client/Components/ShopPanel.razor), (Test-Path src/RaidLoop.Client/Components/PreRaidPanel.razor)`
Expected: all `False`

**Step 3: Write minimal implementation**

Move only the pre-raid markup into the four components, keep shell handlers/derived values, and replace direct markup in `Home.razor` with component invocations.

**Step 4: Run test to verify it passes**

Run: `dotnet build RaidLoop.sln`
Expected: build succeeds with the extracted pre-raid panels.

### Task 3: Extract raid HUD and collapse the shell

**Files:**
- Create: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Write the failing test**

Run a repo-state check that expects `RaidHUD.razor` to exist and `Home.razor` to be at or under the shell line-count requirement.

**Step 2: Run test to verify it fails**

Run: `@(Test-Path src/RaidLoop.Client/Components/RaidHUD.razor), ((Get-Content src/RaidLoop.Client/Pages/Home.razor).Count -le 150)`
Expected: `False False`

**Step 3: Write minimal implementation**

Extract the in-raid markup to `RaidHUD.razor`, leave all handlers in `Home.razor`, and simplify the shell to orchestration markup plus code.

**Step 4: Run test to verify it passes**

Run: `dotnet build RaidLoop.sln`
Expected: build succeeds and `Home.razor` is shell-sized.

### Task 4: Verify the full feature contract

**Files:**
- Verify: `src/RaidLoop.Client/Pages/Home.razor`
- Verify: `src/RaidLoop.Client/Components/StashPanel.razor`
- Verify: `src/RaidLoop.Client/Components/LoadoutPanel.razor`
- Verify: `src/RaidLoop.Client/Components/ShopPanel.razor`
- Verify: `src/RaidLoop.Client/Components/PreRaidPanel.razor`
- Verify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Verify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Run verification**

Run:
- `dotnet test RaidLoop.sln`
- `dotnet build RaidLoop.sln`
- `@(Get-Content src/RaidLoop.Client/Pages/Home.razor).Count`

**Step 2: Confirm expected results**

Expected:
- solution tests pass
- solution build succeeds
- `Home.razor` line count is `<= 150`
