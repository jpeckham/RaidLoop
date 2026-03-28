# Challenge Zero Combat And Loot Design

**Goal:** Make the straight-to-extract `challenge 0` path feel active and worthwhile without leaking higher-tier rewards or overtuned enemies.

**Problem:**
- Current `default_raid_travel` and `extract_approach` weights skew too far toward quiet/neutral outcomes.
- Current travel/extract loot can still pull higher-tier items from generic container tables.
- Current enemy combat on these paths still points at generic loadout tables, which mixes power bands too loosely.

**Approved Design:**
- `challenge 0` straight-to-extract should have more fights and more low-tier loot than it does now.
- `challenge 0` enemy fights use only common guns and no armor.
- `challenge 0` loot is capped to gray/white, with some common backpacks mixed in.
- Enemy drops remain their worn/carried gear, so enemy power and enemy loot stay coupled.
- Enemy stat lines should use the same 27-point-buy system players use.

**Challenge Progression:**
- `0`: common guns only, no armor
- `1`: common armor starts appearing
- `2`: uncommon enemy gear starts appearing
- `3`: rare enemy gear starts appearing
- `4`: epic enemy gear starts appearing
- `5`: legendary enemy gear starts appearing

**Architecture:**
- Author challenge-tiered enemy loadout tables instead of relying on one generic pool.
- Author challenge-tiered enemy stat profiles using legal 27-point-buy allocations.
- Repoint travel/extract challenge generation at tier-appropriate enemy loadout/stat tables.
- Add challenge-0-safe loot tables for travel/extract cache encounters.
- Rebalance `default_raid_travel` and `extract_approach` weights so combat is more common than neutral, while loot remains meaningful.

**Target Feel For Challenge 0 Straight-To-Extract:**
- Travel: combat most common, loot second, neutral least common
- Extract approach: combat most common, loot second, neutral least common
- Loot quality: white/gray only
- Combat reward: common gun drops and occasional common backpacks, no armor

**Testing Strategy:**
- Bind migration tests to the new challenge-tier enemy tables, loadout contents, and challenge-0 loot restrictions.
- Verify travel/extract tables now bias toward combat over neutral.
- Verify challenge `0` combat entries point to no-armor common-gun enemy tables.
- Verify challenge `0` loot tables exclude uncommon+ items.
