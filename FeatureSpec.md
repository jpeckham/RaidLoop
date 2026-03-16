# Feature Specification: Loot Tiers (Rarity System)

**Generated**: 2026-03-15
**Status**: Draft
**Feature**: Four-tier item rarity system with weighted loot tables, rarity-colored UI, varied enemy loadouts, and structured observability events
**Target milestone**: v1.1
**PRD alignment**: Gear Variety Expansion — §1 Equipment Variety, §2 Meaningful Item Differences, §3 Enemy Loadout Variety, §4 Player Access To Variety, §5 Visible In-Game Outcomes
**Predecessor artifacts**: ProjectSnapshot.md · FeatureProposal.md · ArchitectureDecision.md (ADR-008 through ADR-014) · prd.md
**Delivery prerequisite**: None — this feature ships before Home.razor decomposition (v1.2)

---

## 1. Purpose

This document specifies the observable requirements, acceptance criteria, business rules, and non-functional constraints for the Loot Tiers feature. It is the authoritative sign-off source for implementation and QA. All architectural decisions are captured in ADR-008 through ADR-014 (all Accepted); this document does not supersede them.

The feature adds a four-tier rarity model (`Common`, `Uncommon`, `Rare`, `Legendary`) to every `Item` in the game. Rarity drives weighted probabilistic loot tables, rarity-colored item name display in every UI panel, varied enemy encounter equipment, and structured in-process observability events. No save migration is required: existing v3 saves load with all items defaulting to `Common`.

---

## 2. Functional Requirements

### FR-1 — Rarity Enum

**FR-1.1** A new file `src/RaidLoop.Core/Rarity.cs` shall define:

```csharp
public enum Rarity { Common, Uncommon, Rare, Legendary }
```

**FR-1.2** `Rarity` shall be a public type in the `RaidLoop.Core` namespace. No other rarity representation shall exist elsewhere in the solution.

**FR-1.3** The ordinal ordering `Common(0) < Uncommon(1) < Rare(2) < Legendary(3)` shall be stable and relied upon by no production code except display sorting, which may use it.

---

### FR-2 — Item Record Extension

**FR-2.1** The `Item` record in `src/RaidLoop.Core/Models.cs` shall gain a `Rarity Rarity` property defaulting to `Rarity.Common`:

```csharp
// Before
public sealed record Item(string Name, ItemType Type, int Value);
// After
public sealed record Item(string Name, ItemType Type, int Value, Rarity Rarity = Rarity.Common);
```

**FR-2.2** All existing `Item` constructor call sites with positional arguments shall continue to compile without modification because `Rarity` is a trailing defaulted parameter.

**FR-2.3** System.Text.Json deserialization of v3 saves that lack a `"Rarity"` field shall produce `Rarity.Common` for every item (STJ missing-field default). No explicit migration step or version bump is required.

---

### FR-3 — LootTable

**FR-3.1** A new file `src/RaidLoop.Core/LootTable.cs` shall implement an immutable weighted-draw class:

| Member | Signature | Behaviour |
|--------|-----------|-----------|
| Constructor | `LootTable(IReadOnlyList<(Item Item, int Weight)> entries)` | Stores entries; validates all weights > 0 |
| `Draw` | `List<Item> Draw(IRng rng, int count)` | Draws `count` distinct items without replacement using supplied weights |

**FR-3.2** `LootTable.Draw` shall select items **without replacement** within a single call. If `count` exceeds the number of entries, all entries are returned in drawn order (no exception; no duplicates).

**FR-3.3** `LootTable` shall accept an `IRng` instance injected at call time — not stored — so draws are fully deterministic under `SequenceRng` in tests.

**FR-3.4** `LootTable` shall not throw when called with `count = 0`; it shall return an empty list.

---

### FR-4 — LootTables Factory

**FR-4.1** A new file `src/RaidLoop.Core/LootTables.cs` shall provide named static factory methods:

