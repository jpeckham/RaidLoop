# Constitution Health Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Persist player constitution and derived max health, and use that max health end-to-end for raid start and healing while preserving the current default of `10 CON => 30 HP`.

**Architecture:** Add constitution and max-health fields to the player snapshot and normalized save payload, derive max health from constitution with one shared rule, and thread authoritative max health through the client raid state instead of using a constant. Update Supabase raid-start and raid-action functions to start from and clamp to saved max health so the backend remains authoritative.

**Tech Stack:** C#, xUnit, Blazor, Supabase SQL migrations

---

### Task 1: Add failing core tests for constitution-derived health

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Modify: `src/RaidLoop.Core/CombatBalance.cs`

**Step 1: Write the failing test**

Add tests that assert:

```csharp
Assert.Equal(30, CombatBalance.GetMaxHealthFromConstitution(10));
Assert.Equal(34, CombatBalance.GetMaxHealthFromConstitution(12));
Assert.Equal(10, CombatBalance.GetMaxHealthFromConstitution(0));
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter GetMaxHealthFromConstitution`

Expected: FAIL because `GetMaxHealthFromConstitution` does not exist yet.

**Step 3: Write minimal implementation**

Add:

```csharp
public static int GetMaxHealthFromConstitution(int constitution)
{
    return 10 + (2 * Math.Max(0, constitution));
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter GetMaxHealthFromConstitution`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidEngineTests.cs src/RaidLoop.Core/CombatBalance.cs
git commit -m "test: add constitution health formula coverage"
```

### Task 2: Add failing contract tests for persisted constitution and max health

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `src/RaidLoop.Core/Contracts/PlayerSnapshot.cs`

**Step 1: Write the failing test**

Update existing `PlayerSnapshot` construction in tests to include:

```csharp
PlayerConstitution: 10,
PlayerMaxHealth: 30,
```

Add assertions that round-tripped snapshots preserve those values. In raid start API tests, use a non-default `maxHealth` projection and assert the client stores it.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ContractsTests|RaidStartApiTests"`

Expected: FAIL because the new snapshot fields do not exist yet.

**Step 3: Write minimal implementation**

Extend `PlayerSnapshot` with:

```csharp
int PlayerConstitution,
int PlayerMaxHealth,
```

Update affected tests and snapshot creation sites to supply the new values.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ContractsTests|RaidStartApiTests"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs src/RaidLoop.Core/Contracts/PlayerSnapshot.cs
git commit -m "feat: persist constitution and max health in player snapshot"
```

### Task 3: Add failing migration tests for constitution backfill

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Create: `supabase/migrations/2026032304_add_constitution_and_health.sql`

**Step 1: Write the failing test**

Add a test similar to the dexterity migration test that asserts the new migration contains:

- `playerConstitution` backfill from old/new casing with default `10`
- `playerMaxHealth` backfill from old/new casing with default `30`
- formula-based derivation from constitution during normalization/default payload setup

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter Constitution`

Expected: FAIL because the migration file does not exist and assertions fail.

**Step 3: Write minimal implementation**

Create the migration to update:

- `game.normalize_save_payload`
- `game.default_save_payload`

Ensure defaults are `10 CON` and `30 HP`, with max health derived from constitution.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter Constitution`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs supabase/migrations/2026032304_add_constitution_and_health.sql
git commit -m "feat: backfill constitution and max health in save payload"
```

### Task 4: Add failing tests for client max-health hydration and usage

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`

**Step 1: Write the failing test**

Extend raid start/action tests to send a raid projection with:

```json
{ "health": 34 }
```

and a player snapshot with:

```json
{ "playerConstitution": 12, "playerMaxHealth": 34 }
```

Assert the client stores `34` as max health and feeds it to the HUD instead of a constant.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidStartApiTests|RaidActionApiTests"`

Expected: FAIL because the client still uses a hard-coded `30`.

**Step 3: Write minimal implementation**

Replace `private const int MaxHealth = 30;` with a stateful max-health value populated from snapshot/projection state and used anywhere raid max health is displayed or initial raid placeholders are constructed.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidStartApiTests|RaidActionApiTests"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidStartApiTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Components/RaidHUD.razor
git commit -m "feat: use authoritative player max health in client raid state"
```

### Task 5: Add failing tests for SQL raid start and medkit health cap

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Modify: `supabase/migrations/2026031807_game_raid_start_functions.sql`
- Modify: `supabase/migrations/2026031809_game_raid_action_functions.sql`

**Step 1: Write the failing test**

Add assertions proving:

- raid snapshot health is built from saved `playerMaxHealth`
- raid-action fallback health and medkit clamp no longer use hard-coded `30`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`

Expected: FAIL because SQL still hard-codes `30`.

**Step 3: Write minimal implementation**

Update SQL to:

- load `player_max_health` from normalized save payload
- set raid snapshot `'health'` from that value
- clamp medkit healing to saved max health
- use saved max health for any raid-health fallback logic that still assumes `30`

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs supabase/migrations/2026031807_game_raid_start_functions.sql supabase/migrations/2026031809_game_raid_action_functions.sql
git commit -m "feat: use persisted max health in raid sql flows"
```

### Task 6: Run full verification and clean up

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Core/Contracts/PlayerSnapshot.cs`
- Modify: `supabase/migrations/2026032304_add_constitution_and_health.sql`
- Modify: `tests/RaidLoop.Core.Tests/*.cs`

**Step 1: Run targeted suites**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests|ContractsTests|RaidStartApiTests|RaidActionApiTests|HomeMarkupBindingTests"`

Expected: PASS

**Step 2: Run full test project**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`

Expected: PASS

**Step 3: Review changed files**

Run:

```bash
git diff -- docs/plans/2026-03-23-constitution-health-design.md docs/plans/2026-03-23-constitution-health-implementation.md src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Core/Contracts/PlayerSnapshot.cs src/RaidLoop.Client/Pages/Home.razor.cs supabase/migrations/2026032304_add_constitution_and_health.sql supabase/migrations/2026031807_game_raid_start_functions.sql supabase/migrations/2026031809_game_raid_action_functions.sql tests/RaidLoop.Core.Tests/RaidEngineTests.cs tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
```

Expected: Only constitution/max-health related changes.

**Step 4: Final commit**

```bash
git add docs/plans/2026-03-23-constitution-health-design.md docs/plans/2026-03-23-constitution-health-implementation.md src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Core/Contracts/PlayerSnapshot.cs src/RaidLoop.Client/Pages/Home.razor.cs supabase/migrations/2026032304_add_constitution_and_health.sql supabase/migrations/2026031807_game_raid_start_functions.sql supabase/migrations/2026031809_game_raid_action_functions.sql tests/RaidLoop.Core.Tests/RaidEngineTests.cs tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add constitution-based player max health"
```
