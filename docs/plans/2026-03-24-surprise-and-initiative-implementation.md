# Surprise And Initiative Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a lightweight opening-phase system so combat encounters can start with context-driven surprise or initiative before falling back to the existing combat flow.

**Architecture:** Introduce a small opening-phase payload to the raid snapshot and a resolver that maps authored encounter contact states into `surprise` or `initiative` outcomes. Keep the first increment context-driven and compatible with the current authoritative encounter/combat model, with explicit seams for later environment and gear modifiers.

**Tech Stack:** C#, xUnit, Blazor WebAssembly, existing raid snapshot contracts, Supabase-backed game action pipeline

---

### Task 1: Pin the new opening-phase contract in tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Modify: `src/RaidLoop.Core/Contracts/RaidSnapshot.cs`
- Test: `tests/RaidLoop.Core.Tests/ContractsTests.cs`

**Step 1: Write the failing test**

Extend the raid snapshot JSON round-trip test so it asserts new opening-phase fields survive serialization:

```csharp
Assert.Equal("PlayerAmbush", roundTrip.Snapshot.ActiveRaid!.ContactState);
Assert.Equal("Player", roundTrip.Snapshot.ActiveRaid!.SurpriseSide);
Assert.Equal("None", roundTrip.Snapshot.ActiveRaid!.InitiativeWinner);
Assert.Equal(1, roundTrip.Snapshot.ActiveRaid!.OpeningActionsRemaining);
Assert.False(roundTrip.Snapshot.ActiveRaid!.SurprisePersistenceEligible);
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ContractsTests`

Expected: FAIL because `RaidSnapshot` does not yet include the new opening-phase fields.

**Step 3: Write minimal implementation**

Modify `src/RaidLoop.Core/Contracts/RaidSnapshot.cs` to add the new fields with simple string and integer types:

```csharp
string ContactState,
string SurpriseSide,
string InitiativeWinner,
int OpeningActionsRemaining,
bool SurprisePersistenceEligible,
```

Append them near encounter metadata so the payload shape stays readable.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ContractsTests`

Expected: PASS with the updated snapshot contract and JSON round-trip coverage.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Contracts/RaidSnapshot.cs tests/RaidLoop.Core.Tests/ContractsTests.cs
git commit -m "feat: add opening phase fields to raid snapshot"
```

### Task 2: Teach the client page state to consume opening-phase fields safely

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Test: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`

**Step 1: Write the failing test**

Add a page-state test that applies an active raid snapshot containing opening-phase fields and verifies the page stores them without breaking existing encounter behavior. Use reflection access similar to current tests:

```csharp
Assert.Equal("PlayerAmbush", Assert.IsType<string>(GetField(home, "_contactState")));
Assert.Equal("Player", Assert.IsType<string>(GetField(home, "_surpriseSide")));
Assert.Equal("None", Assert.IsType<string>(GetField(home, "_initiativeWinner")));
Assert.Equal(1, Assert.IsType<int>(GetField(home, "_openingActionsRemaining")));
```

Also add a regression assertion that missing or blank values default safely when older snapshots are applied.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ProfileMutationFlowTests`

Expected: FAIL because `Home.razor.cs` does not yet track the new fields.

**Step 3: Write minimal implementation**

In `src/RaidLoop.Client/Pages/Home.razor.cs`:

- add backing fields for opening-phase state
- populate them in `ApplyActiveRaidSnapshot`
- reset them when raid state clears
- default missing values to neutral strings and zero actions remaining

Do not add UI rendering yet. Keep this task focused on projection safety.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter ProfileMutationFlowTests`

Expected: PASS with the new state fields applied and safely reset.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs
git commit -m "feat: project opening phase state into home page"
```

### Task 3: Add player-facing opening-phase presentation without changing combat controls

**Files:**
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Test: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

**Step 1: Write the failing test**

Extend markup binding tests so `RaidHUD` accepts the new parameters and the page passes them through:

```csharp
Assert.Contains("ContactState=\"@_contactState\"", markup);
Assert.Contains("SurpriseSide=\"@_surpriseSide\"", markup);
Assert.Contains("InitiativeWinner=\"@_initiativeWinner\"", markup);
Assert.Contains("OpeningActionsRemaining=\"_openingActionsRemaining\"", markup);
```

Add assertions in the component markup test or content scan that the combat section contains a compact opening-phase summary block.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: FAIL because the page and HUD do not yet reference the new opening-phase fields.

**Step 3: Write minimal implementation**

Update `src/RaidLoop.Client/Pages/Home.razor` and `src/RaidLoop.Client/Components/RaidHUD.razor` to:

- pass the new state into the HUD
- render a short opening-phase line only during combat
- prefer simple language such as:

```razor
You spotted them first.
They ambushed you.
You won initiative.
```

