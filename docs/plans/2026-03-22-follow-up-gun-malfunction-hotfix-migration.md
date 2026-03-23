# Follow-Up Gun Malfunction Hotfix Migration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a new Supabase migration that patches already-applied local databases to remove gun malfunction behavior and clear any currently jammed raid state without requiring a reset.

**Architecture:** Keep `2026032204` as the clean from-scratch migration and add a new `2026032205` follow-up migration that redefines the live raid action SQL and performs one-time payload cleanup against existing `raid_sessions` and `game_saves` rows. Update the file-based regression tests to treat the new migration as the latest authoritative patch.

**Tech Stack:** PostgreSQL PL/pgSQL migrations, xUnit, .NET test runner

---

### Task 1: Add failing migration-file regression coverage

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

- Point the latest gun-damage migration coverage at a new `2026032205` file.
- Assert that the migration clears legacy `weaponMalfunction` flags in existing raid payloads.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`

Expected: FAIL because `2026032205` does not exist yet.

**Step 3: Write minimal implementation**

- Create `supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql`.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`

Expected: PASS

### Task 2: Add the patch migration

**Files:**
- Create: `supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql`

**Step 1: Write the migration**

- Recreate `game.perform_raid_action` and `public.game_action` with the malfunction-free logic.
- Update existing `public.raid_sessions` rows to remove/reset `weaponMalfunction`.
- Update existing `public.game_saves` active raid payloads to remove/reset `weaponMalfunction`.

**Step 2: Verify targeted tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|GameActionApiClientTests|ProfileMutationFlowTests"`

Expected: PASS

**Step 3: Commit**

```bash
git add docs/plans/2026-03-22-follow-up-gun-malfunction-hotfix-migration.md tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql
git commit -m "fix: add malfunction hotfix follow-up migration"
```
