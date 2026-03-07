namespace RaidLoop.Core;

public static class RaidEngine
{
    public static RaidState StartRaid(GameState game, List<Item> loadout, int backpackCapacity, int startingHealth)
    {
        foreach (var item in loadout)
        {
            game.Stash.Remove(item);
        }

        var equippedBackpack = loadout.FirstOrDefault(x => x.Type == ItemType.Backpack);
        var inventory = RaidInventory.FromItems([.. loadout], [], CombatBalance.GetBackpackCapacity(equippedBackpack?.Name));
        return new RaidState(startingHealth, inventory);
    }

    public static void ApplyCombatDamage(RaidState state, int damage)
    {
        state.Health = Math.Max(0, state.Health - Math.Max(0, damage));
    }

    public static bool TryAddLoot(RaidState state, Item item)
    {
        return TryAddCarriedItem(state, item);
    }

    public static void FinalizeRaid(GameState game, RaidState raid, bool extracted)
    {
        if (!extracted || raid.IsDead)
        {
            return;
        }

        game.Stash.AddRange(raid.Inventory.GetExtractableItems());
    }

    public static void StartDiscoveredLootEncounter(RaidState state, IEnumerable<Item> items)
    {
        EncounterLoot.StartLootEncounter(state.Inventory.DiscoveredLoot, items);
    }

    public static void AppendDiscoveredLoot(RaidState state, IEnumerable<Item> items)
    {
        EncounterLoot.AppendDiscoveredLoot(state.Inventory.DiscoveredLoot, items);
    }

    public static void ClearDiscoveredLoot(RaidState state)
    {
        state.Inventory.DiscoveredLoot.Clear();
    }

    public static bool TryLootFromDiscovered(RaidState state, Item item)
    {
        if (!state.Inventory.DiscoveredLoot.Remove(item))
        {
            return false;
        }

        if (string.Equals(item.Name, "Medkit", StringComparison.OrdinalIgnoreCase))
        {
            state.Inventory.MedkitCount++;
            return true;
        }

        if (!TryAddCarriedItem(state, item))
        {
            state.Inventory.DiscoveredLoot.Add(item);
            return false;
        }

        return true;
    }

    public static bool TryDropCarriedToDiscovered(RaidState state, Item item)
    {
        if (!state.Inventory.CarriedItems.Remove(item))
        {
            return false;
        }

        state.Inventory.DiscoveredLoot.Add(item);
        return true;
    }

    public static bool TryDropEquippedToDiscovered(RaidState state, ItemType slotType)
    {
        Item? item = slotType switch
        {
            ItemType.Weapon => state.Inventory.EquippedWeapon,
            ItemType.Armor => state.Inventory.EquippedArmor,
            ItemType.Backpack => state.Inventory.EquippedBackpack,
            _ => null
        };

        if (item is null)
        {
            return false;
        }

        switch (slotType)
        {
            case ItemType.Weapon:
                state.Inventory.EquippedWeapon = null;
                break;
            case ItemType.Armor:
                state.Inventory.EquippedArmor = null;
                break;
            case ItemType.Backpack:
                state.Inventory.EquippedBackpack = null;
                state.Inventory.BackpackCapacity = CombatBalance.GetBackpackCapacity(null);
                // Requested behavior: dropping backpack drops everything carried.
                state.Inventory.DiscoveredLoot.AddRange(state.Inventory.CarriedItems);
                state.Inventory.CarriedItems.Clear();
                break;
            default:
                return false;
        }

        state.BackpackCapacity = state.Inventory.BackpackCapacity;
        state.Inventory.DiscoveredLoot.Add(item);
        return true;
    }

    public static bool TryEquipFromDiscovered(RaidState state, Item item)
    {
        if (!state.Inventory.DiscoveredLoot.Remove(item))
        {
            return false;
        }

        if (!TryEquipItem(state, item))
        {
            state.Inventory.DiscoveredLoot.Add(item);
            return false;
        }

        return true;
    }

    public static bool TryEquipFromCarried(RaidState state, Item item)
    {
        if (!state.Inventory.CarriedItems.Remove(item))
        {
            return false;
        }

        if (!TryEquipItem(state, item))
        {
            state.Inventory.CarriedItems.Add(item);
            return false;
        }

        return true;
    }

    private static bool TryEquipItem(RaidState state, Item item)
    {
        if (item.Type is not (ItemType.Weapon or ItemType.Armor or ItemType.Backpack))
        {
            return false;
        }

        Item? previous = item.Type switch
        {
            ItemType.Weapon => state.Inventory.EquippedWeapon,
            ItemType.Armor => state.Inventory.EquippedArmor,
            ItemType.Backpack => state.Inventory.EquippedBackpack,
            _ => null
        };

        switch (item.Type)
        {
            case ItemType.Weapon:
                state.Inventory.EquippedWeapon = item;
                break;
            case ItemType.Armor:
                state.Inventory.EquippedArmor = item;
                break;
            case ItemType.Backpack:
                state.Inventory.EquippedBackpack = item;
                state.Inventory.BackpackCapacity = CombatBalance.GetBackpackCapacity(item.Name);
                break;
        }

        state.BackpackCapacity = state.Inventory.BackpackCapacity;
        if (previous is not null)
        {
            state.Inventory.DiscoveredLoot.Add(previous);
        }

        SpillOverflowToDiscovered(state);
        return true;
    }

    private static bool TryAddCarriedItem(RaidState state, Item item)
    {
        if (string.Equals(item.Name, "Medkit", StringComparison.OrdinalIgnoreCase))
        {
            state.Inventory.MedkitCount++;
            return true;
        }

        var currentSlots = state.Inventory.CarriedItems.Sum(x => x.Slots);
        if (currentSlots + item.Slots > state.Inventory.BackpackCapacity)
        {
            return false;
        }

        state.Inventory.CarriedItems.Add(item);
        return true;
    }

    private static void SpillOverflowToDiscovered(RaidState state)
    {
        var currentSlots = state.Inventory.CarriedItems.Sum(x => x.Slots);
        while (currentSlots > state.Inventory.BackpackCapacity && state.Inventory.CarriedItems.Count > 0)
        {
            var spill = state.Inventory.CarriedItems[^1];
            state.Inventory.CarriedItems.RemoveAt(state.Inventory.CarriedItems.Count - 1);
            state.Inventory.DiscoveredLoot.Add(spill);
            currentSlots -= spill.Slots;
        }
    }
}
