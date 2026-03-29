# Loot Tiers Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add item rarity, weighted loot tables, structured event logging, and rarity-aware UI updates for the approved Loot Tiers feature while preserving existing gameplay and save compatibility.

**Architecture:** Keep loot generation, rarity assignment, and observability in `RaidLoop.Core`, with `Home.razor.cs` consuming those Core APIs and the decomposed Razor components rendering rarity-aware item names. Extend the existing `Item` record with a defaulted `Rarity` parameter so legacy constructor calls and v3 save payloads remain compatible.

**Tech Stack:** .NET 10, Blazor WebAssembly, xUnit, System.Text.Json, PowerShell verification commands

---

### Task 1: Add rarity to the core model

**Files:**
- Create: `src/RaidLoop.Core/Rarity.cs`
- Modify: `src/RaidLoop.Core/Models.cs`
- Test: `tests/RaidLoop.Core.Tests/RarityTests.cs`

**Step 1: Write the failing test**

Add tests that assert:
- `new Item("Pistol", ItemType.Weapon, 1).Rarity == Rarity.Common`
- `new Item("Field Carbine", ItemType.Weapon, 1, Rarity.Rare).Rarity == Rarity.Rare`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RarityTests"`
Expected: compile/test failure because `Rarity` and the new `Item` constructor parameter do not exist yet.

**Step 3: Write minimal implementation**

- Add `public enum Rarity { Common, Uncommon, Rare, Legendary }` in `src/RaidLoop.Core/Rarity.cs`
- Update `Item` in `src/RaidLoop.Core/Models.cs` to `public sealed record Item(string Name, ItemType Type, int Slots = 1, Rarity Rarity = Rarity.Common);`

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RarityTests"`
Expected: PASS

### Task 2: Implement weighted loot tables

**Files:**
- Create: `src/RaidLoop.Core/LootTable.cs`
- Create: `src/RaidLoop.Core/LootTables.cs`
- Test: `tests/RaidLoop.Core.Tests/LootTableTests.cs`

**Step 1: Write the failing test**

Add tests that cover:
- `Draw(rng, 0)` returns empty
- `Draw(rng, count)` returns distinct items without replacement
- requesting more than entry count returns all items without duplicates
- factory methods return non-null tables
- deterministic multi-draw rarity frequencies from `LootTables.MixedCache()` stay within the allowed band

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~LootTableTests"`
Expected: compile/test failure because `LootTable` and `LootTables` do not exist yet.

**Step 3: Write minimal implementation**

- Add an immutable `LootTable` that validates positive weights and draws without replacement using `IRng`
- Add `LootTables` static factories for `WeaponsCrate`, `ArmourCrate`, `MixedCache`, and `EnemyLoadout`
- Keep all weight constants named and local to `LootTables.cs`

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~LootTableTests"`
Expected: PASS

### Task 3: Add the new EncounterLoot overload

**Files:**
- Modify: `src/RaidLoop.Core/EncounterLoot.cs`
- Test: `tests/RaidLoop.Core.Tests/EncounterLootTests.cs`

**Step 1: Write the failing test**

Add tests that assert:
- `StartLootEncounter(List<Item>, LootTable, IRng, int)` populates the requested number of items
- `discoveredLoot` is cleared before the new draw
- the legacy overload still behaves as before

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~EncounterLootTests"`
Expected: compile/test failure because the overload does not exist yet.

**Step 3: Write minimal implementation**

- Add the new overload that clears `discoveredLoot`, calls `table.Draw(rng, drawCount)`, and appends the results
- Retain the legacy overload unchanged for now

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~EncounterLootTests"`
Expected: PASS

### Task 4: Add the in-memory event log contract

**Files:**
- Create: `src/RaidLoop.Core/GameEventLog.cs`
- Test: `tests/RaidLoop.Core.Tests/GameEventLogTests.cs`

**Step 1: Write the failing test**

Add tests that assert:
- `Append` stores events in order
- `Clear` empties the log
- `Events` is readable when empty and populated
- event/item snapshot constructors preserve name, category, and rarity strings

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~GameEventLogTests"`
Expected: compile/test failure because `GameEventLog`, `GameEvent`, and `ItemSnapshot` do not exist yet.

**Step 3: Write minimal implementation**

- Add `GameEventLog`, `GameEvent`, and `ItemSnapshot` in `src/RaidLoop.Core/GameEventLog.cs`
- Keep the implementation append-only and zero-dependency

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~GameEventLogTests"`
Expected: PASS

### Task 5: Integrate core observability and rarity-aware loot generation

