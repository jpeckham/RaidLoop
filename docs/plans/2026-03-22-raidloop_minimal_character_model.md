# Minimal Character Model (RaidLoop d20 Core v1)

## 1. Objective

Design and implement a **minimal, production-aligned character model** based on the d20 Modern ruleset, adapted for:

- No magic
- Gear-driven modifiers
- Raid-based gameplay loop

This model must support:
- Combat resolution
- Initiative tradeoffs
- Equipment modifiers
- Stealth interactions (future-ready)

---

## 2. Constraints

- No classes (yet)
- No feats (yet)
- No skill system (yet)
- No leveling (yet)
- Must remain compatible with future d20 expansion

---

## 3. Core System Alignment (d20 Modern)

### Attack Resolution
d20 + AttackBonus >= Target Defense

### Defense Calculation
Defense = 10 + DEX modifier + equipment modifiers

### Attack Bonus

#### Melee
AttackBonus = BaseAttackBonus + STR modifier + equipment modifiers

#### Ranged
AttackBonus = BaseAttackBonus + DEX modifier + equipment modifiers

### Initiative
Initiative = d20 + DEX modifier + equipment modifiers

---

## 4. Data Model

### Character

```csharp
class Character
{
    int Strength;
    int Dexterity;
    int Constitution;
    int Intelligence;
    int Wisdom;
    int Charisma;

    int HitPoints;
    int BaseAttackBonus;

    int CurrentHP;

    List<Enhancement> EquippedEnhancements;
}
```

---

### Ability Modifier

```csharp
int GetAbilityModifier(int score)
{
    return (score - 10) / 2;
}
```

---

### Combat Calculations

```csharp
int GetMeleeAttackBonus()
{
    return BaseAttackBonus + GetAbilityModifier(Strength) + GetEquipmentAttackBonus();
}

int GetRangedAttackBonus()
{
    return BaseAttackBonus + GetAbilityModifier(Dexterity) + GetEquipmentAttackBonus();
}

int GetDefense()
{
    return 10 + GetAbilityModifier(Dexterity) + GetEquipmentDefenseBonus();
}

int GetInitiative()
{
    return GetAbilityModifier(Dexterity) + GetEquipmentInitiativeModifier();
}
```

---

## 5. Enhancement System

```csharp
class Enhancement
{
    string Slot;

    int AttackModifier;
    int DefenseModifier;
    int InitiativeModifier;

    List<TriggeredEffect> Effects;
}
```

### Initial Enhancements

- Optic: +2 Attack, -2 Initiative
- Suppressor: Enables stealth chaining
- Stim: +2 Attack (3 rounds), then -1 Defense (2 rounds)

---

## 6. Design Rules

### Bounded Accuracy
Keep modifiers within -3 to +3 typical range

### Deterministic Calculations
All math must be inspectable and loggable

### Equipment-Driven Power
No hidden scaling or passive progression

---

## 7. Simulation Harness

```csharp
SimulateCombat(Character a, Character b, int rounds)
```

Must output:
- Hit chance
- Average damage
- Initiative order

---

## 8. Logging

Example:
Roll: 14 + 5 attack = 19 vs Defense 16 → HIT

---

## 9. Validation Tests

- STR increases melee hit rate
- DEX increases ranged hit, defense, initiative
- Optic increases hit, decreases initiative
- Stim applies buff and penalty correctly
- Hit chance stays within reasonable bounds (25%–75%)

---

## 10. Non-Goals

- No classes
- No skills
- No AI
- No loot system
- No raid loop yet

---

## 11. Acceptance Criteria

- Combat resolves deterministically
- Equipment meaningfully impacts outcomes
- System is extensible for future mechanics
