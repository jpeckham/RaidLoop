# D20 Gun Damage And Fire Modes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the current weapon damage tables with authored d20-style gun damage dice, update burst fire to the new d20 rules, and add a gated full-auto raid action for weapons that support it.

**Architecture:** Weapon combat behavior currently exists in two places: `RaidLoop.Core` combat helpers and the live Supabase SQL combat path. This increment should introduce explicit weapon fire-mode and damage-dice metadata in both seams, replace the old burst behavior with the new attack penalty and extra-die rules, add a `full-auto` live action, and gate the HUD buttons based on the equipped weapon's supported fire modes. Armor penetration and damage reduction stay in place.

**Tech Stack:** C# core combat helpers, Blazor Razor components, Supabase SQL migrations, PostgreSQL PL/pgSQL, xUnit tests, .NET test runner

**Status:** Completed on 2026-03-22. The feature is implemented in core/client/SQL, and verification now includes both Deno Edge Function tests and a local Supabase integration test for the authoritative `public.game_action` path.

---

### Task 1: Pin the new weapon metadata and local UI requirements with failing tests

Status: Completed

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing tests**

Add tests that require:
- d20-style base damage ranges for each weapon:
  - `Makarov` = `2d6`
  - `PPSH` = `2d4`
  - `AK74` = `2d8`
  - `AK47` = `2d10`
  - `SVDS` = `2d12`
  - `PKP` = `2d12`
- authored fire-mode support:
  - `Makarov` = `single`, `burst`
  - `PPSH` = `single`, `burst`, `full-auto`
  - `AK74` = `single`, `burst`, `full-auto`
  - `AK47` = `single`, `burst`, `full-auto`
  - `SVDS` = `single`, `burst`
  - `PKP` = `burst`, `full-auto`
- `RaidHUD.razor` renders a `Full Auto` action callback and supports hiding `Attack` / `Burst Fire` / `Full Auto` independently

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests|HomeMarkupBindingTests"`

Expected: FAIL because the current ranges, attack modes, and HUD parameters do not match the new rules.

**Step 3: Write minimal implementation**

Only add the assertions. Do not change production code yet.

**Step 4: Run tests to verify they still fail for the expected reason**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests|HomeMarkupBindingTests"`

Expected: FAIL with range mismatches and missing HUD markup/parameters.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidEngineTests.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "test: pin d20 gun damage and fire mode requirements"
```

### Task 2: Update core weapon damage and fire-mode metadata

Status: Completed

**Files:**
- Modify: `src/RaidLoop.Core/CombatBalance.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing test**

Extend the core combat tests to assert:
- `CombatBalance.GetDamageRange(...)` returns the new d20-style min/max ranges
- fire-mode helpers distinguish `single`, `burst`, and `full-auto`
- PKP does not support `single`
- Makarov supports `burst` but not `full-auto`
- full-auto capable weapons are limited to the authored list

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidEngineTests`

Expected: FAIL because the current `AttackMode` and range tables still reflect the older flat ranges.

**Step 3: Write minimal implementation**

Update `CombatBalance` so it authors:
- a new `AttackMode` member for `FullAuto`
- weapon base damage ranges equivalent to:
  - `2d4` => `2..8`
  - `2d6` => `2..12`
  - `2d8` => `2..16`
  - `2d10` => `2..20`
  - `2d12` => `2..24`
- helper methods for mode support, such as:
  - `SupportsSingleShot(string weaponName)`
  - `SupportsBurstFire(string weaponName)`
  - `SupportsFullAuto(string weaponName)`
- a burst penalty helper:
  - `GetBurstAttackPenalty(string weaponName)`
- per-die rolling in `RollDamage(...)` so local core behavior matches the d20 dice model instead of sampling a flat range

Do not add ammo spending, attack penalties, or UI changes in this task.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidEngineTests`