Do not add a new panel or dense stat table.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter HomeMarkupBindingTests`

Expected: PASS with the new parameters wired and the opening-phase summary present.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Components/RaidHUD.razor tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs
git commit -m "feat: show surprise and initiative opening state"
```

### Task 4: Introduce a context-driven opening-phase resolver in core combat logic

**Files:**
- Modify: `src/RaidLoop.Core/RaidEngine.cs`
- Modify: `src/RaidLoop.Core/Models.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing test**

Add focused core tests for converting contact state into opening-phase outcomes. Example cases:

```csharp
[Fact]
public void ResolveOpeningPhase_PlayerAmbush_GrantsPlayerSurprise()

[Fact]
public void ResolveOpeningPhase_MutualContact_UsesInitiativeWinner()

[Fact]
public void ResolveOpeningPhase_EnemyAmbush_GrantsEnemyOpeningAction()
```

Assert:

- correct `SurpriseSide`
- correct `InitiativeWinner`
- correct `OpeningActionsRemaining`

Use deterministic inputs for initiative so the tests do not depend on randomness.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidEngineTests`

Expected: FAIL because there is no opening-phase model or resolver in core code.

**Step 3: Write minimal implementation**

Add a small opening-phase model in core code and a resolver method that maps:

- `PlayerAmbush` -> player surprise, 1 opening action
- `EnemyAmbush` -> enemy surprise, 1 opening action
- `PlayerAdvantaged` -> player surprise or player-biased initiative based on the approved design choice
- `EnemyAdvantaged` -> enemy surprise or enemy-biased initiative based on the approved design choice
- `MutualContact` -> no surprise, initiative decides opening control

Keep this resolver isolated from later environment and gear math. Add TODO comments only where they mark genuine future extension seams.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidEngineTests`

Expected: PASS with deterministic opening-phase behavior in core logic.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/RaidEngine.cs src/RaidLoop.Core/Models.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs
git commit -m "feat: add core opening phase resolver"
```

### Task 5: Thread opening-phase results through combat encounter projection

**Files:**
- Modify: `src/RaidLoop.Core/Contracts/RaidSnapshot.cs`
- Modify: any snapshot-mapping code that builds active raid payloads
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`

**Step 1: Write the failing test**

Update API-facing tests so mocked raid snapshots and action results include opening-phase fields, then assert the client preserves them across:

- raid start
- combat action update
- transition out of combat

Add a regression case that neutral, loot, and extraction snapshots clear the opening-phase state.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter \"RaidStartApiTests|RaidActionApiTests\"`

Expected: FAIL because the active raid snapshot builder and test fixtures do not yet provide the new fields consistently.

**Step 3: Write minimal implementation**

Update every active combat raid snapshot creation path to include defaulted opening-phase fields. For combat encounters with authored contact state, populate the resolved values. For non-combat encounters, emit neutral defaults:

```csharp
ContactState: "None",
SurpriseSide: "None",
InitiativeWinner: "None",
OpeningActionsRemaining: 0,
SurprisePersistenceEligible: false,
```

Keep the payload backward-compatible in meaning even though the contract grows.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter \"RaidStartApiTests|RaidActionApiTests\"`

Expected: PASS with consistent projection across raid lifecycle updates.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Contracts/RaidSnapshot.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs
git commit -m "feat: project opening phase through raid snapshots"
```

### Task 6: Add extension seams for future environment and gear modifiers without enabling them yet

**Files:**
- Modify: `src/RaidLoop.Core/Models.cs`
- Modify: `src/RaidLoop.Core/RaidEngine.cs`
- Modify: `docs/plans/2026-03-24-surprise-and-initiative-design.md`
- Test: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing test**

Add a small test that proves the resolver accepts modifier inputs or a structured context object, even when all current modifiers are zero or absent.

```csharp
Assert.Equal("Player", result.SurpriseSide);
Assert.Equal(false, result.SurprisePersistenceEligible);
```

The goal is to force the code into a future-proof shape without turning on unfinished features.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidEngineTests`

Expected: FAIL because the resolver currently accepts only raw contact-state inputs.

**Step 3: Write minimal implementation**

Refactor the resolver to accept a small context object or record that can later hold:

- time-of-day visibility modifier
- environment awareness modifier
- player gear awareness modifier
- enemy localization modifier

Keep all current modifier values zeroed or unused in first-pass authored encounters. Do not implement night, flashlights, NVGs, or suppressors yet.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter RaidEngineTests`

Expected: PASS with no gameplay change beyond the already-approved opening-phase behavior.

**Step 5: Commit**

```bash
git add src/RaidLoop.Core/Models.cs src/RaidLoop.Core/RaidEngine.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs docs/plans/2026-03-24-surprise-and-initiative-design.md
git commit -m "refactor: add future modifier seam for opening phase"
```
