# Inventory Row Icons Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace jagged item-type labels with aligned icons and remove redundant value text from storage-style item rows.

**Architecture:** The client gets a small icon package plus one shared CSS row treatment. A small reusable item-type icon component is used across the affected panels so alignment stays consistent.

**Tech Stack:** Blazor WebAssembly, .NET 10, xUnit

---

### Task 1: Lock layout expectations with failing markup tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/HomeMarkupBindingTests.cs`

Add failing assertions for:
- icon component usage in affected item lists
- removal of raw `@item.Type`/`@entry.Item.Type` text in those rows
- removal of standalone storage value text

### Task 2: Add icon dependency and reusable icon component

**Files:**
- Modify: `src/RaidLoop.Client/RaidLoop.Client.csproj`
- Create: `src/RaidLoop.Client/Components/ItemTypeIcon.razor`

### Task 3: Apply aligned icon-row layout

**Files:**
- Modify: `src/RaidLoop.Client/wwwroot/css/app.css`
- Modify: `src/RaidLoop.Client/Components/LoadoutPanel.razor`
- Modify: `src/RaidLoop.Client/Components/StashPanel.razor`
- Modify: `src/RaidLoop.Client/Components/PreRaidPanel.razor`
- Modify: `src/RaidLoop.Client/Components/RaidHUD.razor`

### Task 4: Verify

**Files:**
- No new files required

Run:
- `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --no-restore --filter HomeMarkupBindingTests`
- `dotnet build src/RaidLoop.Client/RaidLoop.Client.csproj`
