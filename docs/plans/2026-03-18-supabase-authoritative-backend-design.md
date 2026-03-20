# Supabase Authoritative Backend Design

## Goal

Replace browser-local persistence with a fully authoritative Supabase backend. Users must authenticate before gameplay. All persistent state, raid progression, combat resolution, loot rolls, and economic mutations move off the browser so the client cannot cheat by modifying local state or executing game logic.

## Scope

- Replace `localStorage` save/load with Supabase-backed persistence.
- Require authenticated sessions before the game UI loads.
- Support OAuth login with Google, Microsoft, and Facebook through Supabase Auth.
- Move gameplay state transitions server-side.
- Keep the Blazor client as a presentation layer that sends user intent and renders authoritative responses.

## Non-Goals

- Offline mode.
- Anonymous play.
- Client-authoritative fallback behavior.
- Full data normalization of player inventory into many relational tables on day one.

## Recommended Architecture

### High-Level Split

- Blazor client:
  - handles auth UI and session state
  - sends player actions to backend endpoints
  - renders authoritative snapshots returned from backend
- Supabase Auth:
  - manages login/session lifecycle
  - issues JWTs for authenticated users
- Supabase Edge Functions:
  - validate caller identity
  - provide narrow action endpoints
  - apply request validation, rate limiting hooks, and orchestration
  - call database functions using trusted server-side credentials
- Postgres functions in a private schema:
  - execute all authoritative game logic
  - mutate saves, raids, encounters, loot, cooldowns, and economy
- Postgres tables with RLS:
  - persist user save data and active raid/session data
  - restrict user reads to their own rows

### Why Not Pure Stored Procedures Only

Pure SQL-only RPC would keep the client thin, but orchestration, provider-aware auth flows, replay protection, and request-shape validation become harder to maintain there. Edge Functions provide a better trust boundary while still keeping all authoritative game transitions out of the browser. The DB remains the final authority; Edge Functions are the secure entrypoint layer.

## Data Model

### Users

Supabase Auth `auth.users` remains the identity source.

### `public.game_saves`

One row per authenticated user keyed by Supabase user id.

Suggested columns:

- `user_id uuid primary key references auth.users(id)`
- `save_version int not null`
- `payload jsonb not null`
- `created_at timestamptz not null default now()`
- `updated_at timestamptz not null default now()`

The payload stores the current aggregate save shape:

- money
- stash
- on-person items
- luck-run cooldown state
- random character inventory state
- any future profile metadata

This matches the current app structure and minimizes migration risk.

### `public.raid_sessions`

One active raid row per user when in-raid.

Suggested columns:

- `user_id uuid primary key references auth.users(id)`
- `profile text not null`
- `payload jsonb not null`
- `created_at timestamptz not null default now()`
- `updated_at timestamptz not null default now()`

The payload stores authoritative in-progress raid state:

- health
- equipped items
- carried items
- discovered loot
- current encounter
- ammo
- malfunction state
- extract progress
- any generated enemy/loadout context

### Optional `public.game_events`

If event history is worth preserving server-side, store append-only event records. This is optional in the first slice.

## Security Model

### Browser

- never computes authoritative outcomes
- never mutates money/inventory directly
- never rolls loot or encounters
- never stores the source-of-truth save

### RLS

- `game_saves`: authenticated users can only select/update their own row
- `raid_sessions`: authenticated users can only select/update their own row if direct reads are needed
- private schemas/functions should not be exposed directly to the public API unless intentionally allowed

### Trusted Execution

- gameplay mutations are executed by private DB functions
- Edge Functions call those DB functions using trusted server credentials
- service-role secrets stay in Supabase server environment only

### Anti-Cheat Principles

- all RNG and encounter generation server-side
- client sends only intent, such as `attack`, `loot item`, `move to extract`
- backend validates legal transitions against persisted state
- action handlers reject impossible or stale actions

## Auth Flow

### Login Requirements

- login is mandatory before gameplay
- providers: Google, Microsoft, Facebook
- no guest mode

### Client Behavior

- app boots into an auth gate
- if no active session, render sign-in screen only
- after login, fetch authoritative profile snapshot from backend
- session refresh handled by Supabase Auth client

### First Login Bootstrap

If the user has no `game_saves` row, backend creates a default save using the authored starter kit and default money.

## Gameplay API Shape

Expose narrow backend actions, not generic save writes.

Examples:

- `GET profile/bootstrap`
- `POST game/start-main-raid`
- `POST game/start-luck-run`
- `POST game/attack`
- `POST game/burst-fire`
- `POST game/reload`
- `POST game/flee`
- `POST game/use-medkit`
- `POST game/take-loot`
- `POST game/equip-from-discovered`
- `POST game/equip-from-carried`
- `POST game/drop-equipped`
- `POST game/drop-carried`
- `POST game/continue-searching`
- `POST game/move-toward-extract`
- `POST game/attempt-extract`
- `POST game/store-item`
- `POST game/sell-item`
- `POST game/move-stash-to-loadout`
- `POST game/process-luck-run-item`

Each endpoint returns an authoritative snapshot sufficient to redraw the UI.

## Database Function Strategy

Create a private schema for authoritative functions, for example `game`.

Suggested DB functions:

- `game.bootstrap_player(user_id uuid)`
- `game.get_player_snapshot(user_id uuid)`
- `game.start_main_raid(user_id uuid)`
- `game.start_luck_run(user_id uuid)`
- `game.perform_action(user_id uuid, action jsonb)`
- `game.finish_raid(user_id uuid, result jsonb)`

Internally, these functions can:

- load save row
- validate state preconditions
- run loot/combat/economy logic
- update `game_saves` and `raid_sessions`
- return the next snapshot

## Migration Strategy

### Phase 1

- add Supabase client/auth plumbing
- add required config and provider setup docs
- create DB schema and RLS policies
- create starter bootstrap path
- replace `StashStorage` with a server-backed profile service

### Phase 2

- move save/load actions to Edge Functions + DB functions
- keep UI shape similar while sourcing data from backend snapshots

### Phase 3

- migrate in-raid actions to authoritative backend calls
- remove remaining client-side raid logic from `Home.razor.cs`

### Phase 4

- harden with transition validation, conflict checks, and logging

## Testing Strategy

### Client Tests

- auth gate renders when no session
- signed-in session loads profile snapshot
- action buttons call backend services instead of mutating local state

### Core/Contract Tests

- snapshot DTO serialization
- backend request/response contracts
- migration/bootstrap coverage for first login

### Backend Tests

- DB function tests for legal and illegal transitions
- Edge Function integration tests for auth enforcement
- RLS tests proving one user cannot access another user’s save/session rows

## Operational Notes

### Required Supabase Setup

- project URL and publishable key in client config
- service-role key only in Edge Function secrets
- OAuth providers configured in Supabase dashboard
- redirect URLs configured for local and deployed environments

### App Configuration

Client should use typed options for:

- Supabase URL
- Supabase publishable key
- auth redirect path

No secrets belong in the browser bundle.

## Recommendation Summary

- Use Supabase Auth with Google, Microsoft, and Facebook.
- Require login before any gameplay.
- Store one authoritative save row per user as JSONB.
- Store active raids separately from the long-lived save.
- Use Edge Functions as the trusted API boundary.
- Execute authoritative game transitions in private Postgres functions.
- Remove client authority over save, combat, loot, and economy entirely.
