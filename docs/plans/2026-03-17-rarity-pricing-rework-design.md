# Rarity And Pricing Rework Design

## Goal

Rework item quality, pricing, and loot generation so that:
- display rarity is a stable authored property used only for UI presentation
- loot rarity is a separate authored generation concern
- item values are authored from usefulness instead of constructor defaults
- spawn boosters raise quality by shifting tier rolls upward rather than by scaling raw item chances

## Current Problem

The current code mixes three different concerns:
- item value is sometimes authored and sometimes left at the `Item` default of `1`
- the same rarity field is used for both UI coloring and loot table weighting
- generation logic assumes static rarity weights instead of context-sensitive quality shifts

This creates pricing regressions, weak tuning control, and unclear semantics.

## Design Summary

The system will be split into three authored layers:

1. `DisplayRarity`
- categorical UI quality band
- values: `SellOnly`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`
- drives CSS classes and player-facing labels only

2. Loot tier generation
- source-specific tier profiles for loot containers and enemy loadouts
- each profile defines the chance to roll each quality tier
- boosters modify generation by shifting the rolled tier upward before item selection

3. Item definitions
- a centralized authored catalog defines each item's gameplay and economy metadata
- price is stored explicitly in the authored definition
- display rarity is stored explicitly in the authored definition
- items are selected from source pools by tier, not by constructor defaults

## Data Model

### DisplayRarity

Add a new enum in Core:
- `SellOnly`
- `Common`
- `Uncommon`
- `Rare`
- `Epic`
- `Legendary`

This enum is the only source for rarity color and naming in the UI.

### Item

Update `Item` to store `DisplayRarity` instead of the current `Rarity` field.

`Item.Value` remains as the concrete sell/buy/economy number used by the UI and event payloads, but it will be populated from authored definitions instead of ad hoc constructor defaults.

### Item Definitions

Introduce a centralized catalog in Core that authors per-item metadata:
- name
- item type
- value
- slots
- display rarity

This catalog becomes the source of truth for creating items in:
- shop stock
- fallback/starter gear
- random loadouts
- loot container generation
- enemy loadout generation
- medkit materialization from raid inventory

This removes the current drift where different call sites create the same named item with different or missing values.

## Pricing Model

Pricing is authored from usefulness and explicit game balance, not dynamically derived at runtime from display rarity.

Initial authoring rules:
- larger backpacks cost more than smaller backpacks
- better weapons cost more based on their effective combat power
- better armor costs more based on its damage reduction
- medkits have useful but basic pricing
- pure vendor items have explicit low-to-mid values based on intended economy pacing

Display rarity can inform initial authoring, but pricing remains a separate authored number so future balancing can change value without forcing a rarity change.

## Display Rarity Semantics

UI colors:
- `SellOnly` = gray
- `Common` = white
- `Uncommon` = green
- `Rare` = blue
- `Epic` = yellow
- `Legendary` = orange

Semantics:
- `SellOnly` means no direct gameplay use
- `Common` means basic usable gear, including starter-tier weapon, starter armor, and medkits
- higher tiers represent authored quality bands for presentation and player expectation

## Loot Generation Model

Generation becomes tier-first rather than item-first.

### Source Profiles

Each loot source defines a tier profile:
- example sources: weapons crate, armor crate, mixed cache, enemy loadout
- each profile contains chance values for `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`
- `SellOnly` is not a rolled tier by itself; sell-only items appear only in source pools that explicitly include them under the chosen tier strategy

### Roll Process

For each generated item:
1. roll a quality tier from the source profile
2. apply any booster by shifting the result upward
3. select a concrete item from that source's eligible items in the final tier

Boosters do not multiply raw item drop percentages. They operate on the tier result.

### Booster Model

Boosters are contextual modifiers such as:
- boss enemy
- special area
- elite encounter

They affect only generation, not authored item metadata.

A booster may:
- raise the minimum tier floor
- add a chance to shift the rolled tier up by one or more bands

This keeps tuning understandable and avoids inflating low-tier junk when a source is meant to feel more rewarding.

## Source Membership

The same item may be eligible in multiple sources, but source membership is authored explicitly. This allows:
- source identity to remain distinct
- later tuning where an item is common in one source and absent in another

Tier choice is source-driven. Item choice inside a tier is source-local.

## Migration Strategy

1. Introduce `DisplayRarity` and the item catalog.
2. Migrate all item creation sites to use catalog factories/helpers.
3. Replace current loot table rarity weighting with tier-profile rolling.
4. Update UI styling and labels to read `DisplayRarity`.
5. Re-author values and tiers for all currently known items.

## Testing Strategy

Add tests for:
- item catalog metadata for value, slots, and display rarity
- starter gear and medkit materialization preserving authored values
- source tier roll behavior
- booster tier shifting behavior
- known item creation paths no longer defaulting to `1`
- UI markup continuing to bind dynamic label values correctly

## Risks

### Risk: Existing saves contain stale item values

Saved items may already carry old values. If save migration is not handled, players can keep outdated prices after the new catalog ships.

Mitigation:
- normalize loaded items against the authored catalog by name where possible
- preserve unknown items safely

### Risk: Tier tuning feels too sparse or too generous

Changing from weighted item tables to tier-first generation changes the distribution shape.

Mitigation:
- keep deterministic tests for tier probabilities
- tune source profiles separately from item definitions

### Risk: Constructor defaults continue to leak in

Any direct `new Item(...)` call that bypasses the catalog can silently reintroduce bad values or rarity.

Mitigation:
- centralize creation through catalog helpers
- add tests around known named items

## Recommended Implementation

Implement this in Core-first fashion:
- create display rarity and item catalog
- migrate pricing and item creation
- then switch generation to source-tier profiles with boosters
- finally update UI colors and any tests/docs that still reference the old rarity scheme
