# Challenge And Distance Raid Loop Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the current extract-progress mechanic with explicit `Challenge` and `Distance from Extract` raid state, deterministic extraction at distance zero, and extract-state lingering via `Stay at Extract`.

**Architecture:** Rename and reshape the raid projection contract so the backend and client both carry two clear integers instead of `extractProgress / extractRequired`. Update Supabase raid-action functions to drive state transitions from explicit movement actions, keep extraction deterministic at distance zero, and apply a small post-encounter drift chance that can move the player one step away from extract. Update the Blazor HUD to render both stats and show action sets based on whether the player is at extract.

**Tech Stack:** Supabase SQL migrations, Edge Function JavaScript, Blazor WebAssembly, C#, xUnit, Node test runner

---

### Task 1: Add failing contract and projection tests for the new raid-state fields

**Files:**
- Modify: `src/RaidLoop.Core/Contracts/RaidSnapshot.cs`
- Modify: `tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify: `supabase/functions/game-action/handler.test.mjs`

**Step 1: Write the failing tests**

Add or update assertions so raid snapshots and action projections expect:
- `challenge`
- `distanceFromExtract`
- no `extractProgress`
- no `extractRequired`

Cover at least:
- bootstrap/start-raid projection shape
- action-response projection shape
- client snapshot hydration expectations in `Home` tests

**Step 2: Run tests to verify they fail**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests|FullyQualifiedName~RaidStartApiTests|FullyQualifiedName~ProfileMutationFlowTests"`
- `npm test -- --runInBand supabase/functions/game-action/handler.test.mjs`

Expected: failures because contracts, projections, and tests still reference `extractProgress` and `extractRequired`.

### Task 2: Update authoritative raid payload creation and projection mapping

**Files:**
- Modify: `supabase/migrations/2026031807_game_raid_start_functions.sql`
- Modify: `supabase/functions/game-action/handler.mjs`
- Modify: `src/RaidLoop.Core/Contracts/RaidSnapshot.cs`

**Step 1: Write minimal implementation**

Update raid bootstrap payloads and projection mapping so raids now initialize and expose:
- `challenge = 0`
- `distanceFromExtract = 0`

Remove projection reliance on:
- `extractProgress`
- `extractRequired`

Keep the rest of the raid payload shape unchanged unless required by compilation.

**Step 2: Run tests to verify they pass**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests|FullyQualifiedName~RaidStartApiTests"`
- `npm test -- --runInBand supabase/functions/game-action/handler.test.mjs`

Expected: projection tests pass for the new field names and default values.

### Task 3: Add failing backend action tests for movement, extraction, extract camping, and drift

**Files:**
- Modify: `supabase/functions/game-action/handler.test.mjs`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing tests**

Add tests that assert:
- the away-from-extract action set is `Go Deeper` and `Move Toward Extract`
- the at-extract action set is `Attempt Extraction` and `Stay at Extract`
- `Attempt Extraction` succeeds deterministically when `distanceFromExtract = 0`
- `Stay at Extract` keeps `distanceFromExtract = 0` before encounter drift and increments `challenge`
- `Go Deeper` increments both `challenge` and `distanceFromExtract`
- `Move Toward Extract` decrements `distanceFromExtract` and does not go below zero
- a drift event can increase `distanceFromExtract` by `1` after an encounter, including encounters initiated from extract

Where randomness is involved, structure tests to control or stub random outcomes rather than asserting on unstable probabilities.

**Step 2: Run tests to verify they fail**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests"`
- `npm test -- --runInBand supabase/functions/game-action/handler.test.mjs`

Expected: failures because the current system still uses extraction-progress thresholds and the old button labels.

### Task 4: Implement backend state transitions for `Go Deeper`, `Move Toward Extract`, `Stay at Extract`, and deterministic extract

**Files:**
- Modify: `supabase/migrations/2026032501_add_authored_surprise_encounter_styles.sql`
- Modify: `supabase/functions/game-action/handler.mjs`

**Step 1: Write minimal implementation**

