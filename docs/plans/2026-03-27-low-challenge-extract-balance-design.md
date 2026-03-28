# Low Challenge Extract Balance Design

**Date:** 2026-03-27

## Goal

Make the `challenge 0` "go straight to extract" loop a viable low-risk recovery path instead of a near-guaranteed combat gauntlet.

## Current State

- Raids start at `distanceFromExtract = 3`.
- `move-toward-extract` resolves a fresh encounter each step.
- The authored `default_raid_travel` and `extract_approach` tables currently contain only combat entries.
- This makes even the lowest-challenge extraction route effectively forced combat on every travel step.

## Approved Direction

Restore neutral and low-value loot outcomes to the travel and extract-approach encounter families.

- Combat remains possible.
- Travel and extract should no longer be 100% combat.
- Low challenge should skew heavily toward neutral and modest loot.

## Design

### Backend Only

This is an authored encounter-data fix, not a combat-rules fix.

- Add/update encounter table rows in SQL for `default_raid_travel` and `extract_approach`.
- Include neutral outcomes and a small amount of loot on those tables.
- Keep the existing combat contact-state system intact.

### Intended Feel

- Moving toward extract at `challenge 0` should usually give quiet travel or small loot.
- Combat should still happen sometimes, especially closer to extract, but should not be the default every step.

### Testing

- Extend migration binding tests so the authored travel/extract families must include non-combat rows.
- Verify the migration text still routes travel to `default_raid_travel` and extract movement to `extract_approach`.
