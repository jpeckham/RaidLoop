# RaidLoop Blazor PWA Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Deliver a Blazor WebAssembly PWA implementing the PRD raid loop and deploying to GitHub Pages.

**Architecture:** Keep domain logic in `RaidLoop.Core` and UI orchestration in `RaidLoop.Client`, with xUnit tests validating raid invariants.

**Tech Stack:** C# (.NET 10), Blazor WebAssembly, xUnit, GitHub Actions Pages

---

### Task 1: Scaffold solution
- Create Blazor WASM PWA app, core library, and test project.
- Reference core in client and tests.

### Task 2: TDD core raid rules
- Write failing tests for combat damage/death, backpack capacity, extraction transfer, death loss, and loadout removal.
- Implement minimal `RaidEngine`, `GameState`, and `RaidState` to pass tests.

### Task 3: Build Blazor gameplay UI
- Replace template home screen with base/in-raid views.
- Add encounter loop and combat action handlers.
- Persist stash with local storage via JS interop.

### Task 4: GitHub Pages deployment
- Add pages workflow with restore/test/publish.
- Rewrite base href to repo path and copy `index.html` to `404.html` for SPA fallback.

### Task 5: Verify
- Run `dotnet test RaidLoop.sln`
- Run `dotnet build RaidLoop.sln -c Release`
- Run `dotnet publish src/RaidLoop.Client/RaidLoop.Client.csproj -c Release -o publish`
