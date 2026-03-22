# Weapon Armor Penetration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add weapon-authored armor penetration to live raid combat so stronger default cartridges reduce armor damage reduction before damage is applied.

**Architecture:** The live combat path is implemented in Supabase SQL migrations and pinned by migration-content tests. This increment should preserve the existing hit-roll flow and armor damage reduction model, but extract armor mitigation into reusable SQL helpers and subtract a weapon's authored `armor_penetration` value before applying reduced damage. Weapon penetration should be authored by weapon name for now, representing each weapon's default ammunition.

**Tech Stack:** Supabase SQL migrations, PostgreSQL PL/pgSQL, xUnit migration-content tests, .NET test runner

---

### Task 1: Pin the migration requirements with failing tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a migration-content test that expects a new Supabase migration file for the armor penetration increment and asserts that it:
- defines `game.weapon_armor_penetration`
- defines `game.armor_damage_reduction`
- defines `game.apply_armor_damage_reduction`
- updates `game.perform_raid_action`
- applies weapon penetration before armor damage reduction during both player and enemy hit resolution

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the new migration file and required SQL fragments do not exist yet.

**Step 3: Write minimal implementation**

Only add the assertions and the migration path constant for the new file. Do not create SQL yet.

**Step 4: Run test to verify it still fails for the expected reason**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL with missing file or missing SQL text assertions.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "test: pin weapon armor penetration migration requirements"
```

### Task 2: Add reusable SQL helpers for armor penetration and mitigation

**Files:**
- Create: `supabase/migrations/2026032203_add_weapon_armor_penetration.sql`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-content test to assert the helper bodies contain:
- `create or replace function game.weapon_armor_penetration(weapon_name text)`
- authored weapon-name branches for at least a low-penetration pistol and a higher-penetration marksman rifle
- `create or replace function game.armor_damage_reduction(armor_name text)`
- the current armor DR values preserved in helper form
- `create or replace function game.apply_armor_damage_reduction(incoming_damage int, armor_name text, armor_penetration int default 0)`
- `greatest(0` when reducing armor DR by penetration
- `greatest(1` when clamping final damage

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the helper functions are not implemented yet.

**Step 3: Write minimal implementation**

Create the migration with:
- `game.weapon_armor_penetration(weapon_name text)` returning small integers on the current armor DR scale
- `game.armor_damage_reduction(armor_name text)` centralizing the existing armor-name mapping
- `game.apply_armor_damage_reduction(incoming_damage int, armor_name text, armor_penetration int default 0)` computing:
  - `effective_armor_dr := greatest(0, game.armor_damage_reduction(armor_name) - coalesce(armor_penetration, 0))`
  - `return greatest(1, incoming_damage - effective_armor_dr)`

Do not introduce ammo inventory, caliber fields, or UI changes in this increment.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for helper-function assertions, with any remaining failures limited to the yet-unwired combat behavior assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032203_add_weapon_armor_penetration.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add weapon armor penetration helpers"
```

### Task 3: Thread weapon armor penetration through player hits

**Files:**
- Modify: `supabase/migrations/2026032203_add_weapon_armor_penetration.sql`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-content test to assert that player `attack` and `burst-fire` branches:
- derive the equipped weapon name
- call `game.weapon_armor_penetration`
- route hit damage through `game.apply_armor_damage_reduction`
- preserve existing miss handling, ammo consumption, and malfunction sequencing

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because player hits still subtract raw weapon damage from enemy health.

**Step 3: Write minimal implementation**

Update the player hit branches so:
- the raw weapon damage roll still happens only on a hit
- the enemy's equipped armor name is resolved from `enemy_loadout`
- damage is reduced via `game.apply_armor_damage_reduction`
- the final applied damage is subtracted from `enemy_health`
- log text still reports the applied damage value

Keep hit-roll math, ammo use, and malfunction behavior unchanged.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for player-hit migration assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032203_add_weapon_armor_penetration.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: apply weapon armor penetration to player hits"
```

### Task 4: Thread weapon armor penetration through enemy retaliation

**Files:**
- Modify: `supabase/migrations/2026032203_add_weapon_armor_penetration.sql`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-content test to assert that enemy retaliation branches in `perform_raid_action`:
- call `game.weapon_armor_penetration` for the enemy weapon
- route incoming player damage through `game.apply_armor_damage_reduction`
- remove the repeated inline armor-name `case` expression
- preserve miss logs and death handling

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because enemy retaliation still uses the inline armor DR case block.

**Step 3: Write minimal implementation**

Update each enemy retaliation branch after player attack, medkit use in combat, reload in combat, failed flee, and extraction ambush retaliation so that:
- the enemy weapon name is resolved from `enemy_loadout`
- enemy penetration is computed with `game.weapon_armor_penetration`
- player damage is reduced with `game.apply_armor_damage_reduction`
- all repeated inline armor DR logic is replaced by the helper call

Use the same authored integer penetration scale everywhere in this increment.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for enemy-retaliation migration assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032203_add_weapon_armor_penetration.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: apply weapon armor penetration to enemy retaliation"
```

### Task 5: Verify the targeted suites and inspect the diff

**Files:**
- Modify: none expected
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Run targeted .NET tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|RaidActionApiTests|ProfileMutationFlowTests"`

Expected: PASS

**Step 2: Run the broader core test suite if the targeted tests pass**

Run: `dotnet test RaidLoop.sln`

Expected: PASS, or if there are unrelated failures, capture them explicitly.

**Step 3: Inspect the diff**

Run: `git diff -- supabase/migrations/2026032203_add_weapon_armor_penetration.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs docs/plans/2026-03-22-weapon-armor-penetration.md`

Confirm:
- weapon penetration is authored per weapon name
- armor DR is centralized in one helper instead of repeated inline `case` blocks
- penetration reduces armor DR but cannot make armor negative
- ammo systems, caliber inventory, and armor-class conversion were not introduced

**Step 4: Commit**

```bash
git add supabase/migrations/2026032203_add_weapon_armor_penetration.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs docs/plans/2026-03-22-weapon-armor-penetration.md
git commit -m "feat: add weapon armor penetration to live raid combat"
```
