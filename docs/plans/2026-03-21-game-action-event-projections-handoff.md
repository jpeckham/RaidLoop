# Game Action Event Projections Handoff

**Date:** 2026-03-21

**Context**

This handoff captures the in-progress migration from full `PlayerSnapshot` action responses to compact authoritative `GameActionResult` envelopes with typed events and targeted projections.

Primary design docs:
- [`2026-03-20-game-action-event-projections-design.md`](C:/Users/james/source/repos/extractor-shooter-light/docs/plans/2026-03-20-game-action-event-projections-design.md)
- [`2026-03-20-game-action-event-projections-implementation.md`](C:/Users/james/source/repos/extractor-shooter-light/docs/plans/2026-03-20-game-action-event-projections-implementation.md)

## Current Status

Completed and committed locally:
- Task 1: transitional result contract
- Task 2: client `ApplyActionResult(...)` reducer and projection parsing
- Task 3: client/API switch to `GameActionResult`

Implemented but not yet reviewed/committed:
- Task 4: first server-side action family (`ProfileMutated` for out-of-raid profile actions)

Not started:
- Task 5: raid start actions
- Task 6: in-raid action families
- Task 7: remove transitional full snapshots
- Task 8: rollout verification / deployment readiness

## Relevant Commits

Most recent local commits for this rollout:
- `5a6cb45` `Use action result envelope on client`
- `8a85bdf` `Add client action result reducer`
- `8cb727c` `Add transitional game action result contract`
- `f371693` `Document game action event projection design`

Recent unrelated but relevant baseline commits:
- `c1b20a0` `Refresh Supabase tokens only near expiry`
- `f580016` `Rebalance item sell prices`

## Working Tree State

The working tree currently has uncommitted Task 4 changes:
- [`supabase/functions/game-action/handler.mjs`](C:/Users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/handler.mjs)
- [`supabase/functions/game-action/handler.test.mjs`](C:/Users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/handler.test.mjs)
- [`tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`](C:/Users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs)

Current `git status --short` at handoff time:

```text
 M supabase/functions/game-action/handler.mjs
 M supabase/functions/game-action/handler.test.mjs
 M tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs
```

## What Tasks 1-3 Changed

### Task 1

Added the new transitional result contract:
- [`GameActionResult.cs`](C:/Users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/Contracts/GameActionResult.cs)

Kept legacy compatibility shape:
- [`GameActionResponse.cs`](C:/Users/james/source/repos/extractor-shooter-light/src/RaidLoop.Core/Contracts/GameActionResponse.cs)

Added contract coverage:
- [`ContractsTests.cs`](C:/Users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ContractsTests.cs)

Behavior:
- `GameActionResult` supports `eventType`, `event`, `projections`, transitional `snapshot`, and `message`
- legacy `GameActionResponse` remains plain `snapshot` + `message`

### Task 2

Client reducer work in:
- [`Home.razor.cs`](C:/Users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Pages/Home.razor.cs)
- [`ProfileMutationFlowTests.cs`](C:/Users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs)

Behavior:
- Added `ApplyActionResult(...)`
- Added projection parsing for:
  - `economy`
  - `stash`
  - `loadout`
  - `luckRun`
  - `raid`
- Falls back to `ApplySnapshot(...)` when projections are absent and transitional snapshot exists
- Hardened reducer against:
  - trimmed raid projections
  - fresh raid partial projections
  - casing variants in transition payloads
  - malformed inventory entries (skip rather than fabricate bogus items)

Note:
- `Home.razor.cs` reducer/parsing section is growing quickly. This is not a correctness blocker yet, but later extraction into helper/reducer types is likely worthwhile.

### Task 3

Client/API interface switch:
- [`IGameActionApiClient.cs`](C:/Users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Services/IGameActionApiClient.cs)
- [`GameActionApiClient.cs`](C:/Users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Services/GameActionApiClient.cs)
- [`Home.razor.cs`](C:/Users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Pages/Home.razor.cs)

Test coverage:
- [`RaidActionApiTests.cs`](C:/Users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/RaidActionApiTests.cs)
- [`RaidStartApiTests.cs`](C:/Users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/RaidStartApiTests.cs)
- [`GameActionApiClientTests.cs`](C:/Users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/GameActionApiClientTests.cs)
- fallout updates:
  - [`GameEventValueScenarioTests.cs`](C:/Users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/GameEventValueScenarioTests.cs)
  - [`ProfileMutationFlowTests.cs`](C:/Users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs)

