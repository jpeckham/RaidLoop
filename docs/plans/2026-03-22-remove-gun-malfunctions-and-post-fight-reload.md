# Remove Gun Malfunctions And Add Post-Fight Reload Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove gun malfunctions from live gameplay and allow players to reload their gun after combat the same way they can use a medkit outside combat.

**Architecture:** The live authoritative combat path is implemented in the latest Supabase SQL migration, while the Blazor client derives action availability from local raid projection state. This change removes malfunction checks/state updates from the live raid action flow, keeps `reload` as an ammo refill action both in and out of combat, and stops the client from gating combat buttons on malfunction state while still tolerating legacy payloads.

**Tech Stack:** PostgreSQL PL/pgSQL migrations, Blazor/C#, xUnit, .NET test runner

---

### Task 1: Add failing regression tests for raid projections and post-fight reload

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Test: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing tests**

- Add a projection test showing raid projections without `weaponMalfunction` keep combat actions usable based on ammo only.
- Add a test showing `ReloadAsync` can dispatch `reload` while not in combat when the equipped weapon uses ammo.

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileMutationFlowTests"`

Expected: FAIL because the client still depends on `_weaponMalfunction` and/or reload behavior is not fully covered.

**Step 3: Write minimal implementation**

- Remove malfunction gating from client combat availability logic.
- Preserve legacy snapshot parsing without requiring the field.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileMutationFlowTests"`

Expected: PASS

### Task 2: Remove malfunction behavior from the live raid SQL path

**Files:**
- Modify: `supabase/migrations/2026032204_add_d20_gun_damage_and_full_auto.sql`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

- Extend migration/markup coverage to assert the active migration no longer contains malfunction combat/log strings and still exposes `reload` and `use-medkit`.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`

Expected: FAIL because the current migration still contains malfunction logic.

**Step 3: Write minimal implementation**

- Delete malfunction variable reads, random malfunction rolls, malfunction logs, and `weaponMalfunction` writes from the active `perform_raid_action` SQL.
- Keep `reload` available in combat and outside combat; in combat it still triggers retaliation, outside combat it just refills the current magazine for ammo-using weapons.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests"`

Expected: PASS

### Task 3: Verify the full regression slice

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `supabase/functions/game-action/handler.mjs` if projection normalization needs cleanup
- Test: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Run targeted tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ProfileMutationFlowTests|HomeMarkupBindingTests"`

Expected: PASS

**Step 2: Commit**

```bash
git add docs/plans/2026-03-22-remove-gun-malfunctions-and-post-fight-reload.md tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Pages/Home.razor supabase/migrations/2026032204_add_d20_gun_damage_and_full_auto.sql supabase/functions/game-action/handler.mjs
git commit -m "fix: remove gun malfunctions and allow post-fight reload"
```
