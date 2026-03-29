# Economy, Shopkeeper, and Stash Cap Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add money, item selling, shopkeeper purchases, and a 30-item stash cap while keeping the existing raid loop and right-side character inventory flow.

**Architecture:** Keep game flow in `Home.razor` and persistence in `StashStorage.cs` for MVP scope. Introduce lightweight economy helpers (price map + transfer operations) inside `Home.razor` and persist new fields in `GameSave`. Enforce stash capacity by disabling stash actions and validating server-side logic paths before mutating state.

**Tech Stack:** C# (.NET 10), Blazor WebAssembly, xUnit, localStorage persistence via JS interop

---

### Task 1: Add save-model support for money and character inventory

**Files:**
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs` (no change expected, sanity run)

**Step 1: Write the failing compile expectation in plan-driven form**

Target change will make existing calls to `new GameSave(...)` fail until all call sites are updated.

**Step 2: Run compile to verify red**

Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: FAIL once `GameSave` signature changes and call sites are not updated.

**Step 3: Write minimal implementation**

Update `GameSave` record:
```csharp
public sealed record GameSave(
    List<Item> MainStash,
    DateTimeOffset RandomCharacterAvailableAt,
    RandomCharacterState? RandomCharacter,
    int Money,
    List<Item> CharacterInventory);
```

Update defaults/migration in `StashStorage`:
```csharp
private const string SaveKey = "raidloop.save.v3";

return NormalizeSave(new GameSave(
    MainStash: [...],
    RandomCharacterAvailableAt: DateTimeOffset.MinValue,
    RandomCharacter: null,
    Money: 500,
    CharacterInventory: []));
```

Normalization rules:
```csharp
if (save.Money < 0) save = save with { Money = 0 };
save = save with { CharacterInventory = save.CharacterInventory ?? [] };
EnsureKnifeFallback(save.MainStash);
```

**Step 4: Run build to verify green**

Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Services/StashStorage.cs
git commit -m "feat: extend save model with money and character inventory"
```

### Task 2: Route extraction returns to character inventory (not stash)

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write failing test for extraction destination**

Add unit test for a new helper that will be introduced in `Home.razor`-adjacent logic extraction if needed (or introduce minimal static helper in `RaidLoop.Core` for routing):
```csharp
[Fact]
public void Extraction_ReturnsItemsToCharacterInventory_NotMainStash()
{
    var mainStash = new List<Item> { new("Bandage", ItemType.Consumable, 1) };
    var characterInventory = new List<Item>();
    var returned = new List<Item> { new("Medkit", ItemType.Consumable, 1) };

    EconomyRouting.ApplyExtractionResult(mainStash, characterInventory, returned);

    Assert.Single(mainStash);
    Assert.Contains(characterInventory, i => i.Name == "Medkit");
}
```

**Step 2: Run targeted test to verify red**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj -v minimal`
Expected: FAIL with missing helper/class.

**Step 3: Write minimal implementation**

Introduce helper in `src/RaidLoop.Core/EconomyRouting.cs`:
```csharp
public static class EconomyRouting
{
    public static void ApplyExtractionResult(List<Item> mainStash, List<Item> characterInventory, List<Item> returned)
    {
        characterInventory.AddRange(returned);
    }
}
```

Wire `EndRaidAsync` in `Home.razor` to append extracted main-character raid results into `_characterInventory` and not directly into `_mainGame.Stash`.

**Step 4: Run tests to verify green**

Run: `dotnet test RaidLoop.sln -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/EconomyRouting.cs src/RaidLoop.Client/Pages/Home.razor tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: route extracted loot to character inventory"
```

### Task 3: Implement stash cap and stash transfer action

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Write failing test for stash capacity rule**

Add core test helper for capacity check (either in `RaidLoop.Core` helper or existing test surface):
```csharp
[Fact]
public void CanMoveToStash_False_WhenStashIsAtCap()
{
    var stash = Enumerable.Repeat(new Item("Bandage", ItemType.Consumable, 1), 30).ToList();
    var canMove = EconomyRules.CanMoveToStash(stashCount: stash.Count, cap: 30);
    Assert.False(canMove);
}
```

**Step 2: Run test to verify red**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj -v minimal`
Expected: FAIL with missing `EconomyRules`.

**Step 3: Write minimal implementation**

Create `src/RaidLoop.Core/EconomyRules.cs`:
```csharp
public static class EconomyRules
{
    public static bool CanMoveToStash(int stashCount, int cap) => stashCount < cap;
}
```

