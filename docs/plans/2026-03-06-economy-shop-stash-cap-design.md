# Economy, Shopkeeper, and Stash Cap Design

**Date:** 2026-03-06
**Status:** Approved
**Source:** User-approved brainstorming decisions

## Goals
- Track money in persistent save data.
- Let players sell specific items from stash and character inventory.
- Add a shopkeeper with basic supplies and one light weapon.
- Enforce a stash cap of 30 items with explicit UI state.

## Decisions
- No new return box/inbox area.
- Use the existing right-side equipped/inventory area as **Character Inventory**.
- After successful extraction, raid returns go to Character Inventory.
- Players decide item-by-item whether to stash or sell.
- `Stash` action is disabled when stash is full (30/30).

## Economy Rules
- Save model gains `Money`.
- Fixed price table for shop items and sell values.
- `Sell` removes item and increments money.
- Shop purchases deduct money and add item to Character Inventory.

## Inventory Rules
- Stash has hard cap: 30 items.
- Character Inventory has no cap in MVP.
- `Move to Stash` only allowed when `stash.Count < 30`.
- Stash list supports sell action per item.
- Character Inventory list supports stash/sell actions per item.

## Shopkeeper MVP Stock
- `Bandage`
- `Medkit`
- `Ammo Box`
- `Light Pistol` (light weapon)

## Raid Integration
- Successful extract places returned items in Character Inventory.
- Death-loss raid behavior remains unchanged.
- Existing random-character flow remains, with no economy expansion in this iteration beyond main character economy UI.

## UI/UX
- Show money and stash usage (`X/30`) in base UI.
- Disable `Stash` buttons when stash is full.
- Disable `Buy` when money is insufficient.
- Keep action feedback in status/log area.

## Testing Plan
- Verify stash cap enforcement and disabled states.
- Verify sell updates money and removes item.
- Verify buy updates money and adds item.
- Verify extraction routes items to Character Inventory.
- Run full project tests and release build.
