# Strength Encumbrance Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a strength-based encumbrance system that applies to pre-raid inventory, raid loot/equip actions, shop purchases, and luck run generation while preserving existing backpack slot limits.

**Architecture:** Extend authored items with a `Weight` field and add shared encumbrance helpers in `RaidLoop.Core` so the client and backend can enforce the same rules. Keep backpack `Slots` as the raid volume rule and layer weight validation on top for both pre-raid and in-raid actions. Expand raid and player snapshots so the UI can render a compact `current/max lbs` readout and luck run characters can carry randomized strength.

**Tech Stack:** C#/.NET 10, Blazor WebAssembly, xUnit, Supabase SQL migrations, JSON contracts

---

### Task 1: Add failing tests for item weight and strength encumbrance helpers

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Modify: `src/RaidLoop.Core/Models.cs`
- Modify: `src/RaidLoop.Core/ItemCatalog.cs`
- Modify: `src/RaidLoop.Core/CombatBalance.cs`
- Modify: `src/RaidLoop.Core/Contracts/RaidSnapshot.cs`

**Step 1: Write the failing tests**

Add tests that assert:
- authored items expose explicit `Weight`
- heavier gear weighs more than lighter gear
- `CombatBalance.GetMaxEncumbranceFromStrength(8)` is lower than `GetMaxEncumbranceFromStrength(18)`
- medkits contribute weight
- raid snapshot contract can carry `encumbrance` and `maxEncumbrance`

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ItemCatalogTests|FullyQualifiedName~RaidEngineTests|FullyQualifiedName~ContractsTests"`

Expected: FAIL because `Weight`, encumbrance helpers, and snapshot fields do not exist yet.

**Step 3: Write the minimal implementation**

Implement:
- `Item.Weight` in `src/RaidLoop.Core/Models.cs`
- explicit weight values in `src/RaidLoop.Core/ItemCatalog.cs`
- `CombatBalance.GetMaxEncumbranceFromStrength(int strength)`
- `CombatBalance.GetTotalEncumbrance(IEnumerable<Item> items, int medkitCount = 0)`
- `Encumbrance` and `MaxEncumbrance` fields on `RaidSnapshot`

Suggested weight table:
- `Rusty Knife`: 2
- `Light Pistol`: 4
- `Drum SMG`: 8
- `Field Carbine`: 9
- `Battle Rifle`: 10
- `Marksman Rifle`: 11
- `Support Machine Gun`: 18
- `Soft Armor Vest`: 8
- `Reinforced Vest`: 12
- `Light Plate Carrier`: 18
- `Medium Plate Carrier`: 24
- `Heavy Plate Carrier`: 30
- `Assault Plate Carrier`: 22
- `Small Backpack`: 4
- `Large Backpack`: 6
- `Tactical Backpack`: 8
- `Hiking Backpack`: 10
- `Raid Backpack`: 14
- `Medkit`: 3
- `Bandage`: 1
- `Ammo Box`: 2
- `Scrap Metal`: 3
- `Rare Scope`: 2
- `Legendary Trigger Group`: 4

Suggested max encumbrance formula:

```csharp
public static int GetMaxEncumbranceFromStrength(int strength)
{
    return 40 + (5 * Math.Max(0, strength - PlayerStatRules.MinimumScore));
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ItemCatalogTests|FullyQualifiedName~RaidEngineTests|FullyQualifiedName~ContractsTests"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/ItemCatalogTests.cs tests/RaidLoop.Core.Tests/RaidEngineTests.cs tests/RaidLoop.Core.Tests/ContractsTests.cs src/RaidLoop.Core/Models.cs src/RaidLoop.Core/ItemCatalog.cs src/RaidLoop.Core/CombatBalance.cs src/RaidLoop.Core/Contracts/RaidSnapshot.cs
git commit -m "feat: add item weight and encumbrance helpers"
```

### Task 2: Add failing raid-engine tests for weight-gated loot and equip

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`
- Modify: `src/RaidLoop.Core/RaidEngine.cs`
- Modify: `src/RaidLoop.Core/Models.cs`
- Modify: `src/RaidLoop.Core/CombatBalance.cs`

**Step 1: Write the failing tests**

Add tests for:
- `TryLootFromDiscovered` returns `false` when an item would exceed encumbrance even if slots fit
- `TryEquipFromDiscovered` returns `false` when an item would exceed encumbrance with an open slot
- `TryEquipFromCarried` returns `false` for the same condition
- medkits can no longer be looted infinitely when overweight

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidEngineTests"`

