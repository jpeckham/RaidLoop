using RaidLoop.Core;

namespace RaidLoop.Core.Tests;

public class RarityTests
{
    [Fact]
    public void DisplayRarity_DefinesExpectedOrder()
    {
        var values = Enum.GetValues<DisplayRarity>();

        Assert.Equal(
        [
            DisplayRarity.SellOnly,
            DisplayRarity.Common,
            DisplayRarity.Uncommon,
            DisplayRarity.Rare,
            DisplayRarity.Epic,
            DisplayRarity.Legendary
        ], values);
    }

    [Fact]
    public void DisplayRarity_NamesMatchUiLabels()
    {
        var names = Enum.GetNames<DisplayRarity>();

        Assert.Equal(
        [
            "SellOnly",
            "Common",
            "Uncommon",
            "Rare",
            "Epic",
            "Legendary"
        ], names);
    }
}
