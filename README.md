# RaidLoop

<!-- badges:start -->
## Status Badges

[![Continuous Delivery](https://github.com/jpeckham/RaidLoop/actions/workflows/continuous-delivery-dotnet-blazor-github-pages.yml/badge.svg)](https://github.com/jpeckham/RaidLoop/actions/workflows/continuous-delivery-dotnet-blazor-github-pages.yml)
[![Coverage](.github/badges/coverage.svg)](https://jpeckham.github.io/RaidLoop/coverage/)
<!-- badges:end -->

RaidLoop is a lightweight extraction-shooter loop built with Blazor WebAssembly.
You prepare gear, run raids, fight or loot, and try to extract with profit.

## Current Gameplay Features

- Authored item catalog with explicit value, gameplay loot tier, and display rarity.
- Separate display rarity colors:
  - `SellOnly` = gray
  - `Common` = white
  - `Uncommon` = green
  - `Rare` = blue
  - `Epic` = yellow
  - `Legendary` = orange
- Tiered loot generation for containers and enemy loadouts, with support for rarity-tier boosters.
- Item pricing based on usefulness:
  - stronger weapons sell for more
  - higher-reduction armor sells for more
  - larger backpacks sell for more
- Luck Runs with cooldown, random starter kit generation, and post-run loot processing.
- Luck Run starter kits are intentionally capped below `Epic` and `Legendary`.

## How To Play

### 1) Prepare
- Move items between `Storage` and `For Raid`.
- Equip weapon/armor/backpack before entering raid.
- Buy essentials from the shopkeeper.
- Storage and raid-prep rows use item-type icons instead of text labels.

### 2) Raid
- Encounters are combat, loot, extraction opportunities, or clear areas.
- Raid HUD now tracks two explicit raid-state numbers: `Challenge` and `Distance from Extract`.
- When you reach extract, you can `Attempt Extraction` or `Stay at Extract`.
- Away from extract, the main travel choices are `Go Deeper` and `Move Toward Extract`.
- During loot encounters, use:
  - `Discovered Loot` (what you found)
  - `Character Inventory` (equipped + carried)
- You can loot, equip, and drop items directly in raid.
- On discovered loot, `Loot` stays on the far right and `Equip` appears to its left when available.
- Medkits can be used at any time during raid.

### 3) Extract Or Lose It
- If you die, your raid kit is lost.
- If you extract, equipped + carried loot returns to your persistent inventory.
- There is no separate extraction settlement screen; extracted items simply return to your inventory state.

### 4) Luck Run
- Luck Run has a cooldown.
- When ready, enter immediately to generate a surprise random kit.
- After a successful Luck Run, process returned loot (`Store`, `For Raid`, `Sell`) before new raids.

## Item Tiers And Examples

### Starter / Lower Tier
- `Makarov`
- `PPSH`
- `AK74`
- `6B2 body armor`
- `6B13 assault armor`
- `Small Backpack`
- `Tactical Backpack`

### Epic
- `SVDS`
- `FORT Defender-2`
- `Tasmanian Tiger Trooper 35`

### Legendary
- `AK47`
- `PKP`
- `6B43 Zabralo-Sh body armor`
- `NFM THOR`
- `6Sh118`

### Sell-Only Items
- `Bandage`
- `Ammo Box`
- `Scrap Metal`
- `Legendary Trigger Group`

## Developer Guide

### Prerequisites
- .NET SDK 10.x
- Docker Desktop
- Node.js with local npm dependencies installed via `npm install`

### Run Locally
```bash
dotnet restore RaidLoop.sln
dotnet run --project src/RaidLoop.Client/RaidLoop.Client.csproj
```

### Test
```bash
dotnet test RaidLoop.sln
```

### Build
```bash
dotnet build RaidLoop.sln
```

### VS Code Debug
- Use F5 with `RaidLoop Client (http profile)` in `.vscode/launch.json`.
- Browser auto-opens when Kestrel is ready.

## CI / Coverage / Deploy

- `Continuous Delivery` workflow runs quality checks, coverage, Pages deploy, and release tagging in one pipeline.
- Coverage reports are uploaded as workflow artifacts.

## Contributing

### Local Environment

- Local development uses `ASPNETCORE_ENVIRONMENT=Local` from `src/RaidLoop.Client/Properties/launchSettings.json`.
- `src/RaidLoop.Client/wwwroot/appsettings.Local.json` points the client at local Supabase on `http://127.0.0.1:54321`.
- `src/RaidLoop.Client/wwwroot/appsettings.Test.json` is the hosted Supabase smoke-test configuration.
- Keep hosted Supabase for later smoke testing. Day-to-day development should use the local Supabase CLI stack.

### Local Supabase Workflow

From the repo root:

```bash
. .\env.local.ps1
npm install
npx supabase start
npx supabase db reset
```

Use `npx supabase db reset` whenever you need a clean local database rebuilt from migrations. Use `npx supabase db push --include-all` only for quick iteration when you do not need a full reset.
`env.local.ps1` only loads local-safe values from `.env` and refuses hosted Supabase refs, hosted URLs, and remote deploy credentials.

### TDD Workflow

1. Start from the smallest relevant failing test.
2. write the smallest failing test first.
3. Run the narrowest command that proves it fails.
4. Make the minimum code or migration change to pass.
5. Re-run the same narrow test until it is green.
6. Run local integration checks against the local Supabase stack before committing.

Typical narrow commands:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter NameOfTest
deno test supabase/functions/game-action/handler.test.mjs
```

For SQL or migration work, prefer:

```bash
npx supabase db reset
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|RaidActionApiTests|ProfileMutationFlowTests"
```

### Pre-Push Verification

Before pushing commits, run:

```bash
npx supabase db reset
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "HomeMarkupBindingTests|RaidActionApiTests|ProfileMutationFlowTests"
dotnet test RaidLoop.sln
```

If you changed Edge Functions, also run:

```bash
deno test supabase/functions/profile-bootstrap/handler.test.mjs supabase/functions/profile-save/handler.test.mjs
deno test supabase/functions/game-action/handler.test.mjs
```

Push only after local verification is green. Use the hosted Supabase environment after that for smoke testing of integration details such as auth-provider behavior.
