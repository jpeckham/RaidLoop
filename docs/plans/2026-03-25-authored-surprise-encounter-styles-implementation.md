# Authored Surprise Encounter Styles Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make live raids produce backend-authoritative surprise and initiative states by adding authored combat encounter styles and wiring them into the Supabase encounter generator.

**Architecture:** Extend the existing authored encounter-table path so combat entries can carry a `contact_state`, then select weighted encounter families by context such as normal travel, loot interruption, and extract approach. Each family uses an even authored split of `PlayerAmbush`, `EnemyAmbush`, and `MutualContact`. The SQL generator writes opening-phase fields into `activeRaid`; `MutualContact` rolls initiative immediately at encounter creation, while ambush states defer initiative until after surprise resolves.

**Tech Stack:** Supabase SQL migrations, PostgreSQL PL/pgSQL, Node test runner for edge-function tests, xUnit for client projection tests

---

### Task 1: Pin authored encounter schema changes in migration-content tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add a migration-content test for the new encounter-style migration that asserts the authored encounter table gains `contact_state` support and context families:

```csharp
Assert.Contains("contact_state text not null default 'MutualContact'", migration);
Assert.Contains("'default_raid_travel'", migration);
Assert.Contains("'loot_interruption'", migration);
Assert.Contains("'extract_approach'", migration);
Assert.Contains("'PlayerAmbush'", migration);
Assert.Contains("'EnemyAmbush'", migration);
Assert.Contains("'MutualContact'", migration);
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because no migration adds combat contact-state data yet.

**Step 3: Write minimal implementation**

Do not change production code yet. Only add the failing assertions and point them at the new migration filename you intend to create.

**Step 4: Run test to verify it still fails for the expected reason**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL on missing migration content.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "test: pin authored surprise encounter migration"
```

### Task 2: Add authored combat style families and contact states in a new migration

**Files:**
- Create: `supabase/migrations/2026032501_add_authored_surprise_encounter_styles.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend the migration-content test to assert the new migration:

- adds `contact_state` to `game.encounter_table_entries`
- backfills existing combat rows to `MutualContact`
- inserts new combat encounter rows for:
  - travel player ambush
  - travel enemy ambush
  - travel mutual contact
  - loot interruption player ambush
  - loot interruption enemy ambush
  - loot interruption mutual contact
  - extract approach player ambush
  - extract approach enemy ambush
  - extract approach mutual contact

Use exact assertions for some row keys and phrases:

```csharp
Assert.Contains("alter table game.encounter_table_entries add column if not exists contact_state text", migration);
Assert.Contains("'raid_combat_player_spots_camp'", migration);
Assert.Contains("'raid_combat_enemy_ambush_travel'", migration);
Assert.Contains("'raid_combat_loot_interruption_ambush'", migration);
Assert.Contains("'raid_combat_extract_ambush'", migration);
Assert.Contains("'raid_combat_loot_player_hears_movement'", migration);
Assert.Contains("'raid_combat_extract_mutual_contact'", migration);
Assert.Contains("You spot an enemy camp", migration);
Assert.Contains("You are ambushed while looting", migration);
Assert.Contains("You hear movement while looting", migration);
Assert.Contains("You and a patrol notice each other", migration);
Assert.Contains("Enemy movement near extract", migration);
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the migration file does not exist yet.

**Step 3: Write minimal implementation**

Create `supabase/migrations/2026032501_add_authored_surprise_encounter_styles.sql` that:

- adds `contact_state text not null default 'MutualContact'`
- constrains values to the currently supported set
- backfills existing combat entries to `MutualContact`
- adds weighted combat entries grouped by context family:
  - `default_raid_travel`
  - `loot_interruption`
  - `extract_approach`
- uses an even split inside each family across:
  - `PlayerAmbush`
  - `EnemyAmbush`
  - `MutualContact`
- preserves current loot and neutral encounter rows unchanged

Keep the first increment to `PlayerAmbush`, `EnemyAmbush`, and `MutualContact` only.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for the migration-content assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032501_add_authored_surprise_encounter_styles.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add authored surprise encounter styles"
```

### Task 3: Teach the SQL encounter generator to select combat style families by context

**Files:**
- Modify: `supabase/migrations/2026032501_add_authored_surprise_encounter_styles.sql`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add assertions that the migration updates `game.generate_raid_encounter(...)` to branch combat selection by context:

```csharp
Assert.Contains("selected_combat_table_key text", migration);
Assert.Contains("selected_combat_table_key := 'extract_approach'", migration);
Assert.Contains("selected_combat_table_key := 'loot_interruption'", migration);
Assert.Contains("selected_combat_table_key := 'default_raid_travel'", migration);
Assert.Contains("where entries.table_key = selected_combat_table_key", migration);
```

Also pin a small content marker for loot interruption detection such as reading the prior encounter type or discovered-loot state.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the encounter generator still reads only `default_raid` and `extraction_check`.

**Step 3: Write minimal implementation**

Update `game.generate_raid_encounter(...)` in the new migration so it:

- determines a combat family key before weighted selection
- uses `extract_approach` when `moving_to_extract = true`
- uses `loot_interruption` when the player is leaving or transitioning from a loot encounter
- uses `default_raid_travel` otherwise

Do not add gear or time-of-day logic yet.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS for the family-selection assertions.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032501_add_authored_surprise_encounter_styles.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: select surprise encounter families by context"
```

### Task 4: Write opening-phase fields into authoritative raid payloads on combat generation

