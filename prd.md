# Product Requirements Document (PRD)

## Working Title: RaidLoop (2D Extraction RPG)

## 1. Overview

RaidLoop is a lightweight 2D extraction-style RPG inspired by the
risk‑reward gameplay loop found in extraction shooters. Instead of
navigating a full map, encounters are abstracted into sequential events
presented to the player. The player equips a character, enters a raid,
and encounters randomized combat and loot events. The goal is to extract
with valuable gear while managing health, resources, and risk.

## 2. Design Goals

-   Fast gameplay loop (5--10 minute raids)
-   Minimal map navigation; event‑driven encounters
-   High risk/reward decision making
-   Permadeath-style raid loss (lose items brought into raid)
-   Strong gear progression and loot economy

## 3. Core Gameplay Loop

1.  Player prepares for raid
2.  Player equips gear from inventory
3.  Player enters raid
4.  Random encounter occurs:
    -   Enemy encounter
    -   Loot encounter
    -   Neutral event
5.  Player resolves encounter
6.  Player chooses next action:
    -   Continue searching
    -   Move toward extract
7.  Encounters continue until:
    -   Player extracts successfully
    -   Player dies

## 4. Game Structure

### 4.1 Out-of-Raid (Base / Inventory)

Player can: - Manage inventory - Equip gear - Equip weapons - Equip
backpack - Assign skills - Review stash

### 4.2 In-Raid Structure

Raid is a sequence of event nodes.

Each node may generate: - Combat encounter - Loot encounter - Special
event - Extraction opportunity

Exploration is abstracted; players do not walk a map.

## 5. Encounters

### 5.1 Combat Encounter

Screen Layout:

Left Side: Player sprite

Right Side: Enemy sprite

Combat Type: Turn‑based RPG combat.

Possible actions: - Attack - Use skill - Use item - Reload weapon - Flee
(rarely allowed)

Enemy actions: - Attack - Special ability - Defend

### 5.2 Loot Encounter

Player interacts with an object sprite such as: - Filing cabinet -
Weapons crate - Medical container - Dead body

Loot is generated randomly.

Player backpack capacity limits loot.

### 5.3 Random Events

Examples: - Find ammo - Trap - Environmental damage - Hidden stash

## 6. Extraction System

Player may attempt extraction at certain points.

Extraction options: - Immediate extraction - Move toward extract

Moving toward extract may generate encounters.

Possible extraction encounters: - Enemy ambush - Final guard fight

Successful extraction returns player and loot to stash.

## 7. Death Rules

If player dies in raid: - All gear brought into raid is lost - All loot
collected in raid is lost

Permanent stash only contains items successfully extracted.

## 8. Player Character

### 8.1 Stats

Examples: - Health - Stamina - Accuracy - Defense - Critical chance

### 8.2 Skills

Skills modify combat behavior.

Examples: - Burst Fire - Quick Reload - Combat Medic - Scavenger

Skills may: - Increase damage - Reduce ammo use - Improve loot chance -
Improve healing

## 9. Equipment

### 9.1 Weapons

Attributes: - Damage - Ammo type - Magazine size - Reload time

### 9.2 Armor

Attributes: - Damage reduction - Durability

### 9.3 Backpacks

Attributes: - Inventory slots

### 9.4 Consumables

Examples: - Medkits - Bandages - Food - Ammo

## 10. Inventory System

Player stash holds persistent items.

Raid backpack holds temporary raid items.

Inventory constraints: - Slot based - Backpack size determines raid
capacity

## 11. Loot System

Loot tiers: - Common - Uncommon - Rare - Legendary

Loot types: - Weapons - Armor - Consumables - Materials

## 12. Progression

Players improve through: - Better gear - Skill unlocks - Expanded stash

No character leveling required for MVP.

## 13. Art Direction

Style: - Pixel art - 2D sprites

Resolution example: 64x64 characters

Combat screen: Player left Enemy right

Minimal animations required for MVP.

## 14. Technical Architecture (Initial)

Frontend: - MonoGame (C#)

Game State: - Local save files

Possible future backend: - Online stash - Player economy - Leaderboards

## 15. MVP Scope

Included: - Character inventory - Equipment system - Turn-based combat -
Random encounters - Loot system - Extraction system - Permadeath raid
rules

Not included: - Multiplayer - Trading - Large maps - Story campaign

## 16. Example Raid Flow

1.  Equip pistol, armor, backpack
2.  Begin raid
3.  Encounter: enemy scav
4.  Combat
5.  Loot body
6.  Encounter: cabinet
7.  Loot items
8.  Decide: continue searching
9.  Encounter: enemy patrol
10. Combat
11. Health low
12. Attempt extraction
13. Extraction encounter
14. Win fight
15. Extract with loot

## 17. Future Features

-   AI factions
-   Procedural dungeons
-   Weapon modding
-   Insurance system
-   Merchant trading
-   Daily missions
