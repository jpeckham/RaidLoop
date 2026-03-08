# RaidLoop

<!-- badges:start -->
## Status Badges

[![Continuous Delivery](https://github.com/jpeckham/RaidLoop/actions/workflows/continuous-delivery-dotnet-blazor-github-pages.yml/badge.svg)](https://github.com/jpeckham/RaidLoop/actions/workflows/continuous-delivery-dotnet-blazor-github-pages.yml)
[![Coverage](https://raw.githubusercontent.com/jpeckham/RaidLoop/main/.github/badges/coverage.svg)](https://github.com/jpeckham/RaidLoop/actions/workflows/continuous-delivery-dotnet-blazor-github-pages.yml)
<!-- badges:end -->

RaidLoop is a lightweight extraction-shooter loop built with Blazor WebAssembly.
You prepare gear, run raids, fight or loot, and try to extract with profit.

## How To Play

### 1) Prepare
- Move items between `Storage` and `For Raid`.
- Equip weapon/armor/backpack before entering raid.
- Buy essentials from the shopkeeper.

### 2) Raid
- Encounters are combat, loot, extraction opportunities, or clear areas.
- During loot encounters, use:
  - `Discovered Loot` (what you found)
  - `Character Inventory` (equipped + carried)
- You can loot, equip, and drop items directly in raid.
- Medkits can be used at any time during raid.

### 3) Extract Or Lose It
- If you die, your raid kit is lost.
- If you extract, equipped + carried loot returns to your persistent inventory.

### 4) Luck Run
- Luck Run has a cooldown.
- When ready, enter immediately to generate a surprise random kit.
- After a successful Luck Run, process returned loot (`Store`, `For Raid`, `Sell`) before new raids.

## Developer Guide

### Prerequisites
- .NET SDK 10.x

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
