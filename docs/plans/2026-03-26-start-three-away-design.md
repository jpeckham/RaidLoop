# Start Three Away From Extract Design

**Date:** 2026-03-26
**Input:** Update the raid loop so players begin each raid three moves away from extract instead of at extract.

## Approved Rule Change
- Raid start state is now:
  - `challenge = 0`
  - `distanceFromExtract = 3`
- `challenge` and `distanceFromExtract` remain independent variables.
- `Move Toward Extract` reduces `distanceFromExtract` by `1`, with a floor of `0`.
- `Go Deeper` increases `challenge` by `1` and `distanceFromExtract` by `1`.
- Reaching `distanceFromExtract = 0` still unlocks extract-state actions.

## Why This Change Improves The Loop
- Raids now begin with actual travel pressure instead of immediate extract access.
- Short raids are still possible, but require three safe movement steps back to extract.
- `Stay at Extract` remains meaningful because extract becomes an earned state, not the default start state.

## Scope
- Change raid-start defaults for both main raids and random raids.
- Update tests and any user-facing assumptions that still encode a start distance of `0`.
- Do not rebalance challenge progression beyond this start-state change.

## Acceptance Criteria
- New raids begin with `challenge = 0`.
- New raids begin with `distanceFromExtract = 3`.
- Existing movement rules remain unchanged.
- Tests reflect the new start state.
