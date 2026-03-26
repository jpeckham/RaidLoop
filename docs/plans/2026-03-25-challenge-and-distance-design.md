# Challenge And Distance Raid Loop Design

**Date:** 2026-03-25
**Input:** Replace HUD `Extract Progress` with a clearer raid-state model built around `Challenge` and `Distance from Extract`.

## Problem
- The current raid HUD shows `Extract Progress: X / 3`, but backend state allows the value to continue growing past `3`.
- That creates a mismatch between player expectation and actual raid rules.
- The current `Continue Searching` wording also hides the real tradeoff between going farther into the raid and moving back toward safety.

## Goals
- Replace the ambiguous extraction-progress mechanic with explicit, player-readable state.
- Let low-gear players extract immediately from a low-risk run if they return to extract quickly.
- Preserve risk escalation for players who linger, push deeper, or keep farming encounters.
- Keep the implementation small enough to ship before tying loot and enemy scaling to the new pressure stat.

## Core Model
- `Distance from Extract` is a non-negative integer number of moves needed to reach extract.
- `Challenge` is a non-negative integer pressure rating for the current raid.
- Raid starts at `Distance from Extract = 0` and `Challenge = 0`.
- `Go Deeper` increases `Distance from Extract` by `1` and `Challenge` by `1`.
- `Move Toward Extract` decreases `Distance from Extract` by `1`, with a floor of `0`.
- `Attempt Extraction` is available whenever `Distance from Extract = 0`.
- `Stay at Extract` keeps `Distance from Extract = 0` and increases `Challenge` by `1`.

## Encounter And Drift Rules
- Existing encounter categories can stay: combat, loot, extraction-state decisions, and neutral/clear areas.
- The shared travel wording `Continue Searching` should be removed in favor of explicit movement verbs.
- After resolving an encounter, there is a rare drift event that increases `Distance from Extract` by `1`.
- Drift can happen after encounters generated while the player chose `Stay at Extract`, so a player who was at extract may end up `1` move away afterward.
- Initial tuning should use a simple fixed drift chance in the `5%` to `10%` range. Start at `10%` for feel-testing, then tune if needed.
- Drift should be surfaced in the raid log with explicit messaging so the movement feels rule-driven rather than random punishment.

## UI And Player-Facing Language
- The raid HUD should show both `Challenge` and `Distance from Extract`.
- Replace `Extract Progress` display entirely.
- When `Distance from Extract > 0`, the main travel actions should be:
  - `Go Deeper`
  - `Move Toward Extract`
- When `Distance from Extract = 0`, the main travel actions should be:
  - `Attempt Extraction`
  - `Stay at Extract`
- Utility actions such as `Reload` and `Use Medkit` remain separate from travel-state actions.

## Why `Challenge` Instead Of `Depth`
- `Depth` initially sounded spatial, but the approved rules allow pressure to increase even while remaining at extract.
- `Challenge` better matches the intended semantics because it can rise from greed, stalling, or continued exposure.

## Out Of Scope For This Change
- Scaling enemy stats from `Challenge`
- Scaling loot quality from `Challenge`
- Reworking encounter tables beyond the minimum needed to support the new action flow
- Full map or node-based navigation

## Acceptance Criteria
- The HUD shows `Challenge` and `Distance from Extract`.
- The player no longer sees `Extract Progress`.
- `Attempt Extraction` is deterministic at `Distance from Extract = 0`.
- `Stay at Extract` is available at extract and increases `Challenge`.
- `Go Deeper` and `Move Toward Extract` replace ambiguous travel wording away from extract.
- A rare post-encounter drift event can move the player from extract to `Distance from Extract = 1`.
