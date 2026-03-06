# RaidLoop Blazor Web MVP Design

**Date:** 2026-03-06
**Input:** `prd.md`

## Scope
- Blazor WebAssembly client-side app (PWA)
- Event-driven extraction raid loop
- Local stash persistence in browser storage
- GitHub Actions deploy to GitHub Pages

## Architecture
- `src/RaidLoop.Core`: pure game rules (raid lifecycle, loot limits, death/extract outcomes)
- `tests/RaidLoop.Core.Tests`: rule tests
- `src/RaidLoop.Client`: Blazor UI shell + encounter loop + PWA assets
- `.github/workflows/deploy-pages.yml`: build, test, publish, deploy

## Gameplay in MVP
- Base loadout selection from stash
- Raid encounters: combat, loot, neutral, extraction opportunity
- Turn actions: attack, skill, item, reload, flee
- Continue vs move-to-extract decisions
- Permadeath raid loss and stash transfer on extraction
