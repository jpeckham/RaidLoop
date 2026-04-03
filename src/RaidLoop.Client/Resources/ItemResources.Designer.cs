#nullable enable
using System.Globalization;
using System.Resources;

namespace RaidLoop.Client;

public partial class ItemResources
{
    public static ResourceManager ResourceManager { get; } =
        new("RaidLoop.Client.Resources.ItemResources", typeof(ItemResources).Assembly);

    public static CultureInfo? Culture { get; set; }
}