| Method | Returns | Purpose |
|--------|---------|---------|
| `WeaponsCrate()` | `LootTable` | Weapons-only draw pool |
| `ArmourCrate()` | `LootTable` | Armour-only draw pool |
| `MixedCache()` | `LootTable` | Mixed weapons, armour, utility items |
| `EnemyLoadout()` | `LootTable` | Enemy spawn pool used by combat encounters |

**FR-4.2** All rarity weight constants shall be defined as named `private const int` values within `LootTables.cs`. No magic numbers shall appear in draw calls.

**FR-4.3** The default weights shall approximate the following probability distribution per draw:

| Rarity | Weight | Approx. probability |
|--------|--------|---------------------|
| Common | 40 | ~67% |
| Uncommon | 12 | ~20% |
| Rare | 6 | ~10% |
| Legendary | 2 | ~3% |

**FR-4.4** Balance changes (weight adjustments) shall require modifying only `LootTables.cs`. No other file shall hard-code rarity weights.

---

### FR-5 — EncounterLoot Overload

**FR-5.1** `src/RaidLoop.Core/EncounterLoot.cs` shall gain a new overload:

```csharp
public static void StartLootEncounter(
    List<Item> discoveredLoot,
    LootTable table,
    IRng rng,
    int drawCount = 3)
```

**FR-5.2** The existing overload `StartLootEncounter(List<Item>, IEnumerable<Item>)` shall be retained and left functionally unchanged. It shall gain the attribute `[Obsolete("Use the LootTable overload. This overload will be removed in v1.2.")]` once PR-3 (Home.razor migration) is merged.

**FR-5.3** The new overload shall populate `discoveredLoot` by calling `table.Draw(rng, drawCount)` and adding the results to the list. It shall not mutate any other state.

---

### FR-6 — Enemy Loadout Variety

**FR-6.1** Combat encounter initialisation in `Home.razor` shall use `LootTables.EnemyLoadout().Draw(rng, n)` to generate enemy equipment, where `n` is a fixed or bounded value appropriate to the encounter type.

**FR-6.2** Enemy gear generated via `EnemyLoadout()` shall have a `Rarity` property set by the draw, not hardcoded to `Common`.

**FR-6.3** Enemy loot drops (items the player can loot after defeating an enemy) shall retain the `Rarity` value from the enemy's generated loadout.

---

### FR-7 — Rarity-Colored UI

**FR-7.1** Four CSS classes shall be added to the `<style>` block inside `Home.razor`:

| Class | Color | Rarity |
|-------|-------|--------|
| `.rarity-common` | `#b0b0b0` | Common |
| `.rarity-uncommon` | `#1eff00` | Uncommon |
| `.rarity-rare` | `#0070dd` | Rare |
| `.rarity-legendary` | `#ff8000` | Legendary |

**FR-7.2** Every item name rendered in the stash panel, loadout ("For Raid") panel, shop panel, luck-run loot settlement panel, and in-raid loot / inventory panels shall be wrapped in a `<span>` with the appropriate rarity class:

```html
<span class="rarity-@item.Rarity.ToString().ToLower()">@item.Name</span>
```

**FR-7.3** The class derivation expression shall be identical in every panel. No panel shall use a different expression to compute the rarity class name.

**FR-7.4** Items that do not yet carry a `Rarity` value (loaded from a legacy save) display as `Common` (`#b0b0b0`). This is not a visual regression; it is the correct default.

---

### FR-8 — GameEventLog

**FR-8.1** A new static class `GameEventLog` (location: `src/RaidLoop.Core/`) shall provide an in-process, in-memory append-only event log accessible via a static property.

**FR-8.2** `GameEventLog` shall expose:

```csharp
public static IReadOnlyList<GameEvent> Events { get; }
public static void Append(GameEvent evt);
public static void Clear();          // called at raid start
```

**FR-8.3** `GameEvent` shall be a sealed record with at least:

