using System.Reflection;
using Microsoft.JSInterop;
using RaidLoop.Client.Pages;
using RaidLoop.Client.Services;
using RaidLoop.Core;

namespace RaidLoop.Core.Tests;

[Collection("GameEventLog")]
public class GameEventValueScenarioTests : IDisposable
{
    public GameEventValueScenarioTests()
    {
        GameEventLog.Clear();
    }

    [Fact]
    public async Task EndRaidAsync_SuccessfulExtraction_EmitsTotalValue()
    {
        var home = CreateHome();
        var equipped = new Item("PPSH", ItemType.Weapon, Value: 3, Slots: 1, Rarity: Rarity.Uncommon);
        var carried = new Item("Rare Scope", ItemType.Material, Value: 8, Slots: 1, Rarity: Rarity.Rare);
        var raid = new RaidState(
            health: 30,
            inventory: RaidInventory.FromItems([equipped], [carried], backpackCapacity: 4));

        SetField(home, "_mainGame", new GameState([]));
        SetField(home, "_activeGame", new GameState([]));
        SetField(home, "_raid", raid);
        SetField(home, "_activeRaidId", "raid-value");

        await InvokePrivateAsync(home, "EndRaidAsync", true, "Extraction complete.");

        var evt = Assert.Single(GameEventLog.Events);
        Assert.Equal("extraction.complete", evt.EventName);
        Assert.Equal(11, evt.TotalValue);
        Assert.Equal([3, 8], evt.Items.Select(x => x.Value).Order().ToArray());
    }

    [Fact]
    public async Task TakeLootAsync_EmitsLootAcquiredWithItemValue()
    {
        var home = CreateHome();
        var loot = new Item("AK47", ItemType.Weapon, Value: 20, Slots: 1, Rarity: Rarity.Legendary);
        var raid = new RaidState(
            health: 30,
            inventory: RaidInventory.FromItems([], [], backpackCapacity: 4));
        RaidEngine.StartDiscoveredLootEncounter(raid, [loot]);

        SetField(home, "_raid", raid);
        SetField(home, "_activeRaidId", "raid-loot");

        await InvokePrivateAsync(home, "TakeLootAsync", loot);

        var evt = Assert.Single(GameEventLog.Events);
        Assert.Equal("loot.acquired", evt.EventName);
        Assert.Equal(20, Assert.Single(evt.Items).Value);
    }

    [Fact]
    public async Task SellStashItemAsync_UsesItemValueForPayout()
    {
        var home = CreateHome();
        var soldItem = new Item("AK47", ItemType.Weapon, Value: 20, Slots: 1, Rarity: Rarity.Legendary);

        SetField(home, "_mainGame", new GameState([soldItem]));
        SetField(home, "_money", 0);
        SetField(home, "_onPersonItems", new List<OnPersonEntry>());

        await InvokePrivateAsync(home, "SellStashItemAsync", 0);

        var mainGame = Assert.IsType<GameState>(GetField(home, "_mainGame"));
        var fallback = Assert.Single(mainGame.Stash);
        Assert.Equal("Rusty Knife", fallback.Name);
        Assert.Equal(20, Assert.IsType<int>(GetField(home, "_money")));
    }

    [Fact]
    public async Task EndRaidAsync_SuccessfulLuckRun_KeepsLootInLuckRunInventory()
    {
        var home = CreateHome();
        var equipped = ItemCatalog.Create("Makarov");
        var carried = ItemCatalog.Create("Bandage");
        var raid = new RaidState(
            health: 30,
            inventory: RaidInventory.FromItems([equipped], [carried], backpackCapacity: 4));

        SetField(home, "_mainGame", new GameState([]));
        SetField(home, "_activeGame", new GameState([]));
        SetField(home, "_raid", raid);
        SetField(home, "_activeRaidId", "raid-luck");
        SetField(home, "_randomCharacter", new RandomCharacterState("Ghost-101", []));
        SetNestedEnumField(home, "_activeProfile", "Random");

        await InvokePrivateAsync(home, "EndRaidAsync", true, "Luck run extracted.");

        var randomCharacter = Assert.IsType<RandomCharacterState>(GetField(home, "_randomCharacter"));
        Assert.NotEmpty(randomCharacter.Inventory);
    }

    public void Dispose()
    {
        GameEventLog.Clear();
    }

    private static Home CreateHome()
    {
        var home = new Home();
        var storageProperty = home.GetType().GetProperty("Storage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(storageProperty);
        storageProperty!.SetValue(home, new StashStorage(new FakeJsRuntime()));
        return home;
    }

    private static void SetField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static object? GetField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field!.GetValue(instance);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var task = method!.Invoke(instance, args) as Task;
        Assert.NotNull(task);
        await task!;
    }

    private static void SetNestedEnumField(object instance, string fieldName, string enumValue)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var enumType = field!.FieldType;
        field.SetValue(instance, Enum.Parse(enumType, enumValue));
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return identifier switch
            {
                "raidLoopStorage.load" => ValueTask.FromResult((TValue)(object?)null!),
                _ => ValueTask.FromResult(default(TValue)!)
            };
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }
}
