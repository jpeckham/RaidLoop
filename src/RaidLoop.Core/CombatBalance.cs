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
            "pkp" => false,
            _ => true
        };
    }

    public static bool SupportsBurstFire(string weaponName)
    {
        return NormalizeItemName(weaponName) switch
        {
            "makarov" => true,
            "ppsh" => true,
            "ak74" => true,
            "ak47" => true,
            "svds" => true,
            "pkp" => true,
            "Rusty Knife" => false,
            _ => false
        };
    }

    public static bool SupportsFullAuto(string weaponName)
    {
        return NormalizeItemName(weaponName) switch
        {
            "rusty_knife" => false,
            "makarov" => false,
            "svds" => false,
            "Rusty Knife" => false,
            _ => true
        };
    }

    public static int GetBurstAttackPenalty(string weaponName)
    {
        return NormalizeItemName(weaponName) switch
        {
            "makarov" => 3,
            "ppsh" => 2,
            "ak74" => 2,
            "ak47" => 2,
            "svds" => 2,
            "pkp" => 2,
            _ => 3
        };
    }

    public static int GetArmorReduction(string armorName)
    {
        return NormalizeItemName(armorName) switch
        {
            "nfm_thor" => 6,
            "6b43_zabralo_sh_body_armor" => 5,
            "fort_defender_2" => 4,
            "6b13_assault_armor" => 3,
            "bnti_kirasa_n" => 2,
            "6b2_body_armor" => 1,
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
            "makarov" => 240,
            "ppsh" => 650,
            "ak74" => 1250,
            "svds" => 2200,
            "ak47" => 1500,
            "pkp" => 3200,
            "6b2_body_armor" => 380,
            "bnti_kirasa_n" => 640,
            "6b13_assault_armor" => 900,
            "fort_defender_2" => 1500,
            "6b43_zabralo_sh_body_armor" => 1800,
            "nfm_thor" => 2600,
            "small_backpack" => 100,
            "large_backpack" => 200,
            "tactical_backpack" => 300,
            "tasmanian_tiger_trooper_35" => 1600,
            "6sh118" => 2400,
            _ => 100
        };
    }

    public static int GetMagazineCapacity(string weaponName)
    {
        return NormalizeItemName(weaponName) switch
        {
            "rusty_knife" => 0,
            "ppsh" => 35,
            "ak74" => 30,
            "svds" => 20,
            "ak47" => 30,
            "pkp" => 100,
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
            "6sh118" => 10,
            "tasmanian_tiger_trooper_35" => 8,
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
            "Makarov" => "makarov",
            "PPSH" => "ppsh",
            "AK74" => "ak74",
            "SVDS" => "svds",
            "AK47" => "ak47",
            "PKP" => "pkp",
            "Hunting Rifle" => "ak74",
            "Rusty SMG" => "ppsh",
            "Sawed Shotgun" => "ak47",
            "Compact Carbine" => "ak74",
            _ => weaponName
        };
    }

    private static string NormalizeArmorName(string armorName)
    {
        return armorName switch
        {
            "6B2 body armor" => "6b2_body_armor",
            "BNTI Kirasa-N" => "bnti_kirasa_n",
            "6B13 assault armor" => "6b13_assault_armor",
            "FORT Defender-2" => "fort_defender_2",
            "6B43 Zabralo-Sh body armor" => "6b43_zabralo_sh_body_armor",
            "NFM THOR" => "nfm_thor",
            "Soft Vest" => "6b2_body_armor",
            "Plate Carrier" => "6b13_assault_armor",
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
            "ppsh" => 4,
            "ak74" => 8,
            "svds" => 12,
            "ak47" => 10,
            "pkp" => 12,
            "makarov" => 6,
            "rusty_knife" => 6,
            "Rusty Knife" => 6,
            _ => 6
        };
    }
}

