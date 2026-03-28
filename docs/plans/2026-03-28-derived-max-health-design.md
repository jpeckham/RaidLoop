# Derived Max Health Design

**Goal:** Make player max health fully derived from accepted Constitution so stale saved `playerMaxHealth` values can no longer drift from stats.

**Problem:**
- The backend currently persists `playerMaxHealth` and treats it as an input during normalization.
- If a save already has a positive stale value, stat acceptance can update Constitution without updating max health.
- This produces impossible states like `CON 10` with `26 HP`.

**Approved Design:**
- `acceptedStats.constitution` is the source of truth.
- `playerMaxHealth` is derived from accepted Constitution everywhere on the backend.
- Existing saves are repaired by normalization/backfill rather than trusting stored positive values.
- Keep `playerMaxHealth` in payloads for compatibility, but treat it as derived output only.

**Architecture:**
- Replace `game.normalize_save_payload(payload jsonb)` so it always computes `playerMaxHealth` from the normalized accepted Constitution.
- Replace `game.apply_profile_action(...)` so its final projected payload also derives `playerMaxHealth` from the accepted stats it is about to persist.
- Backfill `public.game_saves` via `update ... set payload = game.normalize_save_payload(payload)` in the migration.

**Testing:**
- Add migration binding tests that pin the new derived-health logic and explicitly reject the stale positive-value fallback pattern.
