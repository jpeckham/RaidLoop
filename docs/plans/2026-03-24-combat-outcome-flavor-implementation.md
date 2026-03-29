# Combat Outcome Flavor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add distinct miss, evasion, armor-absorb, and penetrating-hit combat log text for both player and enemy attacks in the authoritative raid combat flow.

**Architecture:** Keep the change in the live Supabase SQL combat path. Introduce explicit armor-bonus data and small SQL helpers so attack branches classify outcomes consistently, while preserving the current damage roll and armor DR pipeline on penetrating hits only.

**Tech Stack:** xUnit, .NET test runner, Supabase SQL migrations, PostgreSQL PL/pgSQL

---

### Task 1: Pin the new combat outcome model in migration-content tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend `D20GunDamageMigrationAddsFireModeHelpersAndFullAutoRaidAction` or add a new test that asserts the latest combat migration contains:

```csharp
Assert.Contains("create or replace function game.armor_hit_bonus", migration);
Assert.Contains("attack_total < 10", migration);
Assert.Contains("attack_total < 10 + dodge_bonus", migration);
Assert.Contains("attack_total < 10 + dodge_bonus + armor_bonus", migration);
Assert.Contains("evades your attack", migration);
Assert.Contains("is stopped by armor", migration);
Assert.Contains("armor absorbs", migration);
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the current migration has no armor-bonus helper and no new combat flavor text.

**Step 3: Write minimal implementation**

Do not change production code yet. Only update the test file with the new assertions and point it at the new latest combat migration path once that filename is chosen.

**Step 4: Run test to verify it still fails for the expected reason**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL with missing SQL fragment assertions tied to the new combat flavor model.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "test: pin combat outcome flavor migration"
```

### Task 2: Add armor-bonus data and lookup helpers

**Files:**
- Create: `supabase/migrations/2026032402_add_combat_outcome_flavor.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

In the migration-content test, add assertions for authored armor bonus support:

```csharp
Assert.Contains("armor_hit_bonus int not null default 0", migration);
Assert.Contains("create or replace function game.armor_hit_bonus(armor_name text)", migration);
Assert.Contains("6B43 Zabralo-Sh body armor", migration);
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the new migration file or armor-bonus SQL does not exist yet.

**Step 3: Write minimal implementation**

Create `supabase/migrations/2026032402_add_combat_outcome_flavor.sql` that:

- adds `armor_hit_bonus` to `public.item_defs`
- backfills authored armor rows with explicit bonus values
- updates the authored-item seed/upsert path if needed
- defines:

```sql
create or replace function game.armor_hit_bonus(armor_name text)
returns int
language plpgsql
stable
as $$
begin
    return coalesce(..., 0);
end;
$$;
```

Choose conservative first-pass armor bonus values that scale with existing armor quality and DR.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for armor-bonus helper assertions, with remaining failures limited to combat outcome classification/log text.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032402_add_combat_outcome_flavor.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add armor bonus lookup for combat outcome flavor"
```

### Task 3: Add reusable SQL helpers for combat outcome classification and log text

**Files:**
- Modify: `supabase/migrations/2026032402_add_combat_outcome_flavor.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add assertions that the migration defines helpers for classifying attack outcomes and building flavor text:

```csharp
Assert.Contains("create or replace function game.classify_attack_outcome", migration);
Assert.Contains("create or replace function game.describe_player_attack_outcome", migration);
Assert.Contains("create or replace function game.describe_enemy_attack_outcome", migration);
Assert.Contains("when 'miss'", migration);
Assert.Contains("when 'evaded'", migration);
Assert.Contains("when 'armor-absorbed'", migration);
Assert.Contains("when 'hit'", migration);
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the helper functions are not present yet.

**Step 3: Write minimal implementation**

In the new migration, add helpers shaped like:

```sql
create or replace function game.classify_attack_outcome(
    attack_total int,
    dodge_bonus int,
    armor_bonus int)