In the authoritative raid-action SQL:
- replace `extractProgress` and `extractRequired` reads/writes with `challenge` and `distanceFromExtract`
- rename the semantic meaning of the non-extract travel action to `go-deeper`
- keep `move-toward-extract`
- add a `stay-at-extract` action
- make `attempt-extract` succeed whenever `distanceFromExtract = 0`
- increment `challenge` on `go-deeper`
- increment `challenge` on `stay-at-extract`
- decrement `distanceFromExtract` on `move-toward-extract`, floor `0`
- add post-encounter drift logic that can increase `distanceFromExtract` by `1`
- append a clear raid-log line when drift fires

Also update any action allow-lists and event echoing in the edge handler to include the new action name.

**Step 2: Run tests to verify they pass**

Run:
- `npm test -- --runInBand supabase/functions/game-action/handler.test.mjs`

Expected: backend action tests pass with deterministic extraction and the new movement semantics.

### Task 5: Update client hydration and HUD bindings to the new raid state

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write minimal implementation**

Update the client page/component flow so it:
- stores `_challenge` and `_distanceFromExtract`
- removes `_extractProgress` and the `ExtractRequired` constant
- hydrates the new values from raid snapshots and action responses
- passes both values into `RaidHUD`
- shows both values in the HUD

In `RaidHUD.razor`:
- replace `Extract Progress` with `Challenge` and `Distance from Extract`
- replace extract-state `Continue Searching` with `Stay at Extract`
- replace non-extract `Continue Searching` with `Go Deeper`
- render action rows based on whether `DistanceFromExtract` equals zero instead of the old progress-based framing

**Step 2: Run tests to verify they pass**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~HomeMarkupBindingTests"`

Expected: client hydration and markup tests pass with the new labels and fields.

### Task 6: Align all remaining migration references and repo documentation

**Files:**
- Modify: `supabase/migrations/2026031810_fix_perform_raid_action_payload_ambiguity.sql`
- Modify: `supabase/migrations/2026032201_add_d20_hit_rolls.sql`
- Modify: `supabase/migrations/2026032202_add_dexterity_stats.sql`
- Modify: `supabase/migrations/2026032203_add_weapon_armor_penetration.sql`
- Modify: `supabase/migrations/2026032204_add_d20_gun_damage_and_full_auto.sql`
- Modify: `supabase/migrations/2026032205_remove_gun_malfunctions_and_clear_jams.sql`
- Modify: `supabase/migrations/2026032302_add_authored_loot_and_encounter_tables.sql`
- Modify: `supabase/migrations/2026032402_add_combat_outcome_flavor.sql`
- Modify: `README.md`

**Step 1: Write minimal implementation**

Update older migration function definitions that still contain copied raid-action logic so the newest schema can be rebuilt without reintroducing `extractProgress` and `extractRequired`.

Refresh player-facing documentation so raid language matches the new model:
- `Challenge`
- `Distance from Extract`
- `Go Deeper`
- `Move Toward Extract`
- `Stay at Extract`

**Step 2: Run verification**

Run:
- `rg -n "extractProgress|extractRequired|Extract Progress|Continue Searching" src tests supabase README.md docs -S`

Expected: no remaining live-code references to the old mechanic outside historical design docs that intentionally preserve past decisions.

### Task 7: Full verification

**Files:**
- Verify: `src/RaidLoop.Core/Contracts/RaidSnapshot.cs`
- Verify: `src/RaidLoop.Client/Pages/Home.razor`
- Verify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Verify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Verify: `supabase/functions/game-action/handler.mjs`
- Verify: `supabase/functions/game-action/handler.test.mjs`
- Verify: `supabase/migrations/*.sql`
- Verify: `tests/RaidLoop.Core.Tests/*.cs`

**Step 1: Run verification**

Run:
- `dotnet test RaidLoop.sln`
- `dotnet build RaidLoop.sln`
- `npm test -- --runInBand supabase/functions/game-action/handler.test.mjs`
- `git diff --stat`

Expected: solution tests/build pass, Supabase handler tests pass, and the diff is limited to the raid-state conversion work.
