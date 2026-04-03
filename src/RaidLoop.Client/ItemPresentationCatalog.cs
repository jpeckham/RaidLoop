using System.Globalization;
using RaidLoop.Core;

namespace RaidLoop.Client;

public static class ItemPresentationCatalog
{
    public static string GetLabel(Item? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        if (item.ItemDefId > 0)
        {
            var label = ItemResources.ResourceManager.GetString($"Items.{item.ItemDefId}", ItemResources.Culture ?? CultureInfo.CurrentUICulture);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }
        }

        return item.Name;
    }
}
