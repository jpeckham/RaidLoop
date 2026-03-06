using RaidLoop.Core;

namespace RaidLoop.Core.Tests;

public class RaidEngineTests
{
    [Fact]
    public void ApplyCombatDamage_ReducesHealth_AndMarksDeathAtZero()
    {
        var state = new RaidState(
            health: 10,
            backpackCapacity: 4,
            broughtItems: [],
            raidLoot: []);

        RaidEngine.ApplyCombatDamage(state, 12);

        Assert.Equal(0, state.Health);
        Assert.True(state.IsDead);
    }

    [Fact]
    public void TryAddLoot_RejectsWhenBackpackCapacityExceeded()
    {
        var state = new RaidState(
            health: 30,
            backpackCapacity: 2,
            broughtItems: [],
            raidLoot: []);

        var addedFirst = RaidEngine.TryAddLoot(state, new Item("Bandage", ItemType.Consumable, 1));
        var addedSecond = RaidEngine.TryAddLoot(state, new Item("Rifle", ItemType.Weapon, 2));

        Assert.True(addedFirst);
        Assert.False(addedSecond);
        Assert.Single(state.RaidLoot);
    }

    [Fact]
    public void FinalizeRaid_Success_TransfersBroughtItemsAndLootToStash()
    {
        var game = new GameState([new Item("Spare Knife", ItemType.Weapon, 1)]);
        var loadout = new List<Item>
        {
            new("Pistol", ItemType.Weapon, 1),
            new("Light Armor", ItemType.Armor, 1)
        };
        var raid = RaidEngine.StartRaid(game, loadout, backpackCapacity: 4, startingHealth: 30);
        RaidEngine.TryAddLoot(raid, new Item("Medkit", ItemType.Consumable, 1));

        RaidEngine.FinalizeRaid(game, raid, extracted: true);

        Assert.Contains(game.Stash, i => i.Name == "Pistol");
        Assert.Contains(game.Stash, i => i.Name == "Light Armor");
        Assert.Contains(game.Stash, i => i.Name == "Medkit");
        Assert.Contains(game.Stash, i => i.Name == "Spare Knife");
    }

    [Fact]
    public void StartRaid_RemovesLoadoutFromStash()
    {
        var pistol = new Item("Pistol", ItemType.Weapon, 1);
        var backpack = new Item("Backpack", ItemType.Backpack, 1);
        var game = new GameState([pistol, backpack]);

        _ = RaidEngine.StartRaid(game, [pistol, backpack], backpackCapacity: 3, startingHealth: 20);

        Assert.Empty(game.Stash);
    }

    [Fact]
    public void FinalizeRaid_Death_LosesBroughtItemsAndRaidLoot()
    {
        var game = new GameState([new Item("Spare Knife", ItemType.Weapon, 1)]);
        var loadout = new List<Item>
        {
            new("Pistol", ItemType.Weapon, 1),
            new("Backpack", ItemType.Backpack, 1)
        };
        var raid = RaidEngine.StartRaid(game, loadout, backpackCapacity: 2, startingHealth: 10);
        RaidEngine.TryAddLoot(raid, new Item("Ammo", ItemType.Material, 1));

        RaidEngine.FinalizeRaid(game, raid, extracted: false);

        Assert.DoesNotContain(game.Stash, i => i.Name == "Pistol");
        Assert.DoesNotContain(game.Stash, i => i.Name == "Backpack");
        Assert.DoesNotContain(game.Stash, i => i.Name == "Ammo");
        Assert.Contains(game.Stash, i => i.Name == "Spare Knife");
    }
}
