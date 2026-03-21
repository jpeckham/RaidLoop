# Game Action Event Projections Design

**Goal:** Replace full `PlayerSnapshot` action responses with compact authoritative event results while keeping bootstrap as the single full-state hydrate path.

**Current State**

- Login/bootstrap returns a full `PlayerSnapshot` via `profile-bootstrap`.
- Every `game-action` also returns a full authoritative `PlayerSnapshot`.
- The client applies every action by replacing local state through `ApplySnapshot(...)` in [`Home.razor.cs`](C:/Users/james/source/repos/extractor-shooter-light/src/RaidLoop.Client/Pages/Home.razor.cs).
- This is simple and safe, but every button click sends and receives far more state than the action actually mutates.

**Constraints**

- The server remains authoritative for all gameplay state.
- The client may hold local state between actions, but it must only apply server-returned authoritative projections.
- `profile-bootstrap` remains the initial full-state hydrate path after login.
- The migration must be incremental. Existing game flows cannot require a big-bang rewrite.

**Recommended Contract**

Keep `profile-bootstrap` unchanged and redesign `game-action` to return a hybrid result:

- `eventType`: semantic category of what happened
- `event`: action-specific resolved event data
- `projections`: authoritative post-action state slices for only the mutated areas
- `message`: optional user-facing message or transition text
- transitional `snapshot`: optional compatibility field during rollout only

Example:

```json
{
  "eventType": "CombatResolved",
  "event": {
    "action": "attack",
    "enemyDamage": 2,
    "playerDamage": 3,
    "ammoSpent": 1,
    "weaponMalfunctioned": true,
    "logEntriesAdded": [
      "You hit Scav for 2.",
      "Scav hits you for 3.",
      "Weapon jammed."
    ]
  },
  "projections": {
    "raid": {
      "health": 17,
      "enemyHealth": 6,
      "ammo": 4,
      "weaponMalfunction": true
    }
  },
  "message": null
}
```

**Why Hybrid Event + Projection**

Three options were considered:

1. Delta-only responses
- Smallest payloads
- Weak fit because the client would need to reconstruct too much meaning and state from many optional deltas

2. Pure typed events with local client reducers
- Clean model
- Too disruptive because every action would need a complete event-sourcing redesign before stability

3. Typed events plus authoritative projections
- Server remains authoritative
- Payloads shrink substantially
- Client reducers stay simple because they receive final authoritative values for mutated slices
- Best fit for incremental rollout

Recommendation: option 3.

**Event Taxonomy**

Use a small event set that groups actions by semantic outcome instead of mirroring every button name:

- `ProfileMutated`
  - sell, buy, equip, unequip, stash moves, luck-run inventory mutations
- `RaidStarted`
  - main raid or luck run begins
- `CombatResolved`
  - attack, burst fire, reload, flee when combat state changes
- `LootResolved`
  - take loot, drop carried, drop equipped, equip from discovered/carried
- `EncounterAdvanced`
  - continue searching, move toward extract, extraction transition, neutral transition
- `RaidFinished`
  - extracted, killed, or otherwise left raid flow
- `AuthRequired`
  - session refresh failed; client must sign in again

**Projection Slices**

Only include the slices touched by the current action:

- `economy`
  - `money`
- `stash`
  - `mainStash`
- `loadout`
  - `onPersonItems`
- `luckRun`
  - `randomCharacter`
  - `randomCharacterAvailableAt`
- `raid`
  - trimmed active raid projection, not automatically the entire `activeRaid` payload
- `session`
  - auth-related transition state only when needed

Typical `raid` projection fields:

- `health`
- `enemyHealth`
- `ammo`
- `weaponMalfunction`
- `medkits`
- `discoveredLoot`
- `carriedLoot`
- `equippedItems`
- `encounterType`
- `encounterTitle`
- `encounterDescription`
- `lootContainer`
- `awaitingDecision`
- `extractProgress`
- `logEntriesAdded`

**Client Application Model**

- `ApplySnapshot(...)` remains for bootstrap and explicit resyncs only.
- New `ApplyActionResult(...)` applies returned projections slice-by-slice.
- Event payloads are used for UI semantics, logs, and future instrumentation, but projections are the authoritative source for post-action client state.
- If an action result is malformed or missing required slices, the client should trigger a resync rather than drift silently.

**Server Application Model**

- Existing action handling remains server-authoritative in Supabase SQL and edge function orchestration.
- After an action mutates authoritative state, the server builds only the mutated projections needed by the client.
- Transitional compatibility can include both `snapshot` and `projections`; once rollout completes, `snapshot` is removed from `game-action` responses.

**Incremental Rollout**

1. Introduce the new result contract alongside the old snapshot response.
2. Convert out-of-raid profile actions first.
3. Convert raid start actions next.
4. Convert in-raid combat and loot actions last.
5. Remove `snapshot` from `game-action` once all actions have moved to projections.

**Risks**

- Partial projection bugs could leave the client visually out of sync.
- Event payload shape drift across actions could create reducer complexity if not standardized early.
- Raid actions are the highest-risk migration slice because they touch the most state.

**Mitigations**

- Keep bootstrap snapshot available as a resync path during rollout.
- Add response-shape tests per action category.
- Standardize projection slice names before implementation begins.
- Migrate easiest action families first and validate reducer patterns before combat.
