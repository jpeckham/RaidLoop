# Rarity Pricing Rework Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Separate display rarity from loot generation rarity, restore authored item pricing, and change loot generation to tier-first rolling with booster-based tier shifts.

**Architecture:** Core owns item definitions, display rarity, source tier profiles, and booster-aware generation. The client becomes a consumer of authored item metadata for pricing and styling. Tests are added first for catalog metadata, tier rolling, booster shifts, and the UI binding regression.

**Tech Stack:** C#/.NET 10, xUnit, Blazor WebAssembly

---

### Task 1: Lock item metadata behavior with failing tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RarityTests.cs`
- Create: `tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`

**Step 1: Write the failing tests**

Add tests that assert:
- display rarity names are `SellOnly`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`
- key items have authored values greater than `1`
- larger backpacks cost more than smaller backpacks
- stronger weapons cost more than weaker weapons
- stronger armor costs more than weaker armor
- sell-only items are gray-tier metadata, not usable gear tiers

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~ItemCatalogTests|FullyQualifiedName~RarityTests"`
Expected: FAIL because the catalog and new display rarity model do not exist yet.

**Step 3: Write minimal implementation**

Create the new enum and item catalog stubs in Core with just enough metadata to satisfy the tests.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~ItemCatalogTests|FullyQualifiedName~RarityTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RarityTests.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs src/RaidLoop.Core
git commit -m "feat: add authored item metadata model"
```

### Task 2: Migrate item creation paths to authored definitions

**Files:**
- Modify: `src/RaidLoop.Core/Models.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Core/GameEventLog.cs`
- Test: `tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`

**Step 1: Write the failing test**

Add tests that assert:
- starter/fallback gear uses authored values and display rarity
- generated random loadout items use authored values
- medkits materialized from raid inventory no longer default to `1`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter FullyQualifiedName~ItemCatalogTests`
Expected: FAIL because creation paths still use raw constructors.

**Step 3: Write minimal implementation**

Route named item creation through catalog helpers/factories and update the affected call sites.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter FullyQualifiedName~ItemCatalogTests`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Models.cs src/RaidLoop.Client/Pages/Home.razor.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs
git commit -m "fix: use authored item values across creation paths"
```

### Task 3: Replace old rarity-weighted loot tables with source tier profiles

**Files:**
- Modify: `src/RaidLoop.Core/LootTables.cs`
- Modify: `src/RaidLoop.Core/LootTable.cs`
- Create: `src/RaidLoop.Core/LootTierProfile.cs`
- Create: `src/RaidLoop.Core/LootBooster.cs`
- Modify: `tests/RaidLoop.Core.Tests/LootTableTests.cs`

**Step 1: Write the failing tests**

Add tests that assert:
- a source rolls from tier probabilities rather than old per-item rarity weights
- higher-tier items can be produced for a source with matching tier profile
- a booster shifts the rolled tier upward
- no booster leaves the base distribution unchanged

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter FullyQualifiedName~LootTableTests`
Expected: FAIL because the source-tier model does not exist yet.

**Step 3: Write minimal implementation**

Implement source tier profiles, booster-aware tier shifting, and item selection within the chosen tier.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter FullyQualifiedName~LootTableTests`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/LootTables.cs src/RaidLoop.Core/LootTable.cs src/RaidLoop.Core/LootTierProfile.cs src/RaidLoop.Core/LootBooster.cs tests/RaidLoop.Core.Tests/LootTableTests.cs
git commit -m "feat: add tier-first loot generation"
```

### Task 4: Update UI rarity styling and price rendering

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Components/LoadoutPanel.razor`
- Modify: `src/RaidLoop.Client/Components/StashPanel.razor`
- Modify: `src/RaidLoop.Client/Components/PreRaidPanel.razor`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add assertions that the home page style block and markup use the new display rarity CSS classes and still bind dynamic strings correctly.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter FullyQualifiedName~HomeMarkupBindingTests`
Expected: FAIL because the old rarity classes are still present.

**Step 3: Write minimal implementation**

Update CSS classes and markup to use the new display rarity names and colors:
- gray `SellOnly`
- white `Common`
- green `Uncommon`
- blue `Rare`
- yellow `Epic`
- orange `Legendary`

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter FullyQualifiedName~HomeMarkupBindingTests`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Components tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: align ui rarity presentation"
```

### Task 5: Normalize save/load and final verification

**Files:**
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`
- Modify: `tests/RaidLoop.Core.Tests/GameEventLogTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/GameEventValueScenarioTests.cs`

**Step 1: Write the failing test**

Add tests that assert:
- loaded known items are normalized to authored values/display rarity
- event snapshots still emit correct value and rarity strings after migration

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~GameEvent|FullyQualifiedName~Stash"`
Expected: FAIL because save normalization and event payload updates are not complete.

**Step 3: Write minimal implementation**

Normalize known items during load/migration and adjust event serialization if needed for the new display rarity naming.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~GameEvent|FullyQualifiedName~Stash"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Services/StashStorage.cs tests/RaidLoop.Core.Tests/GameEventLogTests.cs tests/RaidLoop.Core.Tests/GameEventValueScenarioTests.cs
git commit -m "fix: normalize saved items to authored metadata"
```

### Task 6: Full verification

**Files:**
- No code changes required unless failures appear

**Step 1: Run the full test suite**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore`
Expected: PASS

**Step 2: Run client build**

Run: `dotnet build src/RaidLoop.Client/RaidLoop.Client.csproj --no-restore`
Expected: PASS

**Step 3: Review diffs**

Run: `git diff -- src/RaidLoop.Core src/RaidLoop.Client tests/RaidLoop.Core.Tests`
Expected: only intended rarity/pricing/generation changes

**Step 4: Commit**

```bash
git add src/RaidLoop.Core src/RaidLoop.Client tests/RaidLoop.Core.Tests
git commit -m "feat: rework rarity pricing and loot generation"
```
