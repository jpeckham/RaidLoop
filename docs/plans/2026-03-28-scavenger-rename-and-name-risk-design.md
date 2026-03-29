# Scavenger Rename And Name Risk Design

**Goal:** Remove the old enemy shorthand by renaming it to `Scavenger` everywhere, and produce a conservative repo-wide sweep of names that look legally risky because they match branded products or strongly associated game terminology.

## Scope

- Replace authored uses of the old shorthand across game SQL, tests, docs, and support files with `Scavenger` and `scavenger`.
- Exclude unrelated third-party/vendor code from rename work.
- Scan the full repo for exact authored names that look borrowed from active brands, product lines, or genre-defining competitor terminology.

## Rename Rules

- Rename whole-word authored occurrences only.
- Preserve capitalization when applying the new name.
- Keep behavior unchanged; this is a content rename, not a mechanics change.

## Risk Sweep Rules

- Review all repo text, including docs and tests, because the request covers internal and external names.
- Classify suspicious names conservatively:
  - high-risk: exact branded product or strongly game-associated term
  - medium-risk: real-world model/designation with weaker branding exposure
  - low-risk: generic descriptive term
- Treat findings as product-risk guidance, not legal advice.

## Verification

- Run repo-wide searches to confirm no authored old-shorthand tokens remain.
- Run targeted automated tests covering renamed encounter text and payload expectations.
- Report the flagged-name shortlist with replacement direction for later cleanup.
