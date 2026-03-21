# Game Action Event Projections Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace full `game-action` snapshot responses with compact authoritative event results and targeted projections while preserving full snapshot bootstrap on login.

**Architecture:** Keep `profile-bootstrap` as the single full-state hydrate path. Introduce a new `GameActionResult` response envelope for action calls, then migrate action families incrementally so the client can apply authoritative projections through a new reducer instead of replacing the whole world on every click.

**Tech Stack:** Blazor WebAssembly, C# contracts and reducers, Supabase edge functions, Supabase SQL RPC/action functions, xUnit

---

### Task 1: Add Transitional Contracts

**Files:**
- Create: `src/RaidLoop.Core/Contracts/GameActionResult.cs`
- Modify: `src/RaidLoop.Core/Contracts/GameActionResponse.cs`
- Test: `tests/RaidLoop.Core.Tests/ContractsTests.cs`

**Step 1: Write the failing test**

- Add a contract test that deserializes a `game-action` payload containing `eventType`, `event`, and `projections`.
- Add a contract test that still accepts the transitional compatibility shape if `snapshot` is present.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests"`
Expected: FAIL because the new contract type does not exist yet.

**Step 3: Write minimal implementation**

- Add a `GameActionResult` record with:
  - `string EventType`
  - `JsonElement? Event`
  - `JsonElement? Projections`
  - `string? Message`
  - `PlayerSnapshot? Snapshot`
- Update `GameActionResponse` usage or rename it so client and tests can consume the new envelope.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Contracts/GameActionResult.cs src/RaidLoop.Core/Contracts/GameActionResponse.cs tests/RaidLoop.Core.Tests/ContractsTests.cs
git commit -m "Add transitional game action result contract"
```

### Task 2: Add Client Projection Reducer

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Test: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing test**

- Add tests for a new `ApplyActionResult(...)` path:
  - applies `economy.money`
  - applies `stash.mainStash`
  - applies `loadout.onPersonItems`
  - applies `luckRun.randomCharacter` and `randomCharacterAvailableAt`
  - applies trimmed `raid` projection fields without requiring a full snapshot

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`
Expected: FAIL because `ApplyActionResult(...)` does not exist.

**Step 3: Write minimal implementation**

- Add `ApplyActionResult(...)` in `Home.razor.cs`.
- Keep `ApplySnapshot(...)` for bootstrap/resync.
- If `Snapshot` exists and projections are missing, fall back to `ApplySnapshot(...)` during transition.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs
git commit -m "Add client action result reducer"
```

### Task 3: Wire Client API Calls To New Envelope

**Files:**
- Modify: `src/RaidLoop.Client/Services/GameActionApiClient.cs`
- Modify: `src/RaidLoop.Client/Services/IGameActionApiClient.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`

**Step 1: Write the failing test**

- Update fake action responses in the API/client tests to use `GameActionResult`.
- Assert action handlers call `ApplyActionResult(...)` rather than assuming `Snapshot` is always present.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidActionApiTests|FullyQualifiedName~RaidStartApiTests"`
Expected: FAIL because the client still expects full snapshots.

**Step 3: Write minimal implementation**

- Switch `GameActionApiClient` and `IGameActionApiClient` to `GameActionResult`.
- Update the `Home` action call sites to apply results through the reducer.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidActionApiTests|FullyQualifiedName~RaidStartApiTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Services/GameActionApiClient.cs src/RaidLoop.Client/Services/IGameActionApiClient.cs src/RaidLoop.Client/Pages/Home.razor.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs
git commit -m "Use action result envelope on client"
```

### Task 4: Convert Out-Of-Raid Profile Actions First

**Files:**
- Modify: `supabase/functions/game-action/handler.mjs`
- Modify: `supabase/functions/_shared/profile-rpc.mjs`
- Modify: `supabase/migrations/2026031806_game_inventory_functions.sql`
- Test: `supabase/functions/game-action/handler.test.mjs`
- Test: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing test**

- Add tests for sell/buy/equip/stash actions returning:
  - `eventType: ProfileMutated`
  - authoritative `economy`, `stash`, `loadout`, or `luckRun` projections only
- Keep one transitional test allowing `snapshot` to remain present until rollout completes.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`
Run: `deno test supabase/functions/game-action/handler.test.mjs`
Expected: FAIL because the edge function still returns raw snapshots.

**Step 3: Write minimal implementation**