```csharp
public sealed record GameEvent(
    string EventName,
    string RaidId,
    IReadOnlyList<ItemSnapshot> Items,
    DateTimeOffset Timestamp);

public sealed record ItemSnapshot(string Name, string Category, string Rarity);
```

**FR-8.4** The following event names shall be emitted at the specified points:

| Event name | Emitted when | Required item fields |
|------------|--------------|---------------------|
| `loot.drawn` | `LootTable.Draw` completes | all drawn items with name, category, rarity |
| `enemy.loadout.generated` | Enemy spawn in combat encounter | all enemy items with name, category, rarity |
| `player.equip` | Player equips any item | the equipped item |
| `loot.acquired` | Player takes a loot item | the looted item |
| `extraction.complete` | Player successfully extracts | all retained items |

**FR-8.5** `GameEventLog` shall be safe to call from Blazor WASM's single-threaded environment. No locking or thread-safe collections are required.

**FR-8.6** `GameEventLog.Clear()` shall be called at the start of each new raid so the log reflects only the current session.

---

### FR-9 — Delivery Sequencing

**FR-9.1** The feature shall be delivered as three PRs, each leaving the application in a fully working, deployable state:

| PR | Scope | Gate |
|----|-------|------|
| PR-1 | `Rarity.cs`, `LootTable.cs`, `LootTables.cs`, new `EncounterLoot` overload, Core unit tests (≥38 total) | `dotnet test` green; deterministic draw tests pass |
| PR-2 | `Models.cs` `Rarity` field, `GameEventLog`, `GameEvent`, observability integration in Core | All 31+ prior tests pass; ≥7 new tests for rarity default and event emission |
| PR-3 | `Home.razor` CSS rarity classes, rarity-colored `<span>` elements, migrated loot call sites, `[Obsolete]` on legacy overload | Full game loop smoke test passes; rarity colors visible in browser; event log populated during play |

---

## 3. Non-Functional Requirements

**NFR-1 — No new NuGet packages**: No `<PackageReference>` shall be added to any `.csproj`.

**NFR-2 — RaidLoop.Core zero-dependency invariant**: `Rarity.cs`, `LootTable.cs`, `LootTables.cs`, and `GameEventLog` shall not import any namespace outside `System` and `System.Collections.Generic`. No external NuGet types.

**NFR-3 — Build clean**: `dotnet build` on the full solution shall produce zero errors and zero new compiler warnings after each of the three PRs.

**NFR-4 — Test count non-decreasing**: `dotnet test` total passing count shall be ≥38 after PR-1 merges. It shall not decrease in any subsequent PR.

**NFR-5 — No save migration**: No `StashStorage` version bump, no migration lambda, and no schema change beyond what STJ handles automatically via the `Rarity = Rarity.Common` default.

**NFR-6 — Home.razor change surface**: Net line changes to `Home.razor` from this feature shall be ≤30 lines. The structural monolith is addressed in the separate v1.2 decomposition feature.

**NFR-7 — WCAG AA Large Text contrast**: All four rarity CSS colors must meet a minimum 3:1 contrast ratio against the `#0b0f17` background. Pre-verified values:

| Color | Rarity | Contrast vs. `#0b0f17` |
|-------|--------|------------------------|
| `#b0b0b0` | Common | ≥3:1 |
| `#1eff00` | Uncommon | ≥3:1 |
| `#0070dd` | Rare | ≥3:1 |
| `#ff8000` | Legendary | ≥3:1 |

**NFR-8 — No RaidLoop.Core.Tests file modifications**: Existing test files shall not be edited. New test files may be added.

**NFR-9 — No CSS file changes**: No rules shall be added to `src/RaidLoop.Client/wwwroot/css/app.css`. Rarity classes are scoped to `Home.razor`'s inline `<style>` block.

---

## 4. Business Rules

**BR-1 — Rarity is a property of items, not encounters**: `Rarity` is stored on the `Item` record. It travels with the item from generation through stash persistence, loot display, and extraction. Encounters do not have a rarity; items within encounters do.