In `Home.razor`:
- Add `private const int MainStashCap = 30;`
- Add `_characterInventory` state.
- Add `MoveToStash(Item item)`:
```csharp
if (!EconomyRules.CanMoveToStash(_mainGame.Stash.Count, MainStashCap)) return;
_characterInventory.Remove(item);
_mainGame.Stash.Add(item);
```
- Disable stash button in UI when cap reached.

**Step 4: Run tests/build to verify green**

Run: `dotnet test RaidLoop.sln -v minimal`
Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/EconomyRules.cs src/RaidLoop.Client/Pages/Home.razor tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: enforce stash cap and stash transfer action"
```

### Task 4: Implement selling from stash and character inventory

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Create: `src/RaidLoop.Core/EconomyPricing.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write failing pricing tests**

```csharp
[Theory]
[InlineData("Medkit", 120)]
[InlineData("Bandage", 60)]
[InlineData("Ammo Box", 80)]
public void GetBuyPrice_ReturnsExpected(string name, int expected)
{
    Assert.Equal(expected, EconomyPricing.GetBuyPrice(name));
}

[Fact]
public void GetSellPrice_IsLowerThanBuyPrice()
{
    var buy = EconomyPricing.GetBuyPrice("Medkit");
    var sell = EconomyPricing.GetSellPrice("Medkit");
    Assert.True(sell < buy);
}
```

**Step 2: Run tests to verify red**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj -v minimal`
Expected: FAIL with missing `EconomyPricing`.

**Step 3: Write minimal implementation**

Create pricing class:
```csharp
public static class EconomyPricing
{
    private static readonly Dictionary<string, int> Buy = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bandage"] = 60,
        ["Medkit"] = 120,
        ["Ammo Box"] = 80,
        ["Light Pistol"] = 240
    };

    public static int GetBuyPrice(string name) => Buy.TryGetValue(name, out var p) ? p : 100;
    public static int GetSellPrice(string name) => (int)Math.Floor(GetBuyPrice(name) * 0.6);
}
```

In `Home.razor`:
- Add `_money` state.
- Add `SellFromStash(Item item)` and `SellFromCharacterInventory(Item item)` that remove one specific item and add sell value.
- Persist after every sale.

**Step 4: Run tests/build to verify green**

Run: `dotnet test RaidLoop.sln -v minimal`
Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/EconomyPricing.cs src/RaidLoop.Client/Pages/Home.razor tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: add selling economy for stash and character inventory"
```

### Task 5: Implement shopkeeper panel with basic supplies and light weapon

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Write failing test for purchase affordability rule**

```csharp
[Fact]
public void CanBuyItem_False_WhenMoneyInsufficient()
{
    Assert.False(EconomyRules.CanBuyItem(money: 50, buyPrice: 60));
}
```

**Step 2: Run test to verify red**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj -v minimal`
Expected: FAIL for missing `CanBuyItem`.

**Step 3: Write minimal implementation**

Extend `EconomyRules`:
```csharp
public static bool CanBuyItem(int money, int buyPrice) => money >= buyPrice;
```

In `Home.razor`:
- Add shop stock list: `Bandage`, `Medkit`, `Ammo Box`, `Light Pistol`.
- Render buy buttons with price and disabled state when `_money` too low.
- `BuyFromShop(string itemName, ItemType type)` deducts money and adds item to `_characterInventory`.

**Step 4: Run tests/build to verify green**

Run: `dotnet test RaidLoop.sln -v minimal`
Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/EconomyRules.cs src/RaidLoop.Client/Pages/Home.razor tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: add shopkeeper with basic supplies and light weapon"
```

### Task 6: Wire persistence and final verification

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`

**Step 1: Write failing integration check (manual expectation)**

Document expected failing behavior before wiring save fields: money/inventory reset after refresh.

**Step 2: Run local app to observe red**

Run: `dotnet run --project src/RaidLoop.Client/RaidLoop.Client.csproj`
Expected: economic state not persisted before final wiring.

**Step 3: Write minimal implementation**

Update `OnInitializedAsync` and `SaveAllAsync` in `Home.razor` to round-trip:
```csharp
_money = save.Money;
_characterInventory = save.CharacterInventory;

await Storage.SaveAsync(new GameSave(
    MainStash: [.. _mainGame.Stash],
    RandomCharacterAvailableAt: _randomCharacterAvailableAt,
    RandomCharacter: _randomCharacter,
    Money: _money,
    CharacterInventory: [.. _characterInventory]));
```

**Step 4: Run verification commands**

Run: `dotnet test RaidLoop.sln -v minimal`
Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Run: `dotnet publish src/RaidLoop.Client/RaidLoop.Client.csproj -c Release -o publish -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Services/StashStorage.cs src/RaidLoop.Core/*.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: complete economy, stash cap, and shopkeeper flow"
```
