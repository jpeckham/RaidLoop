# Extract Hold Balance Design

**Date:** 2026-03-28

## Goal

Preserve the extract-camping fantasy as a tense interception playstyle while removing the deterministic high-tier loot exploit from repeated `Stay at Extract` actions.

## Problem

- The current extract loop lets players remain at extract and repeatedly increase `challenge`.
- Recent challenge-scaled encounter/loadout tuning makes higher-pressure extract encounters too reliable a source of strong kits.
- That creates a degenerate strategy where the optimal loot path is to camp extract until enemies with premium gear appear.
- The current system also over-couples raid pressure with guaranteed equipment quality.

## Approved Direction

- Keep extract camping as a supported playstyle.
- Make waiting at extract a committed, risky action rather than an instant farming button.
- Decouple `challenge` from fixed gear bands.
- Let `challenge` primarily increase enemy stats, tactical danger, and the odds of better gear, not guarantee better gear.
- Preserve the value of high `Wisdom` by making careful players less vulnerable to being surprised by extract campers.

## Core Rules

- `Attempt Extraction` remains immediately available at `Distance from Extract = 0`.
- `Stay at Extract` is replaced by a timed hold action lasting 30 seconds.
- Holding at extract should feel tense and exposed, not predictably profitable.
- When the timer resolves, the backend rolls an extract-hold outcome from a dedicated encounter family.
- Possible hold outcomes include:
  - quiet window
  - suspicious movement or neutral tension
  - extract-camper or hunter combat
  - occasional modest loot opportunity
- The expected value of holding should lean toward risk with occasional payoff.

## Challenge And Gear Decoupling

- `Challenge` no longer maps directly to a fixed enemy loadout table or guaranteed equipment band.
- `Challenge` instead affects:
  - enemy stat scaling
  - tactical/surprise behavior odds
  - a modest rarity bias applied to enemy gear rolls
- Enemy gear is rolled from weighted loadout tables that remain probabilistic at every challenge level.
- Very strong kits, including double-legendary outcomes, stay possible but rare.
- High challenge should produce scarier opponents more often without turning them into guaranteed jackpot enemies.

## Wisdom And Surprise

- High `Wisdom` reduces the chance that an extract-hold hunter encounter begins with an enemy ambush.
- Higher `Wisdom` should more often produce:
  - mutual contact
  - player notices movement first
- This supports the intended fantasy that a smart, careful, optics-using player can deliberately clear the extract area.
- Higher `Challenge` can still increase the danger and sophistication of hunters, but should not nullify the value of `Wisdom`.

## Implementation Shape

- The backend owns hold timing, hold resolution, and extract-hold outcome generation.
- The client shows the hold countdown and disables conflicting repositioning actions during the hold.
- The backend adds a dedicated extract-hold action instead of reusing the current instant `stay-at-extract` semantics.
- The backend adds a dedicated extract-hold encounter family so tuning can diverge from normal travel and extract-approach tables.
- Enemy stat scaling remains challenge-aware.
- Enemy equipment generation changes from fixed challenge-band selection to weighted loadout rolls with rarity bias.
- Extract-hold combat outcomes use a wisdom-aware surprise resolver.

## Failure Handling

- Cancelling or interrupting a hold returns the player to the extract decision state without granting a free outcome roll.
- If the backend receives an out-of-date hold-resolution request, it should reject or ignore it safely rather than double-resolve.
- If extract-hold authored data is missing, the system should fall back to a neutral extract state instead of generating invalid combat.

## Testing

- Add backend tests proving repeated extract holds do not guarantee high-tier enemy kits.
- Add probability-shape tests showing higher challenge raises stats and rarity odds while still producing mixed loadouts over many rolls.
- Add backend tests showing higher `Wisdom` reduces enemy-ambush openings during extract-hold encounters.
- Add client or state tests proving the 30-second hold disables conflicting actions and resolves back into a valid raid state.
