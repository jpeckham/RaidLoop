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
        var normalizedWeapon = NormalizeItemName(weaponName);
        var dieCount = GetDamageDieCount(mode);
        var dieSize = GetDamageDieSize(normalizedWeapon);

        return new DamageRange(dieCount, dieCount * dieSize);
    }

    public static int RollDamage(string weaponName, AttackMode mode, IRng rng)
    {
        var dieCount = GetDamageDieCount(mode);
        var dieSize = GetDamageDieSize(NormalizeItemName(weaponName));
        var total = 0;

        for (var currentDie = 0; currentDie < dieCount; currentDie++)
        {
            total += rng.Next(1, dieSize + 1);
        }

        return total;
    }

    public static bool SupportsSingleShot(string weaponName)
    {
        return NormalizeItemName(weaponName) switch
        {
            "support_machine_gun" => false,
            _ => true
        };
    }

    public static bool SupportsBurstFire(string weaponName)
    {
        return NormalizeItemName(weaponName) switch
        {
            "light_pistol" => true,
            "drum_smg" => true,
            "field_carbine" => true,
            "battle_rifle" => true,
            "marksman_rifle" => true,
            "support_machine_gun" => true,
            "Rusty Knife" => false,
            _ => false
        };
    }

    public static bool SupportsFullAuto(string weaponName)
    {
        return NormalizeItemName(weaponName) switch
        {
            "rusty_knife" => false,
            "light_pistol" => false,
            "marksman_rifle" => false,
            "Rusty Knife" => false,
            _ => true
        };
    }

    public static int GetBurstAttackPenalty(string weaponName)
    {
        return NormalizeItemName(weaponName) switch
        {
            "light_pistol" => 3,
            "drum_smg" => 2,
            "field_carbine" => 2,
            "battle_rifle" => 2,
            "marksman_rifle" => 2,
            "support_machine_gun" => 2,
            _ => 3
        };
    }

    public static int GetArmorReduction(string armorName)
    {
        return NormalizeItemName(armorName) switch
        {
            "assault_plate_carrier" => 6,
            "heavy_plate_carrier" => 5,
            "medium_plate_carrier" => 4,
            "light_plate_carrier" => 3,
            "reinforced_vest" => 2,
            "soft_armor_vest" => 1,
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
            "bandage" => 60,
            "medkit" => 120,
            "ammo_box" => 80,
            "light_pistol" => 240,
            "drum_smg" => 650,
            "field_carbine" => 1250,
            "marksman_rifle" => 2200,
            "battle_rifle" => 1500,
            "support_machine_gun" => 3200,
            "soft_armor_vest" => 380,
            "reinforced_vest" => 640,
            "light_plate_carrier" => 900,
            "medium_plate_carrier" => 1500,
            "heavy_plate_carrier" => 1800,
            "assault_plate_carrier" => 2600,
            "small_backpack" => 100,
            "large_backpack" => 200,
            "tactical_backpack" => 300,
            "hiking_backpack" => 1600,
            "raid_backpack" => 2400,
            _ => 100
        };
    }

    public static int GetMagazineCapacity(string weaponName)
    {
        return NormalizeItemName(weaponName) switch
        {
            "rusty_knife" => 0,
            "drum_smg" => 35,
            "field_carbine" => 30,
            "marksman_rifle" => 20,
            "battle_rifle" => 30,
            "support_machine_gun" => 100,
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
        return NormalizeItemName(backpackName ?? string.Empty) switch
        {
            "raid_backpack" => 10,
            "hiking_backpack" => 8,
            "tactical_backpack" => 6,
            "large_backpack" => 4,
            "small_backpack" => 3,
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
        if (ItemCatalog.TryGet(itemName, out var item) && item is not null)
        {
            return item.Key;
        }

        var normalized = NormalizeWeaponName(itemName);
        normalized = NormalizeArmorName(normalized);

        return normalized;
    }

    private static string NormalizeWeaponName(string weaponName)
    {
        return weaponName switch
        {
            "Makarov" => "light_pistol",
            "PPSH" => "drum_smg",
            "AK74" => "field_carbine",
            "SVDS" => "marksman_rifle",
            "AK47" => "battle_rifle",
            "PKP" => "support_machine_gun",
            "Hunting Rifle" => "field_carbine",
            "Rusty SMG" => "drum_smg",
            "Sawed Shotgun" => "battle_rifle",
            "Compact Carbine" => "field_carbine",
            _ => weaponName
        };
    }

    private static string NormalizeArmorName(string armorName)
    {
        return armorName switch
        {
            "6B2 body armor" => "soft_armor_vest",
            "BNTI Kirasa-N" => "reinforced_vest",
            "6B13 assault armor" => "light_plate_carrier",
            "FORT Defender-2" => "medium_plate_carrier",
            "6B43 Zabralo-Sh body armor" => "heavy_plate_carrier",
            "NFM THOR" => "assault_plate_carrier",
            "Soft Vest" => "soft_armor_vest",
            "Plate Carrier" => "light_plate_carrier",
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
        return NormalizeItemName(weaponName) switch
        {
            "drum_smg" => 4,
            "field_carbine" => 8,
            "marksman_rifle" => 12,
            "battle_rifle" => 10,
            "support_machine_gun" => 12,
            "light_pistol" => 6,
            "rusty_knife" => 6,
            "Rusty Knife" => 6,
            _ => 6
        };
    }
}
