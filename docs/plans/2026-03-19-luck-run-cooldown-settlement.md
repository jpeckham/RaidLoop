# Luck Run Cooldown Settlement Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make luck run cooldown settlement server-authoritative so the timer/button always come from the backend and empty luck-run states are repaired automatically.

**Architecture:** Keep `randomCharacter` as the single marker for unresolved luck run state. Add a shared SQL settlement helper that clears empty luck run state and sets `randomCharacterAvailableAt` on the server, then call it from raid resolution and save normalization so both future and already-corrupted saves converge to the same authoritative snapshot.

**Tech Stack:** Blazor WebAssembly, C#, xUnit, Supabase Edge Functions, PostgreSQL PL/pgSQL migrations.

---

### Task 1: Lock in the server-side settlement rules with failing tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing tests**

Add assertions that:
- the SQL migration contains a helper that settles empty luck run state by clearing `randomCharacter`
- the helper sets `randomCharacterAvailableAt` using server time plus cooldown
- raid resolution calls that helper so zero-loot random raids start cooldown immediately
- save normalization/bootstrap calls that helper so existing broken saves heal automatically

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the helper and normalization hook do not exist yet.

### Task 2: Implement authoritative luck run settlement in SQL

**Files:**
- Modify: `supabase/migrations/2026031806_game_inventory_functions.sql`
- Modify: `supabase/migrations/2026031809_game_raid_action_functions.sql`
- Create: `supabase/migrations/2026031811_fix_luck_run_cooldown_settlement.sql`

**Step 1: Add the shared settlement helper**

Create a SQL function that:
- receives `randomCharacter` and `randomCharacterAvailableAt`
- clears `randomCharacter` when its inventory is empty
- sets `randomCharacterAvailableAt` to `now + interval '5 minutes'` when settlement starts
- leaves valid unresolved luck run state untouched

**Step 2: Use the helper in save normalization**

Update save normalization so bootstrap and later actions repair old saves stuck with an empty `randomCharacter`.

**Step 3: Use the helper in random raid resolution**

Update the raid resolution path so a random raid with zero extracted items settles immediately and starts cooldown server-side.

**Step 4: Add a follow-up migration**

Mirror the final SQL into a new migration so the hosted database gets the fix.

### Task 3: Verify and ship

**Files:**
- No additional code files expected

**Step 1: Run focused tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS

**Step 2: Run broader verification**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`

Expected: PASS

**Step 3: Push the database fix if tests pass**

Run the linked Supabase push command so production receives the new migration.