**BR-2 — Without-replacement draw invariant**: A single `LootTable.Draw(rng, count)` call shall never return two identical item entries. The player shall never receive two of the same item from one loot container draw.

**BR-3 — Legacy save backward compatibility**: A v3 save file that predates this feature shall load without error, migration prompt, or data loss. All items from the legacy save shall display with `Common` rarity styling.

**BR-4 — Enemy drop rarity fidelity**: Items the player can loot from a defeated enemy shall carry the exact `Rarity` value assigned when the enemy loadout was generated. Rarity shall not be re-rolled at loot time.

**BR-5 — Weights are balance constants, not game state**: Loot table weights are compile-time constants. They are not stored in saves, exposed in UI, or configurable at runtime.

**BR-6 — Legacy overload preserved through v1.1**: The original `EncounterLoot.StartLootEncounter(List<Item>, IEnumerable<Item>)` overload shall remain present and compilable throughout all three PRs of this feature. Its `[Obsolete]` attribute shall be applied in PR-3 only, after all `Home.razor` call sites have been migrated.

**BR-7 — No game logic in UI**: `LootTable.Draw`, rarity assignment, and event emission shall occur in `RaidLoop.Core` or in `Home.razor` handler methods. No rarity or weight logic shall appear inside Razor markup expressions.

---

## 5. Acceptance Criteria

Each AC is independently verifiable. Forbidden outcomes are stated explicitly where applicable.

### 5.1 Rarity Enum and Item Model

**AC-1** `src/RaidLoop.Core/Rarity.cs` exists and contains a public `enum Rarity` with exactly the members `Common`, `Uncommon`, `Rare`, `Legendary` in that order.

**AC-2** `Item` has a `Rarity Rarity` property. Calling `new Item("Pistol", ItemType.Weapon, 100)` (without specifying `Rarity`) compiles and produces an item where `item.Rarity == Rarity.Common`.

**AC-3** All 31 pre-existing unit tests pass without any test file modification after PR-1 merges. _Forbidden_: any test failure caused by the `Item` parameter change.

**AC-4** A v3 save JSON string that lacks a `"Rarity"` key on an item deserializes via `StashStorage` without exception, and the loaded item's `Rarity` is `Rarity.Common`. _Forbidden_: `JsonException`, `NullReferenceException`, or `InvalidOperationException` during load of a legacy save.

---

### 5.2 LootTable

**AC-5** `LootTable.Draw(rng, 3)` called on a table with ≥3 entries returns exactly 3 items.

**AC-6** `LootTable.Draw(rng, count)` where `count` exceeds the table's entry count returns all entries without duplicates and without throwing. _Forbidden_: exception; _Forbidden_: list containing the same item twice.

**AC-7** `LootTable.Draw(rng, 0)` returns an empty list. _Forbidden_: exception or non-empty list.

**AC-8** Over 10,000 deterministic draws from `LootTables.MixedCache()` using a seeded `SequenceRng`, the observed frequency of each rarity tier is within ±2 percentage points of the weight-implied probability. Test must be deterministic and pass on CI without a fixed-seed dependency on implementation internals.

**AC-9** Over 10,000 deterministic draws, no single draw call returns a list containing the same item name twice (without-replacement invariant verified at scale). _Forbidden_: any duplicate item within one draw call's result.

---

### 5.3 LootTables Factory

**AC-10** `LootTables.WeaponsCrate()`, `LootTables.ArmourCrate()`, `LootTables.MixedCache()`, and `LootTables.EnemyLoadout()` each construct and return a non-null `LootTable` instance without throwing.

**AC-11** `LootTables.EnemyLoadout()` contains at least one entry at each of `Common`, `Uncommon`, and `Rare` rarity. (Legendary enemy gear is not required but is allowed.)

---

### 5.4 EncounterLoot Overload

