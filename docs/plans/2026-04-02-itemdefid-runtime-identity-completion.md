# ItemDefId Runtime Identity Completion Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Finish the item identity migration so gameplay, persistence, and event logic comprehend items only by `itemDefId`, while the client renders player-facing item labels from localization resources and falls back to the numeric `itemDefId`.

**Architecture:** Keep the existing `Item` contract shape for now, but demote `Name` to presentation-only metadata and remove every runtime logic path that depends on it. Route all authored item logic through `Item`, `itemDefId`, or `itemKey`, remove dead client-side gameplay save code, and move event/log payloads from display names to item ids so UI localization happens only at render time.

**Tech Stack:** C#, Blazor WebAssembly, xUnit, System.Text.Json, Supabase-backed profile/action APIs

---

### Task 1: Remove dead browser gameplay save code

**Files:**
- Delete: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Services/StashStorage.cs`
- Delete: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/StashStorageTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add assertions in `HomeMarkupBindingTests.cs` that the repo no longer contains the `StashStorage` service file or `StashStorageTests`.

```csharp
[Fact]
public void RepoNoLongerContainsLegacyGameplayStorage()
{
    Assert.False(File.Exists(Path.Combine(Root, "src", "RaidLoop.Client", "Services", "StashStorage.cs")));
    Assert.False(File.Exists(Path.Combine(Root, "tests", "RaidLoop.Core.Tests", "StashStorageTests.cs")));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RepoNoLongerContainsLegacyGameplayStorage`

Expected: FAIL because the legacy files still exist.

**Step 3: Delete the dead implementation**

- Remove `StashStorage.cs`.
- Remove `StashStorageTests.cs`.
- Keep `SupabaseAuthService` browser storage intact because it stores auth/session material, not gameplay state.
- Update any markup-binding tests that still reference the removed files only as positive existence checks.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RepoNoLongerContainsLegacyGameplayStorage`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git rm src/RaidLoop.Client/Services/StashStorage.cs tests/RaidLoop.Core.Tests/StashStorageTests.cs
git commit -m "refactor: remove legacy client gameplay storage"
```

### Task 2: Make client item labels come only from `itemDefId`

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/ItemPresentationCatalog.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing tests**

Add tests covering both the normal localized label path and the missing-resource fallback path.

```csharp
[Fact]
public void ItemPresentationCatalog_UsesLocalizedLabelByItemDefId()
{
    var item = ItemCatalog.GetByItemDefId(4);
    Assert.Equal("AK74", ItemPresentationCatalog.GetLabel(item));
}

[Fact]
public void ItemPresentationCatalog_FallsBackToItemDefIdWhenResourceMissing()
{
    var item = new Item("ignored", ItemType.Weapon, Weight: 1) { ItemDefId = 999 };
    Assert.Equal("999", ItemPresentationCatalog.GetLabel(item));
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemPresentationCatalog_UsesLocalizedLabelByItemDefId|ItemPresentationCatalog_FallsBackToItemDefIdWhenResourceMissing"`

Expected: the fallback test fails because the current code returns `item.Name`.

**Step 3: Implement the presentation-only label boundary**

Update `ItemPresentationCatalog.GetLabel(Item?)` to:
- return localized `Items.{itemDefId}` when found
- return `item.ItemDefId.ToString()` when `itemDefId > 0` and the resource is missing
- only return `item.Name` for non-authored or invalid items with no `itemDefId`

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemPresentationCatalog_UsesLocalizedLabelByItemDefId|ItemPresentationCatalog_FallsBackToItemDefIdWhenResourceMissing"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/ItemPresentationCatalog.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs
git commit -m "refactor: localize item labels by itemDefId only"
```

### Task 3: Stop client gameplay logic from using localized names

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/CombatBalance.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing tests**

Add tests proving that localized labels do not affect gameplay logic.

```csharp
[Fact]
public void EquippedWeaponCombatCapabilities_DoNotDependOnDisplayLabel()
{
    var item = ItemCatalog.GetByItemDefId(4) with { Name = "Field Carbine" };
    Assert.True(CombatBalance.SupportsBurstFire(item));
    Assert.True(CombatBalance.SupportsFullAuto(item));
    Assert.Equal(30, CombatBalance.GetMagazineCapacity(item));
}

[Fact]
public void HomeUsesEquippedItemIdentityForCombatChecks()
{
    // Set up a raid with an authored weapon whose display label differs from the localized resource.
    // Assert reload/attack capability remains based on identity, not label text.
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "EquippedWeaponCombatCapabilities_DoNotDependOnDisplayLabel|HomeUsesEquippedItemIdentityForCombatChecks"`

Expected: FAIL because `Home.razor.cs` still routes through `GetEquippedWeaponName()`.

**Step 3: Implement identity-based gameplay APIs**

- Add or standardize `CombatBalance` overloads that take `Item` for:
  - `SupportsSingleShot`
  - `SupportsBurstFire`
  - `SupportsFullAuto`
  - `GetMagazineCapacity`
  - `WeaponUsesAmmo`
  - `GetDamageRange`
  - `RollDamage` when practical
- In `Home.razor.cs`, replace `GetEquippedWeaponName()`-driven combat checks with `GetEquippedWeapon()` or direct `Item` access.
- Keep string overloads only as compatibility helpers where still needed by older tests or parsing paths.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "EquippedWeaponCombatCapabilities_DoNotDependOnDisplayLabel|HomeUsesEquippedItemIdentityForCombatChecks"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Core/CombatBalance.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "refactor: use item identity for client combat logic"
```

### Task 4: Refactor `CombatBalance` to comprehend authored items by identity

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/CombatBalance.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/ItemCatalog.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/Models.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing tests**

Add tests asserting authored-item logic succeeds even when `Name` is blank or non-canonical.

```csharp
[Fact]
public void GetBuyPrice_UsesItemDefIdWhenNameIsBlank()
{
    var item = ItemCatalog.GetByItemDefId(4) with { Name = string.Empty };
    Assert.Equal(1250, CombatBalance.GetBuyPrice(item));
}

[Fact]
public void BackpackCapacity_UsesItemDefIdWhenDisplayNameChanges()
{
    var item = ItemCatalog.GetByItemDefId(18) with { Name = "Raid Backpack" };
    Assert.Equal(10, CombatBalance.GetBackpackCapacity(item));
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "GetBuyPrice_UsesItemDefIdWhenNameIsBlank|BackpackCapacity_UsesItemDefIdWhenDisplayNameChanges"`

Expected: at least one FAIL if any fallback still uses `item.Name`.

**Step 3: Implement key/id-first balance helpers**

- In `CombatBalance`, centralize identity resolution:
  - authored items resolve via `itemDefId`
  - fallback via `itemKey`
  - legacy `Name` only for compatibility string overloads
- In `ItemCatalog`, keep `GetByLegacyName`/`TryGetByLegacyName` only as ingestion compatibility APIs.
- In `Models.cs`, leave `Name` on `Item` for now, but ensure authored-item behavior never needs it.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "GetBuyPrice_UsesItemDefIdWhenNameIsBlank|BackpackCapacity_UsesItemDefIdWhenDisplayNameChanges"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Core/ItemCatalog.cs src/RaidLoop.Core/Models.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "refactor: make combat balance itemDefId-first"
```

### Task 5: Make event/log payloads identity-based instead of label-based

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/GameEventLog.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Components/RaidHUD.razor`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/GameEventLogTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/GameEventValueScenarioTests.cs`

**Step 1: Write the failing tests**

Redefine the event snapshot contract around identity.

```csharp
[Fact]
public void CreateItemSnapshots_StoresItemDefIdInsteadOfDisplayName()
{
    var snapshot = Assert.Single(GameEventLog.CreateItemSnapshots([ItemCatalog.GetByItemDefId(4)]));
    Assert.Equal(4, snapshot.ItemDefId);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter CreateItemSnapshots_StoresItemDefIdInsteadOfDisplayName`

Expected: FAIL because `ItemSnapshot` currently stores `Name`.

**Step 3: Implement the new event shape**

- Change `ItemSnapshot` to include `ItemDefId`, category, rarity, and value.
- Remove display-name storage from event creation.
- Update any UI/event rendering path to localize item labels at display time through `ItemPresentationCatalog` or a small helper that reads `itemDefId`.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter CreateItemSnapshots_StoresItemDefIdInsteadOfDisplayName`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/GameEventLog.cs src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Components/RaidHUD.razor tests/RaidLoop.Core.Tests/GameEventLogTests.cs tests/RaidLoop.Core.Tests/GameEventValueScenarioTests.cs
git commit -m "refactor: store item ids in event snapshots"
```

### Task 6: Reduce `ItemCatalog` and JSON parsing to compatibility-only name handling

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/ItemCatalog.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/ItemJsonConverter.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/Models.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`

**Step 1: Write the failing tests**

Add tests proving legacy names are accepted only as input compatibility and that authored items can round-trip meaningfully by `itemDefId`.

```csharp
[Fact]
public void ItemJsonConverter_PrefersItemDefIdOverName()
{
    var json = """{"itemDefId":4,"name":"Wrong Display Label","type":"Weapon","weight":7}""";
    var item = JsonSerializer.Deserialize<Item>(json)!;
    Assert.Equal(4, item.ItemDefId);
}

[Fact]
public void ItemCatalog_LegacyNameLookupRemainsCompatibilityOnly()
{
    Assert.True(ItemCatalog.TryGetByLegacyName("AK74", out var item));
    Assert.Equal(4, item!.ItemDefId);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemJsonConverter_PrefersItemDefIdOverName|ItemCatalog_LegacyNameLookupRemainsCompatibilityOnly"`

Expected: FAIL if converter or catalog still privilege `Name` too early.

**Step 3: Implement the compatibility boundary**

- In `ItemJsonConverter`, resolve authored items by `itemDefId` first, then `itemKey`, then legacy `name`.
- Once an authored item is resolved, do not rely on the incoming `name` for logic.
- Keep `ItemCatalog` legacy-name APIs, but make them clearly compatibility-only helpers.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemJsonConverter_PrefersItemDefIdOverName|ItemCatalog_LegacyNameLookupRemainsCompatibilityOnly"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/ItemCatalog.cs src/RaidLoop.Core/ItemJsonConverter.cs src/RaidLoop.Core/Models.cs tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs
git commit -m "refactor: limit item names to compatibility parsing"
```

### Task 7: Convert client and core tests from name-truth to identity-truth

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/LootTableTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ContractsTests.cs`

**Step 1: Write the failing tests**

Introduce one narrow proving test per major area, then expand replacements file-by-file.

```csharp
[Fact]
public void ShopStockAssertions_UseItemDefIdInsteadOfName()
{
    Assert.Contains(shopStock, stock => stock.Item.ItemDefId == 3);
    Assert.Contains(shopStock, stock => stock.Item.ItemDefId == 8);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ShopStockAssertions_UseItemDefIdInsteadOfName`

Expected: FAIL until test setup and assertions are converted consistently.

**Step 3: Rewrite assertions by intent**

- Logic tests: assert `itemDefId`, `itemKey`, counts, equipment slots, stats, and prices.
- Presentation tests: assert `ItemPresentationCatalog.GetLabel(...)`.
- Compatibility tests: keep a small explicit set for legacy-name parsing.
- Remove brittle assumptions that `item.Name` equals the English authored label.

**Step 4: Run targeted suites**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ProfileMutationFlowTests`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidActionApiTests`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidEngineTests`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidStartApiTests`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs tests/RaidLoop.Core.Tests/LootTableTests.cs tests/RaidLoop.Core.Tests/ContractsTests.cs
git commit -m "test: assert item identity instead of item names"
```

### Task 8: Update localization data and add missing fallback coverage

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Resources/ItemResources.resx`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Resources/ItemResources.Designer.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing tests**

Add tests that pin:
- every authored `itemDefId` has a resource entry
- resource omission falls back to numeric id instead of `item.Name`

```csharp
[Fact]
public void AuthoredItems_HaveLocalizationEntries()
{
    foreach (var itemDefId in Enumerable.Range(1, 24))
    {
        var item = ItemCatalog.GetByItemDefId(itemDefId);
        Assert.NotEmpty(ItemPresentationCatalog.GetLabel(item));
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter AuthoredItems_HaveLocalizationEntries`

Expected: FAIL if any resource entries are missing after refactor changes.

**Step 3: Update resource data**

- Ensure `ItemResources.resx` includes the desired player-facing labels for every authored item id.
- Regenerate `ItemResources.Designer.cs` if the project/tooling requires it.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter AuthoredItems_HaveLocalizationEntries`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Resources/ItemResources.resx src/RaidLoop.Client/Resources/ItemResources.Designer.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: finalize item label resources by itemDefId"
```

### Task 9: Run the full verification suite

**Files:**
- No code changes expected

**Step 1: Run the core automated suite**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`

Expected: PASS

**Step 2: Run a repo-wide search for forbidden gameplay-name dependencies**

Run: `rg -n "item\\.Name|\\.Name\\b|GetByLegacyName|TryGetByLegacyName|CreateLegacy|StashStorage" src tests --glob '!**/bin/**' --glob '!**/obj/**'`

Expected:
- `item.Name` remains only in display-only, compatibility, or clearly intentional non-item-character-name contexts
- `GetByLegacyName` and `TryGetByLegacyName` remain only in parsing/compatibility code and tests
- `StashStorage` no longer appears

**Step 3: Review changed files**

Run: `git status --short`

Expected: only intended files are modified or deleted.

**Step 4: Commit the final verification pass**

```bash
git add -A
git commit -m "refactor: complete itemDefId runtime identity boundary"
```

### Task 10: Document the completed runtime identity boundary

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/README.md`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/docs/plans/2026-03-29-item-identity-refactor-design.md`

**Step 1: Write the failing doc check**

Add or update a doc-oriented test if the repo already has one, otherwise use a manual verification step:
- README should state that item labels are client-localized by `itemDefId`
- README should state that `Item.Name` is presentation-only
- README should state there is no client-side gameplay save storage

**Step 2: Update docs**

- Add a short “current architecture” section to `README.md`
- Add a final note to the earlier design doc linking the completed boundary to the new implementation reality

**Step 3: Verify docs**

Run: `rg -n "presentation-only|itemDefId|client-side gameplay save|StashStorage" README.md docs/plans/2026-03-29-item-identity-refactor-design.md`

Expected: the new architecture language is present.

**Step 4: Commit**

```bash
git add README.md docs/plans/2026-03-29-item-identity-refactor-design.md
git commit -m "docs: describe itemDefId-only runtime identity"
```
