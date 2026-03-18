# Rarity Economy Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement rarity-scaled item values, extend event log payloads with value totals, and surface item and extraction values in the UI.

**Architecture:** Keep rarity-to-value balance centralized in `LootTables.cs` so `Item.Value` remains the single source of truth. Extend `GameEventLog` payload records to carry item value and extraction totals, then have `Home.razor.cs` and the Razor components render those existing values directly without introducing a second lookup table.

**Tech Stack:** .NET 10, Blazor WebAssembly, xUnit, C#

---

### Task 1: Add failing core tests for rarity values and event payloads

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/LootTableTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/GameEventLogTests.cs`

**Step 1: Write the failing tests**

Add tests that assert:
- each loot-table factory yields the expected per-rarity `Value` mapping
- rarity value ordering is strictly increasing
- `ItemSnapshot` preserves the new `Value`
- `GameEvent.TotalValue` defaults to `0`
- emitted `loot.drawn` snapshots include the drawn item value

**Step 2: Run tests to verify they fail**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~LootTableTests"`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~GameEventLogTests"`

Expected: failures because items still all carry slot count in the third constructor parameter, `ItemSnapshot` has no `Value`, and `GameEvent` has no `TotalValue`.

### Task 2: Implement core rarity values and event payload extensions

**Files:**
- Modify: `src/RaidLoop.Core/Models.cs`
- Modify: `src/RaidLoop.Core/LootTables.cs`
- Modify: `src/RaidLoop.Core/GameEventLog.cs`

**Step 1: Write minimal implementation**

- change `Item` to `Item(string Name, ItemType Type, int Value = 1, int Slots = 1, Rarity Rarity = Rarity.Common)`
- add named rarity value constants in `LootTables.cs`
- update every loot-table item constructor to pass `Value` and `Slots` explicitly
- extend `ItemSnapshot` with `int Value`
- extend `GameEvent` with `int TotalValue = 0`
- update `CreateItemSnapshots` to copy `item.Value`

**Step 2: Run tests to verify they pass**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~LootTableTests"`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~GameEventLogTests"`

Expected: PASS.

### Task 3: Add failing integration/UI assertions for extraction and value display

**Files:**
- Create: `tests/RaidLoop.Core.Tests/GameEventValueScenarioTests.cs`

**Step 1: Write the failing tests**

Add tests that assert:
- successful extraction emits `extraction.complete` with the retained item value total
- successful loot pickup emits `loot.acquired` with item value populated

Keep the test host at the `Home` page method level via reflection or direct component instantiation patterns already used in the repo; do not add browser automation.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~GameEventValueScenarioTests"`

Expected: FAIL because extraction events do not set `TotalValue`.

### Task 4: Implement UI and page wiring for value displays

**Files:**
- Modify: `src/RaidLoop.Client/Components/StashPanel.razor`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`

**Step 1: Write minimal implementation**

- render `@item.Value g` immediately after stash item names
- render per-item values and a total extracted value summary for the extraction success result
- compute extraction totals from retained items directly
- emit `extraction.complete` with `TotalValue`
- stop deriving stash sellability/value display from `GetSellPrice(item.Name)` where the spec requires `Item.Value`

**Step 2: Run verification**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~GameEventValueScenarioTests"`
- `rg -n "@item\\.Value|Total value:" src/RaidLoop.Client`

Expected: tests pass and the UI strings are present.

### Task 5: Full verification

**Files:**
- Verify: `src/RaidLoop.Core/GameEventLog.cs`
- Verify: `src/RaidLoop.Core/LootTables.cs`
- Verify: `src/RaidLoop.Core/Models.cs`
- Verify: `src/RaidLoop.Client/Pages/Home.razor`
- Verify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Verify: `src/RaidLoop.Client/Components/StashPanel.razor`
- Verify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Verify: `tests/RaidLoop.Core.Tests/*.cs`

**Step 1: Run verification**

Run:
- `dotnet test RaidLoop.sln`
- `dotnet build RaidLoop.sln`
- `git diff --stat`

**Step 2: Confirm expected results**

Expected:
- targeted and full tests pass
- solution build succeeds
- diff is limited to the planned files plus this plan doc
