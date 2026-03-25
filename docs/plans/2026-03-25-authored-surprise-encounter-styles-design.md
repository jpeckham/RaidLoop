# Authored Surprise Encounter Styles Design

**Problem**

The current authoritative raid generator creates combat encounters with generic contact text and no authored awareness state. Live play therefore produces no meaningful `surprise` or `initiative` variation even though the client and projection layers can now carry opening-phase fields.

**Goals**

- Make live raids produce backend-authoritative surprise and initiative states.
- Add authored encounter styles that feel like extraction-shooter contact situations rather than generic combat triggers.
- Keep the first increment simple and data-driven.
- Preserve a clean path for future visibility, night, suppressors, flashlights, and NVG modifiers.

**Non-Goals**

- No full perception simulation.
- No night or sound system in this increment.
- No per-round tactical initiative ladder.
- No gear-based awareness modifiers enabled yet.

**Approved Encounter Model**

Use a hybrid authored-family model:

- Context chooses the combat family.
- An authored weighted row within that family supplies the exact contact style.
- The selected row writes opening-phase fields directly into `activeRaid`.

The families for the first increment are:

- `default_raid_travel`
- `loot_interruption`
- `extract_approach`

Each family should contain an even split of:

- `PlayerAmbush`
- `EnemyAmbush`
- `MutualContact`

First-pass weighting should be effectively even inside each family:

- `PlayerAmbush` `33%`
- `EnemyAmbush` `33%`
- `MutualContact` `34%`

**Authoring Model**

Each authored combat encounter row should carry:

- `table_key` for the family
- `enemy_name`
- enemy health range
- enemy loadout table
- authored `title`
- authored `description`
- `contact_state`

The description should explain circumstance rather than pretend there is already full stealth simulation. Examples:

- `You spot an enemy camp before they see you.`
- `You hear movement while looting and catch them before they spot you.`
- `You and a patrol notice each other at nearly the same moment.`
- `You are ambushed on the way to extract.`

**Opening-Phase Rules**

- `PlayerAmbush`
  - `surpriseSide = Player`
  - `initiativeWinner = None`
  - `openingActionsRemaining = 1`

- `EnemyAmbush`
  - `surpriseSide = Enemy`
  - `initiativeWinner = None`
  - `openingActionsRemaining = 1`

- `MutualContact`
  - `surpriseSide = None`
  - roll initiative immediately at encounter creation
  - store `initiativeWinner = Player` or `Enemy`
  - `openingActionsRemaining = 0`

Initiative happens after surprise, not instead of surprise. Only `MutualContact` rolls initiative immediately in this increment.

**Authoritative Backend Flow**

1. The SQL encounter generator determines combat family from context:
   - normal movement => `default_raid_travel`
   - transition from or interruption around loot => `loot_interruption`
   - moving toward extract => `extract_approach`
2. It selects a weighted authored combat row from that family.
3. It writes encounter title, description, enemy data, and `contact_state` into `activeRaid`.
4. It derives opening-phase fields from that `contact_state`.
5. For `MutualContact`, it rolls initiative immediately and stores the winner.

**Testing Strategy**

- Migration-content tests for the new schema and authored encounter rows.
- SQL/handler projection tests that verify opening-phase fields are emitted with correct defaults.
- Raid start and raid action tests that verify the client preserves backend-provided values.
- Regression coverage that loot, neutral, and extraction outcomes still clear opening-phase state appropriately.
