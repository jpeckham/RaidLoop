# Player Stat System Design

**Date:** 2026-03-26
**Input:** Design and implement a six-stat player allocation system with point-buy, persistence, migration, and gameplay integration.

## Approved Approach
- Use a shared stat system in `RaidLoop.Core` and keep the client thin.
- Centralize score, modifier, and point-buy logic in one rules component so pricing can be swapped later without rewriting UI.
- Treat editable stats as a between-raids draft and accepted stats as the only gameplay-authoritative values.

## Stat Rules
- Stats: `STR`, `DEX`, `CON`, `INT`, `WIS`, `CHA`
- Minimum score: `8`
- Maximum score: `18`
- Starting score for every stat: `8`
- Initial spendable pool: `27`
- Modifier formula: `floor((score - 10) / 2)`
- Raise costs:
  - `8` to `13`: `1` point per score increase
  - `14` to `15`: `2` points per score increase
  - `16` to `17`: `3` points per score increase
  - `18`: `4` points for the increase from `17`
- Lowering a stat refunds the same amount paid for the most recent increase.

## Player State Model
- Add a player stat model to profile state with:
  - `AcceptedStats`
  - `DraftStats`
  - `AvailableStatPoints`
  - `StatsAccepted`
- `AcceptedStats` are the only stats used for raid gameplay, snapshots, shop calculations, and derived values during a raid.
- `DraftStats` are editable only when the player is not in an active raid.
- `StatsAccepted = false` blocks raid start until the player confirms the current draft.

## Migration And Save Compatibility
- Backfill old saves with:
  - all six accepted stats at `8`
  - all six draft stats at `8`
  - `AvailableStatPoints = 27`
  - `StatsAccepted = false`
- Normalize missing stat data on load so old saves remain valid.
- Keep migration backward compatible and non-destructive.

## Between-Raid Flow
- When `StatsAccepted = false`, the player can raise and lower draft stats within the pool rules.
- `Accept Stats` copies `DraftStats` into `AcceptedStats` and flips `StatsAccepted` to `true`.
- Once accepted, a `Re-Allocate Stats ($5000)` action becomes available between raids only.
- Re-allocation:
  - charges `$5000`
  - resets draft stats to all `8`
  - restores `27` available points
  - sets `StatsAccepted = false`
- Player must accept again before starting another raid.

## Gameplay Mapping
- `STR`
  - Add to the player model now.
  - Expose a derived carry-capacity seam or helper for future encumbrance work.
  - Do not affect hit chance or weapon damage.
- `DEX`
  - Applies to hit bonus.
  - Applies to AC / defense bonus.
  - Preserve existing armor max-dex constraints.
  - Any displayed defense value should reflect the constrained bonus, not the raw modifier.
- `CON`
  - Drives max HP using the existing RaidLoop health rule.
- `INT`
  - Reintroduce weapon malfunction handling.
  - Add a centralized `PreventWeaponMalfunctionDc` constant.
  - Attempt an automatic prevent / clear check using INT modifier before requiring manual recovery.
  - If the automatic check fails, `Fix Malfunction` appears, takes an action, and always succeeds.
- `WIS`
  - Modifies the existing surprise / encounter awareness logic.
  - Higher WIS improves spotting enemies first and resisting enemy ambushes.
  - Keep this compatible with future visibility and suppressor systems.
- `CHA`
  - Modifies shop prices through a shared pricing helper.
  - Unlocks max shop rarity from CHA modifier:
    - `+0`: Common
    - `+1`: Uncommon
    - `+2`: Rare
    - `+3`: Epic
    - `+4`: Legendary

## UI Scope
- Add stat controls to the pre-raid / HUD-adjacent character panel.
- Show:
  - stat name
  - current score
  - modifier
  - up control
  - down control
  - remaining points
- Disable increment when:
  - the stat is `18`
  - the player lacks points for the next increase
- Disable decrement when the stat is `8`.
- Prevent edits during an active raid.
- Keep the interface functional and simple.

## Architectural Seams
- Keep point-buy math in one pluggable rules object or helper.
- Keep derived stat formulas in shared core helpers instead of scattering them across UI and action handlers.
- Keep profile normalization separate from UI state so older saves and API projections remain stable.

## Testing Scope
- Add tests for:
  - stat defaults, min/max, spending, refunds, accept flow, and reallocation
  - DEX hit and armor-capped defense contribution
  - CON health derivation
  - INT auto-check, malfunction persistence, and manual fix action
  - WIS surprise odds moving in the correct direction
  - CHA price adjustments and rarity thresholds
  - old-save normalization to editable `8/8/8/8/8/8` plus `27` points

## Acceptance Criteria
- Stats exist in player persistence and old saves normalize safely.
- New and migrated players start with all stats at `8`, `27` points, and must accept before raiding.
- Gameplay uses accepted stats only.
- Re-allocation is blocked in raid and available between raids for `$5000`.
- Strength provides a future carry-capacity seam.
- Dexterity affects hit and defense while respecting armor limits.
- Constitution affects HP.
- Intelligence restores malfunction handling and `Fix Malfunction`.
- Wisdom affects surprise and anti-surprise odds.
- Charisma affects shop prices and rarity unlocks from modifier thresholds.
