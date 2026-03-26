# Start Three Away From Extract Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Change raid starts so players begin three moves away from extract while keeping challenge at zero and preserving the existing movement rules.

**Architecture:** Update the authoritative raid-start SQL payload defaults to initialize `distanceFromExtract` to `3` and keep `challenge` at `0`. Align edge-function projection tests, client hydration expectations, and any movement-start tests so the new start state is consistently enforced without changing the rest of the challenge/distance mechanic.

**Tech Stack:** Supabase SQL migrations, Edge Function JavaScript tests, Blazor/C# tests, .NET 10

---

### Task 1: Add failing tests for the new raid start distance

**Files:**
- Modify: `supabase/functions/game-action/handler.test.mjs`
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing tests**

Update raid-start expectations so both main raids and random raids start with:
- `challenge = 0`
- `distanceFromExtract = 3`

Keep existing encounter assertions unchanged unless the new defaults require adjacent fixture edits.

**Step 2: Run tests to verify they fail**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidStartApiTests|FullyQualifiedName~ProfileMutationFlowTests"`
- `node --test supabase/functions/game-action/handler.test.mjs`

Expected: FAIL because start payloads still initialize `distanceFromExtract` to `0` or fixtures still assume the old values.

### Task 2: Implement the new raid start defaults

**Files:**
- Modify: `supabase/migrations/2026031807_game_raid_start_functions.sql`
- Modify: `supabase/migrations/2026032202_add_dexterity_stats.sql`

**Step 1: Write minimal implementation**

Update authoritative raid-start payload creation so newly created raid snapshots initialize:
- `challenge` to `0`
- `distanceFromExtract` to `3`

Only change start-state defaults. Do not alter movement logic.

**Step 2: Run tests to verify they pass**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidStartApiTests|FullyQualifiedName~ProfileMutationFlowTests"`
- `node --test supabase/functions/game-action/handler.test.mjs`

Expected: PASS.

### Task 3: Verify local database rebuild still applies the migration chain

**Files:**
- Verify: `supabase/migrations/2026031807_game_raid_start_functions.sql`
- Verify: `supabase/migrations/2026032202_add_dexterity_stats.sql`

**Step 1: Run local migration verification**

Run:
- `. .\\env.local.ps1; npx supabase db reset`

Expected: local database rebuild succeeds with the updated start defaults.

### Task 4: Full verification

**Files:**
- Verify: `supabase/functions/game-action/handler.test.mjs`
- Verify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Verify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Verify: `supabase/migrations/2026031807_game_raid_start_functions.sql`
- Verify: `supabase/migrations/2026032202_add_dexterity_stats.sql`

**Step 1: Run verification**

Run:
- `dotnet test RaidLoop.sln`
- `dotnet build RaidLoop.sln`
- `node --test supabase/functions/game-action/handler.test.mjs`
- `git diff --stat`

Expected: tests/build pass and the diff is limited to the start-distance rebalance.