Expected: PASS for the new damage-range and fire-mode assertions.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/CombatBalance.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: author d20 weapon damage and fire modes"
```

### Task 3: Gate the raid HUD actions by weapon fire mode

Status: Completed

**Files:**
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the markup-binding test to assert:
- `RaidHUD.razor` includes a `Full Auto` button
- the HUD has independent booleans for `CanAttack`, `CanBurstFire`, and `CanFullAuto`
- `Home.razor` passes the new `CanFullAuto` and `OnFullAuto` parameters
- the current `Burst Fire` button remains wired, but visibility/disabled state is driven by the authored fire modes

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the current HUD has no `Full Auto` action and no dedicated parameter.

**Step 3: Write minimal implementation**

Update the client so:
- `RaidHUD.razor` renders:
  - `Attack` only when `CanAttack`
  - `Burst Fire` only when `CanBurstFire`
  - `Full Auto` only when `CanFullAuto`
- `Home.razor` passes the new full-auto callback and availability flag
- `Home.razor.cs` computes action availability from the equipped weapon's authored fire modes

Keep the rest of the raid HUD behavior unchanged.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for the HUD-action assertions.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Components/RaidHUD.razor src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Pages/Home.razor.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: gate raid actions by weapon fire mode"
```

### Task 4: Pin the live SQL migration requirements for d20 gun damage and full-auto

Status: Completed

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a migration-content test that expects a new Supabase migration file and asserts that it:
- defines weapon fire-mode helpers for SQL
- defines d20-style weapon damage helpers using base dice plus extra dice by mode
- updates `game.perform_raid_action`
- adds a `full-auto` action branch
- spends `3` rounds for `burst-fire`
- spends `10` rounds for `full-auto`
- applies attack penalties of:
  - `0` for `attack`
  - `-2` for supported burst weapons
  - `-3` for semi-auto improvised burst weapons
  - `-4` for `full-auto`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the new migration file does not exist yet.

**Step 3: Write minimal implementation**

Only add the test assertions and migration path constant for the new file. Do not create SQL yet.

**Step 4: Run test to verify it still fails for the expected reason**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL with missing file or missing SQL text.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "test: pin d20 gun damage migration requirements"
```

### Task 5: Add SQL helpers for weapon fire modes, attack penalties, and d20 gun damage

Status: Completed

**Files:**
- Create: `supabase/migrations/2026032204_add_d20_gun_damage_and_full_auto.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-content test to assert the new migration defines:
- `game.weapon_supports_single_shot`
- `game.weapon_supports_burst_fire`
- `game.weapon_supports_full_auto`
- a helper for burst attack penalty or mode attack bonus adjustment
- a helper for d20-style weapon damage dice, such as `game.roll_weapon_damage_d20`
- authored SQL branches for:
  - `Makarov = 2d6`
  - `PPSH = 2d4`
  - `AK74 = 2d8`
  - `AK47 = 2d10`
  - `SVDS = 2d12`
  - `PKP = 2d12`
- extra dice by mode:
  - `0` for attack
  - `+1` die for burst
  - `+2` dice for full-auto

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the helper functions are not implemented yet.

**Step 3: Write minimal implementation**

Create the migration with:
- SQL boolean helpers for supported fire modes per weapon
- a reusable d20 gun-damage helper that rolls `n` dice of the authored die size
- an attack adjustment helper or inline helper-friendly pattern for the mode penalties

Do not wire `perform_raid_action` yet in this task.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for helper assertions, with any remaining failures limited to unwired action branches.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032204_add_d20_gun_damage_and_full_auto.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add d20 gun damage and fire mode helpers"
```

### Task 6: Wire single-shot, burst-fire, and full-auto through the live SQL combat path

Status: Completed

**Files:**
- Modify: `supabase/migrations/2026032204_add_d20_gun_damage_and_full_auto.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-content test to assert that:
- `attack` uses the base d20 attack bonus with no extra mode penalty
- `burst-fire` spends `3` ammo and uses:
  - `-2` for authored burst weapons
  - `-3` for semi-auto-only improvised burst weapons
