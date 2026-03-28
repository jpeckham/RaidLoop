# Extract Hold Balance Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace instant extract camping with a timed extract-hold flow, decouple challenge from deterministic enemy kits, and make wisdom reduce extract-hold ambush risk.

**Architecture:** Add a new authoritative backend action for starting and resolving extract holds, plus a new migration that authors extract-hold encounter data and replaces fixed challenge-band enemy loadouts with weighted challenge-biased rolls. Update the client raid HUD and page state to show a 30-second hold countdown and prevent conflicting extract actions while the hold is active. Verify the new contract and authored SQL behavior with focused unit tests in both .NET and Supabase handler suites.

**Tech Stack:** Blazor WebAssembly, C#/.NET tests, Supabase Edge Functions, PostgreSQL migrations, Node test runner

---

### Task 1: Define extract-hold raid state in shared contracts and client projections

**Files:**
- Modify: `src/RaidLoop.Core/Contracts/RaidSnapshot.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`

**Step 1: Write the failing test**

Add a test in `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs` that returns a raid projection with new hold-state fields such as `holdAtExtractUntil` and `extractHoldActive`, then asserts the `Home` page stores them and blocks conflicting extract movement state when active.

```csharp
[Fact]
public async Task StartExtractHold_CallsBackend_And_AppliesHoldProjection()
{
    var actionClient = CreateActionClient("start-extract-hold", _ =>
        CreateRaidResult("""
            {
              "raid": {
                "challenge": 4,
                "distanceFromExtract": 0,
                "extractHoldActive": true,
                "holdAtExtractUntil": "2026-03-28T18:30:00Z",
                "encounterType": "Extraction",
                "encounterDescription": "Holding at extract."
              }
            }
            """));
    var home = CreateHome(actionClient);
    SeedRaid(home);

    await InvokePrivateAsync(home, "StartExtractHoldAsync");

    Assert.True(Assert.IsType<bool>(GetField(home, "_extractHoldActive")));
    Assert.Equal("2026-03-28T18:30:00Z", Assert.IsType<DateTimeOffset?>(GetField(home, "_holdAtExtractUntil"))?.ToString("O"));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~StartExtractHold_CallsBackend_And_AppliesHoldProjection"`

Expected: FAIL because the new action/members do not exist yet.

**Step 3: Write minimal implementation**

- Add hold-state fields to `RaidSnapshot`.
- Add matching private fields in `Home.razor.cs`.
- Update snapshot/projection application so the client stores the new values.
- Add a `StartExtractHoldAsync` method that dispatches `start-extract-hold`.

```csharp
public sealed record RaidSnapshot(
    int Health,
    ...
    bool ExtractHoldActive,
    DateTimeOffset? HoldAtExtractUntil,
    ...
);
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~StartExtractHold_CallsBackend_And_AppliesHoldProjection"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Contracts/RaidSnapshot.cs src/RaidLoop.Client/Pages/Home.razor.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs
git commit -m "feat: add extract hold raid state"
```

