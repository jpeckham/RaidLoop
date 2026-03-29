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
            "PKP" => false,
            _ => true
        };
    }

    public static bool SupportsBurstFire(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "Makarov" => true,
            "PPSH" => true,
            "AK74" => true,
            "AK47" => true,
            "SVDS" => true,
            "PKP" => true,
            "Rusty Knife" => false,
            _ => false
        };
    }

    public static bool SupportsFullAuto(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "Makarov" => false,
            "SVDS" => false,
            "Rusty Knife" => false,
            _ => true
        };
    }

    public static int GetBurstAttackPenalty(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "Makarov" => 3,
            "PPSH" => 2,
            "AK74" => 2,
            "AK47" => 2,
            "SVDS" => 2,
            "PKP" => 2,
            _ => 3
        };
    }

    public static int GetArmorReduction(string armorName)
    {
        return NormalizeArmorName(armorName) switch
        {
            "NFM THOR" => 6,
            "6B43 Zabralo-Sh body armor" => 5,
            "FORT Defender-2" => 4,
            "6B13 assault armor" => 3,
            "BNTI Kirasa-N" => 2,
            "6B2 body armor" => 1,
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
            "Makarov" => 240,
            "PPSH" => 650,
            "AK74" => 1250,
            "SVDS" => 2200,
            "AK47" => 1500,
            "PKP" => 3200,
            "6B2 body armor" => 380,
            "BNTI Kirasa-N" => 640,
            "6B13 assault armor" => 900,
            "FORT Defender-2" => 1500,
            "6B43 Zabralo-Sh body armor" => 1800,
            "NFM THOR" => 2600,
            "Small Backpack" => 100,
            "Large Backpack" => 200,
            "Tactical Backpack" => 300,
            "Tasmanian Tiger Trooper 35" => 1600,
            "6Sh118" => 2400,
            _ => 100
        };
    }

    public static int GetMagazineCapacity(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "PPSH" => 35,
            "AK74" => 30,
            "SVDS" => 20,
            "AK47" => 30,
            "PKP" => 100,
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
            "6Sh118" => 10,
            "Tasmanian Tiger Trooper 35" => 8,
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
            "Hunting Rifle" => "AK74",
            "Rusty SMG" => "PPSH",
            "Sawed Shotgun" => "AK47",
            "Compact Carbine" => "AK74",
            _ => weaponName
        };
    }

    private static string NormalizeArmorName(string armorName)
    {
        return armorName switch
        {
            "Soft Vest" => "6B2 body armor",
            "Plate Carrier" => "6B13 assault armor",
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
            "PPSH" => 4,
            "AK74" => 8,
            "SVDS" => 12,
            "AK47" => 10,
            "PKP" => 12,
            "Makarov" => 6,
            "Rusty Knife" => 6,
            _ => 6
        };
    }
}