- `full-auto` spends `10` ammo and uses `-4`
- burst damage adds `+1` damage die
- full-auto damage adds `+2` damage dice
- unsupported modes log a clear invalid-action message or stay unavailable in the action list

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the live SQL action branches still use the old burst behavior and have no `full-auto` branch.

**Step 3: Write minimal implementation**

Update `game.perform_raid_action` so:
- `attack` rolls the new d20 damage helper in single-shot mode
- `burst-fire` replaces the old burst logic with the new ammo spend, penalty, and extra-die behavior
- `full-auto` is added as a new combat action with the authored support check
- `public.game_action(...)` is redefined so `full-auto` is actually routed into `game.perform_raid_action(...)`
- enemy retaliation can continue using the existing simpler enemy-damage shape in this increment unless the authored helper is already being reused there

Preserve armor penetration, armor DR, malfunction checks, and hit/miss logging.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for the live SQL migration assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032204_add_d20_gun_damage_and_full_auto.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add d20 burst and full auto combat actions"
```

### Task 7: Verify targeted suites and inspect the diff

Status: Completed

**Files:**
- Modify: none expected
- Test: `tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`
- Test: `supabase/functions/game-action/handler.test.mjs`
- Test: `supabase/functions/game-action/local-integration.test.mjs`

**Step 1: Run targeted .NET tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests|HomeMarkupBindingTests|RaidActionApiTests|ProfileMutationFlowTests"`

Expected: PASS

Actual: PASS

**Step 2: Run the full solution test suite**

Run: `dotnet test RaidLoop.sln`

Expected: PASS

Actual: PASS

**Step 3: Run targeted Deno tests for the server path**

Run:

```bash
deno test supabase/functions/profile-bootstrap/handler.test.mjs supabase/functions/profile-save/handler.test.mjs supabase/functions/game-action/handler.test.mjs supabase/functions/_shared/profile-rpc.test.mjs
deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs
```

Expected:
- edge handler tests pass
- the local integration test proves `public.game_action('full-auto', ...)` is reachable and resolved by the authoritative SQL path

Actual:
- PASS

**Step 4: Reset and verify the local Supabase schema**

Run:

```bash
npx supabase db reset
npx supabase status --debug
```

Expected:
- local stack restarts cleanly
- the new migration applies without SQL errors
- local project URL remains `http://127.0.0.1:54321`

Actual:
- PASS

**Step 5: Inspect the diff**

Run:

```bash
git diff -- src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Client/Components/RaidHUD.razor src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Pages/Home.razor.cs supabase/functions/game-action/handler.mjs supabase/functions/game-action/handler.test.mjs supabase/functions/game-action/local-integration.test.mjs supabase/migrations/2026032204_add_d20_gun_damage_and_full_auto.sql tests/RaidLoop.Core.Tests/RaidEngineTests.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs docs/plans/2026-03-22-d20-gun-damage-and-fire-modes.md
```

Confirm:
- d20 dice ranges match the approved weapon list
- PKP lacks `Attack` but supports `Burst Fire` and `Full Auto`
- burst and full-auto ammo spends and penalties match the approved rules
- armor penetration remains intact
- `full-auto` is handled as a combat action in the Edge Function wrapper
- the local integration test exercises the authoritative SQL path instead of only file-content assertions

**Step 6: Commit**

```bash
git add src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Client/Components/RaidHUD.razor src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Pages/Home.razor.cs supabase/functions/game-action/handler.mjs supabase/functions/game-action/handler.test.mjs supabase/functions/game-action/local-integration.test.mjs supabase/migrations/2026032204_add_d20_gun_damage_and_full_auto.sql tests/RaidLoop.Core.Tests/RaidEngineTests.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs docs/plans/2026-03-22-d20-gun-damage-and-fire-modes.md
git commit -m "feat: add d20 gun damage and fire mode actions"
```