returns text
```

Rules:

- `attack_total < 10` => `miss`
- `attack_total < 10 + dodge_bonus` => `evaded`
- `attack_total < 10 + dodge_bonus + armor_bonus` => `armor-absorbed`
- otherwise => `hit`

Add small description helpers that take actor/target names plus absorbed DR and final damage so the player and enemy branches can reuse aligned wording.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for helper assertions, with remaining failures limited to the still-unwired live combat branches.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032402_add_combat_outcome_flavor.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add combat outcome flavor helpers"
```

### Task 4: Wire player attack branches through the new outcome model

**Files:**
- Modify: `supabase/migrations/2026032402_add_combat_outcome_flavor.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add assertions that the migration text now uses armor bonus and flavor helpers in player attack, burst fire, and full auto sections:

```csharp
Assert.Contains("enemy_armor_bonus", migration);
Assert.Contains("player_attack_total", migration);
Assert.Contains("game.classify_attack_outcome(player_attack_total", migration);
Assert.Contains("game.describe_player_attack_outcome", migration);
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the player combat branches still log only plain hit-or-miss text.

**Step 3: Write minimal implementation**

Update player attack, burst-fire, and full-auto handling so each branch:

- computes the rolled total once
- computes enemy dodge bonus from DEX mod
- resolves enemy armor bonus from equipped armor
- classifies the outcome with `game.classify_attack_outcome(...)`
- logs:
  - miss text for totals under `10`
  - evade text for totals in the dodge band
  - armor-stop text for totals in the plate band
  - hit text with absorbed DR amount when penetrating

Use existing weapon damage and DR helpers only in the `hit` path.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for player-attack flavor assertions, with remaining failures limited to enemy retaliation paths.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032402_add_combat_outcome_flavor.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add player combat outcome flavor text"
```

### Task 5: Wire enemy retaliation and other incoming-attack branches through the new outcome model

**Files:**
- Modify: `supabase/migrations/2026032402_add_combat_outcome_flavor.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add assertions that all incoming attack branches use the same helper path:

```csharp
Assert.Contains("player_armor_bonus", migration);
Assert.Contains("enemy_attack_total", migration);
Assert.Contains("game.classify_attack_outcome(enemy_attack_total", migration);
Assert.Contains("game.describe_enemy_attack_outcome", migration);
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because enemy retaliation after attacks, medkit, reload, and failed flee still use plain hit-or-miss logs.

**Step 3: Write minimal implementation**

Update each incoming-attack site so enemy attacks:

- use the same `miss` / `evaded` / `armor-absorbed` / `hit` bands
- apply no health loss outside the `hit` band
- preserve current damage roll and DR reduction only on `hit`
- log absorbed DR when armor reduced penetrating damage

Touch the retaliation after:

- player attack loop
- `use-medkit`
- `reload`
- failed `flee`

If extraction ambush combat shares the same combat loop already, no extra branch is needed beyond the existing retaliation flow.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for the migration-content flavor assertions across both player and enemy combat branches.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032402_add_combat_outcome_flavor.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add incoming combat outcome flavor text"
```

### Task 6: Verify the full test suite and review migration diffs

**Files:**
- Modify: `supabase/migrations/2026032402_add_combat_outcome_flavor.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Modify: `docs/plans/2026-03-24-combat-outcome-flavor-implementation.md`

**Step 1: Run focused tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS.

**Step 2: Run the full suite**

Run: `dotnet test RaidLoop.sln`

Expected: PASS with no new failures.

**Step 3: Review the final diff**

Run: `git diff -- supabase/migrations/2026032402_add_combat_outcome_flavor.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

Confirm:

- combat outcomes are split into miss, evaded, armor-absorbed, and hit
- player and enemy attacks use the same threshold model
- armor DR still applies only after a penetrating hit
- hit logs mention absorbed DR when relevant

**Step 4: Commit**

```bash
git add supabase/migrations/2026032402_add_combat_outcome_flavor.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs docs/plans/2026-03-24-combat-outcome-flavor-implementation.md
git commit -m "feat: add richer combat outcome flavor text"
```
