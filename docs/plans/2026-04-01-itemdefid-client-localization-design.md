# ItemDefId Client Localization Design

## Goal

Make `itemDefId` the only real runtime item identity and move all player-facing item names into Blazor client localization resources so item renames become client localization changes instead of gameplay, persistence, or API migrations.

## Problem

The current branch has already moved most runtime identity to `itemDefId`, but item labels still leak through runtime code paths:

- `ItemPresentationCatalog` hardcodes English labels in C#
- some payloads and compatibility layers still carry `name`
- tests still frequently assert on server-authored names

That leaves the UI coupled to English display text and keeps item naming spread across core code, API contracts, SQL, and client rendering.

## Decision

Use this boundary:

- `itemDefId`: canonical runtime identity
- `item_key`: optional authored/database slug only
- `.resx` resources in `RaidLoop.Client`: only source of player-facing item labels

The client resolves labels from `itemDefId`. Runtime behavior must not depend on display names or textual keys.

## Design

### Runtime Identity

All gameplay and UI runtime lookups should be `itemDefId`-first:

- `Item`
- loadout selection
- stash/on-person operations
- raid actions
- shop entries
- bootstrap item rules

Legacy `name` reads may remain only as narrow compatibility for old payloads. New writes should not depend on display names.

### Client Localization

Use standard Blazor localization with `.resx` resources and `IStringLocalizer`.

The client should own:

- item display labels
- future item descriptions if needed
- any other user-facing item text

The simplest stable mapping is `itemDefId -> resource key`. The resource keys should be stable symbolic names such as:

- `Items.1`
- `Items.2`
- `Items.19`

This avoids making the localization layer depend on mutable English text or on database slugs. The lookup service can centralize the fallback behavior.

### Presentation Service

Replace the hardcoded label dictionary in `ItemPresentationCatalog` with a localizer-backed service or helper that:

1. accepts `Item` or `itemDefId`
2. maps it to a resource key
3. resolves the localized value from `.resx`
4. falls back safely for unknown ids

Fallback rules:

- known `itemDefId`: return localized resource value
- unknown `itemDefId` with legacy `name`: return legacy `name`
- unknown item without either: return empty string

That keeps the client resilient while old payloads still exist.

### Core Catalog Responsibility

`RaidLoop.Core` should not own display labels. It may continue to own:

- `itemDefId`
- item type
- rarity
- value
- weight
- slots
- legacy compatibility metadata such as old names

If a stable client lookup helper is needed, `RaidLoop.Core` can expose only a stable localization token or leave the mapping entirely in the client. The preferred version is to keep item label ownership entirely in the client and derive the resource key directly from `itemDefId`.

### Contracts

Runtime item-bearing contracts should continue moving toward:

- `itemDefId`
- dynamic state only

`name` should remain compatibility-only on inbound reads and should not be the client rendering source of truth. The client may temporarily preserve it on local save round-trips while migration compatibility remains necessary.

## Implementation Shape

### Client

- add localization service registration in Blazor startup
- add item resource `.resx` file(s)
- replace hardcoded item-label dictionary with localizer-backed lookup
- update components and page logic to render labels from localization

### Tests

Add coverage for:

- localized label resolution by `itemDefId`
- fallback behavior for unknown ids / legacy payloads
- no dependency on server-authored item labels for normal UI rendering

### Documentation

Document that:

- `itemDefId` is canonical runtime identity
- `item_key` is authoring-only if retained
- item names are client-localized resources, not runtime data identity

## Non-Goals

- removing every legacy `name` field in one pass if compatibility still needs it
- localizing every other gameplay string in this branch
- replacing authored database slugs if they are still useful for migrations and tooling

## Success Criteria

This work is complete when:

- all normal item rendering in the client resolves labels from `.resx`
- no client gameplay/UI lookup requires `Item.Name`
- `ItemPresentationCatalog` no longer owns English literals
- `itemDefId` is the only identity used for runtime item handling
- legacy item names exist only for compatibility fallback paths
