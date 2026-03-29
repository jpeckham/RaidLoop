namespace RaidLoop.Core;

public enum AttackMode
{
    Standard,
    Burst,
    FullAuto
}

public interface IRng
{
    int Next(int minInclusive, int maxExclusive);
}

public sealed class RandomRng : IRng
{
    private readonly Random _random;

    public RandomRng(Random random)
    {
        _random = random;
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        return _random.Next(minInclusive, maxExclusive);
    }
}

public readonly record struct DamageRange(int Min, int Max);

public enum EncumbranceTier
{
    Light,
    Medium,
    Heavy
}

public static class CombatBalance
{
    private static readonly int[] HeavyLoadByStrength =
    [
        10, 20, 30, 40, 50, 60, 70, 80, 90, 100,
        115, 130, 150, 175, 200, 230, 260, 300, 350, 400,
        460, 520, 600, 700, 800, 920, 1040, 1200, 1400
    ];

    public static int GetAbilityModifier(int score)
    {
        return PlayerStatRules.GetAbilityModifier(score);
    }

    public static int GetRangedAttackBonusFromDexterity(int dexterity)
    {
        return GetAbilityModifier(dexterity);
    }

    public static int GetDefenseFromDexterity(int dexterity, int? maxDexBonus = null)
    {
        var dexterityBonus = GetAbilityModifier(dexterity);
        if (maxDexBonus.HasValue)
        {
            dexterityBonus = Math.Min(dexterityBonus, maxDexBonus.Value);
        }

        return 10 + dexterityBonus;
    }

    public static int GetMaxHealthFromConstitution(int constitution)
    {
        return 10 + (2 * Math.Max(0, constitution));
    }

    public static int GetCarryCapacityFromStrength(int strength)
    {
        return 10 + Math.Max(0, strength - PlayerStatRules.MinimumScore);
    }

    public static int GetMaxEncumbranceFromStrength(int strength)
    {
        return GetHeavyLoadLimit(strength);
    }

    public static EncumbranceTier GetEncumbranceTier(int strength, int carriedWeight)
    {
        var lightLimit = GetLightLoadLimit(strength);
        var mediumLimit = GetMediumLoadLimit(strength);

        if (carriedWeight <= lightLimit)
        {
            return EncumbranceTier.Light;
        }

        if (carriedWeight <= mediumLimit)
        {
            return EncumbranceTier.Medium;
        }

        return EncumbranceTier.Heavy;
    }

    public static int GetEffectiveDexterityModifier(int dexterity, EncumbranceTier encumbranceTier)
    {
        var dexterityModifier = GetAbilityModifier(dexterity);
        var maxDexterityModifier = encumbranceTier switch
        {
            EncumbranceTier.Medium => 3,
            EncumbranceTier.Heavy => 1,
            _ => int.MaxValue
        };

        return Math.Min(dexterityModifier, maxDexterityModifier);
    }

    public static int GetEncumbranceAttackPenalty(EncumbranceTier encumbranceTier)
    {
        return encumbranceTier switch
        {
            EncumbranceTier.Medium => 3,
            EncumbranceTier.Heavy => 6,
            _ => 0
        };
    }

    public static int GetCharismaModifier(int charisma)
    {
        return GetAbilityModifier(charisma);
    }

    public static Rarity GetMaxShopRarityFromChaBonus(int charismaModifier)
    {
        return charismaModifier switch
        {
            >= 4 => Rarity.Legendary,
            3 => Rarity.Epic,
            2 => Rarity.Rare,
            1 => Rarity.Uncommon,
            _ => Rarity.Common
        };
    }

    public static int GetShopPrice(int basePrice, int charismaModifier, bool isBuying)
    {
        var modifier = Math.Max(0, charismaModifier);
        var multiplier = isBuying
            ? 1m - (0.05m * modifier)
            : 1m + (0.05m * modifier);

        return Math.Max(1, (int)Math.Round(basePrice * multiplier, MidpointRounding.AwayFromZero));
    }

    public static bool ResolveAttackRoll(int roll, int attackBonus, int defense)
    {
        if (roll == 1)
        {
            return false;
        }

        if (roll == 20)
        {
            return true;
        }

        return roll + attackBonus >= defense;
    }

    public static DamageRange GetDamageRange(string weaponName, AttackMode mode)
    {
        var normalizedWeapon = NormalizeWeaponName(weaponName);
        var dieCount = GetDamageDieCount(mode);
        var dieSize = GetDamageDieSize(normalizedWeapon);

        return new DamageRange(dieCount, dieCount * dieSize);
    }

    public static int RollDamage(string weaponName, AttackMode mode, IRng rng)
    {
        var dieCount = GetDamageDieCount(mode);
        var dieSize = GetDamageDieSize(NormalizeWeaponName(weaponName));
        var total = 0;

        for (var currentDie = 0; currentDie < dieCount; currentDie++)
        {
            total += rng.Next(1, dieSize + 1);
        }

        return total;
    }

