# Player Stat System Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a six-stat player allocation system with a 27-point escalating point-buy, backward-compatible profile migration, accepted-vs-draft stat state, stat-gated raid entry, and stat-driven gameplay/shop behavior.

**Architecture:** Keep all stat formulas and point-buy rules in shared core code so both tests and the Blazor client use the same logic. Extend profile snapshots and Supabase migrations to persist accepted and draft stats, then wire the client to render/edit draft stats between raids while gameplay continues to use accepted stats only.

**Tech Stack:** C#, Blazor, xUnit, Supabase SQL migrations, shared contracts in `RaidLoop.Core`

---

### Task 1: Add shared stat domain types and point-buy rules

**Files:**
- Create: `src/RaidLoop.Core/PlayerStats.cs`
- Modify: `src/RaidLoop.Core/RaidLoop.Core.csproj`
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing test**

Add tests that assert:
- all six stats default to `8`
- available pool defaults to `27`
- raising `8 -> 13` costs `1` each
- raising `13 -> 14` and `14 -> 15` costs `2`
- raising `15 -> 16` and `16 -> 17` costs `3`
- raising `17 -> 18` costs `4`
- modifier uses `floor((score - 10) / 2)`
- lower/refund mirrors the last purchase cost

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidEngineTests"`

Expected: FAIL because the stat types and rules helpers do not exist yet.

**Step 3: Write minimal implementation**

Add a shared stat model and rules helper in core:
- immutable or record-based container for `Strength`, `Dexterity`, `Constitution`, `Intelligence`, `Wisdom`, `Charisma`
- defaults factory for all `8`
- point-buy constants for min/max/pool
- modifier helper
- raise/refund cost helpers

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidEngineTests"`

Expected: PASS for the new stat-rule tests.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidEngineTests.cs src/RaidLoop.Core/PlayerStats.cs src/RaidLoop.Core/RaidLoop.Core.csproj
git commit -m "feat: add shared player stat rules"
```

### Task 2: Extend combat/shop helpers for stat-driven seams

**Files:**
- Modify: `src/RaidLoop.Core/CombatBalance.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing test**

Add tests that assert:
- DEX attack bonus still comes from ability modifier
- defense uses DEX with an armor-cap seam
- CON max health stays aligned with the existing health rule
- STR exposes a carry-capacity helper seam
- CHA derives shop price adjustments from modifier
- CHA modifier maps to max shop rarity thresholds

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidEngineTests"`

Expected: FAIL because the new helpers and thresholds are missing.

**Step 3: Write minimal implementation**

Update `CombatBalance` to centralize:
- constrained DEX defense calculation with optional armor max-dex input
- existing CON health derivation via new stat helpers where practical
- STR carry capacity seam
- `GetCharismaModifier`
- `GetMaxShopRarityFromChaBonus`
- `GetShopPrice`

Keep existing behavior unless the approved design explicitly changes it.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidEngineTests"`

Expected: PASS for the new balance tests.

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidEngineTests.cs src/RaidLoop.Core/CombatBalance.cs
git commit -m "feat: add stat-driven combat and shop helpers"
```

### Task 3: Extend profile contracts for accepted and draft stats

**Files:**
- Modify: `src/RaidLoop.Core/Contracts/PlayerSnapshot.cs`
- Modify: `src/RaidLoop.Core/Contracts/RaidSnapshot.cs`
- Test: `tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Test: `tests/RaidLoop.Core.Tests/ProfileApiClientTests.cs`

**Step 1: Write the failing test**

Add contract serialization tests that round-trip:
- accepted stats
- draft stats
- available stat points
- stats accepted flag
- any new raid stat-derived fields surfaced to the client

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests|FullyQualifiedName~ProfileApiClientTests"`

Expected: FAIL because the contract members are absent.

**Step 3: Write minimal implementation**

Extend snapshot contracts with explicit stat payloads while preserving backward compatibility with existing JSON naming and old field order assumptions.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests|FullyQualifiedName~ProfileApiClientTests"`

Expected: PASS.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Contracts/PlayerSnapshot.cs src/RaidLoop.Core/Contracts/RaidSnapshot.cs tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/ProfileApiClientTests.cs
git commit -m "feat: extend profile contracts with player stats"
```

### Task 4: Add Supabase migration for stat persistence and normalization

**Files:**
- Create: `supabase/migrations/2026032603_add_player_stat_system.sql`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add migration assertions that verify:
- old saves are backfilled to six `8` scores
- `availableStatPoints` normalizes to `27`
- `statsAccepted` normalizes to `false`
- accepted and draft stats are both persisted in normalized profile payloads
- raid start paths can read accepted stats

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests"`

Expected: FAIL because the migration file and expected SQL text do not exist.

**Step 3: Write minimal implementation**

Create a migration that:
- backfills save payloads
- updates bootstrap/profile normalization functions
- preserves compatibility with older payload field names
- exposes accepted and draft stat fields to the client

If current raid-action/profile SQL already computes CON/DEX directly, update those functions to use accepted stats.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests"`

Expected: PASS.

**Step 5: Commit**

```bash
git add supabase/migrations/2026032603_add_player_stat_system.sql tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: migrate saves to player stat system"
```

