using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaidLoop.Core;

internal sealed class ItemJsonConverter : JsonConverter<Item>
{
    public override Item Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var itemDefId = TryGetInt(root, "itemDefId");
        var itemKey = TryGetString(root, "itemKey");
        var itemName = TryGetString(root, "name");

        if (ItemCatalog.TryResolveAuthoredItem(itemDefId, itemKey, itemName, out var authoredItem) && authoredItem is not null)
        {
            return authoredItem with { };
        }

        return new Item(
            Name: itemName ?? itemKey ?? (itemDefId > 0 ? itemDefId.ToString(CultureInfo.InvariantCulture) : string.Empty),
            Type: TryGetInt(root, "type", TryGetInt(root, "Type")) is var typeValue ? (ItemType)typeValue : ItemType.Weapon,
            Weight: TryGetInt(root, "weight", TryGetInt(root, "Weight")),
            Value: TryGetInt(root, "value", TryGetInt(root, "Value", 1)),
            Slots: TryGetInt(root, "slots", TryGetInt(root, "Slots", 1)),
            Rarity: (Rarity)TryGetInt(root, "rarity", TryGetInt(root, "Rarity")),
            DisplayRarity: (DisplayRarity)TryGetInt(root, "displayRarity", TryGetInt(root, "DisplayRarity")))
        {
            ItemDefId = itemDefId,
            Key = itemKey ?? string.Empty
        };
    }

    public override void Write(Utf8JsonWriter writer, Item value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("itemDefId", value.ItemDefId);
        writer.WriteNumber("type", (int)value.Type);
        writer.WriteNumber("value", value.Value);
        writer.WriteNumber("slots", value.Slots);
        writer.WriteNumber("rarity", (int)value.Rarity);
        writer.WriteNumber("displayRarity", (int)value.DisplayRarity);
        writer.WriteNumber("weight", value.Weight);
        writer.WriteEndObject();
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        if (TryGetProperty(root, propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    private static int TryGetInt(JsonElement root, string propertyName, int defaultValue = 0)
    {
        if (TryGetProperty(root, propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        return defaultValue;
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
