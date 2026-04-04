# Client-Only Item Label Finalization Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Finish the item-name migration so authored item renames are a client resource change only, while legacy `name` payloads remain accepted strictly as ingestion compatibility.

**Architecture:** Treat `itemDefId` as the only trusted runtime identity for authored items. Keep `Item.Name` only as compatibility input and non-authored fallback text. Client rendering resolves authored labels from `ItemResources.resx`; gameplay, pricing, combat, inventory capacity, events, and contracts must not rely on authored English names.

**Tech Stack:** C#, Blazor WebAssembly, xUnit, System.Text.Json, `.resx` localization resources

---

### Task 1: Restore a compilable baseline before touching behavior

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Services/StashStorage.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Services/ClientProfileState.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a narrow compilation-shape test to pin that the shared client state records exist only once in the repo.

```csharp
[Fact]
public void ClientProfileStateTypes_AreDefinedInOneSourceFile()
{
    var stashStorage = File.ReadAllText(StashStoragePath);
    var clientProfileState = File.ReadAllText(ClientProfileStatePath);

    Assert.DoesNotContain("public sealed record RandomCharacterState", stashStorage);
    Assert.Contains("public sealed record RandomCharacterState", clientProfileState);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ClientProfileStateTypes_AreDefinedInOneSourceFile`

Expected: FAIL because `StashStorage.cs` currently duplicates `RandomCharacterState`, `GameSave`, and `OnPersonEntry`.

**Step 3: Remove the duplicate type declarations**

- Keep the shared records only in `ClientProfileState.cs`.
- Remove the duplicate record declarations from the bottom of `StashStorage.cs`.
- Do not change runtime behavior yet.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ClientProfileStateTypes_AreDefinedInOneSourceFile`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Services/StashStorage.cs src/RaidLoop.Client/Services/ClientProfileState.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "build: dedupe client profile state records"
```

### Task 2: Make authored client labels come from resources only

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/ItemPresentationCatalog.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Resources/ItemResources.resx`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ContractsTests.cs`

**Step 1: Write the failing tests**

Add tests for the authored-item resource path and the authored-item missing-resource fallback.

```csharp
[Fact]
public void ItemPresentationCatalog_UsesResourceValueForAuthoredItem()
{
    Assert.Equal("AK74", ItemPresentationCatalog.GetLabel(ItemCatalog.GetByItemDefId(4)));
}

[Fact]
public void ItemPresentationCatalog_FallsBackToItemDefIdForMissingAuthoredResource()
{
    var item = ItemCatalog.GetByItemDefId(4) with { Name = "Wrong Label" };
    using var cultureScope = new TestResourceCultureScope("qps-ploc");

    Assert.Equal("4", ItemPresentationCatalog.GetLabel(item));
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemPresentationCatalog_UsesResourceValueForAuthoredItem|ItemPresentationCatalog_FallsBackToItemDefIdForMissingAuthoredResource"`

Expected: the fallback test fails because authored items currently fall back to `item.Name`.

**Step 3: Implement the authored-label boundary**

Update `ItemPresentationCatalog.GetLabel(Item?)` so that:
- if `item.ItemDefId > 0` and `Items.{itemDefId}` exists, return the resource value
- if `item.ItemDefId > 0` and the resource is missing, return `item.ItemDefId.ToString()`
- only return `item.Name` for non-authored or malformed items with no `itemDefId`

Keep `ItemResources.resx` complete for all current authored ids.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemPresentationCatalog_UsesResourceValueForAuthoredItem|ItemPresentationCatalog_FallsBackToItemDefIdForMissingAuthoredResource"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/ItemPresentationCatalog.cs src/RaidLoop.Client/Resources/ItemResources.resx tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs tests/RaidLoop.Core.Tests/ContractsTests.cs
git commit -m "refactor: source authored item labels from resources only"
```

### Task 3: Remove authored-item gameplay dependence on `Name`

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/CombatBalance.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/Models.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Pages/Home.razor.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing tests**

Pin the key authored-item operations against mutated display names.

```csharp
[Fact]
public void GetBuyPrice_UsesItemDefIdWhenAuthoredNameChanges()
{
    var item = ItemCatalog.GetByItemDefId(4) with { Name = "Field Carbine" };
    Assert.Equal(1250, CombatBalance.GetBuyPrice(item));
}

