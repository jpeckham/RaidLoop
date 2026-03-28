# Respec Half Cash Design

**Date:** 2026-03-27

## Goal

Change stat reallocation from a fixed `$5000` fee to `50%` of the player's current cash on hand, rounded to the nearest whole dollar.

## Current State

- The client enables reallocation only when `_money >= 5000`.
- The button label hardcodes `Re-Allocate ($5000)`.
- The backend `reallocate-stats` branch subtracts a fixed `5000`.

## Approved Rule

- Respec cost is `round(money / 2.0)` using normal nearest-whole-dollar rounding.
- The same rule applies on the client and backend.
- Respec remains unavailable while in raid and still requires already accepted stats.

## Design

### Client

- Add a helper in `Home.razor.cs` that computes the current respec cost from `_money`.
- Replace the fixed affordability checks with `GetReallocateStatCost()`.
- Update the button text in `Home.razor` to show the live cost.

### Backend

- Add a migration that redefines `game.apply_profile_action`.
- In the `reallocate-stats` branch, compute `respec_cost := round(current_money / 2.0)`.
- Use that value for both the affordability guard and the deduction.

### Testing

- Update existing client tests that currently assert the fixed `$5000` gate and label.
- Add migration binding assertions for the new dynamic cost logic.
