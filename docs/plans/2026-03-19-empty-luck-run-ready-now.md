# Empty Luck Run Ready Now Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make corrupted empty luck-run state resolve to "ready now" instead of starting cooldown.

**Architecture:** Keep the client and Supabase normalization paths aligned. When a snapshot or saved payload contains `randomCharacter` with an empty inventory, clear `randomCharacter` and normalize `randomCharacterAvailableAt` to the ready sentinel value instead of adding five minutes. Preserve the flexible timestamp converter so legacy timestamp strings still deserialize cleanly.

**Tech Stack:** C#, xUnit, Blazor WebAssembly, Supabase SQL migrations

---

### Task 1: Update client snapshot expectations

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`

**Step 1: Write the failing test**

Add a test that applies a snapshot with `RandomCharacter` present but an empty inventory and `RandomCharacterAvailableAt = DateTimeOffset.MinValue`, then asserts:
- `_randomCharacter` becomes `null`
- `_randomCharacterAvailableAt` remains `DateTimeOffset.MinValue`

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`
Expected: FAIL because the client currently starts a cooldown when repairing empty luck-run state.

**Step 3: Write minimal implementation**

Update `ApplySnapshot` in `src/RaidLoop.Client/Pages/Home.razor.cs` so empty `RandomCharacter.Inventory` clears `_randomCharacter` without adding cooldown.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests"`
Expected: PASS

### Task 2: Update server normalization semantics

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Add: `supabase/migrations/2026031813_empty_luck_run_means_ready.sql`

**Step 1: Write the failing test**

Add a migration text assertion that verifies the new SQL migration clears empty `randomCharacter` state and sets `randomCharacterAvailableAt` to `'0001-01-01T00:00:00+00:00'` instead of adding five minutes.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests"`
Expected: FAIL because the new migration file does not exist yet.

**Step 3: Write minimal implementation**

Add a migration that redefines `game.settle_random_character` so:
- string timestamps are normalized to ISO UTC
- empty inventory clears `randomCharacter`
- empty inventory resets `randomCharacterAvailableAt` to the ready sentinel value

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests"`
Expected: PASS

### Task 3: Verify the combined behavior

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Modify: `src/RaidLoop.Core/Contracts/PlayerSnapshot.cs`
- Modify: `src/RaidLoop.Core/Contracts/FlexibleDateTimeOffsetJsonConverter.cs`

**Step 1: Re-run the timestamp contract and luck-run tests**

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests|FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~RaidStartApiTests|FullyQualifiedName~HomeMarkupBindingTests"`

Expected: PASS

**Step 2: Commit**

```bash
git add tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs tests/RaidLoop.Core.Tests/ContractsTests.cs src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Core/Contracts/PlayerSnapshot.cs src/RaidLoop.Core/Contracts/FlexibleDateTimeOffsetJsonConverter.cs supabase/migrations/2026031813_empty_luck_run_means_ready.sql docs/plans/2026-03-19-empty-luck-run-ready-now.md
git commit -m "Treat empty luck runs as ready immediately"
```