Behavior:
- `IGameActionApiClient.SendAsync(...)` now returns `GameActionResult`
- `GameActionApiClient.SendAsync(...)` maps both payload shapes:
  - legacy `{ snapshot, message }`
  - new `{ eventType, event, projections, ... }`
- `Home` action flows now consume action results through `ApplyActionResult(...)`

## Task 4 Snapshot

Task 4 was implemented by subagent but has **not** gone through the same spec + quality review loop yet.

Changed behavior in uncommitted diff:
- [`handler.mjs`](C:/Users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/handler.mjs)
  - introduces `PROFILE_MUTATION_ACTIONS`
  - wraps those actions as:
    - `eventType: "ProfileMutated"`
    - `event: { action }`
    - `projections`
    - transitional `snapshot`
    - `message: null`
  - leaves other action families on the old snapshot-only response shape
- [`handler.test.mjs`](C:/Users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/handler.test.mjs)
  - adds tests for:
    - `sell-stash-item`
    - `buy-from-shop`
    - `move-stash-to-on-person`
    - `sell-luck-run-item`
- [`ProfileMutationFlowTests.cs`](C:/Users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs)
  - adds a test that projections should win over the transitional snapshot when both are present

Important implementation note:
- Task 4 **did not** modify [`profile-rpc.mjs`](C:/Users/james/source/repos/extractor-shooter-light/supabase/functions/_shared/profile-rpc.mjs)
- Task 4 **did not** modify [`2026031806_game_inventory_functions.sql`](C:/Users/james/source/repos/extractor-shooter-light/supabase/migrations/2026031806_game_inventory_functions.sql)
- Instead, the edge handler derives projections from the authoritative snapshot returned by the existing RPC path

That is a reasonable transitional strategy, but it still needs the normal review gates.

## Task 4 Subagent Notes

Reported assumptions:
- out-of-raid profile mutation set treated as:
  - `sell-stash-item`
  - `move-stash-to-on-person`
  - `sell-on-person-item`
  - `stash-on-person-item`
  - `equip-on-person-item`
  - `unequip-on-person-item`
  - `buy-from-shop`
  - `store-luck-run-item`
  - `move-luck-run-item-to-on-person`
  - `sell-luck-run-item`

Reported targeted test status from implementer:
- `deno test supabase/functions/game-action/handler.test.mjs`
  - initial fail during TDD
  - final pass
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`
  - initial fail during TDD adjustment
  - final pass

These results should be independently rerun before claiming Task 4 done.

## Recommended Next Steps

1. Review the Task 4 diff in:
   - [`handler.mjs`](C:/Users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/handler.mjs)
   - [`handler.test.mjs`](C:/Users/james/source/repos/extractor-shooter-light/supabase/functions/game-action/handler.test.mjs)
   - [`ProfileMutationFlowTests.cs`](C:/Users/james/source/repos/extractor-shooter-light/tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs)
2. Rerun verification locally:
   - `deno test supabase/functions/game-action/handler.test.mjs`
   - `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`
3. Put Task 4 through:
   - spec-compliance review
   - code-quality review
4. If Task 4 clears, commit with:
   - `Return projections for profile mutation actions`
5. Move to Task 5:
   - convert raid start actions to `RaidStarted`
   - update edge handler tests
   - update raid start client tests only as needed

## Useful Commands

Check working state:

```powershell
git status --short
git log --oneline -n 10
```

Task 4 verification:

```powershell
deno test supabase/functions/game-action/handler.test.mjs
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"
```

Broader verification after Task 4 review:

```powershell
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj
```

## Risk Notes

- The server is still transitional:
  - profile mutations may now emit projections at the edge layer
  - raid start and in-raid actions still need conversion
- Transitional `snapshot` is still present by design
- Client reducer logic in [`Home.razor.cs`](C:/Users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Pages/Home.razor.cs) is becoming large; extraction/refactor can wait until after the main contract migration is stable

## If Starting Fresh

Read in this order:
- [`2026-03-21-game-action-event-projections-handoff.md`](C:/Users/james/source/repos/extractor-shooter-light/docs/plans/2026-03-21-game-action-event-projections-handoff.md)
- [`2026-03-20-game-action-event-projections-design.md`](C:/Users/james/source/repos/extractor-shooter-light/docs/plans/2026-03-20-game-action-event-projections-design.md)
- [`2026-03-20-game-action-event-projections-implementation.md`](C:/Users/james/source/repos/extractor-shooter-light/docs/plans/2026-03-20-game-action-event-projections-implementation.md)

Then inspect current uncommitted Task 4 diff with:

```powershell
git diff -- supabase/functions/game-action/handler.mjs supabase/functions/game-action/handler.test.mjs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs
```
