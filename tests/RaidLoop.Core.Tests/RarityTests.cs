using RaidLoop.Core;

namespace RaidLoop.Core.Tests;

public class RarityTests
{
    [Fact]
    public void Item_DefaultsRarityToCommon()
    {
        var item = new Item("Pistol", ItemType.Weapon, 1);

        Assert.Equal(Rarity.Common, item.Rarity);
    }

    [Fact]
    public void Item_PreservesExplicitRarity()
    {
        var item = new Item("AK74", ItemType.Weapon, 1, Rarity.Rare);

        Assert.Equal(Rarity.Rare, item.Rarity);
    }
}
