# Item Identity Refactor Design

## Goal

Decouple gameplay identity from item labels so item names can change for localization or legal/content reasons without forcing repo-wide gameplay and persistence rewrites.

## Problem

The current system uses item display names as canonical identity across:

- C# gameplay logic
- SQL functions and authored data
- Edge-function payloads
- Persisted save and raid JSON
- Tests and fixtures

That makes any rename risky and expensive. It also conflicts with localization, because the server should not own English labels as canonical data.

## Chosen Model

Use a split identity model:

- Database surrogate key: `item_def_id int generated always as identity`
- Stable app/domain identifier: `item_key text unique not null`
- Client-facing localized label: client-owned catalog text keyed by `item_key`

The server should treat `item_key` as the canonical identifier for gameplay and persistence. The numeric surrogate key exists for relational integrity and efficient joins. Labels are not canonical server data.

## Data Model

### Database

`game.item_defs` becomes the authoritative source of item metadata with:

- `item_def_id int primary key`
- `item_key text unique not null`
- existing gameplay fields such as type, rarity, value, weight, slots
- existing label column can remain temporarily, but it is no longer identity

Authored tables should reference items by `item_def_id` internally where practical. Where a stable textual identifier is preferable in authored logic or contracts, use `item_key`.

### Payloads and Contracts

Item snapshots and item-bearing payloads should carry `itemKey` as the canonical identifier. The client uses that key to resolve localized label and presentation details from its own catalog.

During migration, readers should tolerate legacy payloads that still carry `name` without `itemKey`, but new writes should emit `itemKey`.

### Client

The client item catalog becomes keyed by `itemKey`, not by localized label. UI rendering resolves:

- localized label
- localized description
- iconography
- any client-only presentation metadata

Gameplay code on the client should stop branching on `Name` and branch on `Key`.

## Migration Strategy

### Phase 1: Introduce Identity Without Breaking Reads

- Add `item_def_id` to `game.item_defs` in a forward-only migration
- Preserve and validate unique `item_key`
- Add `itemKey` support to C# domain models and contracts
- Add client compatibility so legacy `name`-only payloads still deserialize

### Phase 2: Move Server Logic to Keys

- Update SQL functions and authored data access to use `item_key` or `item_def_id`
- Update edge functions and snapshots to emit `itemKey`
- Stop using labels as lookup identity in server gameplay logic

### Phase 3: Migrate Persisted Payloads

- Rewrite existing `public.game_saves.payload` and `public.raid_sessions.payload` item entries to include `itemKey`
- Keep compatibility readers for legacy payloads during rollout

### Phase 4: Remove Label Coupling

- Convert remaining C# gameplay logic and tests from name-based matching to key-based matching
- Keep labels as client-owned display text

### Phase 5: Rename Content Safely

Once keys are canonical, item renames become a client catalog/content update rather than a gameplay identity migration. Enemy display terms like `Scav` to `Scavenger` can then be handled as authored text rather than gameplay identity.

## Compatibility Rules

- Existing saves must still load during transition
- Existing tests that prove historical migrations remain untouched should stay untouched
- New migrations must be forward-only
- The client may temporarily accept both `itemKey` and `name`, but new code should prefer `itemKey`

## Testing Strategy

Use TDD and the existing SQL verification discipline.

### C# / Contract Tests

Add failing tests first for:

- `ItemCatalog` resolution by `itemKey`
- contract round-trip with `itemKey`
- client compatibility reading legacy `name` payloads
- gameplay logic branching on keys instead of labels

### SQL Shape Tests

Add migration-shape assertions that verify:

- the new migration adds `item_def_id`
- payload backfill SQL writes `itemKey`
- any rewritten SQL functions reference `item_key` rather than display labels where appropriate

### Runtime SQL Integration Tests

Reset local Supabase, seed legacy payloads, call real RPCs, and prove:

- legacy name-based payloads are upgraded or tolerated
- new RPC responses emit key-based item identity
- save and raid payloads no longer require labels to function

## Non-Goals

- Full localization system implementation
- Immediate removal of every legacy `name` field on day one
- Item label renames in the same refactor

Those should follow after identity is stable.
