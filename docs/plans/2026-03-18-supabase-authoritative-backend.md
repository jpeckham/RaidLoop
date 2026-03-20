# Supabase Authoritative Backend Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace local browser persistence and client-authoritative gameplay with a Google-authenticated, Supabase-backed, server-authoritative backend using Edge Functions plus private Postgres functions.

**Architecture:** The Blazor client becomes a thin authenticated UI. Supabase Auth gates access, `game_saves` and `raid_sessions` store authoritative state, Edge Functions expose narrow action endpoints, and private Postgres functions execute bootstrap, inventory, economy, raid, combat, loot, and extraction transitions.

**Tech Stack:** Blazor WebAssembly, .NET 10, Supabase Auth, Supabase Edge Functions, PostgreSQL SQL migrations, RLS policies, JSONB save/session payloads.

---

### Task 1: Add Supabase Client Configuration And Auth Gate

**Files:**
- Modify: `src/RaidLoop.Client/RaidLoop.Client.csproj`
- Modify: `src/RaidLoop.Client/Program.cs`
- Create: `src/RaidLoop.Client/Configuration/SupabaseOptions.cs`
- Create: `src/RaidLoop.Client/Services/SupabaseAuthService.cs`
- Create: `src/RaidLoop.Client/Components/AuthGate.razor`
- Modify: `src/RaidLoop.Client/App.razor`
- Modify: `src/RaidLoop.Client/wwwroot/appsettings.json` or equivalent client config file if the repo uses one
- Test: `tests/RaidLoop.Core.Tests/` add a focused markup/config test file if needed

**Step 1: Write the failing test**

Add a test that proves the app now contains a Google auth gate rather than loading gameplay directly.

Suggested assertion targets:
- `App.razor` renders `<AuthGate`
- `AuthGate.razor` contains `Sign in with Google`
- `Program.cs` registers `SupabaseAuthService`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~AuthGate"`

Expected: FAIL because no auth gate/service/config exists yet.

**Step 3: Write minimal implementation**

- Add the Supabase .NET client package to `src/RaidLoop.Client/RaidLoop.Client.csproj`.
- Add `SupabaseOptions` with:
  - `Url`
  - `PublishableKey`
  - `RedirectTo`
- Register the options and `SupabaseAuthService` in `Program.cs`.
- Implement `SupabaseAuthService` with:
  - initialize client
  - get current session/user
  - sign in with Google OAuth
  - sign out
  - session changed event plumbing
- Add `AuthGate.razor` to:
  - show loading while auth initializes
  - show `Sign in with Google` when not authenticated
  - render child content only when authenticated
- Wrap the current app/root content in `AuthGate`.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~AuthGate"`

Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/RaidLoop.Client.csproj src/RaidLoop.Client/Program.cs src/RaidLoop.Client/Configuration/SupabaseOptions.cs src/RaidLoop.Client/Services/SupabaseAuthService.cs src/RaidLoop.Client/Components/AuthGate.razor src/RaidLoop.Client/App.razor tests/RaidLoop.Core.Tests
git commit -m "Add Supabase Google auth gate"
```

### Task 2: Define Shared Backend Snapshot Contracts

**Files:**
- Create: `src/RaidLoop.Core/Contracts/PlayerSnapshot.cs`
- Create: `src/RaidLoop.Core/Contracts/RaidSnapshot.cs`
- Create: `src/RaidLoop.Core/Contracts/AuthBootstrapResponse.cs`
- Create: `src/RaidLoop.Core/Contracts/GameActionRequest.cs`
- Create: `src/RaidLoop.Core/Contracts/GameActionResponse.cs`
- Modify: `src/RaidLoop.Core/RaidLoop.Core.csproj` if folder inclusion needs adjustment
- Test: `tests/RaidLoop.Core.Tests/ContractsTests.cs`

**Step 1: Write the failing test**

Create tests asserting JSON-serializable contracts exist for:
- top-level player snapshot
- optional active raid snapshot
- action request/response DTOs

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~ContractsTests"`

Expected: FAIL because the DTOs do not exist.

**Step 3: Write minimal implementation**

Create record types representing:
- authenticated player bootstrap response
- save snapshot fields currently spread across `GameSave` and `Home`
- raid snapshot fields currently rendered by `RaidHUD`
- action request/response envelopes for Edge Functions

Keep DTOs flat and explicit; do not reuse UI-only state classes directly.

**Step 4: Run test to verify it passes**

