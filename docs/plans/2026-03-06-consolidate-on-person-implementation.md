# Consolidated On-Person Inventory Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Merge pre-raid equipped and post-raid inventory into one `On Person` area with item-level equipped state, while gating raid entry based on unequipped items and equipped weapon presence.

**Architecture:** Replace current character-inventory list with a typed on-person entry model in save data and UI state. Drive raid loadout from `OnPerson` entries marked equipped and enforce strict UI gating with explicit reason text.

**Tech Stack:** C# (.NET 10), Blazor WebAssembly, localStorage persistence, xUnit

---

### Task 1: Introduce on-person entry model in save layer

**Files:**
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`

**Step 1: Write failing compile setup**

Update `GameSave` to replace `CharacterInventory` with `OnPersonItems` so existing code fails until updated.

**Step 2: Run build to verify red**

Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: FAIL due to old `CharacterInventory` references.

**Step 3: Write minimal implementation**

Add:
```csharp
public sealed record OnPersonEntry(Item Item, bool IsEquipped);
```

Update `GameSave`:
```csharp
List<OnPersonEntry> OnPersonItems
```

Migration behavior in `LoadAsync`:
- If old `CharacterInventory` exists, map each item to `new OnPersonEntry(item, false)`.
- Normalize null lists to empty.

**Step 4: Run build to verify green at this layer**

Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: build may still fail until UI updates are complete; confirm save-layer compiles.

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Services/StashStorage.cs
git commit -m "feat: add on-person entry model to persisted save"
```

### Task 2: Replace base UI with Stash + On Person only

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Write failing state references intentionally**

Remove/replace old selected-loadout and character-inventory references so build fails until full wiring is done.

**Step 2: Run build to verify red**

Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: FAIL for missing symbols.

**Step 3: Write minimal implementation**

- Remove separate “Selected Loadout” section.
- Keep exactly two areas: `Stash` and `On Person`.
- Render on-person rows with:
  - Equipped toggle
  - Stash button
  - Sell button

**Step 4: Run build to verify green**

Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor
git commit -m "feat: consolidate inventory UI into stash and on-person areas"
```

### Task 3: Enforce raid entry gate and messages

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Write failing behavior check (manual acceptance assertions in code comments/tests where practical)**

Required conditions:
- Disabled if any on-person item is unequipped.
- Disabled if no equipped weapon.
- Show exact reason text.

**Step 2: Implement gate logic**

Add helpers:
```csharp
private bool HasUnequippedOnPersonItems => _onPersonItems.Any(x => !x.IsEquipped);
private bool HasEquippedWeapon => _onPersonItems.Any(x => x.IsEquipped && x.Item.Type == ItemType.Weapon);
private bool CanEnterRaid => !HasUnequippedOnPersonItems && HasEquippedWeapon;
private string? RaidBlockReason => HasUnequippedOnPersonItems
    ? "You need to move your unequipped items to stash or sell them."
    : (!HasEquippedWeapon ? "You don't have a weapon equipped." : null);
```

**Step 3: Wire UI messaging**

Show `RaidBlockReason` directly under disabled button.

**Step 4: Run build/tests**

Run: `dotnet test RaidLoop.sln -v minimal`
Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor
git commit -m "feat: add strict on-person raid entry gate with explicit reasons"
```

### Task 4: Drive raid loadout from equipped on-person items

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Implement loadout source change**

Replace selection-based loadout creation with:
```csharp
_raidLoadout = _onPersonItems.Where(x => x.IsEquipped).Select(x => x.Item).ToList();
```

**Step 2: Update extraction/shop destinations**

- Extracted items append to `_onPersonItems` with `IsEquipped=false`.
- Shop purchases append with `IsEquipped=false`.

**Step 3: Update move-to-stash and sell to operate on OnPersonEntry indices**

Ensure stash cap still disables stash button when full.

**Step 4: Run verification**

Run: `dotnet test RaidLoop.sln -v minimal`
Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Run: `dotnet publish src/RaidLoop.Client/RaidLoop.Client.csproj -c Release -o publish -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor
git commit -m "feat: build raids from equipped on-person items"
```

### Task 5: Persist on-person equipped state end-to-end

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`
- Modify: `src/RaidLoop.Client/Services/StashStorage.cs`

**Step 1: Wire OnInitializedAsync hydration**

Load `_onPersonItems` from `save.OnPersonItems`.

**Step 2: Wire SaveAllAsync**

Persist updated on-person entries and all existing save fields.

**Step 3: Verify persistence manually**

Run app, toggle equipped states, refresh page, confirm state remains.

**Step 4: Run final verification commands**

Run: `dotnet test RaidLoop.sln -v minimal`
Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Run: `dotnet publish src/RaidLoop.Client/RaidLoop.Client.csproj -c Release -o publish -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor src/RaidLoop.Client/Services/StashStorage.cs
git commit -m "feat: persist and enforce unified on-person equipped state"
```