Expected: FAIL because raid engine still checks slots only and medkits bypass carrying limits.

**Step 3: Write the minimal implementation**

Add raid-state encumbrance support:
- store player strength or `MaxEncumbrance` on `RaidState` / `RaidInventory`
- add helper methods in `RaidEngine`:
  - `CanCarryByWeight`
  - `CanEquipByWeight`
- apply weight validation in:
  - `TryLootFromDiscovered`
  - `TryEquipFromDiscovered`
  - `TryEquipFromCarried`
  - medkit pickup path

Use a temporary item-set calculation before mutating collections.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidEngineTests"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidEngineTests.cs src/RaidLoop.Core/RaidEngine.cs src/RaidLoop.Core/Models.cs src/RaidLoop.Core/CombatBalance.cs
git commit -m "feat: enforce encumbrance in raid engine"
```

### Task 3: Add failing client tests for pre-raid encumbrance gating

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Components/LoadoutPanel.razor`

**Step 1: Write the failing tests**

Add tests that assert:
- stash-to-on-person move is blocked when accepted strength budget is exceeded
- shop purchase into `On Person` is blocked when accepted strength budget is exceeded
- the page exposes minimal pre-raid encumbrance text
- the loadout start area still shows the existing raid block reason plus a weight-specific one when relevant

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~HomeMarkupBindingTests"`

Expected: FAIL because pre-raid gating does not calculate or render encumbrance.

**Step 3: Write the minimal implementation**

Add helpers to `Home.razor.cs`:
- `GetOnPersonEncumbrance()`
- `GetMainCharacterMaxEncumbrance()`
- `CanAddOnPersonItem(Item item)`
- `GetPreRaidEncumbranceText()`

Wire those helpers into:
- stash move action guard
- buy-from-shop action guard
- loadout UI disabled state and reason text

Render minimal text such as `40/100 lbs` in `LoadoutPanel.razor` or directly beside it in `Home.razor`.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~HomeMarkupBindingTests"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Components/LoadoutPanel.razor
git commit -m "feat: gate pre-raid inventory by encumbrance"
```

### Task 4: Add failing client tests for raid HUD encumbrance display and button gating

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Write the failing tests**

Add tests that assert:
- raid HUD renders compact weight text like `40/100 lbs`
- `CanLootItem` returns `false` when overweight
- `Equip` button is disabled for overweight discovered/carried items even when slot is open
- raid page emits weight-specific helper text when some items are too heavy

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests|FullyQualifiedName~RaidActionApiTests"`

Expected: FAIL because the HUD only knows about slots and the equip buttons are unconditional.

**Step 3: Write the minimal implementation**

Extend `Home.razor.cs` with:
- `GetRaidEncumbrance()`
- `GetRaidMaxEncumbrance()`
- `CanEquipRaidItem(Item item)`
- `GetRaidEncumbranceText()`

Update `RaidHUD.razor`:
- display compact weight text near inventory info
- disable discovered/carried `Equip` buttons when overweight
- keep `Loot` disabled for slot-full or overweight cases
- add short text for weight-blocked items

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests|FullyQualifiedName~RaidActionApiTests"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs tests/RaidLoop.Core.Tests/RaidActionApiTests.cs src/RaidLoop.Client/Components/RaidHUD.razor src/RaidLoop.Client/Pages/Home.razor.cs src/RaidLoop.Client/Pages/Home.razor
git commit -m "feat: show raid encumbrance and disable overweight actions"
```

### Task 5: Add failing contract and snapshot tests for random-character stats and encumbrance projections

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ContractsTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify: `src/RaidLoop.Core/Contracts/PlayerSnapshot.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`

**Step 1: Write the failing tests**

Add tests that assert:
- `RandomCharacterSnapshot` includes stats
- client can hydrate random-character stats from projections
- random raid start preserves those stats in state used for encumbrance

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests|FullyQualifiedName~RaidStartApiTests|FullyQualifiedName~ProfileMutationFlowTests"`

Expected: FAIL because random characters currently only have name and inventory.

**Step 3: Write the minimal implementation**

