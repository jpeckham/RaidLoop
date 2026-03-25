# Surprise And Initiative Design

**Problem**

Combat encounters currently begin as a flat contact state. The game has no concept of one side spotting the other first, no opening-phase advantage, and no forward-compatible hook for future visibility and stealth gear such as night vision, flashlights, or suppressors.

**Goals**

- Add a lightweight d20-inspired `surprise` concept that fits the current encounter-resolution model.
- Add a lightweight `initiative` concept that determines opening control when neither side is surprised.
- Keep the first increment context-driven from authored encounter events rather than from full character stealth simulation.
- Create data and rule seams for later modifiers from environment, time of day, and gear.
- Keep player-facing text simple and readable.

**Non-Goals**

- No full tactical turn-order combat system.
- No per-combatant round loop rewrite.
- No first-pass implementation of night, visibility ranges, sound propagation, or suppressor gear logic.
- No new permanent stat screen for awareness or initiative values in this increment.

**Approved Model**

Combat starts with an authored `opening phase` before normal combat resolution:

- Encounter context chooses a combat family.
- A weighted authored combat row inside that family supplies the exact `contact state`, title, and description.
- `Contact state` determines whether either side begins with `surprise`.
- If one side has surprise, that side gets the opening action window.
- If neither side has surprise, both sides resolve `initiative` and the winner gets opening control.
- After the opening phase is consumed, combat falls back to the normal combat flow.

This keeps the d20 feel of ambush and quick-draw advantage without requiring a full initiative ladder across the entire fight.

The first implemented authored families are:

- `default_raid_travel`
- `loot_interruption`
- `extract_approach`

Each family uses an effectively even split across:

- `PlayerAmbush` `33%`
- `EnemyAmbush` `33%`
- `MutualContact` `34%`

**Contact States**

The implemented contact states are:

- `MutualContact`
- `PlayerAmbush`
- `EnemyAmbush`

These are authored on combat encounter rows in the SQL encounter tables. The backend resolver converts them into opening-phase effects.

**Opening Phase Rules**

- `PlayerAmbush`: player surprise, `openingActionsRemaining = 1`, `initiativeWinner = None`
- `EnemyAmbush`: enemy surprise, `openingActionsRemaining = 1`, `initiativeWinner = None`
- `MutualContact`: no surprise, roll initiative immediately, `openingActionsRemaining = 0`
- Surprise normally expires after the opening action window is consumed.
- Initiative happens after surprise, not instead of surprise.

For the first increment, `opening control` means the acting side resolves the first attack opportunity before the normal exchange proceeds. `MutualContact` is the only state that rolls initiative immediately at encounter creation.

**Future Extension Hooks**

The design should preserve explicit fields for future awareness math:

- `environment modifiers`: night, indoors, open ground, weather
- `player gear modifiers`: flashlight, night vision, suppressor
- `enemy modifiers`: sentry posture, alertness, cover, optics

Future suppressor behavior should use `surprise persistence` instead of guaranteed extra free turns:

- a suppressed opening attack can require a localization check
- failed localization allows one additional reduced-strength surprise action or preserves concealment briefly

This avoids hard-coding “suppressor equals extra round” while supporting the intended feel.

**Data Model Direction**

The raid/combat payload should gain a small opening-phase projection rather than overloading encounter description text alone. Combat encounter rows should also carry an authored `contact_state`, and the core resolver should accept a structured opening-phase context so future modifier values can be carried without changing the method shape again. A minimal future-proof shape is:

- `ContactState`
- `SurpriseSide`
- `InitiativeWinner`
- `OpeningActionsRemaining`
- `SurprisePersistenceEligible`
- `OpeningPhaseContext` with modifier slots for visibility, environment, player gear, and enemy localization

The first implementation can populate only the fields needed for the new behavior while keeping room for later expansion.

**Player-Facing Presentation**

Keep the wording readable and low-math:

- `You spotted them first.`
- `They ambushed you.`
- `You won initiative.`
- `Your suppressor kept you concealed.`

The UI should communicate the opening state as encounter flavor and log text, not as a dense tactical panel.

**Approach**

Implement the mechanic as an opening-phase layer around existing combat resolution. Context selects an authored combat family, the backend chooses a weighted encounter row from that family, then the resolver turns its `contact_state` into surprise or initiative state and continues through the current combat path. Do not introduce full encounter-round sequencing in this increment.

**Testing Strategy**

- Contract tests for any new raid snapshot fields needed to project opening-phase state.
- Projection and page-state tests that confirm new payload fields are consumed correctly by the client.
- Resolver tests for contact-state to surprise/initiative behavior.
- Regression coverage that combat, loot, extraction, and existing encounter descriptions still work when opening-phase data is absent.
