# NPC Loadout Scaling Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add challenge-scaled NPC health, stats, and gear in the authoritative raid flow, while keeping NPC medkits loot-only and ensuring enemy drops match their spawned loadout.

**Architecture:** Extend the authored encounter/loadout data model in Supabase migrations so challenge selects a concrete enemy profile with realized health and stat fields written into the raid payload. Update combat resolution and client projection handling to preserve those fields, and remove any loot fallback that could desync drops from spawned gear.

**Tech Stack:** PostgreSQL/Supabase SQL migrations, Blazor client state projection code, xUnit tests

---

### Task 1: Add failing tests for new enemy stat and loot behavior

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify: `supabase/functions/game-action/handler.test.mjs`

**Step 1: Write the failing tests**

Add assertions that the latest migration includes:

- persisted `enemyConstitution`
- persisted `enemyStrength`
- challenge-aware combat encounter authoring/loadout selection
- loot after enemy death sourced from existing `enemyLoadout` only

Add client projection tests that verify trimmed raid projections can patch in the new enemy stat fields without clearing existing state.

Add edge-function projection tests that verify `enemyConstitution` and `enemyStrength` round-trip in raid projections.

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|ProfileMutationFlowTests"`

Run: `npm test -- supabase/functions/game-action/handler.test.mjs`

Expected: FAIL because the new enemy stat fields and authored scaling behavior do not exist yet.

**Step 3: Write minimal implementation**

No production code in this task.

**Step 4: Run test to verify it still fails for the expected reasons**

Run the same commands and confirm the failures point at missing migration/projection behavior rather than malformed tests.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs supabase/functions/game-action/handler.test.mjs
git commit -m "test: cover npc loadout scaling behavior"
```

### Task 2: Author challenge-scaled enemy profile data in the latest migration

**Files:**
- Modify: `supabase/migrations/2026032601_fix_challenge_distance_prod_upgrade.sql`

**Step 1: Write the failing test**

Use the tests from Task 1 as the failing red state.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL with missing migration text for new enemy stat fields or challenge-aware enemy authoring.

**Step 3: Write minimal implementation**

In the latest migration:

- extend authored encounter/profile data to support challenge-tier enemy stat/loadout selection
- add persisted raid payload fields for `enemyConstitution` and `enemyStrength`
- ensure challenge `0` selects low-tier enemy loadouts
- ensure encounter generation writes full realized enemy stats into the raid payload

Prefer additive authored tables or challenge-band logic that keeps `generate_raid_encounter` data-driven.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/migrations/2026032601_fix_challenge_distance_prod_upgrade.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: author challenge-scaled npc encounter profiles"
```

### Task 3: Wire combat resolution and loot to the realized enemy snapshot

**Files:**
- Modify: `supabase/migrations/2026032601_fix_challenge_distance_prod_upgrade.sql`

**Step 1: Write the failing test**

Use the tests from Task 1 that assert:

- combat reads/preserves the new enemy stat fields
- enemy death drops existing `enemyLoadout` only
- no random fallback loadout is injected on death

**Step 2: Run test to verify it fails**

Run: `npm test -- supabase/functions/game-action/handler.test.mjs`

Expected: FAIL because combat projections or death loot behavior still rely on older fields/fallbacks.

**Step 3: Write minimal implementation**

In `game.perform_raid_action`:

- read `enemyConstitution` and `enemyStrength` from the raid payload
- preserve them across updates
- keep NPC medkits loot-only
- on enemy death, move the realized `enemyLoadout` into `discoveredLoot` without random replacement

**Step 4: Run test to verify it passes**

Run: `npm test -- supabase/functions/game-action/handler.test.mjs`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/migrations/2026032601_fix_challenge_distance_prod_upgrade.sql supabase/functions/game-action/handler.test.mjs
git commit -m "feat: use realized npc loadouts for combat drops"
```

### Task 4: Preserve new enemy stat fields in client raid state

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Core/Contracts/RaidSnapshot.cs`
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing test**

Add or expand raid projection tests to verify the client preserves:

- `enemyDexterity`
- `enemyConstitution`
- `enemyStrength`

when applying a full snapshot and when patching a trimmed raid projection.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ProfileMutationFlowTests`

Expected: FAIL because the new raid fields are not parsed and retained yet.

**Step 3: Write minimal implementation**

Update the raid contract and client projection code so the new enemy fields are parsed safely and preserved across partial raid updates without disturbing unrelated state.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ProfileMutationFlowTests`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Core/Contracts/RaidSnapshot.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs
git commit -m "feat: preserve npc scaling fields in raid projections"
```

### Task 5: Run focused verification and then the full relevant suite

**Files:**
- Modify: none

**Step 1: Run focused tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|ProfileMutationFlowTests"`

Run: `npm test -- supabase/functions/game-action/handler.test.mjs`

Expected: PASS

**Step 2: Run broader regression coverage**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`

Expected: PASS

**Step 3: Inspect git diff**

Run: `git diff --stat`

Expected: Changes limited to migration, contracts/client parsing, and tests/docs.

**Step 4: Commit**

```bash
git add docs/plans/2026-03-26-npc-loadout-scaling-design.md docs/plans/2026-03-26-npc-loadout-scaling-implementation.md
git commit -m "docs: add npc loadout scaling design and plan"
```
