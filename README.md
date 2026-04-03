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

### Item Identity

- `game.item_defs.item_def_id` is the database surrogate key for authored items.
- `itemDefId` is the canonical runtime gameplay and contract identity.
- `game.item_defs.item_key` is an internal authored lookup key, not the runtime contract identity.
- Player-facing labels are client-owned display data and should not be used as server-side identity.
- Bootstrap returns the full non-localized item rules catalog once, keyed by `itemDefId`.
- Runtime action payloads stay lean and should not repeat the rules catalog or server-authored item labels.
- Localization should resolve labels from client-owned assets keyed by `itemDefId`, not from server-authored English names.
- Payload identity changes must use forward-only migrations. Do not rewrite already-applied migrations to rename or re-key persisted item data.

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

### Edge Functions In Local Supabase

Local Edge Functions are served from the checked-out files in `supabase/functions/`. They are not deployed with `supabase functions deploy` during normal local development.

Typical local function workflow:

```bash
. .\env.local.ps1
npx supabase start
npx supabase functions serve
```

Call local functions through the gateway at:

```text
http://127.0.0.1:54321/functions/v1/<function-name>
```

Use the lightest restart that matches the change:

- SQL migrations changed:
  ```bash
  npx supabase db reset
  ```
- Edge Function code changed:
  restart `npx supabase functions serve`
- Both SQL and Edge Functions changed:
  1. `npx supabase db reset`
  2. restart `npx supabase functions serve`

Do not use full `npx supabase stop` / `npx supabase start` for every function edit. Only restart the full stack when the local Supabase services themselves are unhealthy.

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

### SQL Testing

For SQL changes, do not stop at static migration inspection. Use a three-layer test process:

1. Migration shape checks
2. Fresh local database rebuild
3. Runtime SQL integration tests against local Supabase

#### 1. Migration Shape Checks

Add or update narrow tests that read the migration file and assert the critical SQL is present.

Use this layer to verify things like:
- a new function or `jsonb_set` path exists
- the canonical stat source expression is correct
- an `update public.game_saves` or `update public.raid_sessions` statement exists
- a known bad expression is no longer present

Run:

```bash
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemCatalogTests|HomeMarkupBindingTests"
```

This layer is fast, but it does not prove the SQL behaves correctly at runtime.

#### 2. Fresh Local Database Rebuild

Apply the entire migration chain to a clean local database:

```bash
. .\env.local.ps1
npx supabase start
npx supabase db reset
```

Use `npx supabase db reset` for SQL verification, not `db push --include-all`.

This layer proves:
- the migrations still apply in order
- patch migrations still match the current function text
- there are no hidden dependency or ordering problems

#### 3. Runtime SQL Integration Tests

For any bug involving persisted state, stats, raid snapshots, or migration repair behavior, add a runtime test that exercises the real database path.

The preferred pattern is:

1. Create a real local auth user
2. Seed or mutate `public.game_saves` and/or `public.raid_sessions` into a known stale or bad state
3. Call the real RPC path such as `profile_bootstrap` or `game_action`
4. Assert on the returned snapshot

Examples of what to test this way:
- stale `playerMaxHealth` repaired from accepted constitution
- stale raid `maxEncumbrance` repaired from accepted strength
- stale raid payload stats ignored in favor of canonical save stats
- migration rewrites persisted raid payload fields correctly

Run:

```bash
deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs
```

Use these runtime tests as the source of truth for SQL behavior. Static file checks are support checks only.

#### Recommended SQL TDD Loop

When changing SQL:

1. Write the smallest runtime failing case first if the bug involves persisted or derived database state.
2. Add or update static migration-shape assertions for the critical SQL text.
3. Run the narrow tests and confirm the runtime case fails for the right reason.
4. Add a new forward-only migration. Do not edit already-applied migrations.
5. Run `npx supabase db reset`.
6. Re-run the runtime SQL test until it passes.
7. Re-run the narrow C# and handler tests.

Typical command sequence:

```bash
. .\env.local.ps1
npx supabase start
dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemCatalogTests|HomeMarkupBindingTests"
npx supabase db reset
deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs
node --test supabase/functions/game-action/handler.test.mjs
```

If the SQL change affects profile bootstrap or other Edge Functions, also run:

```bash
deno test supabase/functions/profile-bootstrap/handler.test.mjs supabase/functions/profile-save/handler.test.mjs
```

If the change affects the `game-action` Edge Function response shape or projection behavior, also run:

```bash
node --test supabase/functions/game-action/handler.test.mjs
deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs
```

Do not call SQL work complete until:
- the migration applies cleanly on `db reset`
- the runtime SQL integration case passes
- the relevant narrow tests pass
- any changed Edge Function handler tests pass

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
node --test supabase/functions/game-action/handler.test.mjs
deno test --allow-env --allow-net supabase/functions/game-action/local-integration.test.mjs
```

Push only after local verification is green. Use the hosted Supabase environment after that for smoke testing of integration details such as auth-provider behavior.

### Supabase CI/CD Notes

GitHub Actions production deploy expects:

- Repository variables:
  - `SUPABASE_PROJECT_ID`
  - `SUPABASE_URL`
  - `SUPABASE_PUBLISHABLE_KEY`
- Repository secrets:
  - `SUPABASE_ACCESS_TOKEN`
  - `SUPABASE_DB_PASSWORD`

The project id is the Supabase project ref, for example `dblgbpzlrglcdwqyagnx`, not the project display name.

These values belong in GitHub repository variables and secrets only. Do not put remote project refs, remote database passwords, or access tokens in the repo-root `.env`.

Remote Supabase changes are CI-only:

- database migrations run from GitHub Actions using repository variables and secrets
- Edge Functions deploy from GitHub Actions using repository variables and secrets
- local developer shells must not run `supabase link` against hosted projects

### Supabase Manual Verification

After applying migrations, useful local checks are:

1. Verify tables exist:

```sql
select to_regclass('public.game_saves');
select to_regclass('public.raid_sessions');
```

2. Verify RLS is enabled:

```sql
select relname, relrowsecurity
from pg_class
where relname in ('game_saves', 'raid_sessions');
```

3. Verify bootstrap function exists:

```sql
select routine_name
from information_schema.routines
where routine_schema = 'game'
  and routine_name = 'bootstrap_player';
```

4. Verify bootstrap creates a starter save for an authenticated user:

```sql
select game.bootstrap_player(auth.uid());
```

5. Verify the created save row:

```sql
select user_id, save_version, payload, created_at, updated_at
from public.game_saves
where user_id = auth.uid();
```
