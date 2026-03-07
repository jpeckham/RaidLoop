namespace RaidLoop.Core;

public enum AttackMode
{
    Standard,
    Burst
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
    public static DamageRange GetDamageRange(string weaponName, AttackMode mode)
    {
        return (NormalizeWeaponName(weaponName), mode) switch
        {
            ("PPSH", AttackMode.Standard) => new DamageRange(6, 10),
            ("AK74", AttackMode.Standard) => new DamageRange(8, 12),
            ("AK47", AttackMode.Standard) => new DamageRange(9, 14),
            ("Makarov", AttackMode.Burst) => new DamageRange(8, 12),
            ("PPSH", AttackMode.Burst) => new DamageRange(10, 15),
            ("AK74", AttackMode.Burst) => new DamageRange(12, 17),
            ("AK47", AttackMode.Burst) => new DamageRange(13, 19),
            _ => new DamageRange(5, 8)
        };
    }

    public static int RollDamage(string weaponName, AttackMode mode, IRng rng)
    {
        var range = GetDamageRange(weaponName, mode);
        return rng.Next(range.Min, range.Max + 1);
    }

    public static int GetArmorReduction(string armorName)
    {
        return NormalizeArmorName(armorName) switch
        {
            "6B43 Zabralo-Sh body armor" => 5,
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
            "AK47" => 1700,
            "6B2 body armor" => 380,
            "6B13 assault armor" => 900,
            "6B43 Zabralo-Sh body armor" => 1800,
            _ => 100
        };
    }

    public static int GetMagazineCapacity(string weaponName)
    {
        return NormalizeWeaponName(weaponName) switch
        {
            "PPSH" => 35,
            "AK74" => 30,
            "AK47" => 30,
            "Rusty Knife" => 0,
            _ => 8
        };
    }

    public static bool WeaponUsesAmmo(string weaponName)
    {
        return GetMagazineCapacity(weaponName) > 0;
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
}
