# D20 Encumbrance Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the current linear strength encumbrance cap with the approved D20 medium/heavy load rules and apply only the current-game-equivalent combat penalties.

**Architecture:** Add shared encumbrance-tier helpers in `RaidLoop.Core`, mirror the same thresholds in a new Supabase migration, and apply the resulting dex cap and attack penalty in authoritative backend combat resolution. Keep movement unchanged and continue treating the heavy-load limit as the hard carry cap for gating/UI.

**Tech Stack:** C#, xUnit, Blazor, Supabase SQL migrations, existing RPC/game-action flow

---

### Task 1: Add failing core tests for D20 load thresholds

**Files:**
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\RaidEngineTests.cs`

**Step 1: Write the failing test**

Add tests that assert representative D20 heavy-load caps and tier boundaries for strength scores 8, 10, 14, and 18.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests"`
Expected: FAIL because the current linear formula returns different encumbrance limits and there is no tier classification helper yet.

**Step 3: Write minimal implementation**

Implement the minimal `CombatBalance` helpers required to classify light/medium/heavy and expose D20 heavy-load thresholds.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj tests/RaidLoop.Core.Tests/RaidEngineTests.cs src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Core/Models.cs
git commit -m "test: cover d20 encumbrance thresholds"
```

### Task 2: Add failing core tests for dex cap and attack penalty helpers

**Files:**
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\RaidEngineTests.cs`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Core\CombatBalance.cs`

**Step 1: Write the failing test**

Add tests for:

- Medium load caps effective dex modifier at `+3`
- Heavy load caps effective dex modifier at `+1`
- Medium load attack penalty is `-3`
- Heavy load attack penalty is `-6`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests"`
Expected: FAIL because the helper methods do not exist yet.

**Step 3: Write minimal implementation**

Add helper methods in `CombatBalance` to derive:

- Encumbrance tier
- Effective dexterity modifier under load
- Attack penalty under load

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidEngineTests.cs src/RaidLoop.Core/CombatBalance.cs
git commit -m "feat: add encumbrance combat penalty helpers"
```

### Task 3: Add failing migration regression tests for D20 SQL rules

**Files:**
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\tests\RaidLoop.Core.Tests\HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a test that expects a new migration file to:

- redefine `game.max_encumbrance`
- add tier or penalty helpers for D20 encumbrance
- apply the encumbrance attack penalty in backend combat
- apply the encumbrance dex cap in backend combat-facing calculations

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`
Expected: FAIL because the new migration file and assertions do not exist yet.

**Step 3: Write minimal implementation**

Create the new migration file with the required function redefinitions and combat rule changes.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs supabase/migrations/20260327*_d20_*.sql
git commit -m "test: lock d20 encumbrance migration contract"
```

### Task 4: Apply the backend D20 encumbrance rules

**Files:**
- Create: `C:\Users\james\source\repos\extractor-shooter-light\supabase\migrations\2026032703_apply_d20_encumbrance_thresholds.sql`
- Inspect: `C:\Users\james\source\repos\extractor-shooter-light\supabase\migrations\2026032701_add_strength_encumbrance.sql`
- Inspect: `C:\Users\james\source\repos\extractor-shooter-light\supabase\migrations\2026032202_add_dexterity_stats.sql`
- Inspect: `C:\Users\james\source\repos\extractor-shooter-light\supabase\migrations\2026032204_add_d20_gun_damage_and_full_auto.sql`
- Inspect: `C:\Users\james\source\repos\extractor-shooter-light\supabase\migrations\2026032205_remove_gun_malfunctions_and_clear_jams.sql`

**Step 1: Write the failing test**

Use the migration regression test from Task 3 as the failing contract.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`
Expected: FAIL until the migration contains the expected D20 rules.

**Step 3: Write minimal implementation**

In the migration:

- redefine `game.max_encumbrance(strength int)` to use D20 heavy-load thresholds
- add helper logic for medium/heavy thresholds and attack/dex penalties
- update raid snapshot/build helpers to project the resolved tier and heavy cap
- update combat resolution to cap dex-derived combat values and subtract the tier attack penalty

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add supabase/migrations/2026032703_apply_d20_encumbrance_thresholds.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: enforce d20 encumbrance penalties in backend combat"
```

### Task 5: Update client/core usage to the new D20 rules

**Files:**
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Core\CombatBalance.cs`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Core\Models.cs`
- Modify: `C:\Users\james\source\repos\extractor-shooter-light\src\RaidLoop.Client\Pages\Home.razor.cs`

**Step 1: Write the failing test**

Add or update tests that expect client-side heavy-cap gating to match the D20 table instead of the linear formula.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileMutationFlowTests|RaidEngineTests"`
Expected: FAIL where tests still assume the old cap math.

**Step 3: Write minimal implementation**

Update the client/core code to call the new D20 helpers and keep overweight gating bound to the heavy-load threshold.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileMutationFlowTests|RaidEngineTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Core/Models.cs src/RaidLoop.Client/Pages/Home.razor.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: align client encumbrance logic with d20 load rules"
```

### Task 6: Run full verification

**Files:**
- Verify only

**Step 1: Run targeted test suites**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidEngineTests|ProfileMutationFlowTests|HomeMarkupBindingTests|RaidActionApiTests|RaidStartApiTests"`
Expected: PASS

**Step 2: Run full solution tests**

Run: `dotnet test RaidLoop.sln`
Expected: PASS

**Step 3: Review diff**

Run: `git diff --stat`
Expected: Only the intended encumbrance docs, tests, core, client, and migration changes.

**Step 4: Commit**

```bash
git add .
git commit -m "docs: finalize d20 encumbrance rollout" || true
```