**AC-12** `EncounterLoot.StartLootEncounter(List<Item>, LootTable, IRng, int)` exists as a public static method.

**AC-13** After calling the new overload with `drawCount = 3` and a table with ≥3 entries, `discoveredLoot` contains exactly 3 items each bearing a non-null `Rarity` value. _Forbidden_: `discoveredLoot` remaining empty or containing more items than `drawCount`.

**AC-14** The legacy overload `StartLootEncounter(List<Item>, IEnumerable<Item>)` is present and compiles without error after all three PRs. In PR-3 and later it carries `[Obsolete(...)]`. _Forbidden_: removing or altering the legacy overload's behaviour.

---

### 5.5 GameEventLog

**AC-15** After a raid session that includes at least one loot encounter, `GameEventLog.Events` contains at least one event with `EventName == "loot.drawn"`. Each item in that event's `Items` list has a non-null, non-empty `Rarity` string.

**AC-16** After a combat encounter resolves, `GameEventLog.Events` contains at least one event with `EventName == "enemy.loadout.generated"`. The event's `Items` list is non-empty and each item carries a `Rarity` string. _Forbidden_: enemy loadout event with empty `Items`.

**AC-17** After a player successfully extracts, `GameEventLog.Events` contains exactly one event with `EventName == "extraction.complete"` for that raid. Its `Items` list matches the items retained after extraction.

**AC-18** `GameEventLog.Events` is empty at the start of each new raid (i.e., `Clear()` was called). _Forbidden_: events from a prior raid appearing in the log for a subsequent raid.

**AC-19** Reading `GameEventLog.Events` from outside the log (e.g., in a test assertion) never throws. _Forbidden_: `InvalidOperationException` or `NullReferenceException` when reading the event list at any time.

---

### 5.6 Rarity-Colored UI

**AC-20** After PR-3 merges, every item name in the stash panel renders as:
```html
<span class="rarity-common">...</span>
```
for `Common` items, with the corresponding class for each other rarity. The class is derived from `item.Rarity.ToString().ToLower()` or an equivalent expression that produces `"common"`, `"uncommon"`, `"rare"`, or `"legendary"`.

**AC-21** The CSS classes `.rarity-common`, `.rarity-uncommon`, `.rarity-rare`, `.rarity-legendary` appear in the `<style>` block of `Home.razor`. _Forbidden_: these classes appearing in `app.css`.

**AC-22** A player who loads a pre-feature v3 save sees all existing item names displayed in the Common color (`#b0b0b0`). No items display without a `rarity-*` class. _Forbidden_: unstyled item names after legacy save load.

**AC-23** Rarity-colored item names appear in all of the following UI panels: stash ("Storage"), loadout ("For Raid"), shop, luck-run loot settlement, in-raid loot discovery, in-raid carried inventory, in-raid equipped slots.

---

### 5.7 Enemy Loadout Variety

**AC-24** Over 3 consecutive simulated raids (using any RNG seed), the enemy loadout events in `GameEventLog.Events` contain items from at least 2 distinct `Rarity` values. _Forbidden_: all enemy items having `Rarity == "common"` in every raid.

**AC-25** Items looted from a defeated enemy display the same rarity color as recorded in that enemy's `enemy.loadout.generated` event. _Forbidden_: enemy drop rarity differing from the generated loadout rarity.

---

### 5.8 Observability (Agentic QA)

**AC-26** After running 3 consecutive full game loops (prepare → raid → loot → extract), `GameEventLog.Events` contains:
- ≥1 `loot.drawn` event
- ≥1 `enemy.loadout.generated` event
- ≥1 `extraction.complete` event

An agent reading `GameEventLog.Events` can verify gear variety without browser automation.

**AC-27** Across the 3 game loops in AC-26, `GameEventLog.Events` contains items from at least 2 distinct `Rarity` values. This satisfies PRD AC-5 (agentic QA can use emitted evidence to verify variety in actual gameplay scenarios).

---

### 5.9 Build and Test Health