**Files:**
- Modify: `supabase/migrations/2026032501_add_authored_surprise_encounter_styles.sql`
- Modify: `supabase/functions/game-action/handler.test.mjs`
- Test: `supabase/functions/game-action/handler.test.mjs`

**Step 1: Write the failing test**

Extend the game-action handler tests with combat encounter payloads that now include authored contact states and assert they project through correctly.

Add at least:

```javascript
assert.equal(body.projections.raid.contactState, "EnemyAmbush");
assert.equal(body.projections.raid.surpriseSide, "Enemy");
assert.equal(body.projections.raid.initiativeWinner, "None");
assert.equal(body.projections.raid.openingActionsRemaining, 1);
```

Also add:

```javascript
assert.equal(body.projections.raid.contactState, "MutualContact");
assert.equal(body.projections.raid.surpriseSide, "None");
assert.equal(body.projections.raid.initiativeWinner, "Player");
assert.equal(body.projections.raid.openingActionsRemaining, 0);
```

and a neutral/non-combat assertion that the defaults remain `None/0/false`.

**Step 2: Run test to verify it fails**

Run: `node --test supabase/functions/game-action/handler.test.mjs`

Expected: FAIL because the SQL/backend payload does not yet set the opening-phase values on generated combat encounters.

**Step 3: Write minimal implementation**

Update the SQL combat-generation path so when a combat row is selected it writes:

```sql
updated_payload := jsonb_set(updated_payload, '{contactState}', to_jsonb(coalesce(selected_entry.contact_state, 'MutualContact')), true);
updated_payload := jsonb_set(updated_payload, '{surpriseSide}', ... , true);
updated_payload := jsonb_set(updated_payload, '{initiativeWinner}', to_jsonb('None'::text), true);
updated_payload := jsonb_set(updated_payload, '{openingActionsRemaining}', to_jsonb(1 or 0), true);
updated_payload := jsonb_set(updated_payload, '{surprisePersistenceEligible}', 'false'::jsonb, true);
```

Rules for first increment:

- `PlayerAmbush` => `surpriseSide = 'Player'`, `openingActionsRemaining = 1`
- `EnemyAmbush` => `surpriseSide = 'Enemy'`, `openingActionsRemaining = 1`
- `MutualContact` => `surpriseSide = 'None'`, roll initiative immediately, set `initiativeWinner` to `Player` or `Enemy`, `openingActionsRemaining = 0`

For ambush cases, keep `initiativeWinner = 'None'` at encounter creation and defer initiative until surprise resolves.

Also clear these fields to neutral defaults on non-combat encounters and successful extraction.

**Step 4: Run test to verify it passes**

Run: `node --test supabase/functions/game-action/handler.test.mjs`

Expected: PASS with the authoritative payload now carrying opening-phase state.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032501_add_authored_surprise_encounter_styles.sql supabase/functions/game-action/handler.test.mjs
git commit -m "feat: write opening phase state during combat generation"
```

### Task 5: Pin client-visible raid start and raid action behavior for authored surprise styles

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`

**Step 1: Write the failing test**

Add tests or assertions showing that:

- combat raid start can arrive with `EnemyAmbush` or `PlayerAmbush`
- loot/extraction/non-combat responses still clear to neutral defaults
- action updates preserve the backend-provided fields exactly

Examples:

```csharp
Assert.Equal("EnemyAmbush", Assert.IsType<string>(GetField(home, "_contactState")));
Assert.Equal("Enemy", Assert.IsType<string>(GetField(home, "_surpriseSide")));
Assert.Equal(1, Assert.IsType<int>(GetField(home, "_openingActionsRemaining")));
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter \"RaidStartApiTests|RaidActionApiTests\"`

Expected: FAIL until the mocked payloads and assertions match the new authored combat-style behavior.

**Step 3: Write minimal implementation**

Update the existing tests and any minimal fixture payloads so they represent:

- at least one combat start with `EnemyAmbush`
- at least one combat/action update with `PlayerAmbush`
- at least one combat start or action update with `MutualContact` and an explicit initiative winner
- neutral defaults for loot and extraction cases

Do not change production client files unless a real regression is exposed.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter \"RaidStartApiTests|RaidActionApiTests\"`

Expected: PASS with client-visible coverage for authored surprise styles.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidStartApiTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs
git commit -m "test: cover authored surprise encounter styles"
```

### Task 6: Update the design doc to reflect the approved hybrid authored-family model

**Files:**
- Modify: `docs/plans/2026-03-24-surprise-and-initiative-design.md`
- Test: `docs/plans/2026-03-24-surprise-and-initiative-design.md`

**Step 1: Write the failing test**

No automated test is needed. Instead, identify the outdated sections that still describe the first increment as context-only without naming the approved hybrid model.

**Step 2: Run review to verify the document is stale**

Review: `docs/plans/2026-03-24-surprise-and-initiative-design.md`

Expected: The design doc still lacks the approved family model of travel combat, loot interruption, and extract approach.

**Step 3: Write minimal implementation**

Update the design doc so it explicitly describes:

- hybrid authored-family encounter generation
- family selection by context
- authored combat rows carrying `contact_state`
- initiative occurring after surprise rounds rather than replacing them
- immediate initiative roll on `MutualContact`

Keep it short and aligned with what was actually implemented.

**Step 4: Review the document to verify it matches implementation**

Review: `docs/plans/2026-03-24-surprise-and-initiative-design.md`

Expected: The approved model is documented accurately and succinctly.

**Step 5: Commit**

```bash
git add docs/plans/2026-03-24-surprise-and-initiative-design.md
git commit -m "docs: update surprise design for authored encounter styles"
```
