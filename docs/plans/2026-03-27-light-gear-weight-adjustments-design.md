# Light Gear Weight Adjustments Design

**Goal:** Reduce the weight burden of common starter gear by making Medkits lighter, reducing the Makarov and Small Backpack weights, and slightly increasing 6B2 body armor to reflect its current role.

**Scope:** Current-authority only. Update the shared item catalog used by the client/tests and add a forward Supabase migration that updates `game.item_defs` for the same four items. Do not rewrite historical snapshot migrations.

**Design:**
- Set `Medkit` to `1 lb`.
- Set `Makarov` to `2 lb`.
- Set `6B2 body armor` to `9 lb`.
- Set `Small Backpack` to `1 lb`.
- Keep the integer-weight model intact.

**Testing:**
- Update canonical item-weight assertions in `ItemCatalogTests`.
- Update an encumbrance total assertion so medkit weight changes are exercised behaviorally.
- Add a migration-binding test that pins the new SQL weight updates.