**AC-28** After PR-1: `dotnet test` reports ≥38 passing tests, 0 failing. All new tests are deterministic (no `Random.Shared`, no `DateTime.Now`).

**AC-29** After PR-2: `dotnet test` reports ≥45 passing tests (≥7 new for rarity default, event emission, and `GameEventLog`), 0 failing.

**AC-30** After PR-3: `dotnet build` on the full solution reports 0 errors, 0 new warnings. `dotnet test` reports all tests passing.

**AC-31** No `<PackageReference>` is added to any `.csproj` file across all three PRs.

---

### 5.10 Constraint Compliance

**AC-32** No file in `src/RaidLoop.Core/` introduced by this feature imports a NuGet-sourced namespace. All imports are from `System.*`.

**AC-33** `src/RaidLoop.Client/Services/StashStorage.cs` is not modified.

**AC-34** `tests/RaidLoop.Core.Tests/RaidEngineTests.cs` is not modified.

**AC-35** `src/RaidLoop.Client/wwwroot/css/app.css` contains no new CSS rules.

**AC-36** Net line delta to `src/RaidLoop.Client/Pages/Home.razor` from all three PRs combined is ≤30 lines.

---

## 6. Out of Scope

| Item | Reason |
|------|--------|
| Sell-price multipliers by rarity | Economy extension — separate feature |
| `RarityBadge` Razor component | Viable post-decomposition (v1.2); intentionally deferred per ADR-013 |
| Item level or stat scaling by rarity | Combat balance overhaul — out of PRD scope for v1.1 |
| Shop stock filtered by rarity tier | Future shop economy feature |
| Rarity-weighted shop pricing | Future economy feature |
| Cloud save or cross-device sync | Out of scope for entire project |
| Home.razor structural decomposition | v1.2 — separate FeatureSpec; this feature is a prerequisite for it |
| `LootTable` serialization or persistence | Weights are compile-time constants; no save impact |

---

## 7. File Delivery Checklist

| File | Change type | PR |
|------|-------------|-----|
| `src/RaidLoop.Core/Rarity.cs` | New | PR-1 |
| `src/RaidLoop.Core/LootTable.cs` | New | PR-1 |
| `src/RaidLoop.Core/LootTables.cs` | New | PR-1 |
| `src/RaidLoop.Core/EncounterLoot.cs` | Modified (new overload added) | PR-1 |
| `src/RaidLoop.Core/Models.cs` | Modified (`Rarity` field on `Item`) | PR-2 |
| `src/RaidLoop.Core/GameEventLog.cs` | New | PR-2 |
| `src/RaidLoop.Client/Pages/Home.razor` | Modified (CSS classes, spans, migrated calls) | PR-3 |
| `tests/RaidLoop.Core.Tests/LootTableTests.cs` | New | PR-1 |
| `tests/RaidLoop.Core.Tests/GameEventLogTests.cs` | New | PR-2 |

Files that **must not** change:
`src/RaidLoop.Client/Services/StashStorage.cs`, `src/RaidLoop.Client/wwwroot/css/app.css`, `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`, all `.csproj` files.

---

## 8. Definition of Done

The feature is done when all of the following are satisfied:

1. All 36 acceptance criteria above pass.
2. `dotnet build` is clean — zero errors, zero new warnings.
3. `dotnet test` reports ≥45 passing tests, zero failing.
4. Manual smoke test confirms rarity-colored item names appear in every panel, enemy loot drops reflect the generated loadout rarity, and a legacy v3 save loads without error with all items displayed as Common.
5. `GameEventLog.Events` after 3 simulated raids contains `loot.drawn`, `enemy.loadout.generated`, and `extraction.complete` events with ≥2 distinct rarity values across item fields.
6. All three PRs have been individually reviewed and approved before merge to `main`.
7. The legacy `EncounterLoot` overload carries `[Obsolete]` in PR-3 and no active call site in `Home.razor` uses it.