### Task 5: Add profile actions and client state for accept/reallocate/edit flow

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`
- Test: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing test**

Add tests for:
- snapshot application reads accepted/draft stats and available points
- missing stat fields normalize safely in client state
- raid start is blocked while stats are unaccepted
- stat increment/decrement methods respect min/max and available points
- accept action sends the correct payload
- reallocate action is blocked in raid and charges `$5000`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`

Expected: FAIL because the state fields and handlers do not exist.

**Step 3: Write minimal implementation**

Update `Home.razor.cs` to:
- hold accepted/draft stat state
- calculate remaining points from shared rules
- gate raid start on `StatsAccepted`
- add handlers for increment/decrement, accept, and reallocate
- use CHA-aware prices
- use accepted stats for derived values like max HP and shop behavior

Update local storage fallback models only if still used by tests or offline bootstrap paths.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`

Expected: PASS.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Services/StashStorage.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs
git commit -m "feat: add client-side player stat flow"
```

### Task 6: Render stat controls in the pre-raid UI and HUD

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Components/PreRaidPanel.razor`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Modify: `src/RaidLoop.Client/Components/ShopPanel.razor`
- Modify: `src/RaidLoop.Client/wwwroot/css/app.css`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Add markup-binding tests that assert:
- pre-raid view shows stat names, values, modifiers, and remaining points
- accept and reallocate actions are present
- stat buttons are disabled in the documented states
- HUD shows accepted stats during raid
- shop markup uses the CHA-adjusted pricing delegate

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests"`

Expected: FAIL because the components do not render the new controls yet.

**Step 3: Write minimal implementation**

Keep the UI simple:
- add a stat editor block to the pre-raid panel or surrounding preparation view
- render up/down controls and remaining points
- show accepted stats in raid HUD
- surface the `Accept Stats` and `Re-Allocate Stats ($5000)` actions clearly
- filter or disable shop items above the CHA rarity cap

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests"`

Expected: PASS.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Components/PreRaidPanel.razor src/RaidLoop.Client/Components/RaidHUD.razor src/RaidLoop.Client/Components/ShopPanel.razor src/RaidLoop.Client/wwwroot/css/app.css tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: add player stat controls to raid preparation UI"
```

### Task 7: Reintroduce INT-based malfunction handling and manual fix action

**Files:**
- Modify: `src/RaidLoop.Core/CombatBalance.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Modify: `supabase/migrations/2026032603_add_player_stat_system.sql`

**Step 1: Write the failing test**

Add tests that assert:
- INT modifier contributes to a named malfunction-prevent/clear DC
- failed auto-check leaves the weapon malfunctioned
- `Fix Malfunction` action is rendered again when needed
- `Fix Malfunction` dispatches an action and consumes the turn according to returned raid state

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~RaidActionApiTests|FullyQualifiedName~HomeMarkupBindingTests"`

Expected: FAIL because the action and UI are currently absent.

**Step 3: Write minimal implementation**

Restore the malfunction seam with:
- shared named DC constant
- shared INT bonus helper
- client button wiring for manual fix
- backend migration updates where current SQL removed malfunctions

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~RaidActionApiTests|FullyQualifiedName~HomeMarkupBindingTests"`

Expected: PASS.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Components/RaidHUD.razor tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs supabase/migrations/2026032603_add_player_stat_system.sql
git commit -m "feat: restore intelligence-driven malfunction recovery"
```

### Task 8: Apply WIS surprise adjustments and DEX armor-cap behavior to opening/combat flows

**Files:**
- Modify: `src/RaidLoop.Core/Models.cs`
- Modify: `src/RaidLoop.Core/RaidEngine.cs`
- Modify: `src/RaidLoop.Core/CombatBalance.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `supabase/migrations/2026032603_add_player_stat_system.sql`

**Step 1: Write the failing test**

Add tests that assert:
- higher WIS improves player-side surprise outcome compared with lower WIS
- lower WIS makes enemy ambush more likely
- DEX defense display/calculation honors armor max-dex constraints
- raid start/opening payloads surface the constrained values used in practice

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidEngineTests|FullyQualifiedName~RaidStartApiTests"`

Expected: FAIL because WIS is not integrated and armor-capped DEX helpers are not applied.

**Step 3: Write minimal implementation**

Update the opening/combat seam to:
- pass WIS-based awareness modifiers into existing surprise logic
- keep current opening phase shape
- preserve armor constraints while using DEX-derived bonuses

Mirror the gameplay logic in the migration SQL where raid start/combat resolution happens server-side.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidEngineTests|FullyQualifiedName~RaidStartApiTests"`

Expected: PASS.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Models.cs src/RaidLoop.Core/RaidEngine.cs src/RaidLoop.Core/CombatBalance.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs supabase/migrations/2026032603_add_player_stat_system.sql
git commit -m "feat: apply wisdom surprise and capped dex defense"
```

### Task 9: Run full verification and fix any integration regressions

**Files:**
- Modify only what fails during verification
- Test: `tests/RaidLoop.Core.Tests/*.cs`

**Step 1: Run targeted suites**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidEngineTests"`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidActionApiTests"`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidStartApiTests"`
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests|FullyQualifiedName~ProfileApiClientTests|FullyQualifiedName~HomeMarkupBindingTests"`

Expected: PASS.

**Step 2: Run the full test project**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`

Expected: PASS with no new failures.

**Step 3: Refactor only if still green**

Tighten helper names, remove duplication, and keep formulas centralized.

**Step 4: Final commit**

```bash
git add .
git commit -m "feat: ship player stat allocation system"
```
