# Architecture Decision Records

**Project**: RaidLoop (extractor-shooter-light)
**Generated**: 2026-03-15
**Status of this document**: Living — append new ADRs as decisions are made; do not edit superseded records (mark them Superseded and add a forward reference).

---

## Index

| ID | Title | Status |
|----|-------|--------|
| [ADR-001](#adr-001) | Separate Core library from Blazor WASM client | Accepted |
| [ADR-002](#adr-002) | Static facades for domain engines | Accepted |
| [ADR-003](#adr-003) | IRng interface for injectable randomness | Accepted |
| [ADR-004](#adr-004) | Sealed records and sealed classes for all domain models | Accepted |
| [ADR-005](#adr-005) | Browser localStorage via JS interop for save persistence | Accepted |
| [ADR-006](#adr-006) | Versioned save keys with forward-migration on load | Accepted |
| [ADR-007](#adr-007) | Monolithic Home.razor deferred — not decomposed at v1.0 | Accepted (revisit at v1.1) |
| [ADR-008](#adr-008) | Rarity as a defaulted property on Item, not a wrapper type | Accepted |
| [ADR-009](#adr-009) | LootTable as an immutable, side-effect-free draw class | Accepted |
| [ADR-010](#adr-010) | LootTables as a static factory — weights co-located, not data-driven | Accepted |
| [ADR-011](#adr-011) | Without-replacement draw semantics within a single encounter | Accepted |
| [ADR-012](#adr-012) | Backward-compatible EncounterLoot overload — legacy API retained | Accepted |
| [ADR-013](#adr-013) | CSS-class rarity colouring — no structural UI changes | Accepted |
| [ADR-014](#adr-014) | No save migration for Rarity — default to Common on deserialize | Accepted |
| [ADR-015](#adr-015) | Decompose Home.razor into five panel components with state-up / events-down | Accepted |
| [ADR-016](#adr-016) | No GameStateService in DI — state owned by orchestrating shell during decomposition | Accepted |
| [ADR-017](#adr-017) | EncounterType promoted to GameEnums.cs in the Client namespace | Accepted |
| [ADR-018](#adr-018) | GameEventLog as an in-process, in-memory, zero-dependency observability log in RaidLoop.Core | Accepted |
| [ADR-019](#adr-019) | Agentic QA validation protocol — event log as the machine-readable evidence interface | Accepted |

---

## ADR-001

### Separate Core library from Blazor WASM client

**Status**: Accepted
**Date**: pre-v1.0

#### Context

The project is a browser game delivered as Blazor WebAssembly. Early prototyping put all game logic in `Home.razor`. This made automated testing impossible (Razor requires a browser host to instantiate) and coupled game rules to the UI framework.

#### Decision

Split the solution into two projects:

- `RaidLoop.Core` — a plain .NET class library with zero external dependencies. Contains all game rules (`RaidEngine`, `CombatBalance`, `EncounterLoot`) and all domain models (`Item`, `GameState`, `RaidState`, `RaidInventory`).
- `RaidLoop.Client` — the Blazor WASM host. References `RaidLoop.Core` but adds no game logic of its own; its only responsibilities are rendering, user-input handling, and save I/O.

`RaidLoop.Core` has no `<PackageReference>` entries. It cannot import Blazor types and therefore cannot be coupled to the browser environment.

#### Consequences

**Positive**

- `RaidLoop.Core` is fully unit-testable with xUnit and a plain `dotnet test` invocation (no browser, no Playwright, no bUnit).
- 31 tests run in < 1 s in CI with no additional infrastructure.
- Core game rules can be reused in a future server-authoritative mode or a console harness without modification.
- Dependency direction is one-way: Client → Core. Core never knows about Blazor.

**Negative / Trade-offs**

- The separation requires discipline: developers must resist placing logic in `Home.razor`. The 1,386-line Home.razor (R1) is evidence that this boundary has eroded under feature pressure on the client side.
- Adding a third hosting layer (e.g. a REST API) would require extracting save logic from `StashStorage` as well.

**Impact on feature work**: Every new game mechanic must be implemented first in `RaidLoop.Core` and exposed via a pure method or model. The Loot Tiers feature (ADR-008 through ADR-014) follows this constraint: `LootTable`, `LootTables`, and `Rarity` all live in Core; `Home.razor` only maps `Rarity` values to CSS class names.

---

## ADR-002

### Static facades for domain engines

**Status**: Accepted
**Date**: pre-v1.0

#### Context

Domain operations such as starting a raid, applying damage, and finalising a raid require no persistent state of their own — they transform `RaidState` and `GameState` objects that are owned by the caller. Two implementation options were considered:

1. Instance services registered in DI (e.g. `IRaidEngine`).
2. Static classes with pure methods (e.g. `RaidEngine.StartRaid(...)`).

#### Decision

Use static classes (`RaidEngine`, `CombatBalance`, `EncounterLoot`). All methods accept their dependencies as parameters; none store instance state.

#### Consequences

**Positive**

- Zero DI ceremony in tests — call `RaidEngine.FinalizeRaid(game, raid, extracted)` directly with no mocking framework.
- Methods are trivially testable as pure functions.
- No allocation of service objects; WASM memory pressure is reduced.

**Negative / Trade-offs**

- Extensibility via interface substitution is not available. If a future feature requires pluggable raid engines (e.g. a tutorial engine with different rules), this becomes an interface extraction refactor.
- `CombatBalance` uses hard-coded lookup tables (string switch expressions). Modifying balance data requires a code change, not a configuration change.

**Impact on feature work**: `LootTable.Draw` and the `LootTables` factory follow the same pattern. `EncounterLoot.StartLootEncounter` remains a static method accepting `LootTable` and `IRng` as explicit parameters, matching the existing API surface.

---

## ADR-003

### IRng interface for injectable randomness

**Status**: Accepted
**Date**: pre-v1.0

#### Context

Combat damage, loot generation, and encounter selection all involve randomness. Using `System.Random` directly in engine methods makes tests non-deterministic: tests would need wide assertion ranges or would be flaky by design.

#### Decision

Define `IRng` in `CombatBalance.cs`:

```csharp
public interface IRng
{
    int Next(int minInclusive, int maxExclusive);
}
```

Production code uses `RandomRng` (wraps `System.Random`). Tests inject a `SequenceRng` that returns a pre-defined sequence of integers.

`IRng` is passed explicitly to every method that needs randomness. No ambient/thread-local random state exists.

#### Consequences

**Positive**

- All 31 existing tests are fully deterministic and run identically on every machine and CI run.
- Edge cases (minimum damage, maximum ammo, zero-weight loot) can be forced by crafting specific sequences.
- Statistical correctness of the loot table (NFR-3) can be tested by running a seeded `SequenceRng` over 10,000 iterations.

**Negative / Trade-offs**

- Every randomness-consuming method signature gains an `IRng rng` parameter. This is noise-free in practice (the pattern is already established) but it is a leaky abstraction — callers must always supply the RNG instance.
- `IRng` exposes only `Next(int, int)`. If a future feature requires `NextDouble()` (e.g. percentage-based loot modifiers), the interface will need extending or a second interface will be required.

**Impact on feature work**: `LootTable.Draw(IRng rng, int count)` is parameterised by `IRng` in accordance with this decision. The `LootTableTests.cs` test file uses a deterministic stub to verify weight distribution (AC-4), without-replacement semantics (AC-3), and edge cases.

---

## ADR-004

### Sealed records and sealed classes for all domain models

**Status**: Accepted
**Date**: pre-v1.0

#### Context

Domain models (`Item`, `GameState`, `RaidState`, `RaidInventory`, `GameSave`) need to be safe across the codebase without defensive copies. Three options:

1. Open classes — full mutability, full inheritance.
2. `record` types — value equality, `with` expressions, open to inheritance.
3. `sealed record` / `sealed class` — value equality where appropriate, no inheritance.

#### Decision

- **Value objects** (`Item`, `OnPersonEntry`, `RandomCharacterState`, `GameSave`, `DamageRange`): `sealed record`. Value equality is correct; copying with `with` is the update path.
- **Aggregates** (`GameState`, `RaidState`, `RaidInventory`): `sealed class`. They hold mutable `List<T>` collections and are identity-based, not value-based.

No domain type is left `open` (un-sealed).

#### Consequences

**Positive**

- Compiler enforces the model surface; no accidental subclassing in the Client layer.
- `sealed record Item` gains structural equality for free — two items with the same name, type, slots, and rarity are equal. This simplifies assertions in tests.
- JSON serialization with `System.Text.Json` works correctly with sealed records (no polymorphic type discriminator needed).

**Negative / Trade-offs**

- Adding a property to a `sealed record` is a binary-breaking change. Callers using positional construction must be updated. (Mitigated by providing defaults — see ADR-008.)

**Impact on feature work**: `Rarity` is added to the `Item` record as a defaulted positional parameter (`Rarity Rarity = Rarity.Common`). All 80+ existing `new Item(...)` call sites in production code and tests compile without modification because the parameter is last and has a default.

---

## ADR-005

### Browser localStorage via JS interop for save persistence

**Status**: Accepted
**Date**: pre-v1.0

#### Context

RaidLoop is a pure client-side Blazor WASM application with no server component. Persistence options available without a backend:

1. `localStorage` via JS interop.
2. `IndexedDB` via JS interop (async, larger capacity).
3. URL-encoded state (no persistence across sessions).
4. File download / upload (manual, no auto-save).

#### Decision

Use `localStorage` through a thin JS bridge (`wwwroot/js/storage.js`):

```javascript
window.raidLoopStorage = {
  load: function (key) { return window.localStorage.getItem(key); },
  save: function (key, value) { window.localStorage.setItem(key, value); }
};
```

`StashStorage` serializes `GameSave` to JSON via `System.Text.Json` and calls these functions via `IJSRuntime`.

#### Consequences

**Positive**

- Minimal infrastructure. The JS bridge is 4 lines; `StashStorage` is self-contained.
- No server required — the game runs fully offline (PWA-capable).
- Synchronous read-on-init is possible because `localStorage` is synchronous in the browser.

**Negative / Trade-offs**

- Save data is browser-specific and origin-bound. Players cannot share saves across devices.
- `localStorage` is limited to ~5 MB. The current save payload (items as JSON) is well within this limit, but a large stash cap increase could approach it (NFR-5 constrains item payload growth to ≤20 bytes per item for the Rarity feature).
- `localStorage` is cleared when users clear site data. No cloud backup fallback exists.

**Impact on feature work**: ADR-008 (Rarity on Item) adds at most ~12 bytes per item to the serialized payload (the string `"Legendary"` plus JSON punctuation). NFR-5 requires this stays under 20 bytes — no structural change to the save schema is needed.

---

## ADR-006

### Versioned save keys with forward-migration on load

**Status**: Accepted
**Date**: pre-v1.0

#### Context

The game save format has evolved:

- **v1**: Stash list only.
- **v2**: Added `RandomCharacterAvailableAt`, `RandomCharacter`, `Money`.
- **v3** (current): Added `OnPersonItems` (replaces `CharacterInventory`).

Each format change risks silently corrupting existing player saves if not handled.

#### Decision

- The active save key is `"raidloop.save.v3"`.
- On load, `StashStorage` attempts to deserialize `v3`. If deserialization fails or returns null, it falls back to loading earlier key formats (`v2`, `v1`) and migrating the data forward.
- Item names are normalized on load via `CombatBalance.NormalizeItemName` to absorb renames.
- If all formats fail, a default save (starter kit loadout) is returned silently.

#### Consequences

**Positive**

- Players are never presented with a blank stash due to a format change.
- The migration path is explicit and auditable in `StashStorage.LoadAsync`.

**Negative / Trade-offs**

- Silent failure on corrupt data (R3 from ProjectSnapshot). A corrupted v3 save that partially deserializes may return an incomplete stash with no user-visible warning.
- Each format version adds a migration branch that must be maintained indefinitely.

**Impact on feature work**: ADR-014 explicitly decides **not** to create a v4 save key for the Rarity feature. `Rarity` defaults to `Common` on a missing JSON field, so v3 saves load cleanly with no migration branch needed. This keeps the migration surface flat.

---

## ADR-007

### Monolithic Home.razor deferred — not decomposed at v1.0

**Status**: Accepted (revisit at v1.1)
**Date**: pre-v1.0

#### Context

`Home.razor` is 1,386 lines and contains the entire game UI: raid setup, combat encounter, loot encounter, extraction, shop, and stash panels. This was identified as risk R1 in ProjectSnapshot.md. Decomposition into sub-components (`CombatPanel.razor`, `LootPanel.razor`, etc.) was evaluated for v1.0.

#### Decision

Defer decomposition. Ship v1.0 with the monolith.

Rationale:
- Decomposition is a pure refactor with no user-visible benefit. It carries risk (broken event bindings, cascading parameter wiring, render cycle regressions) without feature upside.
- The current test suite has zero bUnit coverage of the client layer (R2), so decomposition would happen without a safety net.
- The team capacity available for v1.0 was better spent on game mechanics.

#### Consequences

**Positive**

- v1.0 shipped without UI regression risk from component extraction.

**Negative / Trade-offs**

- Every UI feature that touches multiple panels must be implemented in `Home.razor`, making PRs harder to review.
- Rarity colour coding (ADR-013) adds ~10 more lines to an already large file.
- Without bUnit tests, correctness of UI logic must be verified manually.

**Mitigation path**: Decompose `Home.razor` into five panel components with clean parameter/callback contracts before adding any further UI-significant features. The full decomposition design is captured in ADR-015 and ADR-016, and the acceptance criteria are codified in FeatureSpec.md.

---

## ADR-008

### Rarity as a defaulted property on Item, not a wrapper type

**Status**: Accepted
**Date**: 2026-03-15 (Loot Tiers feature)

#### Context

The Loot Tiers feature requires every item to carry a rarity classification. Three design options were considered:

1. **Enum property on Item** with a default: `Rarity Rarity = Rarity.Common`.
2. **Wrapper type** `RarityItem` that contains an `Item` plus `Rarity`.
3. **Separate dictionary** `Dictionary<Item, Rarity>` maintained alongside item lists.

#### Decision

Option 1: add `Rarity` as a defaulted positional parameter on the `Item` record.

```csharp
public enum Rarity { Common, Uncommon, Rare, Legendary }
public sealed record Item(string Name, ItemType Type, int Slots = 1, Rarity Rarity = Rarity.Common);
```

#### Consequences

**Positive**

- All ~80 existing `new Item(...)` call sites compile without modification (default value is last and optional).
- `Rarity` travels with the item through every collection, save, and deserialization path automatically — no parallel data structure to keep in sync.
- STJ deserializes v3 saves that lack a `rarity` field by applying the CLR default (`Common = 0`), so save compatibility (BR-6, ADR-014) is free.
- Value equality on `Item` automatically incorporates `Rarity` — two items that differ only in rarity are not equal.

**Negative / Trade-offs**

- `Item` is now a larger struct in memory (one additional `int`-backed enum field). Negligible for a browser game.
- `Rarity` is baked into the identity of an `Item`. If the design ever requires the same item to exist at different rarities simultaneously (e.g., a `Common Makarov` and a `Rare Makarov` in the same stash), they are distinct instances with different equality — which is the correct behaviour for the extraction genre but must be understood by implementors.

**Impact on other components**: `CombatBalance.GetBuyPrice`, `CombatBalance.GetDamageRange`, and similar methods match on `item.Name`; they are unaffected by the new field.

---

## ADR-009

### LootTable as an immutable, side-effect-free draw class

**Status**: Accepted
**Date**: 2026-03-15 (Loot Tiers feature)

#### Context

`LootTable` needs to draw items probabilistically from a weighted pool. Two state models were considered:

1. **Mutable table**: `Draw` removes selected entries from internal state. Callers get a fresh table per encounter.
2. **Immutable table**: `Draw` works from a local copy of the pool. The same table instance can be used for many encounters.

#### Decision

Immutable table. `Draw(IRng rng, int count)` copies the entry array into a local working list, performs without-replacement selection on the copy, and returns a new `List<Item>`. The `LootTable` instance is never mutated.

```csharp
public sealed class LootTable
{
    private readonly (Item Item, int Weight)[] _entries;
    private readonly int _totalWeight;

    public LootTable(IEnumerable<(Item Item, int Weight)> entries) { ... }
    public List<Item> Draw(IRng rng, int count) { /* pure, no mutation of _entries */ }
}
```

#### Consequences

**Positive**

- `LootTables.WeaponsCrate()` can be cached as a static field if needed; multiple simultaneous encounters (if the game ever supports co-op) would not corrupt each other.
- Tests can call `Draw` multiple times on the same instance and get independent results with the same seed (FR-2.6).
- No object churn from re-constructing tables every encounter.

**Negative / Trade-offs**

- `Draw` must allocate a working copy of `_entries` on each call (O(n) allocation). For tables of ≤50 entries this is negligible.
- The without-replacement invariant (FR-2.4) requires the working copy to be modified during `Draw`; if the copy is large, GC pressure increases slightly. Acceptable for the current table sizes.

**Impact on testing**: FR-2.6 and AC-3 are directly enabled by this decision — the same seeded `SequenceRng` + same `LootTable` instance produces identical draw results on each invocation, making the statistical test in AC-4 reproducible.

---

## ADR-010

### LootTables as a static factory — weights co-located, not data-driven

**Status**: Accepted
**Date**: 2026-03-15 (Loot Tiers feature)

#### Context

Container-specific loot pools need to be defined somewhere. Two options:

1. **Static factory methods** in `LootTables.cs` with weights as integer constants in the same file.
2. **Data-driven configuration** (JSON file, embedded resource, or `appsettings`-style class) loaded at runtime.

#### Decision

Option 1: static factory class with inline integer constants.

```csharp
public static class LootTables
{
    private const int WCommon    = 40;
    private const int WUncommon  = 12;
    private const int WRare      = 6;
    private const int WLegendary = 2;

    public static LootTable WeaponsCrate() => new([
        (new Item("Rusty Knife",  ItemType.Weapon,     1, Rarity.Common),    WCommon),
        (new Item("Makarov",      ItemType.Weapon,     1, Rarity.Common),    20),
        (new Item("PPSH",         ItemType.Weapon,     1, Rarity.Uncommon),  WUncommon),
        (new Item("AK74",         ItemType.Weapon,     2, Rarity.Rare),      WRare),
        (new Item("AK47",         ItemType.Weapon,     2, Rarity.Legendary), WLegendary),
        (new Item("Ammo Box",     ItemType.Consumable, 1, Rarity.Common),    30),
    ]);
    // ...
}
```

Weights are defined as named constants local to `LootTables.cs`. They do not appear in `LootTable.cs` or anywhere else (FR-3.4).

#### Consequences

**Positive**

- Zero runtime I/O. No JSON parsing on startup; no error path for a missing asset file.
- Designer iteration is one code file (`LootTables.cs`) with a clear weight-to-probability mental model.
- Weights are visible at code review; balance changes are auditable in git history.
- Fully compatible with Blazor WASM's AOT and tree-shaking — no reflection-based config loading.

**Negative / Trade-offs**

- Changing weights requires a code change, compile, and redeploy. There is no live-rebalancing path.
- As the number of container types grows, `LootTables.cs` will accumulate. This is acceptable at the current scale (4 tables, ~50 entries total) but should be reviewed if the table count exceeds ~10.

**Impact on other components**: `Home.razor` calls `LootTables.WeaponsCrate()` etc. directly. No DI registration is required. The static call pattern matches the existing use of `CombatBalance` static methods.

---

## ADR-011

### Without-replacement draw semantics within a single encounter

**Status**: Accepted
**Date**: 2026-03-15 (Loot Tiers feature)

#### Context

When drawing multiple items from a loot container in one encounter (e.g. 3 items from a `WeaponsCrate`), two draw models are available:

1. **With replacement**: each draw is independent; the same item can appear multiple times.
2. **Without replacement**: each drawn item is removed from the pool for subsequent draws in the same call.

#### Decision

Without-replacement within a single `Draw(rng, count)` call (FR-2.4). Between separate calls (i.e. separate encounters), the full table is available again.

#### Consequences

**Positive**

- A single container cannot yield three copies of the same legendary weapon. This preserves game balance without requiring explicit duplicate-prevention logic in `Home.razor`.
- Consistent with player expectations from the extraction-shooter genre.
- AC-3 (no duplicates in a single draw) is directly enforced by the algorithm.

**Negative / Trade-offs**

- For very small tables (fewer items than `drawCount`), `Draw` returns all remaining items and stops. This is the correct behaviour (FR-2.5) and `Home.razor` must handle a result list smaller than the requested count.
- Statistical weight distribution is slightly altered by without-replacement: after drawing a high-probability item, its weight is removed for subsequent draws in the same call, marginally boosting the relative probability of remaining items. At table sizes of ≥20 entries this effect is negligible and within the NFR-3 ±2% tolerance band.

---

## ADR-012

### Backward-compatible EncounterLoot overload — legacy API retained

**Status**: Accepted
**Date**: 2026-03-15 (Loot Tiers feature)

#### Context

`EncounterLoot.StartLootEncounter(List<Item> discoveredLoot, IEnumerable<Item> items)` is the existing API. `Home.razor` has multiple call sites that pass hand-crafted item lists. Migrating all call sites in the same PR as the Core feature would increase PR scope and the Home.razor blast radius.

#### Decision

Add a new overload accepting `LootTable` and `IRng` (FR-4.1); leave the existing overload unchanged (FR-4.3, BR-7).

```csharp
// Existing — unchanged
public static void StartLootEncounter(List<Item> discoveredLoot, IEnumerable<Item> items) { ... }

// New
public static void StartLootEncounter(List<Item> discoveredLoot, LootTable table, IRng rng, int drawCount = 3)
{
    discoveredLoot.Clear();
    discoveredLoot.AddRange(table.Draw(rng, drawCount));
}
```

`Home.razor` call sites are migrated to the new overload as a follow-up, not as part of this PR.

#### Consequences

**Positive**

- The Core feature ships and is testable without requiring simultaneous changes across the large `Home.razor` file.
- Reduces risk of UI regressions in the same PR as the Core logic change.
- The feature spec (BR-7) and test count floor (≥38 tests) are met independently of UI migration timing.

**Negative / Trade-offs**

- `EncounterLoot` temporarily has two APIs for the same operation. This is technical debt that must be resolved before the monolith decomposition (ADR-007 mitigation path) begins, to avoid carrying the ambiguity into extracted components.
- Developers new to the codebase may use the legacy overload by accident. A `[Obsolete]` attribute on the old overload should be added once all call sites are migrated.

---

## ADR-013

### CSS-class rarity colouring — no structural UI changes

**Status**: Accepted
**Date**: 2026-03-15 (Loot Tiers feature)

#### Context

Rarity must be visible to the player (BR-5). Options:

1. **CSS class on item name spans** — add `class="rarity-@item.Rarity.ToString().ToLower()"` to existing `<span>` elements.
2. **Inline style** — emit `style="color: #1eff00"` directly.
3. **New `RarityBadge` component** — a reusable component that renders a coloured name.
4. **Rarity icon/badge** — add a visual indicator alongside the name.

#### Decision

Option 1: four CSS classes in `app.css` (or a `<style>` block in `Home.razor`) applied to existing `<span>` elements. No new components, no inline hex (NFR-6), no layout changes (FR-5.5).

```css
.rarity-common    { color: #b0b0b0; }
.rarity-uncommon  { color: #1eff00; }
.rarity-rare      { color: #0070dd; }
.rarity-legendary { color: #ff8000; }
```

Razor usage: `<span class="rarity-@item.Rarity.ToString().ToLower()">@item.Name</span>`

#### Consequences

**Positive**

- Minimal diff to `Home.razor` (~10 lines) — does not worsen the monolith risk (R1).
- CSS classes are reusable: when `Home.razor` is eventually decomposed, each sub-component inherits the classes from the shared stylesheet.
- Colours meet WCAG AA Large Text 3:1 contrast against the `#0b0f17` dark background (NFR-2).
- No inline hex in Razor markup (NFR-6) — the mapping from `Rarity` value to presentation lives entirely in CSS.

**Negative / Trade-offs**

- `Rarity.ToString().ToLower()` in Razor is a minor allocation per render cycle. For a game with ~30 stash items and ~5 loot items, this is inconsequential.
- The `RarityBadge` component path (Option 3) would be cleaner after Home.razor decomposition. This decision assumes decomposition has not yet started (ADR-007 still in effect). If decomposition begins before rarity colouring is implemented, the `RarityBadge` option should be reconsidered.

---

## ADR-014

### No save migration for Rarity — default to Common on deserialize

**Status**: Accepted
**Date**: 2026-03-15 (Loot Tiers feature)

#### Context

Existing player saves (v3 format) do not contain a `rarity` field on `Item` entries. Three options for handling this:

1. **New save key `v4`** with a migration branch in `StashStorage` that assigns `Common` to all items.
2. **In-place default**: rely on `System.Text.Json` to apply the C# default value (`Rarity.Common`) when deserializing an object with a missing field.
3. **Explicit migration logic**: on save load, iterate all items and set `Rarity = Common` if null.

#### Decision

Option 2: rely on STJ's missing-field behaviour. When a v3 save is loaded, each `Item` that lacks a `rarity` JSON key will be deserialized with `Rarity = Common` (the `enum` default and the C# default specified on the record parameter). No migration code, no new save key.

#### Consequences

**Positive**

- Zero migration code to write, test, or maintain.
- No new save key means no risk of players losing their v3 save during the transition.
- The save format remains at `v3` — ADR-006's migration stack does not grow.
- BR-6 (save compatibility) and AC-2 (items from old saves are `Common`) are satisfied automatically.

**Negative / Trade-offs**

- This approach depends on STJ's default-on-missing-field behaviour being stable. It is a documented and stable behaviour of `System.Text.Json` since .NET 5 and is not expected to change.
- If a future feature adds a field that cannot be safely defaulted (e.g. a non-nullable reference type with no meaningful default), a migration will be required at that time. The pattern set here should not be blindly followed for such cases.
- Items acquired before the feature ships will display as `Common` in rarity colour, even if a designer later decides they should be `Uncommon`. This is acceptable: rarity is a forward-looking property applied at loot-draw time, not retroactively to existing stash items.

**Impact on save payload size**: Adding `"rarity":"Common"` to every item costs 16 bytes per item in the worst case. For a 30-item stash this is ~480 bytes — well within the `localStorage` limit and within the NFR-5 cap of ≤20 bytes per item.

---

## ADR-015

### Decompose Home.razor into five panel components with state-up / events-down

**Status**: Accepted
**Date**: 2026-03-15 (Home.razor Decomposition — O1 / R1 mitigation)

#### Context

`Home.razor` is ~1,400 lines and contains the full game UI across five distinct phases: stash management, loadout configuration, shop, pre-raid preparation, and the in-raid HUD. Risk R1 (ProjectSnapshot.md) classifies this as HIGH severity: merge conflicts are inevitable on every UI feature branch, bUnit tests are blocked, and cognitive overhead increases with every added mechanic. FeatureSpec.md documents this decomposition as the next prioritised work item.

#### Decision

Extract five focused Blazor components from `Home.razor`:

| Component | Responsibility |
|-----------|----------------|
| `StashPanel.razor` | Display and manage the persistent stash (sell, inspect) |
| `LoadoutPanel.razor` | Assign stash items to the "For Raid" loadout slots |
| `ShopPanel.razor` | Buy items and manage money |
| `PreRaidPanel.razor` | Final confirmation, random character toggle, launch raid |
| `RaidHUD.razor` | All in-raid UI: combat, loot encounters, extraction, inventory |

After decomposition, `Home.razor` becomes an orchestration shell of ≤150 lines. It:
- Owns all top-level state fields (the single source of truth).
- Passes read-only data down as `[Parameter]` values.
- Receives mutations back as `EventCallback` / `EventCallback<T>` callbacks.
- Calls `StashStorage.SaveAsync` after any state-mutating callback.

State flows in one direction only: downward through parameters. No child component calls `StateHasChanged` on its parent; it invokes a callback and the shell re-renders.

#### Consequences

**Positive**

- Each component is independently understandable, reviewable, and testable.
- bUnit tests can mount `RaidHUD` in isolation by supplying a `RaidState` parameter and an `EventCallback` mock — no 1,400-line rendering required (R2 mitigation).
- PRs for future features only touch the components they affect; cross-panel merge conflicts disappear.
- Rarity CSS classes (ADR-013) and future UI features land in the correct component with no secondary structural pass needed (AC-8 of FeatureSpec.md).

**Negative / Trade-offs**

- Pure refactor with no user-visible benefit. Carries risk of broken event bindings or render cycle regressions if the parameter and callback contracts are wired incorrectly.
- The absence of a bUnit test suite during the refactor (R2) means the safety net is manual testing of the complete game loop after each extracted component.
- Extraction must be done in PR-order (StashPanel → LoadoutPanel → ShopPanel → PreRaidPanel → RaidHUD) to keep the game in a working state throughout. Violating the order puts multiple game phases in an untested intermediate state simultaneously.

**Prerequisite ordering**: Loot Tiers (O2) must land *before* this refactor ships, or the two workstreams must coordinate on AC-8 compliance. ADR-015 does not block Loot Tiers but Loot Tiers must not be merged to a partially-decomposed `Home.razor`.

**Impact on feature work**

| Item | Impact |
|------|--------|
| R2 bUnit suite | **Directly unblocked.** Each panel component has a defined parameter surface that bUnit can exercise. |
| O2 Loot Tiers | **Ordering constraint.** Loot Tiers should merge before or after decomposition, not during it. |
| O3 Procedural Encounters | New encounter types will be added to `RaidHUD` only — ~200-line scope instead of ~1,400. |
| O5 Character Progression | Progression stats would surface in `StashPanel` and `PreRaidPanel`; clear component ownership makes this addition straightforward. |
| ADR-012 legacy overload | The legacy `EncounterLoot` overload must be migrated away from before decomposition begins — `RaidHUD` should use the `LootTable`-based overload exclusively. |

---

## ADR-016

### No GameStateService in DI — state owned by orchestrating shell during decomposition

**Status**: Accepted
**Date**: 2026-03-15 (Home.razor Decomposition — O1 / R1 mitigation)

#### Context

When `Home.razor` is split into sub-components (ADR-015), the ~50 state fields it currently owns must live somewhere. Two options:

1. **Keep state in the shell** (`Home.razor` ≤150 lines): shell owns all fields, passes them down as parameters.
2. **Extract a `GameStateService`** registered in DI: components inject the service and read/write state through it.

#### Decision

Option 1: state stays in the orchestrating shell for the scope of the O1 decomposition work. No new DI service is introduced.

Rationale:
- A `GameStateService` introduces a shared mutable object reachable from any component. Without careful lifecycle design, this opens the door to out-of-order state updates and cascading re-renders that are harder to debug than a simple callback chain.
- The decomposition is a structural refactor with zero behaviour change. Introducing a new architectural layer (a DI service) simultaneously increases risk without adding user-visible value.
- The unidirectional data flow (parameters down, callbacks up) enforced by Option 1 is idiomatic Blazor and directly compatible with the bUnit testing model — a component's behaviour is fully described by its parameters.

#### Consequences

**Positive**

- Each component is a pure function of its parameters; no hidden shared mutable state.
- bUnit tests are straightforward: supply parameters, invoke callbacks, assert re-render.
- The shell is the authoritative single source of truth — no synchronization logic between a DI service and component parameters.

**Negative / Trade-offs**

- Parameter lists on deeply nested components can grow (parameter drilling). For the five flat panels proposed in ADR-015 this is manageable; a sixth layer of nesting would make this decision worth revisiting.
- If a second page ever needs access to game state (e.g. a statistics screen), it would need to duplicate the state or the decision would need to be reversed in favour of a DI service. This is the explicit future decision point (TBD-A from the pre-existing open decisions list).

**Supersedes TBD-A**: The open question "should a `GameStateService` be introduced in DI?" is now resolved for the O1 decomposition scope. It remains open for the scenario where a second page is introduced.

**Impact on feature work**

| Item | Impact |
|------|--------|
| R2 bUnit tests | **Positive.** Pure parameter-driven components are the easiest target for bUnit. `[Parameter]` inputs and `EventCallback` mocks are all that is needed. |
| O5 Character Progression | If progression requires a persistent service (e.g. XP that persists across raids independently of the stash), revisit TBD-A at that time. |
| O6 Cloud Save Sync | Unaffected. Sync logic belongs in an infrastructure service alongside `StashStorage`, not in a game-state service. |

---

## ADR-017

### EncounterType promoted to GameEnums.cs in the Client namespace

**Status**: Accepted
**Date**: 2026-03-15 (Home.razor Decomposition — v1.2)

#### Context

`EncounterType` is currently defined as a `private enum` inside `Home.razor`'s `@code` block. When `RaidHUD` is extracted as a sub-component (ADR-015), it must accept `EncounterType` as a `[Parameter]`. Blazor does not allow a component parameter type to be `private` — the type must be visible to both the shell and the component file. Three resolution paths were considered:

1. **Move `EncounterType` to `RaidLoop.Core`**: universally accessible across both projects.
2. **Promote to a namespace-level type in `RaidLoop.Client`** (e.g. a new `GameEnums.cs` file).
3. **Replace with `string` or `int`** in the `RaidHUD` parameter contract.

#### Decision

Option 2: promote `EncounterType` to a non-private, namespace-level type in `src/RaidLoop.Client/`. The canonical host is a new file `GameEnums.cs` (or equivalent) in the `RaidLoop.Client` namespace. The enum does **not** move to `RaidLoop.Core`.

Rationale for not moving to Core:
- `EncounterType` values (`Combat`, `Loot`, `Neutral`, `Extraction`, `None`) describe the current rendering-phase of the UI, not a domain concept used in game logic such as damage calculation or loot generation.
- Placing a UI-phase indicator in Core would violate the principle in ADR-001 that Core has zero presentation concerns.
- NFR-8 of FeatureSpec.md prohibits all Core modifications during the v1.2 decomposition feature.

Rationale for not using `string`/`int`:
- Stringly-typed parameters sacrifice compile-time safety; integer parameters lose self-documentation. The enum preserves both and allows exhaustive pattern-matching in `RaidHUD`.

#### Consequences

**Positive**

- `RaidHUD` can declare `[Parameter] public EncounterType EncounterType { get; set; }` without indirection.
- Both `Home.razor` (shell) and `RaidHUD` compile against the same type reference from the same namespace.
- The Core/Client boundary established by ADR-001 is respected: the new file is in `RaidLoop.Client`, not `RaidLoop.Core`.
- `GameEnums.cs` provides a canonical home for any future UI-specific enum promotions, preventing future private-enum-in-page-code patterns.

**Negative / Trade-offs**

- One additional file is introduced to the client project. The trade-off is minimal: the file has a single purpose and is a natural place for future UI-phase enums.
- If `EncounterType` ever acquires game-logic significance (e.g. Core methods branch on encounter type), the decision to keep it out of Core must be revisited.

**Impact on feature work**: AC-14 of FeatureSpec.md directly verifies this decision — `EncounterType` must not be `private` to `Home.razor` before PR-5 (`RaidHUD` extraction) is opened.

---

## ADR-018

### GameEventLog as an in-process, in-memory, zero-dependency observability log in RaidLoop.Core

**Status**: Accepted
**Date**: 2026-03-15 (Loot Tiers — v1.1 observability contract)

#### Context

The Loot Tiers feature adds player-visible behaviour (weighted loot draws, enemy loadout variance, rarity coloring) that must be verifiable without browser automation. The PRD §Observability Requirements states that agentic QA must be able to inspect meaningful in-game behaviour through emitted evidence rather than through UI scraping.

Four candidate approaches were evaluated:

| Approach | Verdict | Reason rejected |
|----------|---------|-----------------|
| `Console.WriteLine` / `console.log` | Rejected | Browser console output is not addressable by in-process test assertions. Agents cannot read it without headless browser infrastructure. |
| NuGet structured logging (`Microsoft.Extensions.Logging`, `Serilog`) | Rejected | Violates ADR-001's zero-external-dependency invariant for `RaidLoop.Core`. Sinks are not queryable from tests without additional test infrastructure. |
| File-system write (JSON lines) | Rejected | Blazor WASM runs in the browser sandbox with no file system access. Even in test hosts, file I/O introduces process-level side effects and test-ordering dependencies. |
| JS interop + `localStorage` write | Rejected | Would couple `RaidLoop.Core` to the browser host and require `IJSRuntime`, violating ADR-001. Core must remain framework-agnostic. |
| **In-memory static `List<GameEvent>` with typed record schema** | **Accepted** | Zero dependencies. Readable by any .NET test assertion without a browser or sink. Cleared per-raid so events stay scoped and queryable. Blazor WASM is single-threaded so no lock is needed. |

#### Decision

A new static class `GameEventLog` is placed in `src/RaidLoop.Core/`. It maintains a private `List<GameEvent>` exposed as `IReadOnlyList<GameEvent>` via a static property. It provides `Append(GameEvent)` and `Clear()` methods. The type is self-contained — no NuGet references, no framework imports beyond `System` and `System.Collections.Generic`.

**Observability contract — event schema:**

Every `GameEvent` is a sealed record with the following fields:

```csharp
public sealed record GameEvent(
    string EventName,       // dot-notation name (e.g. "loot.drawn")
    string RaidId,          // correlation ID for the current raid session
    IReadOnlyList<ItemSnapshot> Items,  // zero or more item witnesses
    DateTimeOffset Timestamp);          // UTC wall clock at emission

public sealed record ItemSnapshot(
    string Name,            // item display name
    string Category,        // ItemType.ToString() — "Weapon", "Armour", etc.
    string Rarity);         // Rarity.ToString() — "Common", "Uncommon", "Rare", "Legendary"
```

`ItemSnapshot` uses `string` fields for `Category` and `Rarity` rather than the enum types themselves. This decouples the log schema from enum evolution: a consumer reading the evidence bundle does not need to import `RaidLoop.Core` types to interpret the record.

**Canonical event names and emission sites:**

| Event name | Emission site | Items payload |
|------------|---------------|---------------|
| `loot.drawn` | `LootTable.Draw` — after draw completes | All drawn items with name, category, rarity |
| `enemy.loadout.generated` | Combat encounter initialisation in `Home.razor` | All enemy items with name, category, rarity |
| `player.equip` | Player equip handler in `Home.razor` | The equipped item |
| `loot.acquired` | Player takes loot item in `Home.razor` | The looted item |
| `extraction.complete` | Extraction handler in `Home.razor` | All retained items |

**Lifecycle rule:** `GameEventLog.Clear()` is called at the start of each new raid. The log is not cleared at any other time. This ensures that after any raid completes, the full event sequence for that raid is readable without temporal interference from a concurrent session. (Concurrency is impossible in Blazor WASM single-thread model, but the rule is stated explicitly for clarity and portability.)

**Placement in Core, not Client:** Although events are emitted from both `LootTable.Draw` (Core) and `Home.razor` handler methods (Client), the log class itself lives in Core. This means the deterministic Core tests (xUnit, no browser) can assert on events emitted by `LootTable.Draw` without any Client dependency. Home.razor events are verified in integration paths. The placement does not violate ADR-001 — `GameEventLog` has no Blazor imports.

#### Consequences

**Positive**

- `LootTable.Draw`, `EncounterLoot.StartLootEncounter`, and related Core methods can emit events that xUnit tests can assert on directly, with no test doubles.
- Agentic QA can inspect `GameEventLog.Events` after a scenario without browser startup, DOM queries, or screenshot diffing.
- The typed record schema is machine-readable: an agent can filter `Events` by `EventName`, group by `RaidId`, count distinct `Rarity` values, and verify AC-26 and AC-27 with simple LINQ.
- `ItemSnapshot` string fields are forward-compatible: if the `Rarity` enum gains a new member, evidence bundles from before the change remain parseable.

**Negative / Trade-offs**

- Events emitted from `Home.razor` are only observable in a live browser session (or via future bUnit tests once ADR-015 ships). The log is not a substitute for end-to-end testing; it is a complement that eliminates the need for E2E tests for the observability acceptance criteria specifically.
- If `RaidLoop.Core` is ever used in a multi-threaded context (console harness, server-side), `GameEventLog` would require synchronization. The current decision is valid for the single-threaded WASM target and any sequential test runner.
- The static log accumulates until `Clear()` is called. Tests that do not call `Clear()` at setup may observe stale events. This is mitigated by making `Clear()` the first statement in the "start new raid" handler and documenting the requirement for test setup.

**Impact on feature work**

| Item | Impact |
|------|--------|
| AC-15 through AC-19 (GameEventLog ACs in FeatureSpec.md) | These ACs are directly satisfied by this decision. No alternative log infrastructure is needed. |
| AC-26, AC-27 (Agentic QA evidence) | An agent scenario can load `GameEventLog.Events` post-raid and assert on it without a browser. ADR-019 defines the validation protocol. |
| R2 — No client-side tests | Home.razor-emitted events (`player.equip`, `loot.acquired`, `extraction.complete`) require either a live session or bUnit tests. This partially mitigates R2 for the observability surface; full R2 remediation depends on ADR-015. |

---

## ADR-019

### Agentic QA validation protocol — event log as the machine-readable evidence interface

**Status**: Accepted
**Date**: 2026-03-15 (Loot Tiers — v1.1 agentic QA design)

#### Context

The PRD §Observability Requirements specifies that "agentic QA can verify variety via emitted evidence" without requiring browser automation. FeatureSpec.md AC-26 and AC-27 are the canonical acceptance criteria: after 3 consecutive full game loops, `GameEventLog.Events` must contain events from ≥2 distinct rarity values. These criteria must be verifiable by an agent in CI without a running browser, without Playwright, and without screenshot comparison.

The question this ADR resolves is: *how exactly does an autonomous agent validate the Loot Tiers feature through event logs?* Three sub-questions arise:

1. What constitutes a "scenario" — what code does the agent drive?
2. What is the structure of an "evidence bundle" — what snapshot does the agent capture?
3. What are the pass/fail rules the agent applies to the evidence bundle?

#### Decision

**1. Scenario execution model**

An agentic QA scenario is a deterministic xUnit test method (or group of methods) that:
- Instantiates `RaidEngine` with a known `IRng` seed via `SequenceRng`.
- Calls `GameEventLog.Clear()` at the start of each raid.
- Drives the `RaidEngine` state machine through all phases: loadout selection → raid start → encounter resolution (loot, combat, neutral) → extraction.
- Does **not** instantiate Blazor components or a browser host. State transitions are exercised via the same `RaidEngine` public methods that `Home.razor` calls.
- After each raid, captures `GameEventLog.Events.ToList()` as the evidence snapshot for that raid.

The three-raid simulation required by AC-26/AC-27 is a single xUnit fact (not a theory): three sequential raid loops, each fully driven through `RaidEngine`, with `Clear()` called between raids.

**2. State transition coverage**

The following `RaidEngine` state transitions must appear in a scenario covering AC-26/AC-27:

| Transition | Verified by | Evidence |
|------------|-------------|----------|
| `Preparing → InRaid` | Raid start call | `enemy.loadout.generated` event appears |
| `InRaid (Loot encounter)` | Loot encounter step | `loot.drawn` event appears |
| `InRaid (Combat encounter)` | Combat encounter step | `enemy.loadout.generated` event appears |
| `InRaid → Extracting` | Extract call | State field on `RaidState` transitions |
| `Extracting → Idle (success)` | Extraction resolution | `extraction.complete` event appears |

These transitions are the observable "seams" that the scenario drives. Each transition produces or is correlated with at least one event in `GameEventLog`.

**3. Evidence bundle definition**

An **evidence bundle** is a `List<GameEvent>` snapshot taken immediately after `extraction.complete` is appended for a given raid. For the three-raid AC-26/AC-27 scenario, three bundles are captured — one per raid. They are concatenated into a **session evidence log** for cross-raid assertions.

An evidence bundle is valid if it satisfies all of the following predicates:

| Predicate | Field(s) checked | Failure message |
|-----------|------------------|-----------------|
| Contains `loot.drawn` | `EventName` | Missing loot.drawn event — loot encounter not driven |
| Contains `enemy.loadout.generated` | `EventName` | Missing enemy.loadout.generated event — combat not driven |
| Contains `extraction.complete` | `EventName` | Missing extraction.complete event — extraction not resolved |
| All events carry a non-empty `RaidId` | `RaidId` | RaidId not set — correlation identifier missing |
| All `ItemSnapshot.Rarity` values are non-null and non-empty | `Items[*].Rarity` | Rarity not emitted — event incomplete |
| `extraction.complete` Items match retained items | Cross-check with `RaidState.Inventory` | Extraction item list does not match engine state |

The **cross-raid assertion** (AC-27) is applied to the concatenated session evidence log:

```
DISTINCT(session_evidence_log.SelectMany(e => e.Items).Select(i => i.Rarity)).Count() >= 2
```

This is the machine-readable expression of "gear variety is demonstrable across raids."

**4. Pass/fail reporting — evidence bundle format**

A failing evidence bundle is reported as a structured object, not a prose message. The canonical format is a JSON-serialisable snapshot:

```json
{
  "scenarioName": "ThreeRaidVarietyCheck",
  "raidCount": 3,
  "distinctRaritiesObserved": ["Common", "Uncommon"],
  "passFail": "PASS",
  "eventCounts": {
    "loot.drawn": 3,
    "enemy.loadout.generated": 6,
    "player.equip": 4,
    "loot.acquired": 7,
    "extraction.complete": 3
  },
  "violations": []
}
```

A failing run populates `violations` with one entry per failed predicate, including the `RaidId` and `EventName` of the offending event. This allows an agent to pinpoint exactly which raid and which event caused the failure without re-running the scenario.

**5. Agent-readable vs. human-readable output**

The evidence bundle JSON is the agent-readable artifact. It is also the human-readable QA sign-off artifact — the same structure serves both audiences. No additional prose summary is generated by the QA scenario method; the bundle is self-describing.

**6. What this protocol does NOT cover**

- **CSS rendering verification** (AC-20 through AC-23): Rarity color classes cannot be asserted from `GameEventLog`. These require a browser session or bUnit component tests (available post-ADR-015). They are a known gap in the agentic QA protocol; manual smoke test covers them for v1.1.
- **Without-replacement invariant at scale** (AC-9): Verified by a dedicated statistical unit test (`LootTableTests.cs`), not by the three-raid scenario. The scenario is not the right instrument for distribution tests.
- **Save backward-compatibility** (AC-4, AC-22): Verified by a dedicated `StashStorage` unit test with a hardcoded legacy JSON string. Not observable through the event log.

#### Consequences

**Positive**

- An agent can execute the three-raid scenario, read `GameEventLog.Events`, and produce a structured evidence bundle with zero browser infrastructure.
- The evidence bundle is deterministic under a fixed `IRng` seed, so CI can assert on exact event counts without flakiness.
- The predicate list above is directly traceable to AC-15 through AC-19, AC-26, and AC-27 in FeatureSpec.md — each predicate maps to at most one AC.
- The JSON format is agent-parseable: a future agent that validates the bundle can do so with standard JSON tooling without understanding .NET types.
- The "violations" array gives an agent a structured failure diagnosis path, enabling automated retry with a different seed or a structured bug report without human intervention.

**Negative / Trade-offs**

- CSS rarity coloring (AC-20–AC-23) is explicitly out of scope for this protocol. These remain a manual verification gap until bUnit coverage is added (post ADR-015). This is a known, documented gap — not an oversight.
- The static `GameEventLog` must be cleared between tests. If a test runner parallelises test classes, shared static state could corrupt evidence bundles. Mitigation: all `GameEventLog`-reading tests must be in the same xUnit test class, or `CollectionDefinition` must serialize access. Document this in `GameEventLogTests.cs`.
- The evidence bundle JSON format is not versioned. If `GameEvent` schema changes in a future feature, old bundle snapshots become incompatible. This is acceptable at v1.1 scale; version the schema if it evolves significantly.

**Impact on feature work**

| Item | Impact |
|------|--------|
| AC-26, AC-27 | These ACs are the specification of this protocol. The protocol is accepted because the ACs are accepted. |
| R2 — No client-side tests | The agentic QA protocol covers Core-emitted events fully. `Home.razor`-emitted events are covered only in a live session until bUnit tests are added. |
| TBD-B (IRng extension) | The scenario protocol relies on `SequenceRng` for determinism. If `IRng` is extended with `NextDouble()` (TBD-B), all existing scenario seeds remain valid — the extension does not break existing seeded sequences. |
| Future E2E agent tooling | The evidence bundle format is defined here as a stable interface. Future agentic QA tooling should read from this format, not from browser DOM. |

---

---

## Cross-Cutting Impact Analysis

*Produced 2026-03-15 as part of the architect role. Synthesises how all current ADRs interact with the risk register (ProjectSnapshot.md) and the approved Loot Tiers spec (FeatureSpec.md). Updated whenever new ADRs are added.*

---

### How the ADR stack shapes the Loot Tiers implementation

The seven Loot Tiers ADRs (008–014) are not independent. Each one exploits or is constrained by a pre-v1.0 foundational decision. The table below makes those dependencies explicit.

### How the decomposition ADRs depend on each other (v1.2)

| Decomposition ADR | Directly depends on | Why |
|-------------------|---------------------|-----|
| ADR-015 (component extraction) | ADR-007 (monolith deferred) | ADR-015 is the resolution of the explicit "revisit at v1.1" note in ADR-007. The extraction design is only safe once Loot Tiers (which adds lines to Home.razor) has landed. |
| ADR-016 (no GameStateService) | ADR-015 (five panel components) | The "state stays in the shell" ruling is only needed once ADR-015 establishes that sub-components exist. It resolves TBD-A, which only arose from the ADR-015 design. |
| ADR-017 (EncounterType promotion) | ADR-015 (RaidHUD extraction) | A private enum in Home.razor is invisible to RaidHUD. The promotion decision is a direct consequence of ADR-015 naming RaidHUD as a separate component file that must accept EncounterType as a `[Parameter]`. |
| ADR-017 | ADR-001 (Core/Client separation) | The decision to host `EncounterType` in `RaidLoop.Client` rather than `RaidLoop.Core` follows directly from ADR-001's principle that Core contains no presentation-layer concerns. |

| Loot Tiers ADR | Directly depends on | Why |
|----------------|---------------------|-----|
| ADR-008 (Rarity on Item) | ADR-004 (sealed record) | Defaulted positional parameter on a sealed record is the zero-migration path; works because STJ handles missing fields with CLR defaults. |
| ADR-008 | ADR-006 (versioned saves) | The v3 key is NOT bumped because the STJ default-on-missing behaviour removes the need for a migration branch. This is the explicit coupling that ADR-014 resolves. |
| ADR-009 (immutable LootTable) | ADR-003 (IRng) | `Draw(IRng, int)` passes the RNG explicitly. Without ADR-003, draw results would be non-deterministic and AC-4 (weight distribution test) could not be written. |
| ADR-010 (LootTables factory) | ADR-002 (static facades) | The pattern is consistent: static class, no DI, weights as compile-time constants. The zero-NuGet-dependency constraint (NFR-7) is inherited from ADR-001. |
| ADR-011 (without-replacement) | ADR-009 (immutable table) | Immutability is a prerequisite: `Draw` copies `_entries` locally to implement without-replacement without corrupting the shared table instance. |
| ADR-012 (legacy overload) | ADR-007 (monolith deferred) | The legacy `IEnumerable<Item>` overload exists precisely because Home.razor cannot be safely refactored in the same PR as the Core change while ADR-007 is still in effect. |
| ADR-013 (CSS colouring) | ADR-007 (monolith deferred) | Choosing CSS classes over a `RarityBadge` component is correct *only while* ADR-007 is in effect. Once ADR-015 ships, `RarityBadge` becomes the preferred approach and ADR-013 should be reconsidered. |
| ADR-014 (no migration) | ADR-008 (defaulted property) | The `= Rarity.Common` default on the record parameter is what makes the STJ missing-field behaviour safe. Remove the default and ADR-014 breaks. |
| ADR-018 (GameEventLog) | ADR-001 (Core/Client separation) | Placing the log in Core is only valid because Core has no Blazor imports — a constraint enforced by ADR-001. If Core ever acquired framework dependencies, the log would need to move or use abstractions. |
| ADR-018 | ADR-003 (IRng) | The agentic QA scenario (ADR-019) relies on deterministic draws via `SequenceRng`. `GameEventLog` events are only reproducible because `LootTable.Draw` accepts `IRng` explicitly — the log captures what `IRng` produced, not a re-rolled result. |
| ADR-019 (Agentic QA protocol) | ADR-018 (GameEventLog) | The validation protocol is only executable because ADR-018 makes the event log readable from pure .NET test code. If events were written to the browser console, ADR-019 would require headless E2E infrastructure. |
| ADR-019 | ADR-003 (IRng) | Deterministic scenario execution — AC-26, AC-27 — depends on `SequenceRng` reproducibility. The protocol's pass/fail rules are only stable because the RNG seed fully determines which rarities appear in draws. |

---

### Risk register vs. ADR coverage

Each risk from ProjectSnapshot.md is either mitigated, accepted, or deferred by a specific ADR.

| Risk | Severity | Mitigating ADR(s) | Status |
|------|----------|-------------------|--------|
| R1 — Monolithic Home.razor | HIGH | ADR-007 (accepted), ADR-015/016/017 (remediation fully specced) | **Open.** ADR-015/016/017 resolve this; FeatureSpec.md Draft exists; implementation pending ApprovalPacket sign-off. |
| R2 — No client-side tests | MEDIUM | ADR-015 (decomposition unblocks bUnit), ADR-018/019 (event log covers Core-emitted behaviour without browser) | **Partially mitigated.** ADR-018/019 reduce the observable gap for Core events. `Home.razor`-emitted events (`player.equip`, `loot.acquired`, `extraction.complete`) remain a gap until ADR-015 ships. |
| R3 — Silent save migration failures | MEDIUM | ADR-006 (accepted with known gap), ADR-014 (avoids adding a new migration branch) | **Partially mitigated.** ADR-014 stops the gap from growing; the silent-failure behaviour in `StashStorage` remains. Harden as a standalone backlog item. |
| R4 — Unpinned .NET SDK | LOW-MEDIUM | No ADR. Supply-chain concern, not an architectural pattern. | **Open.** Add `global.json` as a pre-feature hygiene step. |
| R5 — Shared workflow pinned to `@main` | LOW | No ADR. CI/CD configuration, not a domain decision. | **Open.** Pin to a commit SHA. |
| R6 — Single monolithic test file | LOW | ADR-001 (Core isolation prevents test sprawl into UI tests), ADR-003 (injectable RNG keeps tests self-contained) | **Accepted.** Split `RaidEngineTests.cs` by module when count exceeds 60. |

---

### Ordering constraints derived from ADRs

The following sequencing rules are **hard constraints** imposed by the decisions above — violating the order puts the codebase in an inconsistent state.

```
1. global.json + workflow SHA pin (R4, R5)      — no ADR; pre-feature hygiene
       ↓
2. Loot Tiers implementation (ADR-008–014)      — FeatureSpec.md Approved
   + GameEventLog (ADR-018) + Agentic QA (ADR-019) land in PR-2 and PR-3
       ↓
3. Migrate Home.razor call sites to new         — ADR-012 technical debt clearance
   EncounterLoot overload; mark old [Obsolete]
       ↓
4. Home.razor Decomposition (ADR-015/016/017)   — FeatureSpec.md Draft; ApprovalPacket.md pending sign-off
       ↓
5. Add bUnit tests for panel components (R2)    — unblocked by step 4
   (ADR-019 evidence bundle extended to include bUnit-driven Home.razor events)
```

Step 3 is a hard prerequisite for step 4: ADR-015 states that the legacy `EncounterLoot` overload must be retired before `RaidHUD` is extracted, so the extracted component starts with a clean API surface.

Steps 2 and 4 must not be interleaved. If decomposition is already in progress when Loot Tiers lands, the `Home.razor` changes required by ADR-013 (CSS class additions) must be applied to whichever component owns the item display at that point — not to a partially-decomposed shell.

ADR-018 and ADR-019 are scoped to step 2. The evidence bundle format defined in ADR-019 is deliberately forward-compatible: when bUnit tests are added in step 5, those tests can populate the same `GameEventLog` and produce evidence bundles in the same JSON format. No change to the protocol is required when R2 is remediated.

---

### Open architectural questions

| ID | Question | Resolution path |
|----|----------|-----------------|
| TBD-A | Should a `GameStateService` be introduced in DI? | Resolved as "No" for O1 decomposition scope (ADR-016). Re-evaluate if a second page requiring game state access is introduced. |
| TBD-B | Should `IRng` expose `NextDouble()` for percentage-based loot modifiers? | Defer until a feature requires it. At that time, extend the interface or introduce a second `IRngFloat` interface. Do not extend `IRng` speculatively. |
| TBD-C | Should `LootTables.cs` move to a data-driven format (JSON/YAML) when table count exceeds ~10? | Review at the 10-table threshold. Current count: 4. The static-factory decision (ADR-010) is efficient at current scale. |
| TBD-D | Should the legacy `EncounterLoot` overload carry `[Obsolete]`? | Yes, after all `Home.razor` call sites are migrated (step 3 above). Tag with `[Obsolete("Use the LootTable overload instead.")]`. |
| TBD-E | Should `GameEventLog` evidence bundles be versioned? | Defer until the `GameEvent` schema changes in a future feature. At that time, add a `SchemaVersion` field to the JSON output format and document the migration from v1 bundles. Do not version speculatively. |
| TBD-F | Should `GameEventLog` emit events for Home.razor-driven interactions (`player.equip`, `loot.acquired`, `extraction.complete`) via a testable abstraction once ADR-015 ships? | Yes. Once panels are bUnit-testable, emit the same events from the extracted component handlers. The event contract (ADR-018) is the stable interface; the emission site shifts from Home.razor monolith to the owning panel component. No protocol change needed (ADR-019 is forward-compatible). Resolve as part of the R2 remediation work. |

---

*End of Architecture Decision Records — updated 2026-03-15 (ADR-018 and ADR-019 added: observability contract and agentic QA protocol for Loot Tiers v1.1; cross-cutting analysis, risk register, ordering constraints, and open questions updated)*