Update contracts and hydration:
- add `PlayerStats Stats` to `RandomCharacterSnapshot`
- update `Home.razor.cs` random-character parsing to read and store stats
- update any local random-character state shape in client code

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~ContractsTests|FullyQualifiedName~RaidStartApiTests|FullyQualifiedName~ProfileMutationFlowTests"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/ContractsTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs src/RaidLoop.Core/Contracts/PlayerSnapshot.cs src/RaidLoop.Client/Pages/Home.razor.cs
git commit -m "feat: add stats to luck run character snapshots"
```

### Task 6: Add failing SQL-binding tests for backend encumbrance functions and projections

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Create: `supabase/migrations/2026032701_add_strength_encumbrance.sql`

**Step 1: Write the failing tests**

Add assertions that the new migration contains:
- item weight lookup helper
- max encumbrance helper
- total encumbrance helper
- random luck run stat generation
- random luck run loadout validation against encumbrance
- raid snapshot encumbrance projections
- overweight checks in `take-loot`, `equip-from-discovered`, and `equip-from-carried`

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests|FullyQualifiedName~ItemCatalogTests"`

Expected: FAIL because no migration contains encumbrance support yet.

**Step 3: Write the minimal implementation**

Create `supabase/migrations/2026032701_add_strength_encumbrance.sql` with:
- `game.item_weight(item_name text)`
- `game.max_encumbrance(strength int)`
- `game.current_encumbrance(items jsonb, medkits int default 0)`
- updated random-character generation that stores `stats`
- updated `game.random_luck_run_loadout(...)` or replacement helper that ensures legal inventory under generated strength
- updated `game.build_raid_snapshot(...)` to emit `encumbrance` and `maxEncumbrance`
- updated raid action handling to reject overweight loot/equip actions while preserving slot checks

Prefer reusing existing item lookup helpers introduced in `supabase/migrations/2026032303_add_item_lookup_helpers.sql`.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~HomeMarkupBindingTests|FullyQualifiedName~ItemCatalogTests"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs tests/RaidLoop.Core.Tests/ItemCatalogTests.cs supabase/migrations/2026032701_add_strength_encumbrance.sql
git commit -m "feat: add backend strength encumbrance rules"
```

### Task 7: Add failing integration tests for client/backend parity

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Modify: `src/RaidLoop.Client/Pages/Home.razor.cs`

**Step 1: Write the failing tests**

Add tests that prove:
- a raid payload with overweight items hydrates encumbrance correctly
- client gating matches backend projections for raid actions
- random character snapshots include legal strength/loadout combinations

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidActionApiTests|FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~RaidStartApiTests"`

Expected: FAIL until all contract, hydration, and gating code lines up.

**Step 3: Write the minimal implementation**

Fill any remaining parity gaps:
- normalize snapshot parsing for encumbrance fields
- ensure UI uses authoritative projections when available
- ensure random character state always carries stats needed for encumbrance gating

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "FullyQualifiedName~RaidActionApiTests|FullyQualifiedName~ProfileMutationFlowTests|FullyQualifiedName~RaidStartApiTests"`

Expected: PASS

**Step 5: Commit**

```bash
git add tests/RaidLoop.Core.Tests/RaidActionApiTests.cs tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs tests/RaidLoop.Core.Tests/RaidStartApiTests.cs src/RaidLoop.Client/Pages/Home.razor.cs
git commit -m "test: align encumbrance projections across client and backend"
```

### Task 8: Run full verification and update docs if needed

**Files:**
- Modify: `README.md`
- Modify: `docs/plans/2026-03-27-strength-encumbrance-design.md`

**Step 1: Write the failing doc/test expectation**

Add a short checklist item to update player-facing text if README or design wording no longer matches actual behavior.

**Step 2: Run full verification**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`

Expected: PASS with all encumbrance, raid start, profile mutation, markup binding, and contract tests green.

**Step 3: Make minimal doc cleanup**

If implementation details differ from the approved design in a user-visible way, update:
- `README.md`
- `docs/plans/2026-03-27-strength-encumbrance-design.md`

Do not expand scope beyond documenting actual shipped behavior.

**Step 4: Run full verification again**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj`

Expected: PASS

**Step 5: Commit**

```bash
git add README.md docs/plans/2026-03-27-strength-encumbrance-design.md
git commit -m "docs: document encumbrance rules"
```
