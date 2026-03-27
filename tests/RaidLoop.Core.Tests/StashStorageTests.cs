using System.Text.Json;
using Microsoft.JSInterop;
using RaidLoop.Client.Services;
using RaidLoop.Core;

namespace RaidLoop.Core.Tests;

public sealed class StashStorageTests
{
    [Fact]
    public async Task LoadAsync_NormalizesKnownItemsToAuthoredCatalog()
    {
        var raw = JsonSerializer.Serialize(new GameSave(
            MainStash:
            [
                new Item("Makarov", ItemType.Weapon, Weight: 4, Value: 1, Slots: 1),
                new Item("Bandage", ItemType.Sellable, Weight: 1, Value: 1, Slots: 1)
            ],
            RandomCharacterAvailableAt: DateTimeOffset.MinValue,
            RandomCharacter: null,
            Money: 500,
            OnPersonItems:
            [
                new OnPersonEntry(new Item("Medkit", ItemType.Consumable, Weight: 3, Value: 1, Slots: 1), false)
            ]));

        var storage = new StashStorage(new FakeJsRuntime(raw));

        var save = await storage.LoadAsync();

        Assert.Equal(ItemCatalog.Get("Makarov"), save.MainStash[0]);
        Assert.Equal(ItemCatalog.Get("Bandage"), save.MainStash[1]);
        Assert.Equal(ItemCatalog.Get("Medkit"), save.OnPersonItems[0].Item);
    }

    [Fact]
    public async Task LoadAsync_NormalizesLegacyAliasesToCatalogDefinitions()
    {
        var raw = JsonSerializer.Serialize(new GameSave(
            MainStash:
            [
                new Item("Hunting Rifle", ItemType.Weapon, Weight: 1, Value: 1, Slots: 1)
            ],
            RandomCharacterAvailableAt: DateTimeOffset.MinValue,
            RandomCharacter: null,
            Money: 500,
            OnPersonItems: []));

        var storage = new StashStorage(new FakeJsRuntime(raw));

        var save = await storage.LoadAsync();

        Assert.Equal([ItemCatalog.Get("AK74")], save.MainStash);
    }

    private sealed class FakeJsRuntime(string? raw) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return identifier switch
            {
                "raidLoopStorage.load" => ValueTask.FromResult((TValue)(object?)raw!),
                _ => ValueTask.FromResult(default(TValue)!)
            };
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }
}
