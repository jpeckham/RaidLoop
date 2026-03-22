# D20 Hit Roll Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a live hit/miss attack roll to raid combat so player and enemy attacks can miss while preserving the existing weapon damage and armor reduction pipeline on hits.

**Architecture:** The live combat path is currently implemented in Supabase SQL, not the C# core combat helpers. This increment should therefore introduce the d20-shaped hit gate in a new Supabase migration by adding a reusable SQL attack-roll helper and routing player and enemy combat branches through it. Existing damage ranges, armor reduction, ammo handling, malfunction handling, and raid projections stay in place.

**Tech Stack:** Supabase SQL migrations, PostgreSQL PL/pgSQL, xUnit migration-content tests, .NET test runner

---

### Task 1: Pin the migration requirements with failing tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a migration-content test that expects a new Supabase migration file for the hit/miss increment and asserts that it:
- defines `game.roll_attack_d20`
- includes natural 1 automatic miss logic
- includes natural 20 automatic hit logic
- updates `game.perform_raid_action`
- logs miss outcomes for both player and enemy attacks

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the new migration file and required SQL fragments do not exist yet.

**Step 3: Write minimal implementation**

Do not touch SQL yet. Only add the assertions that describe the required migration behavior.

**Step 4: Run test to verify it still fails for the expected reason**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL with missing file or missing SQL text assertions.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "test: pin d20 hit roll migration requirements"
```

### Task 2: Add the SQL helper for d20 hit resolution

**Files:**
- Create: `supabase/migrations/2026032201_add_d20_hit_rolls.sql`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Ensure the test from Task 1 specifically asserts the helper body contains:
- a `floor(random() * 20)::int + 1` or equivalent d20 roll
- `roll = 1` miss handling
- `roll = 20` hit handling
- comparison using the d20 shape `roll + attack_bonus >= defense`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the helper function is not implemented yet.

**Step 3: Write minimal implementation**

Create the migration with:
- `create or replace function game.roll_attack_d20(attack_bonus int default 0, defense int default 10)`
- local `roll` variable
- natural 1 / natural 20 branches
- default comparison branch returning boolean

Keep attack bonus and defense fixed via call sites for now. Do not introduce character stats, equipment bonuses, or new payload fields in this increment.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for helper-function assertions, with any remaining failures limited to the yet-unwired combat behavior assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032201_add_d20_hit_rolls.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add d20 attack roll helper"
```

### Task 3: Wire the player combat branches through the hit/miss gate

**Files:**
- Modify: `supabase/migrations/2026032201_add_d20_hit_rolls.sql`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-content test to assert that the player `attack` and `burst-fire` branches:
- call `game.roll_attack_d20`
- log a miss outcome without rolling weapon damage
- only subtract ammo after the existing malfunction/ammo checks still pass

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because `perform_raid_action` still always damages on successful action execution.

**Step 3: Write minimal implementation**

Update the player attack branches so:
- attack roll occurs after malfunction and ammo validation
- miss appends a player miss log entry and skips damage
- hit appends the existing hit log and damage path
- burst fire uses the same hit gate before burst damage is rolled

Keep ammo spending and malfunction behavior otherwise unchanged.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for player-attack migration assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032201_add_d20_hit_rolls.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: gate player attacks behind d20 hit rolls"
```

### Task 4: Wire enemy retaliation through the same hit/miss gate

**Files:**
- Modify: `supabase/migrations/2026032201_add_d20_hit_rolls.sql`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-content test to assert that enemy retaliation branches in `perform_raid_action`:
- call `game.roll_attack_d20`
- append miss logs when retaliation fails
- only apply armor reduction and HP loss on successful hits

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the enemy retaliation branches still always hit.

**Step 3: Write minimal implementation**

Update each enemy retaliation branch after player attack, medkit use in combat, reload in combat, failed flee, and extraction ambush retaliation so that:
- an enemy hit roll occurs first
- miss logs do not alter player health
- hit preserves the current incoming damage and armor reduction behavior

Use the same temporary fixed values for `attack_bonus` and `defense` at every call site in this increment.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for enemy retaliation migration assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032201_add_d20_hit_rolls.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add d20 miss handling to enemy retaliation"
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

Run: `git diff -- supabase/migrations/2026032201_add_d20_hit_rolls.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

Confirm:
- natural 1 and natural 20 rules exist
- both player and enemy attacks can miss
- no new character-stat system was introduced
- damage and armor logic still execute only on hits

**Step 4: Commit**

```bash
git add supabase/migrations/2026032201_add_d20_hit_rolls.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs docs/plans/2026-03-22-d20-hit-roll-implementation.md
git commit -m "feat: add d20 hit rolls to live raid combat"
```
