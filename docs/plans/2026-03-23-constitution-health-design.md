# Constitution Health Design

**Problem**

The game currently hard-codes player max health as `30` across the client and Supabase raid functions. We want to introduce a persisted `CON` stat and derive max health from it using `10 + (2 * con)`, while keeping current behavior unchanged by starting all players at `10 CON` for `30 HP`.

**Goals**

- Persist `playerConstitution` like `playerDexterity`.
- Persist `playerMaxHealth` alongside `playerConstitution`.
- Use one shared health formula so new code and backfills agree.
- Replace hard-coded `30 HP` assumptions in client and backend raid flows.
- Add tests that prove the new path is real, not accidentally still passing because `10 CON` equals `30 HP`.

**Non-Goals**

- No UI for changing constitution yet.
- No balancing changes beyond preserving the current `30 HP` default.
- No redesign of enemy health or enemy stat generation in this change.

**Approach**

Add `playerConstitution` and `playerMaxHealth` to the normalized save payload, default payload, and client contract snapshot. Compute max health from constitution with a single formula in `CombatBalance` and mirror that formula in Supabase SQL for migration and raid-state generation. Keep `playerMaxHealth` persisted as requested, but treat it as derived data that must match the constitution formula whenever new or normalized player state is generated.

On the client, replace the hard-coded max-health constant with a field populated from the authoritative snapshot/projection payload. In backend raid flows, start raid health from saved max health and clamp medkit healing to saved max health instead of `30`.

**Data Model**

- `playerConstitution: int`
- `playerMaxHealth: int`
- Default/backfill values:
  - `playerConstitution = 10`
  - `playerMaxHealth = 30`

**Health Rule**

- `maxHealth = 10 + (2 * constitution)`

**Testing Strategy**

- Core unit tests for the constitution-to-health formula.
- Contract tests for `PlayerSnapshot` round-tripping the new fields.
- Migration tests proving old payloads normalize to `10 CON` and `30 HP`.
- Client/API tests proving raid hydration uses non-default max health values when supplied.
- Backend-facing markup/migration tests proving SQL start-raid and medkit-cap logic use saved max health rather than hard-coded `30`.
