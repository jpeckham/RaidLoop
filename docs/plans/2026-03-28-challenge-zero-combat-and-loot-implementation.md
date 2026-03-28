# Challenge Zero Combat And Loot Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rebalance the `challenge 0` straight-to-extract path so it has more low-tier fights and gray/white loot, while introducing challenge-tiered enemy loadouts and stat profiles.

**Architecture:** Keep the logic authoritative in Supabase. Add authored enemy loadout/stat tables by challenge band, add challenge-0-safe loot tables, and update encounter generation to select challenge-appropriate enemies and loot while preserving the rule that enemies drop the gear they use.

**Tech Stack:** Supabase SQL migrations, C#, xUnit

---

### Task 1: Write failing migration-binding tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing tests**
- Add assertions for a new migration that creates challenge-tier enemy loadout/stat tables.
- Add assertions that challenge `0` loadouts use common guns only and no armor.
- Add assertions that challenge `0` loot tables only contain white/gray items and can include common backpacks.
- Add assertions that travel/extract encounter weights bias toward combat over neutral.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`
Expected: FAIL because the new migration does not yet exist and the authored data is unchanged.

### Task 2: Add the authoritative migration

**Files:**
- Create: `supabase/migrations/2026032801_rebalance_challenge_zero_travel_and_enemy_progression.sql`

**Step 1: Write minimal implementation**
- Add challenge-tier enemy loadout tables.
- Add challenge-tier enemy stat profile helpers using 27-point-buy-authored spreads.
- Add challenge-0-only loot tables capped to white/gray.
- Reweight `default_raid_travel` and `extract_approach`.
- Update encounter generation to pick enemy gear/stats by challenge.

**Step 2: Run focused tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`
Expected: PASS

### Task 3: Verify downstream behavior

**Files:**
- Modify as needed: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify as needed: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify as needed: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`

**Step 1: Run broader raid/API tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|RaidStartApiTests|RaidActionApiTests|ProfileMutationFlowTests"`
Expected: PASS after any necessary expectation updates for the new authored payloads.

**Step 2: Run full verification**

Run: `dotnet test RaidLoop.sln`
Expected: PASS

### Task 4: Apply the migration locally

**Files:**
- Create: `supabase/migrations/2026032801_rebalance_challenge_zero_travel_and_enemy_progression.sql`

**Step 1: Replay against local Supabase**

Run:
```powershell
docker cp supabase/migrations/2026032801_rebalance_challenge_zero_travel_and_enemy_progression.sql supabase_db_supabase-authoritative-backend:/tmp/2026032801_rebalance_challenge_zero_travel_and_enemy_progression.sql
docker exec supabase_db_supabase-authoritative-backend psql -U postgres -d postgres -f /tmp/2026032801_rebalance_challenge_zero_travel_and_enemy_progression.sql
```

Expected: SQL applies cleanly and authored travel/extract encounters reflect the new tiering.
