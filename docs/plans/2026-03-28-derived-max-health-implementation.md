# Derived Max Health Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove stale saved max-health drift by deriving `playerMaxHealth` from accepted Constitution in backend normalization and profile actions.

**Architecture:** Keep the payload field for compatibility, but stop using it as authoritative input. Recompute max health from normalized accepted stats in `game.normalize_save_payload` and `game.apply_profile_action`, then backfill existing saves.

**Tech Stack:** Supabase SQL migrations, C#, xUnit

---

### Task 1: Write failing migration-binding tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing tests**
- Add assertions for a new migration that derives max health from accepted Constitution.
- Assert that the old “prefer existing positive playerMaxHealth” pattern is absent.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`
Expected: FAIL because the new migration does not exist yet.

### Task 2: Add the backend fix migration

**Files:**
- Create: `supabase/migrations/2026032802_derive_player_max_health_from_constitution.sql`

**Step 1: Write minimal implementation**
- Replace `game.normalize_save_payload(payload jsonb)` with accepted-stats-derived max health.
- Replace `game.apply_profile_action(...)` final save projection to derive `playerMaxHealth` from accepted stats.
- Backfill `public.game_saves`.

**Step 2: Run focused tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`
Expected: PASS

### Task 3: Verify broader behavior

**Files:**
- Modify as needed: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Run targeted behavior tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|ProfileMutationFlowTests"`
Expected: PASS

**Step 2: Run full verification**

Run: `dotnet test RaidLoop.sln`
Expected: PASS