[Fact]
public void BackpackCapacity_UsesItemDefIdWhenAuthoredNameChanges()
{
    var item = ItemCatalog.GetByItemDefId(18) with { Name = "Raid Pack" };
    Assert.Equal(10, CombatBalance.GetBackpackCapacity(item));
}

[Fact]
public void IsMedkit_UsesItemDefIdWhenAuthoredNameChanges()
{
    var item = ItemCatalog.GetByItemDefId(19) with { Name = "Trauma Kit" };
    Assert.True(CombatBalance.IsMedkit(item));
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "GetBuyPrice_UsesItemDefIdWhenAuthoredNameChanges|BackpackCapacity_UsesItemDefIdWhenAuthoredNameChanges|IsMedkit_UsesItemDefIdWhenAuthoredNameChanges"`

Expected: FAIL if any authored-item path still falls back to `item.Name`.

**Step 3: Implement item-first helper paths**

- Add a single internal authored-item resolver in `CombatBalance` that prefers:
  - `itemDefId`
  - then `itemKey`
  - then legacy name only for compatibility
- Route `GetBuyPrice(Item)`, `GetBackpackCapacity(Item?)`, `IsMedkit(Item?)`, and any similar authored-item helpers through that resolver.
- In `Home.razor.cs`, use `Item`-based combat helpers where authored equipped items are already available, instead of converting them back to names.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "GetBuyPrice_UsesItemDefIdWhenAuthoredNameChanges|BackpackCapacity_UsesItemDefIdWhenAuthoredNameChanges|IsMedkit_UsesItemDefIdWhenAuthoredNameChanges"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Core/Models.cs src/RaidLoop.Client/Pages/Home.razor.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs
git commit -m "refactor: use authored item identity for gameplay helpers"
```

### Task 4: Confine legacy name handling to ingestion compatibility

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/ItemJsonConverter.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/ItemCatalog.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Services/StashStorage.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Test: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/StashStorageTests.cs`

**Step 1: Write the failing tests**

Add tests that separate compatibility input from normal runtime truth.

```csharp
[Fact]
public void ItemJsonConverter_ResolvesAuthoredItemByItemDefIdEvenWhenNameDisagrees()
{
    const string json = """{"itemDefId":4,"name":"Wrong Label","type":0,"value":320,"slots":1,"rarity":2,"displayRarity":3,"weight":7}""";
    var item = JsonSerializer.Deserialize<Item>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    Assert.Equal(4, item!.ItemDefId);
    Assert.Equal("ak74", item.Key);
}

[Fact]
public void LegacyNamePayloads_AreAcceptedOnlyAsCompatibilityInput()
{
    const string json = """{"name":"Makarov","type":0,"value":60,"slots":1,"rarity":0,"displayRarity":1,"weight":2}""";
    var item = JsonSerializer.Deserialize<Item>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    Assert.Equal(2, item!.ItemDefId);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemJsonConverter_ResolvesAuthoredItemByItemDefIdEvenWhenNameDisagrees|LegacyNamePayloads_AreAcceptedOnlyAsCompatibilityInput"`

Expected: FAIL if any path still privileges incoming `name` over authored identity.

**Step 3: Tighten the compatibility boundary**

- Keep `TryResolveAuthoredItem` order as `itemDefId`, then `itemKey`, then legacy `name`.
- Ensure authored resolution discards incoming display text as runtime truth.
- In `StashStorage`, keep legacy normalization only for old local payload ingestion.
- Do not add any new code that converts authored runtime objects back to names for gameplay use.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemJsonConverter_ResolvesAuthoredItemByItemDefIdEvenWhenNameDisagrees|LegacyNamePayloads_AreAcceptedOnlyAsCompatibilityInput"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/ItemJsonConverter.cs src/RaidLoop.Core/ItemCatalog.cs src/RaidLoop.Client/Services/StashStorage.cs tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/StashStorageTests.cs
git commit -m "refactor: limit item names to compatibility ingestion"
```

### Task 5: Convert tests from English-name truth to identity truth

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`

**Step 1: Write the failing tests**

Add one proving test that explicitly mutates authored display names and verifies runtime behavior survives.

```csharp
[Fact]
public void AuthoredItemRuntimeBehavior_DoesNotDependOnEnglishName()
{
    var weapon = ItemCatalog.GetByItemDefId(4) with { Name = "Renamed in UI" };

    Assert.True(CombatBalance.SupportsBurstFire(weapon));
    Assert.True(CombatBalance.SupportsFullAuto(weapon));
    Assert.Equal(30, CombatBalance.GetMagazineCapacity(weapon));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter AuthoredItemRuntimeBehavior_DoesNotDependOnEnglishName`

Expected: FAIL until all relevant logic uses identity-first helpers.

**Step 3: Rewrite assertions by intent**

- Logic tests should assert `itemDefId`, `itemKey`, counts, values, slots, and equipment roles.
- Presentation tests should assert `ItemPresentationCatalog.GetLabel(...)`.
- Keep only a small explicit set of tests asserting `Name`, and only for compatibility ingestion or non-authored items.

**Step 4: Run targeted suites**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ContractsTests`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ItemCatalogTests`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ProfileMutationFlowTests`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidActionApiTests`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidEngineTests`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidStartApiTests`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs
git commit -m "test: assert item identity instead of authored names"
```

### Task 6: Add the decisive rename-proof checks and run full verification

**Files:**
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Modify: `C:/users/james/source/repos/extractor-shooter-light/README.md`

**Step 1: Write the failing tests**

Add final proof tests that capture the actual business goal.

```csharp
[Fact]
public void AuthoredItemLabelChanges_AreClientResourceOnly()
{
    var item = ItemCatalog.GetByItemDefId(2) with { Name = "Server Old Name" };

    Assert.Equal("Makarov", ItemPresentationCatalog.GetLabel(item));
    Assert.Equal(240, CombatBalance.GetBuyPrice(item));
}

[Fact]
public void AuthoredItemSerialization_DoesNotWriteName()
{
    var json = JsonSerializer.Serialize(ItemCatalog.GetByItemDefId(2), new JsonSerializerOptions(JsonSerializerDefaults.Web));

    Assert.Contains("\"itemDefId\":2", json);
    Assert.DoesNotContain("\"name\":", json);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "AuthoredItemLabelChanges_AreClientResourceOnly|AuthoredItemSerialization_DoesNotWriteName"`

Expected: FAIL if any authored-item rendering or serialization still depends on `Name`.

**Step 3: Update docs**

Document the final boundary in `README.md`:
- authored item labels come from client resources keyed by `itemDefId`
- authored item renames are content/localization changes, not gameplay migrations
- legacy `name` fields are accepted only for old payload ingestion

**Step 4: Run full verification**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`
- `rg -n -F "return item.Name;" src tests`
- `rg -n -F "return GetBuyPrice(item.Name);" src tests`
- `rg -n -F "return GetBackpackCapacity(backpack.Name);" src tests`
- `rg -n -F "NormalizeItemName(item.Name)" src tests`
- `git status --short`

Expected:
- full test suite PASS
- remaining `item.Name` usages are only in explicit compatibility ingestion or intentional presentation fallback for non-authored items
- no authored gameplay path falls back to `Name`
- working tree contains only intended changes

**Step 5: Commit**

```bash
git add README.md tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs tests/RaidLoop.Core.Tests/ContractsTests.cs
git commit -m "refactor: finalize client-only authored item labels"
```
