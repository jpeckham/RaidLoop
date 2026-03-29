using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.JSInterop;
using RaidLoop.Core;

namespace RaidLoop.Client.Services;

public sealed class StashStorage
{
    private const string SaveKey = "raidloop.save.v3";
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);
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
            var normalizedRaw = NormalizeLegacyRandomCharacterStats(raw);
            var save = JsonSerializer.Deserialize<GameSave>(normalizedRaw, WebJsonOptions);
            if (save is null)
            {
                return CreateDefaultSave();
            }

            if ((save.RandomCharacter is null || string.IsNullOrWhiteSpace(save.RandomCharacter.Name))
                && TryReadLegacyRandomCharacter(raw, out var legacyRandomCharacter))
            {
                save = save with { RandomCharacter = legacyRandomCharacter };
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

    private static bool TryReadLegacyRandomCharacter(string raw, out RandomCharacterState randomCharacter)
    {
        randomCharacter = null!;
        try
        {
            var root = JsonNode.Parse(raw) as JsonObject;
            if (root is null)
            {
                return false;
            }

            if (!TryGetNodeProperty(root, "RandomCharacter", out var characterNode)
                && !TryGetNodeProperty(root, "randomCharacter", out characterNode))
            {
                return false;
            }

            if (characterNode is not JsonObject characterObject)
            {
                return false;
            }

            var name = TryGetString(characterObject, "Name", out var canonicalName)
                ? canonicalName
                : TryGetString(characterObject, "name", out var legacyName)
                    ? legacyName
                    : string.Empty;

            var inventory = TryGetNodeProperty(characterObject, "Inventory", out var canonicalInventory)
                ? TryReadItemListFromNode(canonicalInventory, out var parsedCanonicalInventory)
                    ? parsedCanonicalInventory
                    : []
                : TryGetNodeProperty(characterObject, "inventory", out var legacyInventory)
                    ? TryReadItemListFromNode(legacyInventory, out var parsedLegacyInventory)
                        ? parsedLegacyInventory
                        : []
                    : [];

            var stats = TryGetNodeProperty(characterObject, "Stats", out var canonicalStats)
                ? TryReadPlayerStatsFromNode(canonicalStats, out var parsedCanonicalStats)
                    ? parsedCanonicalStats
                    : PlayerStats.Default
                : TryGetNodeProperty(characterObject, "stats", out var legacyStats)
                    ? TryReadPlayerStatsFromNode(legacyStats, out var parsedLegacyStats)
                        ? parsedLegacyStats
                        : PlayerStats.Default
                    : PlayerStats.Default;

            randomCharacter = new RandomCharacterState(name, inventory, stats);
            return true;
        }
        catch
        {
            return false;
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
                ItemCatalog.Create("Light Pistol"),
                ItemCatalog.Create("Drum SMG"),
                ItemCatalog.Create("Field Carbine"),
                ItemCatalog.Create("Soft Armor Vest"),
                ItemCatalog.Create("Light Plate Carrier"),
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

    private static string NormalizeLegacyRandomCharacterStats(string raw)
    {
        try
        {
            var root = JsonNode.Parse(raw) as JsonObject;
            if (root is null)
            {
                return raw;
            }

            if (TryGetNodeProperty(root, "randomCharacter", out var camelRandomCharacter) && camelRandomCharacter is not null)
            {
                if (!root.ContainsKey("RandomCharacter"))
                {
                    root["RandomCharacter"] = camelRandomCharacter.DeepClone();
                    root.Remove("randomCharacter");
                }
            }

            if (TryGetNodeProperty(root, "RandomCharacter", out var randomCharacterNode) && randomCharacterNode is JsonObject randomCharacter)
            {
                MoveProperty(randomCharacter, "name", "Name");
                MoveProperty(randomCharacter, "inventory", "Inventory");
                MoveProperty(randomCharacter, "stats", "Stats");

                if (TryGetNodeProperty(randomCharacter, "stats", out var camelStats) && camelStats is not null && !HasProperty(randomCharacter, "Stats"))
                {
                    randomCharacter["Stats"] = camelStats.DeepClone();
                    randomCharacter.Remove("stats");
                }

                if (!HasProperty(randomCharacter, "Stats"))
                {
                    randomCharacter["Stats"] = JsonSerializer.SerializeToNode(PlayerStats.Default);
                }
            }

            return root.ToJsonString();
        }
        catch
        {
            return raw;
        }
    }

    private static void MoveProperty(JsonObject node, string sourceName, string targetName)
    {
        if (string.Equals(sourceName, targetName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (HasProperty(node, targetName))
        {
            return;
        }

        foreach (var entry in node)
        {
            if (!string.Equals(entry.Key, sourceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (entry.Value is null)
            {
                return;
            }

            node[targetName] = entry.Value.DeepClone();
            node.Remove(entry.Key);
            return;
        }
    }

    private static bool TryGetNodeProperty(JsonObject root, string propertyName, [NotNullWhen(true)] out JsonNode? value)
    {
        foreach (var entry in root)
        {
            if (!string.Equals(entry.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = entry.Value;
            return value is not null;
        }

        value = null;
        return false;
    }

    private static bool TryGetString(JsonObject root, string propertyName, [NotNullWhen(true)] out string? value)
    {
        foreach (var entry in root)
        {
            if (!string.Equals(entry.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (entry.Value is JsonValue jsonValue
                && jsonValue.TryGetValue<string>(out var parsedValue)
                && parsedValue is not null)
            {
                value = parsedValue;
                return true;
            }

            break;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryReadItemListFromNode(JsonNode items, out List<Item> parsedItems)
    {
        try
        {
            if (items is not JsonArray)
            {
                parsedItems = [];
                return false;
            }

            var legacy = JsonSerializer.Deserialize<List<Item>>(items.ToJsonString(), WebJsonOptions);
            parsedItems = legacy ?? [];
            return true;
        }
        catch
        {
            parsedItems = [];
            return false;
        }
    }

    private static bool TryReadPlayerStatsFromNode(JsonNode stats, out PlayerStats parsedStats)
    {
        try
        {
            if (stats is not JsonObject)
            {
                parsedStats = PlayerStats.Default;
                return false;
            }

            var legacy = JsonSerializer.Deserialize<PlayerStats>(stats.ToJsonString(), WebJsonOptions);
            parsedStats = legacy ?? PlayerStats.Default;
            return true;
        }
        catch
        {
            parsedStats = PlayerStats.Default;
            return false;
        }
    }

    private static bool HasProperty(JsonObject node, string propertyName)
    {
        foreach (var entry in node)
        {
            if (string.Equals(entry.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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

public sealed record RandomCharacterState
{
    public RandomCharacterState(string Name, List<Item> Inventory, PlayerStats Stats)
    {
        ArgumentNullException.ThrowIfNull(Stats);
        this.Name = Name;
        this.Inventory = Inventory;
        this.Stats = Stats;
    }

    public string Name { get; init; }
    public List<Item> Inventory { get; init; }
    public PlayerStats Stats { get; init; }
}

public sealed record GameSave(
    List<Item> MainStash,
    DateTimeOffset RandomCharacterAvailableAt,
    RandomCharacterState? RandomCharacter,
    int Money,
    List<OnPersonEntry> OnPersonItems);

public sealed record OnPersonEntry(Item Item, bool IsEquipped);
