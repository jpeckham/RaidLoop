using System.Text.Json;
using Microsoft.JSInterop;
using RaidLoop.Core;

namespace RaidLoop.Client.Services;

public sealed class StashStorage
{
    private const string SaveKey = "raidloop.save.v3";
    private readonly IJSRuntime _js;

    public StashStorage(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<GameSave> LoadAsync()
    {
        string? raw;
        try
        {
            raw = await _js.InvokeAsync<string?>("raidLoopStorage.load", SaveKey);
        }
        catch
        {
            return CreateDefaultSave();
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return CreateDefaultSave();
        }

        try
        {
            var save = JsonSerializer.Deserialize<GameSave>(raw);
            if (save is null)
            {
                return CreateDefaultSave();
            }

            var onPerson = save.OnPersonItems ?? [];
            // Migration from v3 shape where inventory was List<Item> CharacterInventory.
            if (onPerson.Count == 0)
            {
                var legacyItems = ExtractLegacyCharacterInventory(raw);
                if (legacyItems.Count > 0)
                {
                    onPerson = legacyItems.Select(i => new OnPersonEntry(i, false)).ToList();
                }
            }

            save = save with { OnPersonItems = onPerson };
            return NormalizeSave(save);
        }
        catch
        {
            // Migration path from v1 (stash-only payload)
            try
            {
                var legacyStash = JsonSerializer.Deserialize<List<Item>>(raw);
                if (legacyStash is not null)
                {
                    return NormalizeSave(new GameSave(
                        MainStash: legacyStash,
                        RandomCharacterAvailableAt: DateTimeOffset.MinValue,
                        RandomCharacter: null,
                        Money: 500,
                        OnPersonItems: []));
                }
            }
            catch
            {
                // fall through to default
            }

            return CreateDefaultSave();
        }
    }

    public async Task SaveAsync(GameSave save)
    {
        var payload = JsonSerializer.Serialize(save);
        await _js.InvokeVoidAsync("raidLoopStorage.save", SaveKey, payload);
    }

    private static GameSave CreateDefaultSave()
    {
        return NormalizeSave(new GameSave(
            MainStash:
            [
                new Item("Makarov", ItemType.Weapon, 1),
                new Item("Hunting Rifle", ItemType.Weapon, 1),
                new Item("Soft Vest", ItemType.Armor, 1),
                new Item("Plate Carrier", ItemType.Armor, 1),
                new Item("Small Backpack", ItemType.Backpack, 1),
                new Item("Tactical Backpack", ItemType.Backpack, 1),
                new Item("Medkit", ItemType.Consumable, 1),
                new Item("Bandage", ItemType.Consumable, 1),
                new Item("Ammo Box", ItemType.Material, 1)
            ],
            RandomCharacterAvailableAt: DateTimeOffset.MinValue,
            RandomCharacter: null,
            Money: 500,
            OnPersonItems: []));
    }

    private static GameSave NormalizeSave(GameSave save)
    {
        var money = Math.Max(0, save.Money);
        var inventory = save.OnPersonItems ?? [];
        EnsureKnifeFallback(save.MainStash, inventory);
        return save with { Money = money, OnPersonItems = inventory };
    }

    private static List<Item> ExtractLegacyCharacterInventory(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("CharacterInventory", out var invNode) || invNode.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var legacy = JsonSerializer.Deserialize<List<Item>>(invNode.GetRawText());
            return legacy ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void EnsureKnifeFallback(List<Item> stash, List<OnPersonEntry> onPerson)
    {
        var stashHasWeapon = stash.Any(item => item.Type == ItemType.Weapon);
        var onPersonHasWeapon = onPerson.Any(entry => entry.Item.Type == ItemType.Weapon);
        if (!stashHasWeapon && !onPersonHasWeapon)
        {
            stash.Add(new Item("Rusty Knife", ItemType.Weapon, 1));
        }
    }
}

public sealed record RandomCharacterState(string Name, List<Item> Inventory);

public sealed record GameSave(
    List<Item> MainStash,
    DateTimeOffset RandomCharacterAvailableAt,
    RandomCharacterState? RandomCharacter,
    int Money,
    List<OnPersonEntry> OnPersonItems);

public sealed record OnPersonEntry(Item Item, bool IsEquipped);