Run the focused contracts tests.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Contracts tests/RaidLoop.Core.Tests/ContractsTests.cs
git commit -m "Add authoritative backend snapshot contracts"
```

### Task 3: Add Supabase SQL Schema, RLS, And Bootstrap Function

**Files:**
- Create: `supabase/migrations/2026031801_create_game_saves.sql`
- Create: `supabase/migrations/2026031802_create_raid_sessions.sql`
- Create: `supabase/migrations/2026031803_enable_rls.sql`
- Create: `supabase/migrations/2026031804_game_bootstrap_functions.sql`
- Create: `supabase/README.md`
- Test: `supabase/tests/` if SQL tests are supported in your workflow, otherwise document manual verification in `supabase/README.md`

**Step 1: Write the failing test**

If SQL test harness is available, add checks for:
- `game_saves` exists
- `raid_sessions` exists
- RLS enabled
- bootstrap function returns a default save payload for a new user

If not, write the migration files first and document the exact manual SQL verification commands in `supabase/README.md`.

**Step 2: Run test to verify it fails**

Run the SQL verification workflow available in the repo, or note that the migrations are not yet present.

**Step 3: Write minimal implementation**

Create:
- `public.game_saves`
- `public.raid_sessions`
- RLS policies using `auth.uid()`
- private schema `game`
- `game.bootstrap_player(user_id uuid)` that:
  - inserts default save row if missing
  - uses the current starter save shape and authored starter items
  - returns the current save payload

Document how to apply the migrations in Supabase.

**Step 4: Run verification**

Run the repo’s SQL/migration verification if present. If not present, record manual verification commands in `supabase/README.md`.

**Step 5: Commit**

```bash
git add supabase
git commit -m "Add Supabase save schema and bootstrap function"
```

### Task 4: Replace `StashStorage` With A Server-Backed Profile Service

**Files:**
- Create: `src/RaidLoop.Client/Services/ProfileApiClient.cs`
- Create: `src/RaidLoop.Client/Services/ProfileState.cs`
- Modify: `src/RaidLoop.Client/Program.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Delete: `src/RaidLoop.Client/Services/StashStorage.cs` or leave temporarily as dead code only if needed for staged migration
- Modify: `tests/RaidLoop.Core.Tests/StashStorageTests.cs`
- Create: `tests/RaidLoop.Core.Tests/ProfileApiClientTests.cs`

**Step 1: Write the failing test**

Add tests proving:
- `Home.razor.cs` no longer calls `Storage.LoadAsync`
- profile bootstrap flows through `ProfileApiClient`
- `StashStorage` is not the active persistence mechanism

**Step 2: Run test to verify it fails**

Run the focused profile/storage tests.

**Step 3: Write minimal implementation**

- Introduce `ProfileApiClient` that calls backend bootstrap/profile endpoints.
- Introduce `ProfileState` to hold the current authoritative snapshot in the client.
- Update `Home.razor.cs` initialization to fetch profile bootstrap from backend after auth.
- Remove or isolate `StashStorage` from runtime service registration.

Do not move gameplay logic yet; just stop loading/saving from browser local storage.

**Step 4: Run test to verify it passes**

Run focused profile/storage tests.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Services src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Program.cs tests/RaidLoop.Core.Tests
git commit -m "Replace local storage with profile API client"
```

### Task 5: Add Edge Function For Authenticated Bootstrap/Profile Read

**Files:**
- Create: `supabase/functions/profile-bootstrap/index.ts`
- Create: `supabase/functions/_shared/auth.ts`
- Create: `supabase/functions/_shared/cors.ts`
- Create: `supabase/functions/_shared/contracts.ts`
- Modify: `supabase/config.toml` if required by Supabase function layout
- Test: `supabase/functions/profile-bootstrap/index.test.ts` or repo-equivalent function tests if available

**Step 1: Write the failing test**

Add a function test proving:
- unauthenticated request returns 401
- authenticated request bootstraps missing save and returns snapshot

**Step 2: Run test to verify it fails**

Run the Edge Function test command.

**Step 3: Write minimal implementation**

Implement `profile-bootstrap`:
- read JWT from request
- validate authenticated user
- call `game.bootstrap_player`
- map DB response to `AuthBootstrapResponse`

Keep service-role usage inside the function only.

**Step 4: Run test to verify it passes**

Run the focused function tests.

**Step 5: Commit**

```bash
git add supabase/functions
git commit -m "Add authenticated profile bootstrap function"
```

### Task 6: Move Pre-Raid Inventory And Economy Actions Server-Side

**Files:**
- Create: `supabase/migrations/2026031805_game_inventory_functions.sql`
- Create: `supabase/functions/game-action/index.ts`
- Modify: `supabase/functions/_shared/contracts.ts`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Components/LoadoutPanel.razor`
- Modify: `src/RaidLoop.Client/Components/StashPanel.razor`
- Modify: `src/RaidLoop.Client/Components/PreRaidPanel.razor`
- Create: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing test**

Add tests for actions such as:
- move stash to loadout
- store on-person item
- sell stash/on-person/luck-run item
- process luck-run item

The tests should assert the UI action methods delegate to the API client rather than mutating state directly.

**Step 2: Run test to verify it fails**

Run focused mutation flow tests.

**Step 3: Write minimal implementation**

- Create private DB functions for inventory/economy mutations.
- Add `game-action` Edge Function for pre-raid profile mutations.
- Update `Home.razor.cs` action handlers to call backend and replace local snapshot with the response.
- Remove direct money/stash/on-person mutations from those methods.

