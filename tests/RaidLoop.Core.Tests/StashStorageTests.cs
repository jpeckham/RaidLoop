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
        const string raw = """
            {
              "MainStash": [
                { "Name": "Hunting Rifle", "Type": 0, "Value": 1, "Slots": 1, "Weight": 1 }
              ],
              "RandomCharacterAvailableAt": "0001-01-01T00:00:00+00:00",
              "RandomCharacter": null,
              "Money": 500,
              "OnPersonItems": []
            }
            """;

        var storage = new StashStorage(new FakeJsRuntime(raw));

        var save = await storage.LoadAsync();

        Assert.Equal([ItemCatalog.Get("AK74")], save.MainStash);
    }

    [Fact]
    public async Task LoadAsync_PrefersItemDefinitionIdOverLegacyNameWhenBothArePresent()
    {
        const string raw = """
            {
              "MainStash": [
                {
                  "name": "Server-authored alias",
                  "itemDefId": "Makarov",
                  "type": 0,
                  "value": 60,
                  "slots": 1,
                  "weight": 2
                }
              ],
              "RandomCharacterAvailableAt": "0001-01-01T00:00:00+00:00",
              "RandomCharacter": null,
              "Money": 500,
              "OnPersonItems": []
            }
            """;

        var storage = new StashStorage(new FakeJsRuntime(raw));

        var save = await storage.LoadAsync();

        Assert.Equal([ItemCatalog.Get("Makarov")], save.MainStash);
    }

    [Fact]
    public async Task LoadAsync_UsesLegacyNameWhenItemDefinitionIdIsMissing()
    {
        const string raw = """
            {
              "MainStash": [
                {
                  "name": "Legacy label",
                  "type": 0,
                  "value": 60,
                  "slots": 1,
                  "weight": 2
                }
              ],
              "RandomCharacterAvailableAt": "0001-01-01T00:00:00+00:00",
              "RandomCharacter": null,
              "Money": 500,
              "OnPersonItems": []
            }
            """;

        var storage = new StashStorage(new FakeJsRuntime(raw));

        var save = await storage.LoadAsync();

        Assert.Equal("Legacy label", Assert.Single(save.MainStash).Name);
    }

    [Fact]
    public async Task LoadAsync_UsesLegacyNameWhenItemDefinitionIdIsUnknown()
    {
        const string raw = """
            {
              "MainStash": [
                {
                  "name": "Legacy label",
                  "itemDefId": 9999,
                  "type": 0,
                  "value": 60,
                  "slots": 1,
                  "weight": 2
                }
              ],
              "RandomCharacterAvailableAt": "0001-01-01T00:00:00+00:00",
              "RandomCharacter": null,
              "Money": 500,
              "OnPersonItems": []
            }
            """;

        var storage = new StashStorage(new FakeJsRuntime(raw));

        var save = await storage.LoadAsync();

        Assert.Equal("Legacy label", Assert.Single(save.MainStash).Name);
    }

    [Fact]
    public async Task LoadAsync_MigratesLegacyRandomCharacterWithoutStats()
    {
        const string raw = """
            {
              "MainStash": [],
              "RandomCharacterAvailableAt": "0001-01-01T00:00:00+00:00",
              "RandomCharacter": {
                "Name": "Ghost-101",
                "Inventory": [
                  { "Name": "Bandage", "Type": 4, "Value": 15, "Slots": 1, "Weight": 1 }
                ]
              },
              "Money": 500,
              "OnPersonItems": []
            }
            """;

        var storage = new StashStorage(new FakeJsRuntime(raw));

        var save = await storage.LoadAsync();

        Assert.NotNull(save.RandomCharacter);
        Assert.Equal("Ghost-101", save.RandomCharacter!.Name);
        Assert.Equal(PlayerStats.Default, save.RandomCharacter.Stats);
        Assert.Equal("Bandage", Assert.Single(save.RandomCharacter.Inventory).Name);
    }

    [Fact]
    public async Task LoadAsync_MigratesCamelCaseRandomCharacterWithoutStats()
    {
        const string raw = """
            {
              "mainStash": [
                { "name": "Makarov", "type": 0, "value": 60, "slots": 1, "weight": 4 }
              ],
              "randomCharacterAvailableAt": "2026-03-20T08:00:00Z",
              "randomCharacter": {
                "name": "Ghost-101",
                "inventory": [
                  { "name": "Bandage", "type": 4, "value": 15, "slots": 1, "weight": 1 }
                ]
              },
              "money": 500,
              "onPersonItems": []
            }
            """;

        var storage = new StashStorage(new FakeJsRuntime(raw));

        var save = await storage.LoadAsync();

        Assert.Equal(DateTimeOffset.Parse("2026-03-20T08:00:00Z"), save.RandomCharacterAvailableAt);
        Assert.NotNull(save.RandomCharacter);
        Assert.Equal("Ghost-101", save.RandomCharacter!.Name);
        Assert.Equal(PlayerStats.Default, save.RandomCharacter.Stats);
        Assert.Equal("Bandage", Assert.Single(save.RandomCharacter.Inventory).Name);
        Assert.Equal("Makarov", Assert.Single(save.MainStash).Name);
        Assert.Empty(save.OnPersonItems);
        Assert.Equal(500, save.Money);
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
