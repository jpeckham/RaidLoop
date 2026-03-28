# D20 Encumbrance Design

**Date:** 2026-03-27

## Goal

Replace the current linear strength-based encumbrance cap with D20 medium/heavy load thresholds and apply only the penalties that map to existing game systems: effective Dexterity cap and attack-roll penalty.

## Current State

- The client and core logic use a single `GetMaxEncumbranceFromStrength` formula.
- Supabase migrations define a matching `game.max_encumbrance(strength int)` formula.
- Raid combat already uses Dexterity for attack and defense, and attack resolution is authoritative on the backend.
- Raid movement exists, but the approved scope excludes speed or movement penalties.

## Approved Scope

Use the D20 load table for strength scores.

- `Light`: no combat penalty.
- `Medium`: cap effective Dexterity modifier at `+3`; apply `-3` attack penalty.
- `Heavy`: cap effective Dexterity modifier at `+1`; apply `-6` attack penalty.

Out of scope for this change:

- Movement speed adjustments
- Skill penalties
- Run multiplier changes
- New stamina or action economy systems

## Design

### Shared Rules

Add a shared core representation for encumbrance tiers and D20 thresholds so tests and UI-facing code can reason about the same values:

- Strength score -> light/medium/heavy breakpoints
- Current carried weight -> `Light`, `Medium`, or `Heavy`
- Tier -> effective max Dexterity modifier and attack penalty

The shared core rules will remain the source for client-side display and local logic, but backend SQL must mirror the same thresholds because combat is resolved in Supabase.

### Backend Rules

Add a new migration that redefines the encumbrance functions with D20 table semantics:

- `game.max_encumbrance(strength int)` should represent the heavy-load cap.
- Add or redefine helper logic to derive the load tier from current encumbrance and strength.
- Apply the encumbrance-derived Dexterity cap and attack-roll penalty where player combat rolls are computed.
- Preserve the existing raw `encumbrance` projection so current UI can still show carried weight.
- Add enough projected metadata for the client to display the tier cleanly if needed.

### Client Rules

Update the client/core encumbrance helpers to match the D20 table:

- Replace the linear cap formula with the heavy-load threshold table.
- Add tier classification helpers and penalty helpers.
- Use effective Dexterity rules anywhere the client computes combat-facing numbers from Dex.

No movement logic changes will be added.

### UI

The UI may show the derived tier and the heavy-load ceiling, but should not invent new movement warnings. Existing overweight gating should continue to use the heavy-load cap.

## Testing Strategy

Use TDD with representative threshold tests:

- Strength 8, 10, 14, and 18 boundary values
- Light-to-medium transition
- Medium-to-heavy transition
- Effective Dexterity cap for medium and heavy
- Attack penalty application for medium and heavy
- Migration text regression tests for the D20 lookup and combat penalty usage

## Risks

- The backend and client can drift if table values are duplicated incorrectly.
- Existing tests that assume the linear formula will need to be updated carefully.
- Some current UI text uses "max encumbrance" as a single cap; that should remain the heavy-load ceiling to avoid broader UI churn.
