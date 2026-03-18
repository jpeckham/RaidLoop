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
                ItemCatalog.Create("Makarov"),
                ItemCatalog.Create("PPSH"),
                ItemCatalog.Create("AK74"),
                ItemCatalog.Create("6B2 body armor"),
                ItemCatalog.Create("6B13 assault armor"),
                ItemCatalog.Create("Small Backpack"),
                ItemCatalog.Create("Tactical Backpack"),
                ItemCatalog.Create("Medkit"),
                ItemCatalog.Create("Bandage"),
                ItemCatalog.Create("Ammo Box")
            ],
            RandomCharacterAvailableAt: DateTimeOffset.MinValue,
            RandomCharacter: null,
            Money: 500,
            OnPersonItems: []));
    }

    private static GameSave NormalizeSave(GameSave save)
    {
        var money = Math.Max(0, save.Money);
        var stash = NormalizeItems(save.MainStash);
        var inventory = NormalizeOnPersonItems(save.OnPersonItems ?? []);
        EnsureKnifeFallback(stash, inventory);
        return save with { Money = money, MainStash = stash, OnPersonItems = inventory };
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
            stash.Add(ItemCatalog.Create("Rusty Knife"));
        }
    }

    private static List<Item> NormalizeItems(List<Item> items)
    {
        return items
            .Select(NormalizeItem)
            .ToList();
    }

    private static List<OnPersonEntry> NormalizeOnPersonItems(List<OnPersonEntry> items)
    {
        return items
            .Select(i => i with { Item = NormalizeItem(i.Item) })
            .ToList();
    }

    private static Item NormalizeItem(Item item)
    {
        var normalizedName = CombatBalance.NormalizeItemName(item.Name);
        var normalizedType = normalizedName switch
        {
            "Bandage" => ItemType.Sellable,
            "Ammo Box" => ItemType.Sellable,
            "Medkit" => ItemType.Consumable,
            _ => item.Type
        };

        if (ItemCatalog.TryGet(normalizedName, out var catalogItem))
        {
            return catalogItem!;
        }

        return item with { Name = normalizedName, Type = normalizedType };
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
