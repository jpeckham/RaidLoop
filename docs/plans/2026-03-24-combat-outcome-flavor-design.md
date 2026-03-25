# Combat Outcome Flavor Design

**Problem**

Combat logs currently flatten several distinct outcomes into simple hit-or-miss text. That loses useful flavor and feedback, especially now that combat already models d20 hit rolls, armor penetration, and armor damage reduction.

**Goals**

- Distinguish four combat outcomes in the authoritative combat log for both player and enemy attacks.
- Keep dodge/evasion separate from armor/plate protection.
- Preserve existing damage and armor DR behavior on successful hits.
- Add the new flavor without requiring client-side combat rendering changes.

**Non-Goals**

- No combat UI redesign beyond consuming the updated log text.
- No changes to weapon balance, armor DR values, or encounter pacing.
- No new player-facing stat screens in this increment.

**Approved Outcome Model**

- `attack total < 10`: miss
- `attack total >= 10` and `< 10 + dodge bonus`: evaded
- `attack total >= 10 + dodge bonus` and `< 10 + dodge bonus + armor bonus`: absorbed by armor
- `attack total >= 10 + dodge bonus + armor bonus`: hit, then apply armor DR to damage

For now, `dodge bonus` is the defender's DEX modifier. Later features can extend that same threshold with feat-based dodge bonuses without changing the outcome model. `armor bonus` is distinct from DR and represents the plate/coverage threshold an attack must beat to turn contact into a penetrating hit.

**Approach**

Keep the change in the authoritative Supabase SQL combat path. Add reusable helpers so the combat branches can classify an attack result once, build the correct flavor text once, and keep the player and enemy log wording aligned.

Because the current backend models only armor DR and armor penetration, add a separate armor bonus value for armor items and expose it through a helper similar to the existing DR lookup. On penetrating hits, continue to apply armor penetration against DR only unless implementation review shows the plate bonus should also partially interact with penetration.

**Data Model**

- Add a separate per-armor `armor_bonus` value in the authored item definition path.
- Add a SQL helper for resolving armor bonus from equipped armor.
- Keep armor DR as the post-hit damage reduction layer.

**Combat Log Intent**

- Miss: the attacker never really threatened the target.
- Evaded: the attack was on line, but the target moved clear.
- Absorbed by armor: the attack reached the target but was stopped by plates.
- Hit: the attack penetrated and dealt damage, with any absorbed DR called out.

**Testing Strategy**

- Migration content tests that pin the new armor-bonus helper and the new combat text fragments.
- Deterministic or migration-level checks that player and enemy branches both use the same outcome bands.
- Regression coverage that hit outcomes still apply DR and preserve current raid-state mutations.