**Step 4: Run test to verify it passes**

Run focused mutation tests plus the client build.

**Step 5: Commit**

```bash
git add supabase src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Components tests/RaidLoop.Core.Tests
git commit -m "Move inventory and economy actions server-side"
```

### Task 7: Move Raid Start And Active Raid State Server-Side

**Files:**
- Create: `supabase/migrations/2026031806_game_raid_start_functions.sql`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Create: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`

**Step 1: Write the failing test**

Add tests asserting:
- starting main raid calls backend
- starting luck run calls backend
- current raid snapshot comes from API response, not local `RaidEngine.StartRaid`

**Step 2: Run test to verify it fails**

Run focused raid-start tests.

**Step 3: Write minimal implementation**

- Add DB functions for `start_main_raid` and `start_luck_run`.
- Persist authoritative raid session payload in `raid_sessions`.
- Update `Home.razor.cs` to hydrate `_raid`, encounter data, ammo, extract progress, and related state from the server response.
- Stop creating new authoritative raid state locally.

**Step 4: Run test to verify it passes**

Run raid-start tests and client build.

**Step 5: Commit**

```bash
git add supabase src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Components/RaidHUD.razor tests/RaidLoop.Core.Tests
git commit -m "Move raid start and session state server-side"
```

### Task 8: Move Combat, Loot, And Extraction Actions Server-Side

**Files:**
- Create: `supabase/migrations/2026031807_game_raid_action_functions.sql`
- Modify: `supabase/functions/game-action/index.ts`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Create: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`

**Step 1: Write the failing test**

Add tests for:
- `AttackAsync`
- `BurstFireAsync`
- `ReloadAsync`
- `FleeAsync`
- `TakeLootAsync`
- `EquipFromDiscoveredAsync`
- `EquipFromCarriedAsync`
- `DropEquippedAsync`
- `DropCarriedAsync`
- `ContinueSearching`
- `MoveTowardExtract`
- `AttemptExtractAsync`

Assert these handlers call backend APIs and consume authoritative snapshots.

**Step 2: Run test to verify it fails**

Run focused raid action tests.

**Step 3: Write minimal implementation**

- Implement private DB functions for raid actions and encounter progression.
- Centralize RNG, loot, combat, extraction, and settlement in Postgres functions.
- Update `game-action` Edge Function to dispatch authorized actions.
- Strip local mutation logic from `Home.razor.cs` so the browser only renders server responses.

**Step 4: Run test to verify it passes**

Run raid action tests, full client tests, and build.

**Step 5: Commit**

```bash
git add supabase src/RaidLoop.Client/Pages/Home.razor.cs tests/RaidLoop.Core.Tests
git commit -m "Move raid combat and loot flow server-side"
```

### Task 9: Remove Dead Local-Authority Paths And Document Deployment

**Files:**
- Modify: `README.md`
- Delete: `src/RaidLoop.Client/wwwroot/js/storage.js` if no longer used
- Modify: `src/RaidLoop.Client/wwwroot/index.html` or script includes if `storage.js` was loaded
- Modify: `docs/plans/2026-03-18-supabase-authoritative-backend-design.md` if implementation deviates
- Create: `docs/supabase-setup.md`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs` or a new infrastructure test

**Step 1: Write the failing test**

Add a test proving the runtime no longer references the local storage bridge.

**Step 2: Run test to verify it fails**

Run the focused infrastructure test.

**Step 3: Write minimal implementation**

- Remove dead local-storage JS and service wiring.
- Update docs with:
  - Supabase URL/publishable key config
  - Google provider setup
  - local URL `http://localhost:5214/`
  - production URL `https://jpeckham.github.io/RaidLoop/`
  - required SQL migration/apply steps
  - Edge Function deployment steps

**Step 4: Run test to verify it passes**

Run focused infrastructure tests and a full solution build.

**Step 5: Commit**

```bash
git add README.md docs src/RaidLoop.Client/wwwroot tests/RaidLoop.Core.Tests
git commit -m "Remove local save path and document Supabase deployment"
```

### Task 10: Final Verification

**Files:**
- No new files required

**Step 1: Run the full test suite**

Run: `dotnet test RaidLoop.sln --no-restore`

Expected: PASS

**Step 2: Run the full build**

Run: `dotnet build RaidLoop.sln --no-restore`

Expected: PASS

**Step 3: Verify Supabase deployment artifacts exist**

Check:
- `supabase/migrations/`
- `supabase/functions/profile-bootstrap/`
- `supabase/functions/game-action/`
- `docs/supabase-setup.md`

**Step 4: Manual verification checklist**

- Sign in with Google at `http://localhost:5214/`
- Verify first login bootstraps a save
- Verify page refresh preserves state via Supabase
- Verify inventory/shop actions work while logged in
- Verify raid actions continue to work with browser devtools state tampering blocked by backend authority
- Verify logged-out users cannot access gameplay

**Step 5: Commit any final fixups**

```bash
git add -A
git commit -m "Finalize Supabase authoritative backend rollout"
```