**Files:**
- Modify: `src/RaidLoop.Core/LootTable.cs`
- Modify: `src/RaidLoop.Core/EncounterLoot.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Test: `tests/RaidLoop.Core.Tests/GameEventLogScenarioTests.cs`

**Step 1: Write the failing test**

Add deterministic scenario tests that assert:
- `loot.drawn` is emitted from loot-table draws with item rarity populated
- `enemy.loadout.generated` is emitted for generated enemy loadouts
- a new raid clears old event state before recording fresh events

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~GameEventLogScenarioTests"`
Expected: FAIL because the events are not emitted yet.

**Step 3: Write minimal implementation**

- Emit `loot.drawn` from `LootTable.Draw`
- Clear `GameEventLog` at raid start in `Home.razor.cs`
- Replace random flat enemy drops/loadouts with `LootTables.EnemyLoadout().Draw(...)`
- Emit `enemy.loadout.generated` with the generated items
- Route loot-container generation through the new loot-table overloads instead of flat random item lists

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~GameEventLogScenarioTests"`
Expected: PASS

### Task 6: Add extraction and player interaction observability

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Test: `tests/RaidLoop.Core.Tests/GameEventLogScenarioTests.cs`

**Step 1: Write the failing test**

Extend scenario tests to assert:
- successful equip actions append `player.equip`
- successful loot pickup appends `loot.acquired`
- successful extraction appends exactly one `extraction.complete` event with retained items

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~GameEventLogScenarioTests"`
Expected: FAIL because those events are not emitted yet.

**Step 3: Write minimal implementation**

- Append `player.equip` in the successful equip paths
- Append `loot.acquired` when loot is actually taken
- Append `extraction.complete` exactly once on successful extraction with the retained item snapshots

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~GameEventLogScenarioTests"`
Expected: PASS

### Task 7: Render rarity-aware UI in the decomposed components

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Components/StashPanel.razor`
- Modify: `src/RaidLoop.Client/Components/LoadoutPanel.razor`
- Modify: `src/RaidLoop.Client/Components/ShopPanel.razor`
- Modify: `src/RaidLoop.Client/Components/PreRaidPanel.razor`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`

**Step 1: Write the failing test**

Run a repo-state/UI contract check that expects:
- all rarity CSS classes to exist in `Home.razor`
- each item-rendering component to contain `rarity-@`

**Step 2: Run test to verify it fails**

Run: `@(Select-String -Path src/RaidLoop.Client/Pages/Home.razor -Pattern "rarity-common").Count, @(Select-String -Path src/RaidLoop.Client/Components/*.razor -Pattern "rarity-@").Count`
Expected: zero matches before the UI update.

**Step 3: Write minimal implementation**

- Add the four rarity classes to the `Home.razor` inline `<style>` block
- Wrap every rendered item name in the components with a `<span>` using the shared `rarity-@item.Rarity.ToString().ToLower()` pattern
- Keep all rarity logic out of the code-behind except for passing the enriched `Item` instances through

**Step 4: Run test to verify it passes**

Run: `@(Select-String -Path src/RaidLoop.Client/Pages/Home.razor -Pattern "rarity-common").Count, @(Select-String -Path src/RaidLoop.Client/Components/*.razor -Pattern "rarity-@").Count`
Expected: CSS class present and non-zero component matches.

### Task 8: Final solution verification and branch completion

**Files:**
- Verify: `src/RaidLoop.Core/Rarity.cs`
- Verify: `src/RaidLoop.Core/LootTable.cs`
- Verify: `src/RaidLoop.Core/LootTables.cs`
- Verify: `src/RaidLoop.Core/EncounterLoot.cs`
- Verify: `src/RaidLoop.Core/Models.cs`
- Verify: `src/RaidLoop.Core/GameEventLog.cs`
- Verify: `src/RaidLoop.Client/Pages/Home.razor`
- Verify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Verify: `src/RaidLoop.Client/Components/StashPanel.razor`
- Verify: `src/RaidLoop.Client/Components/LoadoutPanel.razor`
- Verify: `src/RaidLoop.Client/Components/ShopPanel.razor`
- Verify: `src/RaidLoop.Client/Components/PreRaidPanel.razor`
- Verify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Verify: `tests/RaidLoop.Core.Tests/*.cs`

**Step 1: Run verification**

Run:
- `dotnet test RaidLoop.sln`
- `dotnet build RaidLoop.sln`
- `git diff --stat`

**Step 2: Confirm expected results**

Expected:
- solution tests pass
- solution build succeeds with no new warnings
- diff only touches planned files plus the saved plan doc

**Step 3: Commit and push**

Run:
- `git add src/RaidLoop.Core src/RaidLoop.Client tests/RaidLoop.Core.Tests docs/plans/2026-03-16-loot-tiers-implementation.md`
- `git commit -m "feat: implement loot tiers"`
- `git push`

