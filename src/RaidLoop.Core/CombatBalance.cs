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

public static class CombatBalance
{
    public static int GetAbilityModifier(int score)
    {
        return (int)Math.Floor((score - 10) / 2.0);
    }

    public static int GetRangedAttackBonusFromDexterity(int dexterity)
    {
        return GetAbilityModifier(dexterity);
    }

    public static int GetDefenseFromDexterity(int dexterity)
    {
        return 10 + GetAbilityModifier(dexterity);
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
            "6B13 assault armor" => 900,
            "FORT Defender-2" => 1500,
            "6B43 Zabralo-Sh body armor" => 1800,
            "NFM THOR" => 2600,
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
            "Small Backpack" => 3,
            _ => 2
        };
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
