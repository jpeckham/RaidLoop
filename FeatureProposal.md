# Feature Proposal: Loot Tiers (Rarity System)

**Generated**: 2026-03-15
**Status**: Proposed
**Feature**: Add a four-tier rarity system to items, weighted loot tables, and rarity-colored UI
**Target milestone**: v1.1
**PRD alignment**: Gear Variety Expansion — Equipment Variety (§1), Meaningful Item Differences (§2), Enemy Loadout Variety (§3), Player Access To Variety (§4), Visible In-Game Outcomes (§5)
**Architecture**: Fully designed — ADR-008 through ADR-014 (all Accepted)
**Predecessor artifacts**: ProjectSnapshot.md · ArchitectureDecision.md (ADR-008–014) · prd.md

---

## 1. Summary

RaidLoop's current loot model has no rarity differentiation. Every item has equal probability and identical visual weight. This makes the loot chase flat: there is no moment of excitement when a rare weapon drops, no loadout tension from carrying a legendary item into a raid, and no visible feedback that variety exists at all.

This proposal implements a four-tier rarity system (`Common`, `Uncommon`, `Rare`, `Legendary`) with:
- weighted probabilistic loot tables replacing the current flat item lists,
- CSS class rarity coloring on every item name in the UI,
- enemy encounter equipment drawn from the same weighted tables for consistency,
- structured observability events that make new behavior inspectable by agents and tests.

Zero architecture changes are required. All seven design decisions (ADR-008 through ADR-014) are already accepted. Implementation extends existing patterns in `RaidLoop.Core` and adds ~10 lines to `Home.razor`.

---

## 2. Problem Statement

| Symptom | Evidence |
|---------|----------|
| Flat loot experience | Every item drops at equal weight; no reward gradient |
| No player feedback on item value | Common and rare items are visually identical |
| Predictable enemy loadouts | Enemy equipment follows a fixed pattern per encounter type |
| Loot chase absent | Players have no reason to run repeated raids hoping for better drops |
| PRD acceptance criteria not met | §AC-1 through §AC-5 cannot be satisfied without this feature |

---

## 3. Value Delivered

### Player-visible outcomes

| Outcome | How it appears in the game |
|---------|---------------------------|
| Rarity-colored item names | Gray (Common), Green (Uncommon), Blue (Rare), Orange (Legendary) names everywhere items appear |
| Weighted loot drops | Legendary weapons are rare (~3% weight); Common ammo is frequent (~40%) |
| Varied enemy loadouts | Enemies can spawn with Uncommon or Rare gear, creating more interesting combat variance |
| Loot excitement | Players notice meaningful drops for the first time |
| Loadout decisions | Carrying a Legendary item into a raid raises the stakes of extraction vs. loss |

### Risk and opportunity coverage

| ID | Before | After |
|----|--------|-------|
| O1 — Loot Tiers System | Ready to implement | **Delivered** |
| PRD AC-1 | Not met | Met — broader spread of items accessible |
| PRD AC-2 | Not met | Met — enemies present varied loadouts |
| PRD AC-3 | Not met | Met — variety produces player-visible differences |
| PRD AC-4 | Not met | Met — structured observability events added |
| PRD AC-5 | Not met | Met — agentic QA can verify variety via emitted evidence |

### Unlocked follow-on work

- Home.razor decomposition (ADR-015) can proceed safely — this feature lands while Home.razor is still intact, meeting the ordering constraint
- The legacy `EncounterLoot` overload can be marked `[Obsolete]` once call sites are migrated (ADR-012 / TBD-D)
- Future economy features (item pricing by rarity, sell-price multipliers) have a clean data model to extend

---

## 4. Proposed Solution

All seven design decisions are locked (ADR-008–014). This section summarises implementation scope only.

### New files — `RaidLoop.Core`

| File | Contents | Estimated lines |
|------|----------|----------------|
| `Rarity.cs` | `public enum Rarity { Common, Uncommon, Rare, Legendary }` | ≤10 |
| `LootTable.cs` | Immutable weighted draw class; `Draw(IRng rng, int count)` with without-replacement semantics | ≤60 |
| `LootTables.cs` | Static factory: `WeaponsCrate()`, `ArmourCrate()`, `MixedCache()`, `EnemyLoadout()` | ≤80 |

### Modified files

| File | Change |
|------|--------|
| `Models.cs` | Add `Rarity Rarity = Rarity.Common` defaulted positional parameter to `Item` record (~1 line) |
| `EncounterLoot.cs` | Add `StartLootEncounter(List<Item>, LootTable, IRng, int drawCount = 3)` overload; retain existing overload unchanged (~6 lines) |
| `Home.razor` | Switch loot encounter call sites to new overload; add four CSS rarity classes to `<style>` block; wrap all item-name `<span>` elements with `class="rarity-@item.Rarity.ToString().ToLower()"` (~25 lines net change) |

### Unchanged files

`RaidLoop.Core.Tests/`, `StashStorage.cs`, `Program.cs`, all `.csproj`, all CI workflows. No save migration — existing v3 saves load with all items as `Common` via STJ missing-field default (ADR-014).

### Loot table weight sketch

