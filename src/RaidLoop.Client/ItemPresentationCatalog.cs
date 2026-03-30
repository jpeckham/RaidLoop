using RaidLoop.Core;

namespace RaidLoop.Client;

public static class ItemPresentationCatalog
{
    private static readonly IReadOnlyDictionary<int, string> Labels = new Dictionary<int, string>
    {
        [1] = "Rusty Knife",
        [2] = "Makarov",
        [3] = "PPSH",
        [4] = "AK74",
        [5] = "AK47",
        [6] = "SVDS",
        [7] = "PKP",
        [8] = "6B2 body armor",
        [9] = "BNTI Kirasa-N",
        [10] = "6B13 assault armor",
        [11] = "FORT Defender-2",
        [12] = "6B43 Zabralo-Sh body armor",
        [13] = "NFM THOR",
        [14] = "Small Backpack",
        [15] = "Large Backpack",
        [16] = "Tactical Backpack",
        [17] = "Tasmanian Tiger Trooper 35",
        [18] = "6Sh118",
        [19] = "Medkit",
        [20] = "Bandage",
        [21] = "Ammo Box",
        [22] = "Scrap Metal",
        [23] = "Rare Scope",
        [24] = "Legendary Trigger Group",
    };

    public static string GetLabel(Item? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        if (item.ItemDefId > 0 && Labels.TryGetValue(item.ItemDefId, out var label))
        {
            return label;
        }

        return item.Name;
    }
}
