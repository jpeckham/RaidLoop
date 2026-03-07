# Slot-Locked Equip Buttons Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement explicit equip/unequip button UX with one-equipped-per-slot enforcement and stash fast-equip behavior.

**Architecture:** Keep all equip behavior in `Home.razor` using helper methods for slot detection and slot-state transitions. Reuse existing `OnPersonEntry` persistence model and update action handlers to save after state changes.

**Tech Stack:** C# (.NET 10), Blazor WebAssembly, localStorage persistence, xUnit

---

### Task 1: Replace on-person checkbox with equip/unequip button actions

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Remove checkbox UI and wire button placeholders**

Replace checkbox section with button rendering for slotted items.

**Step 2: Add helpers and handlers**

Add:
```csharp
private static bool IsSlotType(ItemType type) => type is ItemType.Weapon or ItemType.Armor or ItemType.Backpack;
private int? FindEquippedIndexForSlot(ItemType slotType) => ...;
private async Task EquipOnPersonItemAsync(int index) => ...;
private async Task UnequipOnPersonItemAsync(int index) => ...;
```

`EquipOnPersonItemAsync` must auto-unequip currently equipped same-slot item.

**Step 3: Persist state after each action**

Call `SaveAllAsync()` after equip/unequip changes.

**Step 4: Verify build**

Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor
git commit -m "feat: replace on-person checkbox with equip-unequip buttons"
```

### Task 2: Implement stash button label logic (`Equip` vs `On Person`)

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Add stash label helper**

```csharp
private string GetStashPrimaryActionLabel(Item item)
```

Rules:
- slotted + no equipped item in slot => `Equip`
- otherwise => `On Person`

**Step 2: Update stash action method**

Change move method signature:
```csharp
private async Task MoveStashToOnPersonAsync(int stashIndex, bool equipImmediately)
```

When `equipImmediately` and slotted:
- add to on-person as equipped,
- auto-unequip existing same-slot entry if needed.

**Step 3: Update UI call site**

Stash button click computes `equipImmediately` from helper result.

**Step 4: Verify build/test**

Run: `dotnet test RaidLoop.sln -v minimal`
Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor
git commit -m "feat: add stash fast-equip action labels by slot state"
```

### Task 3: Enforce one-equipped-per-slot invariant everywhere

**Files:**
- Modify: `src/RaidLoop.Client/Pages/Home.razor`

**Step 1: Add invariant normalization helper**

```csharp
private void NormalizeEquippedSlots()
```

Ensure at most one equipped item for each slotted type after load/migrations/actions.

**Step 2: Call normalization at key points**

- After loading on-person items in `OnInitializedAsync`
- After moving from stash with equip
- After explicit equip operations

**Step 3: Verify raid gate still works with new button model**

- No unequipped items + weapon equipped => allowed
- Otherwise blocked with existing reason text

**Step 4: Verify build/test/publish**

Run: `dotnet test RaidLoop.sln -v minimal`
Run: `dotnet build RaidLoop.sln -c Release -v minimal`
Run: `dotnet publish src/RaidLoop.Client/RaidLoop.Client.csproj -c Release -o publish -v minimal`
Expected: PASS

**Step 5: Commit**

```bash
git add src/RaidLoop.Client/Pages/Home.razor
git commit -m "feat: enforce one-equipped-per-slot invariant across inventory actions"
```