| Rarity | Weight | Approx. probability (per draw) |
|--------|--------|-------------------------------|
| Common | 40 | ~67% |
| Uncommon | 12 | ~20% |
| Rare | 6 | ~10% |
| Legendary | 2 | ~3% |

Exact weights are authored in `LootTables.cs` as named constants; balance changes are a single file edit.

---

## 5. Observability Design

Per PRD §Observability Requirements, the implementation must emit structured records inspectable by agents. The approach:

- `LootTable.Draw` returns a `List<Item>` with `Rarity` carried on each item — every downstream consumer already receives structured rarity data.
- `EncounterLoot.StartLootEncounter` populates `discoveredLoot` with rarity-tagged items; callers (Home.razor) can log or emit the full list.
- A `GameEventLog` static class (or equivalent lightweight append-only log in `RaidLoop.Core`) will record the following event types:

| Event name | Fields |
|-----------|--------|
| `loot.drawn` | raid-id, source (container type), items: [{name, category, rarity}] |
| `enemy.loadout.generated` | raid-id, encounter-index, items: [{name, category, rarity}] |
| `player.equip` | raid-id, item: {name, category, rarity} |
| `loot.acquired` | raid-id, item: {name, category, rarity} |
| `extraction.complete` | raid-id, retained-items: [{name, category, rarity}] |

The log is an in-memory `List<GameEvent>` accessible via a static property, readable by test assertions and agentic QA scenarios without browser automation.

---

## 6. Delivery Strategy

Three PRs, each leaving the application in a fully working, deployable state.

| PR | Scope | Acceptance gate |
|----|-------|-----------------|
| PR-1 | `Rarity.cs`, `LootTable.cs`, `LootTables.cs` + new `EncounterLoot` overload + Core tests (≥38 total) | `dotnet test` green; `LootTable.Draw` verified by deterministic seed tests |
| PR-2 | `Models.cs` `Rarity` field + `GameEventLog` + observability integration in Core | Existing 31 tests still pass; 7+ new tests for Rarity default, event emission |
| PR-3 | `Home.razor` CSS classes, rarity-colored spans, migrated call sites | Full game loop smoke-tested; rarity colors visible in browser; event log populated during play |

---

## 7. Acceptance Criteria

1. `Rarity` enum exists in `RaidLoop.Core` with values `Common`, `Uncommon`, `Rare`, `Legendary`.
2. All items loaded from a pre-feature v3 save display as `Common` — no save migration errors, no data loss.
3. Items drawn from `LootTable.Draw` never contain duplicates within a single call (without-replacement invariant).
4. Over 10,000 deterministic draws from a seeded `SequenceRng`, observed rarity frequencies are within ±2% of the weight-implied probabilities.
5. `Home.razor` renders all item names in the stash, loadout, shop, and loot panels with the correct rarity CSS class.
6. Rarity colors meet WCAG AA Large Text (3:1) contrast against the `#0b0f17` background: Gray `#b0b0b0`, Green `#1eff00`, Blue `#0070dd`, Orange `#ff8000`.
7. The existing `EncounterLoot.StartLootEncounter(List<Item>, IEnumerable<Item>)` overload is present and unmodified; all pre-existing call sites that have not yet been migrated continue to compile.
8. Total test count is ≥38 (was 31); all tests deterministic and passing on CI.
9. `dotnet build` produces zero errors and zero new warnings.
10. No `RaidLoop.Core.Tests` files are modified (tests only added).
11. `GameEventLog` emits at minimum `loot.drawn`, `enemy.loadout.generated`, and `extraction.complete` events with item-level rarity fields.
12. An agentic QA run over 3 consecutive simulated raids produces event log evidence showing ≥2 distinct rarity values appearing across loot drops.

---

## 8. Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Existing `Item` call sites broken by record parameter change | Low | Medium | `Rarity = Rarity.Common` default makes it backward-compatible; CI will catch any break immediately |
| CSS contrast fails WCAG check | Low | Low | Colors are pre-verified (AC-6); tooling can recheck |
| Without-replacement produces shorter-than-expected lists | Medium | Low | AC-3 test explicitly covers small-table edge case; `Home.razor` already handles variable-length loot lists |
| Loot table weights feel wrong to player | Medium | Low | Weights are constants in one file; balance tuning is a single-file PR with no API changes |
| GameEventLog introduces thread-safety concerns | Low | Low | Blazor WASM is single-threaded; no synchronization needed |

---

## 9. Effort

**Medium** — estimated 2–3 sessions across three PRs. The Core work (PR-1 and PR-2) is the bulk; Home.razor changes (PR-3) are deliberately small per ADR-013.

---

## 10. Recommendation

**Approve.** Loot Tiers is the next highest-value player-visible feature in the backlog:

- Directly satisfies all five functional requirements and all five acceptance criteria in the PRD.
- Every architecture decision is settled (ADR-008–014 Accepted) — no open design questions remain.
- Implementation is additive and backward-compatible; no save migration, no new NuGet packages, no breaking changes.
- Enemy loadout variety and rarity-colored item names are immediately player-noticeable.
- Structured observability events satisfy the PRD's agentic QA requirement.
- Delivery in three focused PRs keeps each merge low-risk and independently deployable.
- Completing this feature unblocks the Home.razor decomposition (ADR-015), which is a hard sequencing constraint.