### Task 2: Add client-side extract-hold countdown and action wiring

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`

**Step 1: Write the failing test**

Add a test proving the page dispatches `start-extract-hold` instead of `stay-at-extract`, and add/update a binding test if needed so the HUD renders hold status text when `extractHoldActive` is true.

```csharp
[Fact]
public async Task StartExtractHoldAsync_DispatchesStartExtractHoldAction()
{
    var actionClient = CreateActionClient("start-extract-hold", _ => CreateRaidResult("""{ "raid": { "extractHoldActive": true } }"""));
    var home = CreateHome(actionClient);
    SeedRaid(home);

    await InvokePrivateAsync(home, "StartExtractHoldAsync");

    Assert.Single(actionClient.Requests);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~StartExtractHoldAsync_DispatchesStartExtractHoldAction"`

Expected: FAIL because the UI still calls `stay-at-extract`.

**Step 3: Write minimal implementation**

- Replace the `OnStayAtExtract` binding with `OnStartExtractHold`.
- Rename the player-facing action text to `Hold at Extract`.
- Add countdown text derived from `HoldAtExtractUntil`.
- Disable `Attempt Extraction`, `Go Deeper`, and `Move Toward Extract` while a hold is active unless you also add an explicit cancel action in the same task.

```csharp
private async Task StartExtractHoldAsync()
{
    if (_raid is null || _extractHoldActive)
    {
        return;
    }

    await ExecuteRaidActionAsync("start-extract-hold", new { });
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~StartExtractHoldAsync_DispatchesStartExtractHoldAction"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Components/RaidHUD.razor tests/RaidLoop.Core.Tests/RaidActionApiTests.cs
git commit -m "feat: add extract hold client flow"
```

### Task 3: Add edge-function support for extract-hold actions

**Files:**
- Modify: `supabase/functions/game-action/handler.mjs`
- Modify: `supabase/functions/game-action/handler.test.mjs`

**Step 1: Write the failing test**

Add a handler test that posts `start-extract-hold` and expects an `EncounterAdvanced`-style raid projection with `extractHoldActive = true`.

```javascript
test("game-action returns encounter-advanced projections for start-extract-hold", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action, payload) => {
      assert.equal(action, "start-extract-hold");
      assert.equal(payload.knownLogCount, 0);
      return {
        money: 500,
        mainStash: [],
        onPersonItems: [],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: {
          challenge: 2,
          distanceFromExtract: 0,
          extractHoldActive: true,
          holdAtExtractUntil: "2026-03-28T18:30:00Z",
          encounterType: "Extraction",
          encounterTitle: "Extraction Opportunity",
          encounterDescription: "Holding at extract.",
          discoveredLoot: [],
          carriedLoot: [],
          equippedItems: [],
          logEntries: ["You begin holding at extract."],
        },
      };
    },
  });
});
```

**Step 2: Run test to verify it fails**

Run: `node --test supabase/functions/game-action/handler.test.mjs --test-name-pattern "start-extract-hold"`

Expected: FAIL because the handler rejects the new action.

**Step 3: Write minimal implementation**

- Add `start-extract-hold` and, if needed, `resolve-extract-hold` and `cancel-extract-hold` to the allowed action set.
- Ensure the response classification still returns raid projections with the new hold fields.

```javascript
const raidActions = new Set([
  "go-deeper",
  "move-toward-extract",
  "start-extract-hold",
  "resolve-extract-hold",
  "attempt-extract",
]);
```

**Step 4: Run test to verify it passes**

Run: `node --test supabase/functions/game-action/handler.test.mjs --test-name-pattern "start-extract-hold"`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/functions/game-action/handler.mjs supabase/functions/game-action/handler.test.mjs
git commit -m "feat: allow extract hold raid actions"
```

### Task 4: Author the SQL migration for extract-hold actions and timer state

**Files:**
- Create: `supabase/migrations/2026032803_extract_hold_balance.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add assertions in `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs` proving the new migration text includes:
- `start-extract-hold`
- `resolve-extract-hold`
- hold-state fields in the raid payload
- a dedicated extract-hold encounter family

```csharp
[Fact]
public void ExtractHoldBalanceMigration_DefinesExtractHoldActions_And_State()
{
    var migration = File.ReadAllText("supabase/migrations/2026032803_extract_hold_balance.sql");

    Assert.Contains("start-extract-hold", migration);
    Assert.Contains("resolve-extract-hold", migration);
    Assert.Contains("extractHoldActive", migration);
    Assert.Contains("holdAtExtractUntil", migration);
    Assert.Contains("extract_hold", migration);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ExtractHoldBalanceMigration"`

Expected: FAIL because the migration does not exist yet.

**Step 3: Write minimal implementation**

Create the migration to:
- add new raid payload fields for hold state
- add/replace `perform_raid_action` branches for `start-extract-hold`, `resolve-extract-hold`, and optional `cancel-extract-hold`
- set a server-authored `holdAtExtractUntil`
- prevent double-resolution or free rewards from stale/cancelled hold requests
- add extract-hold encounter tables and authored entries

```sql
elsif action = 'start-extract-hold' then
    raid_payload := jsonb_set(raid_payload, '{extractHoldActive}', 'true'::jsonb, true);
    raid_payload := jsonb_set(raid_payload, '{holdAtExtractUntil}', to_jsonb(now() + interval '30 seconds'), true);
    raid_payload := game.raid_append_log(raid_payload, 'You begin holding at extract.');
elsif action = 'resolve-extract-hold' then
    raid_payload := game.resolve_extract_hold(raid_payload);
end if;
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ExtractHoldBalanceMigration"`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/migrations/2026032803_extract_hold_balance.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add extract hold backend migration"
```

### Task 5: Replace deterministic challenge kits with weighted challenge-biased loadouts

**Files:**
- Modify: `supabase/migrations/2026032803_extract_hold_balance.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-binding test to assert the new migration no longer relies on a direct `challenge_enemy_loadout_table(challenge)` call for combat loadouts and instead references a weighted roll function that applies rarity bias.

```csharp
[Fact]
public void ExtractHoldBalanceMigration_UsesWeightedEnemyLoadoutRolls()
{
    var migration = File.ReadAllText("supabase/migrations/2026032803_extract_hold_balance.sql");

    Assert.Contains("create or replace function game.roll_enemy_loadout", migration);
    Assert.Contains("rarity_bias", migration);
    Assert.DoesNotContain("random_enemy_loadout_from_table(game.challenge_enemy_loadout_table(challenge))", migration);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~UsesWeightedEnemyLoadoutRolls"`

Expected: FAIL because the old deterministic mapping is still in force.

**Step 3: Write minimal implementation**

In the migration:
- add a weighted enemy loadout roll helper
- keep `challenge_enemy_stats(challenge)` or equivalent stat scaling
- apply a modest rarity bias term derived from challenge
- preserve rare access to strong kits at all challenges

```sql
enemy_loadout := game.roll_enemy_loadout(challenge);
enemy_stats := game.challenge_enemy_stats(challenge);
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~UsesWeightedEnemyLoadoutRolls"`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/migrations/2026032803_extract_hold_balance.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: randomize enemy loadouts with challenge bias"
```

### Task 6: Add wisdom-aware extract-hold surprise resolution

**Files:**
- Modify: `supabase/migrations/2026032803_extract_hold_balance.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a migration-binding test that checks the migration includes a dedicated surprise resolver using player wisdom for extract-hold combat.

```csharp
[Fact]
public void ExtractHoldBalanceMigration_UsesWisdomAwareSurpriseResolution()
{
    var migration = File.ReadAllText("supabase/migrations/2026032803_extract_hold_balance.sql");

    Assert.Contains("resolve_extract_hold_contact_state", migration);
    Assert.Contains("wisdom", migration);
    Assert.Contains("PlayerAmbush", migration);
    Assert.Contains("EnemyAmbush", migration);
    Assert.Contains("MutualContact", migration);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~WisdomAwareSurpriseResolution"`

Expected: FAIL because extract-hold surprise logic does not exist yet.

**Step 3: Write minimal implementation**

In the migration:
- add a helper such as `game.resolve_extract_hold_contact_state(player_wisdom int, challenge int)`
- bias the result away from `EnemyAmbush` when player wisdom is higher
- use that resolver only for extract-hold combat outcome generation

```sql
contact_state := game.resolve_extract_hold_contact_state(player_wisdom, challenge);
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~WisdomAwareSurpriseResolution"`

Expected: PASS

**Step 5: Commit**

```bash
git add supabase/migrations/2026032803_extract_hold_balance.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: make extract hold surprise wisdom-aware"
```

### Task 7: Verify end-to-end contract behavior for the new extract-hold flow

**Files:**
- Modify: `supabase/functions/game-action/handler.test.mjs`
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`

**Step 1: Write the failing test**

Add end-to-end style tests covering:
- `start-extract-hold` round-trips hold-state projections
- `resolve-extract-hold` returns a valid extraction, neutral, loot, or combat encounter
- old `stay-at-extract` is no longer used by the client

```javascript
test("game-action returns encounter-advanced projections for resolve-extract-hold", async () => {
  const handler = createGameActionHandler({
    dispatchAction: async (accessToken, action) => {
      assert.equal(action, "resolve-extract-hold");
      return {
        money: 500,
        mainStash: [],
        onPersonItems: [],
        randomCharacterAvailableAt: "0001-01-01T00:00:00+00:00",
        randomCharacter: null,
        activeRaid: {
          challenge: 3,
          distanceFromExtract: 0,
          extractHoldActive: false,
          encounterType: "Combat",
          encounterTitle: "Hunter Contact",
          encounterDescription: "Movement breaks the tree line.",
          logEntries: ["You finish holding at extract."],
          discoveredLoot: [],
          carriedLoot: [],
          equippedItems: [],
        },
      };
    },
  });
});
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ExtractHold"`  
Run: `node --test supabase/functions/game-action/handler.test.mjs --test-name-pattern "extract-hold"`

Expected: FAIL until both client and edge-function flows are aligned.

**Step 3: Write minimal implementation**

- Update tests and projection mapping to cover both start and resolution paths.
- Remove or deprecate remaining `stay-at-extract` assumptions from client test fixtures.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ExtractHold"`  
Run: `node --test supabase/functions/game-action/handler.test.mjs --test-name-pattern "extract-hold"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidActionApiTests.cs supabase/functions/game-action/handler.test.mjs
git commit -m "test: cover extract hold action flow"
```

### Task 8: Run full verification and record any residual risk

**Files:**
- Modify: `docs/plans/2026-03-28-extract-hold-balance.md`

**Step 1: Run focused .NET tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidActionApiTests|FullyQualifiedName~HomeMarkupBindingTests"`

Expected: PASS

**Step 2: Run edge-function tests**

Run: `node --test supabase/functions/game-action/handler.test.mjs`

Expected: PASS

**Step 3: Run any migration-local integration test if needed**

Run: `node --test supabase/functions/game-action/local-integration.test.mjs`

Expected: PASS, or document why it was skipped.

**Step 4: Update plan notes with final verification status**

Append a short verification note to this plan documenting what passed, what was skipped, and any remaining balancing risk such as timer feel or rarity tuning.

**Step 5: Commit**

```bash
git add docs/plans/2026-03-28-extract-hold-balance.md
git commit -m "docs: record extract hold verification results"
```