- Add projection builders for out-of-raid state slices.
- Return `ProfileMutated` envelopes from the edge function for these action families.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`
Run: `deno test supabase/functions/game-action/handler.test.mjs`
Expected: PASS

**Step 5: Commit**

```bash
git add supabase/functions/game-action/handler.mjs supabase/functions/_shared/profile-rpc.mjs supabase/migrations/2026031806_game_inventory_functions.sql supabase/functions/game-action/handler.test.mjs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs
git commit -m "Return projections for profile mutation actions"
```

### Task 5: Convert Raid Start Actions

**Files:**
- Modify: `supabase/functions/game-action/handler.mjs`
- Modify: `supabase/migrations/2026031807_game_raid_start_functions.sql`
- Test: `supabase/functions/game-action/handler.test.mjs`
- Test: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`

**Step 1: Write the failing test**

- Add tests for `start-main-raid` and `start-random-raid` returning:
  - `eventType: RaidStarted`
  - `raid` projection
  - `luckRun` projection when relevant

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidStartApiTests"`
Run: `deno test supabase/functions/game-action/handler.test.mjs`
Expected: FAIL because raid start still returns full snapshots only.

**Step 3: Write minimal implementation**

- Build a compact raid-start result envelope in the edge function.
- Return only the raid and luck-run slices needed for post-start UI.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidStartApiTests"`
Run: `deno test supabase/functions/game-action/handler.test.mjs`
Expected: PASS

**Step 5: Commit**

```bash
git add supabase/functions/game-action/handler.mjs supabase/migrations/2026031807_game_raid_start_functions.sql supabase/functions/game-action/handler.test.mjs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs
git commit -m "Return projections for raid start actions"
```

### Task 6: Convert In-Raid Actions

**Files:**
- Modify: `supabase/functions/game-action/handler.mjs`
- Modify: `supabase/migrations/2026031809_game_raid_action_functions.sql`
- Test: `supabase/functions/game-action/handler.test.mjs`
- Test: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`

**Step 1: Write the failing test**

- Add tests for each in-raid action family:
  - `CombatResolved`
  - `LootResolved`
  - `EncounterAdvanced`
  - `RaidFinished`
- Assert the server returns event details plus authoritative trimmed raid projections.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidActionApiTests"`
Run: `deno test supabase/functions/game-action/handler.test.mjs`
Expected: FAIL because combat and loot still serialize full snapshots.

**Step 3: Write minimal implementation**

- Add result builders per in-raid action family.
- Include only touched raid fields and `logEntriesAdded`.
- Preserve server authority by always projecting post-action values from the saved authoritative payload.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidActionApiTests"`
Run: `deno test supabase/functions/game-action/handler.test.mjs`
Expected: PASS

**Step 5: Commit**

```bash
git add supabase/functions/game-action/handler.mjs supabase/migrations/2026031809_game_raid_action_functions.sql supabase/functions/game-action/handler.test.mjs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs
git commit -m "Return compact event projections for raid actions"
```

### Task 7: Remove Transitional Full Snapshots

**Files:**
- Modify: `src/RaidLoop.Core/Contracts/GameActionResponse.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `supabase/functions/game-action/handler.mjs`
- Test: `tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Test: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`

**Step 1: Write the failing test**

- Remove transitional tests that still allow `snapshot`.
- Add assertions that action responses no longer require or return full snapshots.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests|FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~RaidActionApiTests|FullyQualifiedName~RaidStartApiTests"`
Expected: FAIL because compatibility snapshot is still present.

**Step 3: Write minimal implementation**

- Remove `snapshot` from action response handling.
- Keep `profile-bootstrap` as the only full hydrate path.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Contracts/GameActionResponse.cs src/RaidLoop.Client/Pages/Home.razor.cs supabase/functions/game-action/handler.mjs tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs
git commit -m "Remove full snapshots from action responses"
```

### Task 8: Verify Deployment Readiness

**Files:**
- Review: `supabase/README.md`
- Review: `.github/workflows/supabase-deploy.yml`

**Step 1: Run full verification**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`
Expected: PASS with `0` failures

**Step 2: Run edge-function tests**

Run: `deno test supabase/functions/game-action/handler.test.mjs`
Expected: PASS

**Step 3: Review rollout notes**

- Confirm bootstrap still returns full snapshot.
- Confirm every action family has an event type and a defined projection slice.
- Confirm no client action path still requires `ApplySnapshot(...)` except fallback/resync.

**Step 4: Commit final cleanup**

```bash
git add docs/plans/2026-03-20-game-action-event-projections-design.md docs/plans/2026-03-20-game-action-event-projections-implementation.md
git commit -m "Document game action event projection rollout"
```
