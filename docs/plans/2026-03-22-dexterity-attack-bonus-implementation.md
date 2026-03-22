# Dexterity Attack Bonus Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add persistent player DEX and encounter-scoped enemy DEX, then use DEX modifiers in the live d20 hit/miss combat rules for both sides.

**Architecture:** The authoritative combat path lives in Supabase SQL migrations, so this increment should extend save normalization and raid snapshot generation there rather than adding a parallel C# rules layer. Player DEX is persisted as a top-level save field with a default backfill of `10`, enemy DEX is added to raid payloads with a default of `10`, and the live `roll_attack_d20` call sites are updated to use DEX-derived attack bonuses and defense targets while preserving natural `1`/`20`, damage, armor, ammo, and malfunction behavior.

**Tech Stack:** Supabase SQL migrations, PostgreSQL PL/pgSQL, xUnit migration-content tests, .NET test runner

---

### Task 1: Pin the DEX migration requirements with failing tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a migration-content test for a new DEX migration file that asserts it:
- defines or updates save normalization to include `playerDexterity`
- backfills missing `playerDexterity` to `10`
- adds `enemyDexterity` to raid snapshots
- defines a reusable DEX modifier helper or equivalent DEX math
- updates the d20 hit roll call sites to pass attack bonus and defense values derived from DEX

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the new migration file and DEX SQL fragments do not exist yet.

**Step 3: Write minimal implementation**

Only add the failing migration-content assertions. Do not modify SQL yet.

**Step 4: Run test to verify it still fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL with missing file or missing SQL fragment assertions.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "test: pin dexterity migration requirements"
```

### Task 2: Add save normalization and backfill for persistent player DEX

**Files:**
- Create: `supabase/migrations/2026032202_add_dexterity_stats.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Make the migration-content test assert the new migration includes:
- `playerDexterity`
- default `10`
- normalization through `game.normalize_save_payload`
- default-save payload including `playerDexterity`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the migration is not implemented yet.

**Step 3: Write minimal implementation**

Create the migration and redefine the minimum required normalization functions so that:
- `game.normalize_save_payload` writes `playerDexterity`
- missing player DEX becomes `10`
- `game.default_save_payload` includes `playerDexterity: 10`

Do not add client UI or C# contracts for DEX in this increment unless tests prove they are required.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for player DEX normalization assertions, with remaining failures limited to enemy DEX and combat-use assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032202_add_dexterity_stats.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: persist player dexterity in normalized saves"
```

### Task 3: Add enemy DEX to live raid payload generation

**Files:**
- Modify: `supabase/migrations/2026032202_add_dexterity_stats.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-content test to assert that raid snapshot generation now includes:
- `enemyDexterity`
- combat encounter enemy DEX assignment
- a default of `10` when no enemy stat is otherwise set

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because raid snapshot generation still omits enemy DEX.

**Step 3: Write minimal implementation**

Update the raid-start migration definitions in the new migration so:
- `game.build_raid_snapshot` includes `enemyDexterity`
- combat encounters assign enemy DEX, starting with `10` or a small bounded random range around it if you need slight variation
- non-combat raids still carry a safe default field value

Keep the field backend-only; do not change the client UI.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for enemy DEX raid-payload assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032202_add_dexterity_stats.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add enemy dexterity to raid payloads"
```

### Task 4: Add DEX modifier math to live d20 combat

**Files:**
- Modify: `supabase/migrations/2026032202_add_dexterity_stats.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-content test to assert the migration includes:
- a helper like `game.ability_modifier(score int)` or equivalent DEX modifier math
- player attack uses `playerDexterity` modifier as attack bonus
- enemy defense uses `10 + enemyDexterity modifier`
- enemy attack uses `enemyDexterity` modifier as attack bonus
- player defense uses `10 + playerDexterity modifier`

Make the assertions specific enough that the SQL clearly wires DEX into both player and enemy hit resolution.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because combat still calls `game.roll_attack_d20(0, 10)`.

**Step 3: Write minimal implementation**

Update the migration so:
- DEX modifier math uses integer d20-style ability modifiers
- player attack branches pass `game.ability_modifier(player_dexterity)` and `10 + game.ability_modifier(enemy_dexterity)` into `game.roll_attack_d20`
- enemy retaliation branches pass `game.ability_modifier(enemy_dexterity)` and `10 + game.ability_modifier(player_dexterity)` into `game.roll_attack_d20`
- natural `1` and `20` behavior remains untouched

Do not add STR, BAB, gear bonuses, or other stats in this increment.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for the DEX combat wiring assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032202_add_dexterity_stats.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: apply dexterity modifiers to d20 combat"
```

### Task 5: Add deterministic DEX combat tests that prove the rule changes hit outcomes

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Create or Modify: `src/RaidLoop.Core/CombatBalance.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing test**

Add new C# tests around a deterministic helper that mirrors the d20 DEX math. Cover:
- higher DEX increases ranged attack bonus
- higher DEX increases defense
- the same d20 roll can miss at low DEX and hit at higher DEX
- the same enemy d20 roll can hit a low-DEX player and miss a higher-DEX player
- natural `1` always misses
- natural `20` always hits

Use fixed roll values in the tests so the assertions prove the DEX bonus behavior directly instead of relying on randomness.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidEngineTests`

Expected: FAIL because the deterministic DEX helper does not exist yet.

**Step 3: Write minimal implementation**

Add the smallest C# helper API needed to support the tests, for example:
- `CombatBalance.GetAbilityModifier(int score)`
- `CombatBalance.RollHits(int roll, int attackBonus, int defense)`
- optional small helpers for `GetRangedAttackBonusFromDex(int dexterity)` and `GetDefenseFromDex(int dexterity)`

This helper exists for deterministic rule verification only; live authoritative combat remains in SQL.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidEngineTests`

Expected: PASS, proving DEX bonuses affect hit outcomes for both sides and that natural 1/20 still work.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/CombatBalance.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "test: verify dexterity changes d20 hit outcomes"
```

### Task 6: Verify targeted and full test suites, then inspect the diff

**Files:**
- Modify: none expected

**Step 1: Run targeted tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|RaidEngineTests|RaidActionApiTests|ProfileMutationFlowTests"`

Expected: PASS

**Step 2: Run the full solution tests**

Run: `dotnet test RaidLoop.sln`

Expected: PASS, or if there are unrelated failures, capture them explicitly.

**Step 3: Inspect the diff**

Run: `git diff -- docs/plans/2026-03-22-dexterity-attack-bonus-implementation.md supabase/migrations/2026032202_add_dexterity_stats.sql src/RaidLoop.Core/CombatBalance.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

Confirm:
- old saves gain `playerDexterity: 10`
- raid payloads carry `enemyDexterity`
- player and enemy hit rolls both use DEX modifiers
- deterministic tests prove higher DEX changes hit outcomes
- no client-side DEX UI was added

**Step 4: Commit**

```bash
git add docs/plans/2026-03-22-dexterity-attack-bonus-implementation.md supabase/migrations/2026032202_add_dexterity_stats.sql src/RaidLoop.Core/CombatBalance.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: add dexterity modifiers to live d20 combat"
```
