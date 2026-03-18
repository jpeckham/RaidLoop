# Inventory Row Icons Design

## Goal

Fix the item-row layout regressions introduced by type labels and redundant value text by:
- replacing visible type labels with icons
- removing standalone value text from storage-style rows
- aligning item rows into consistent columns so names and actions do not appear jagged

## Scope

Apply the icon-column layout anywhere item type text currently causes horizontal misalignment:
- `For Raid`
- `Storage`
- `Luck Run` results
- in-raid item rows that currently show item type text

## Design

- Add a small icon library to the client.
- Create a shared visual row structure:
  - fixed icon column
  - flexible item-name column
  - trailing action block
- Keep rarity color on the item name only.
- Use neutral icons for item type:
  - weapon
  - armor
  - backpack
  - consumable
  - sell/material
- Remove the standalone item value text from the `Storage` panel because the sell button already shows the value.

## Constraints

- Do not change the action flow or semantics.
- Preserve the existing rarity-color behavior.
- Keep the layout stable on narrow screens.