    public static bool SupportsSingleShot(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "Support Machine Gun" => false,
            _ => true
        };
    }

    public static bool SupportsBurstFire(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "Light Pistol" => true,
            "Drum SMG" => true,
            "Field Carbine" => true,
            "Battle Rifle" => true,
            "Marksman Rifle" => true,
            "Support Machine Gun" => true,
            "Rusty Knife" => false,
            _ => false
        };
    }

    public static bool SupportsFullAuto(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "Light Pistol" => false,
            "Marksman Rifle" => false,
            "Rusty Knife" => false,
            _ => true
        };
    }

    public static int GetBurstAttackPenalty(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "Light Pistol" => 3,
            "Drum SMG" => 2,
            "Field Carbine" => 2,
            "Battle Rifle" => 2,
            "Marksman Rifle" => 2,
            "Support Machine Gun" => 2,
            _ => 3
        };
    }

    public static int GetArmorReduction(string armorName)
    {
        return NormalizeArmorName(armorName) switch
        {
            "Assault Plate Carrier" => 6,
            "Heavy Plate Carrier" => 5,
            "Medium Plate Carrier" => 4,
            "Light Plate Carrier" => 3,
            "Reinforced Vest" => 2,
            "Soft Armor Vest" => 1,
            _ => 0
        };
    }

    public static int ApplyArmorReduction(int incomingDamage, int armorReduction)
    {
        var incoming = Math.Max(0, incomingDamage);
        var reduced = incoming - Math.Max(0, armorReduction);
        return Math.Max(1, reduced);
    }

    public static int GetBuyPrice(string itemName)
    {
        return NormalizeItemName(itemName) switch
        {
            "Bandage" => 60,
            "Medkit" => 120,
            "Ammo Box" => 80,
            "Light Pistol" => 240,
            "Drum SMG" => 650,
            "Field Carbine" => 1250,
            "Marksman Rifle" => 2200,
            "Battle Rifle" => 1500,
            "Support Machine Gun" => 3200,
            "Soft Armor Vest" => 380,
            "Reinforced Vest" => 640,
            "Light Plate Carrier" => 900,
            "Medium Plate Carrier" => 1500,
            "Heavy Plate Carrier" => 1800,
            "Assault Plate Carrier" => 2600,
            "Small Backpack" => 100,
            "Large Backpack" => 200,
            "Tactical Backpack" => 300,
            "Hiking Backpack" => 1600,
            "Raid Backpack" => 2400,
            _ => 100
        };
    }

    public static int GetMagazineCapacity(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "Drum SMG" => 35,
            "Field Carbine" => 30,
            "Marksman Rifle" => 20,
            "Battle Rifle" => 30,
            "Support Machine Gun" => 100,
            "Rusty Knife" => 0,
            _ => 8
        };
    }

    public static bool WeaponUsesAmmo(string weaponName)
    {
        return GetMagazineCapacity(weaponName) > 0;
    }

    public static int GetBackpackCapacity(string? backpackName)
    {
        return backpackName switch
        {
            "Raid Backpack" => 10,
            "Hiking Backpack" => 8,
            "Tactical Backpack" => 6,
            "Large Backpack" => 4,
            "Small Backpack" => 3,
            _ => 2
        };
    }

    public static int GetTotalEncumbrance(IEnumerable<Item> items)
    {
        var total = items.Sum(item => Math.Max(0, item.Weight));
        return total;
    }

    public static string NormalizeItemName(string itemName)
    {
        var normalized = NormalizeWeaponName(itemName);
        normalized = NormalizeArmorName(normalized);

        return normalized;
    }

    private static string NormalizeWeaponName(string weaponName)
    {
        return weaponName switch
        {
            "Hunting Rifle" => "Field Carbine",
            "Rusty SMG" => "Drum SMG",
            "Sawed Shotgun" => "Battle Rifle",
            "Compact Carbine" => "Field Carbine",
            _ => weaponName
        };
    }

    private static string NormalizeArmorName(string armorName)
    {
        return armorName switch
        {
            "Soft Vest" => "Soft Armor Vest",
            "Plate Carrier" => "Light Plate Carrier",
            _ => armorName
        };
    }

    private static int GetLightLoadLimit(int strength)
    {
        return GetHeavyLoadLimit(strength) / 3;
    }

    private static int GetMediumLoadLimit(int strength)
    {
        return (GetHeavyLoadLimit(strength) * 2) / 3;
    }

    private static int GetHeavyLoadLimit(int strength)
    {
        var normalizedStrength = Math.Max(1, strength);
        if (normalizedStrength <= HeavyLoadByStrength.Length)
        {
            return HeavyLoadByStrength[normalizedStrength - 1];
        }

        return GetHeavyLoadLimit(normalizedStrength - 10) * 4;
    }

    private static int GetDamageDieCount(AttackMode mode)
    {
        return mode switch
        {
            AttackMode.Standard => 2,
            AttackMode.Burst => 3,
            AttackMode.FullAuto => 4,
            _ => 2
        };
    }

    private static int GetDamageDieSize(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "Drum SMG" => 4,
            "Field Carbine" => 8,
            "Marksman Rifle" => 12,
            "Battle Rifle" => 10,
            "Support Machine Gun" => 12,
            "Light Pistol" => 6,
            "Rusty Knife" => 6,
            _ => 6
        };
    }
}
