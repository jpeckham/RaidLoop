# Item Identity Refactor Design

## Goal

Decouple gameplay identity from item labels so item names can change for localization or legal/content reasons by editing client localization assets instead of gameplay logic, persistence, or API contracts.

## Principles

The finished system must satisfy these constraints:

- Runtime identity uses only `itemDefId`
- Localizable text lives only in client-owned assets
- Bootstrap downloads the full non-localized item rules catalog once
- Action payloads send only dynamic state plus stable ids
- Item rule changes happen in one server-owned place
- Item label changes happen in one client-owned localization place
- Avoid repeated or duplicated runtime item metadata in action payloads

## Problem

The current system still leaks item display names and textual keys across:

- C# gameplay logic
- SQL functions and authored data
- Edge-function payloads
- Persisted save and raid JSON
- Tests and fixtures

That makes renames risky and expensive. It also conflicts with localization, because the server should not own English labels or other presentation text in runtime contracts.

## Chosen Model

Use a stricter split between server rules and client presentation:

- Database identity and API item identity: `item_def_id int generated always as identity`
- Stable authored lookup key: `item_key text unique not null`
- Client-facing presentation: client-owned localization assets keyed by `item_def_id`

The server uses `item_def_id` in payloads and persisted item references. The server may still keep `item_key` internally for authored lookups and migration compatibility, but contracts should not depend on it. Labels, names, descriptions, and encounter display text are client-owned presentation data.

## Data Model

### Database

`game.item_defs` becomes the authoritative source of item metadata with:

- `item_def_id int primary key`
- `item_key text unique not null`
- existing gameplay fields such as type, rarity, value, weight, slots
- existing label column may remain temporarily for authored compatibility, but it is not part of runtime contracts

Authored tables should reference items by `item_def_id` internally where practical. `item_key` remains useful for migration and authored tooling, not as client/runtime identity.

### Payloads and Contracts

Item snapshots and item-bearing payloads should carry `itemDefId` as the canonical identifier. They should not carry `name` or `itemKey`.

Item-bearing payloads should include only dynamic state plus identity:

- `itemDefId`
- quantity, if relevant
- equipped/location state, if relevant
- shop offer state such as `price` and `stock`, if relevant

During migration, readers should tolerate legacy payloads that still carry `name` or `itemKey`, but new writes should emit `itemDefId`.

Bootstrap should also return a lightweight item rules catalog keyed by `itemDefId` so the client can support local UX without server-authored presentation text. That catalog should include only non-authoritative static facts the UI may need, such as:

- `type`
- `weight`
- `slots`
- optionally rarity if the client uses it only for non-text presentation

### Client

The client maintains two separate concerns:

- a downloaded rules catalog keyed by `itemDefId` for lightweight UX
- local localization assets keyed by `itemDefId` for all presentation

UI rendering resolves:

- localized label
- localized description
- iconography
- any client-only presentation metadata

Client code should stop branching on `Name` or `itemKey`. Any local lookups should use `itemDefId`.

## Remaining End-State

The branch is not complete until these are all true:

- bootstrap returns the full item rules catalog and runtime payload items only use `itemDefId`
- raid and profile actions use `itemDefId` end to end without handler-side name rewriting
- the client resolves item weight, slots, type, and rarity-like UX facts from downloaded `itemRules`
- the client resolves labels and other localizable text from one local asset layer
- runtime UI logic does not require server-authored item names
- legacy `name` and `itemKey` handling exists only as narrow compatibility for old payload reads

## Migration Strategy

### Phase 1: Introduce Identity Without Breaking Reads

- Add `item_def_id` to `game.item_defs` in a forward-only migration
- Preserve and validate unique `item_key`
- Add `itemDefId` support to C# domain models and contracts
- Add client compatibility so legacy payloads still deserialize during migration

### Phase 2: Move Server Logic to Surrogate Identity

- Update SQL functions and authored data access to resolve items by `item_def_id`
- Update edge functions and snapshots to emit `itemDefId`
- Stop using labels as lookup identity in server gameplay logic

### Phase 3: Migrate Persisted Payloads

- Rewrite existing `public.game_saves.payload` and `public.raid_sessions.payload` item entries to include `itemDefId`
- Keep compatibility readers for legacy payloads during rollout so old saves remain loadable

### Phase 4: Add Client Rules Catalog And Remove Contract Presentation Leakage

- Return a lightweight item rules catalog at bootstrap keyed by `itemDefId`
- Remove `name` and `itemKey` from runtime item payloads
- Keep labels and names fully client-owned display text

### Phase 5: Rename Content Safely

Once `itemDefId` is canonical and labels are client-only, item renames become a client localization/content update rather than a gameplay identity migration. Enemy display terms like `Scav` to `Scavenger` can then be handled as authored/localized text rather than gameplay identity.

## Compatibility Rules

- Existing saves must still load during transition
- Existing tests that prove historical migrations remain untouched should stay untouched
- New migrations must be forward-only
- The client may temporarily accept legacy `name` and `itemKey`, but new code should prefer `itemDefId`

## Testing Strategy

Use TDD and the existing SQL verification discipline.

### C# / Contract Tests

Add failing tests first for:

- contract round-trip with `itemDefId`
- client compatibility reading legacy `name`/`itemKey` payloads
- bootstrap rules-catalog download keyed by `itemDefId`
- gameplay logic and client lookups branching on `itemDefId` instead of labels or textual keys

### SQL Shape Tests

Add migration-shape assertions that verify:

- the new migration adds `item_def_id`
- payload backfill SQL writes `itemDefId`
- bootstrap emits an item rules catalog
- outbound SQL/edge paths stop requiring `name` and `itemKey` in runtime contracts where appropriate

### Runtime SQL Integration Tests

Reset local Supabase, seed legacy payloads, call real RPCs, and prove:

- legacy name-based payloads are upgraded or tolerated
- new RPC responses emit `itemDefId`
- bootstrap returns the rules catalog needed for local UX
- save and raid payloads no longer require labels or textual keys to function

## Non-Goals

- Changing the actual item labels in this branch
- Immediate removal of every legacy authored label from the database on day one
- Eliminating every compatibility reader before old payloads are migrated

Those should follow after identity and the client localization boundary are stable.
