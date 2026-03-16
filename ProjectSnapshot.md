# ProjectSnapshot — RaidLoop (extractor-shooter-light)

**Date:** 2026-03-15
**Branch:** feature/home-decomposition (off main)
**Last Commit (main):** e294119 — ci: pass repository visibility to shared CD workflow
**Version:** 1.0
**Live URL:** https://jpeckham.github.io/RaidLoop

---

## 1. Project Summary

**RaidLoop** is a browser-based extraction-shooter idle game built with Blazor WebAssembly and hosted as a static PWA on GitHub Pages. Players prepare a raid loadout from a persistent stash, run procedural encounter sequences (combat, loot, neutral), and either extract with profit or lose everything they brought. It is a fully self-contained single-player game — no server, no database, no accounts.

---

## 2. Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Language | C# | .NET 10.0 |
| UI Framework | Blazor WebAssembly | 10.0.0 |
| CSS | Bootstrap (vendored) | 5.x |
| Testing | xUnit | 2.9.3 |
| Coverage | coverlet | 6.0.4 |
| Hosting | GitHub Pages | — |

**Architectural invariant:** `RaidLoop.Core` has zero external NuGet dependencies — pure .NET BCL only.

---

## 3. Repository Structure

```
extractor-shooter-light/
├── src/
│   ├── RaidLoop.Core/              # Domain library (zero deps)
│   │   ├── Models.cs               # Item, GameState, RaidState, RaidInventory
│   │   ├── RaidEngine.cs           # Raid state machine (225 lines)
│   │   ├── CombatBalance.cs        # Balance tables & formulas (147 lines)
│   │   └── EncounterLoot.cs        # Loot encounter logic (16 lines)
│   └── RaidLoop.Client/            # Blazor WASM frontend
│       ├── Program.cs              # DI & host setup
│       ├── Services/StashStorage.cs  # Versioned localStorage persistence (186 lines)
│       └── Pages/Home.razor        # Monolithic game UI (1,386 lines) ← Risk R1
├── tests/
│   └── RaidLoop.Core.Tests/
│       └── RaidEngineTests.cs      # 31 xUnit tests (304 lines)
├── docs/plans/                     # 16+ design & implementation plan files
├── .github/workflows/              # CI/CD pipelines
├── ArchitectureDecision.md         # 16 living ADRs
├── prd.md                          # Product Requirements Document
├── version.json                    # {"major": 1, "minor": 0}
└── README.md                       # User guide with CI/coverage badges
```

---

## 4. Current State

### Source Code

| File | Lines | Status |
|------|-------|--------|
| `RaidLoop.Core/Models.cs` | 124 | Stable |
| `RaidLoop.Core/RaidEngine.cs` | 225 | Stable |
| `RaidLoop.Core/CombatBalance.cs` | 147 | Stable |
| `RaidLoop.Core/EncounterLoot.cs` | 16 | Stable |
| `RaidLoop.Client/Services/StashStorage.cs` | 186 | Risk R3 |
| `RaidLoop.Client/Pages/Home.razor` | **1,386** | **Risk R1** |

### Tests

- 31 xUnit tests in `RaidEngineTests.cs`, all covering `RaidLoop.Core`
- Deterministic via injectable `IRng` / `SequenceRng` stub
- No client-side tests (UI, persistence, JS interop) — Risk R2
- Coverage badge auto-generated and published to GitHub Pages

### CI/CD

- Single pipeline delegates to shared reusable workflow at `jpeckham/.github@main`
- On every push to `main`: build → test → coverage badge → publish → deploy to Pages → semver tag
- PRs run build + test (no deploy)
- `sync-readme-status-badges.yml` keeps README badge links current

### Active Branch Work (feature/home-decomposition)

**Modified files:**
- `prd.md` — reformatted/cleaned (same content, ~225 lines vs prior ~292)
- `src/RaidLoop.Client/Pages/Home.razor` — removed private `EncounterType` enum (-8 lines)

**New (untracked) files:**

| File | Contents |
|------|----------|
| `src/RaidLoop.Client/EncounterType.cs` | `EncounterType` enum promoted from Home.razor to shared client-level type |
| `ArchitectureDecision.md` | 17 living ADRs (ADR-001 through ADR-017, includes decomposition design) |
| `FeatureProposal.md` | Home.razor decomposition proposal (status: Approved) |
| `FeatureSpec.md` | Decomposition feature spec — 5 components, 6 FR groups, 7 NFRs |
| `ApprovalPacket.md` | Approval records for decomposition planning phase |
| `docs/plans/2026-03-15-home-decomposition-implementation.md` | Task-by-task implementation plan (Task 1 complete) |
| `ProjectSnapshot.md` | This file |
| `runs/` | AI assistant run artifacts (5 timestamped directories) |

**Implementation progress:**
- Task 1 (EncounterType.cs + shell prep): **COMPLETE**
- Task 2 (Extract StashPanel, LoadoutPanel, ShopPanel, PreRaidPanel): **PENDING**
- Task 3 (Extract RaidHUD): **PENDING**
- Task 4 (Full verification — tests pass, Home.razor ≤150 lines): **PENDING**

**Action required:** Commit untracked planning artifacts and `EncounterType.cs` before continuing with Task 2.

---

## 5. Risks

### R1 — Monolithic Home.razor (HIGH)
**File:** `src/RaidLoop.Client/Pages/Home.razor` (~1,378 lines after Task 1)
**Impact:** Merge conflicts on parallel feature work; untestable UI logic; high cognitive load on every change.
**ADR:** Decomposition deferred to v1.1 (ADR-007). Active work now in progress: five sub-components (StashPanel, LoadoutPanel, ShopPanel, PreRaidPanel, RaidHUD) per ADR-015/016/017.
**Effort to fix:** Large (Tasks 2–4 remaining).

### R2 — No Client-Side Tests (MEDIUM)
**Scope:** Home.razor UI logic, StashStorage save/load/migration, JS interop.
**Impact:** Silent regressions in persistence or UI state transitions. Coverage metric reflects Core only.
**Fix:** Add bUnit tests for critical paths (stash load, loot encounter, extraction).
**Effort:** Medium.

### R3 — Silent Save Migration Failures (MEDIUM)
**File:** `src/RaidLoop.Client/Services/StashStorage.cs`
**Issue:** All migration `try/catch` blocks silently fall back to `CreateDefaultSave()`. Players lose progress with no indication.
**Fix:** Surface errors to player UI; log version mismatch details.
**Effort:** Small.

### R4 — .NET 10.0 Preview SDK Unpinned (LOW-MEDIUM)
**Issue:** No `global.json`. CI and developer machines float to latest `10.0.x`, which may include breaking preview changes.
**Fix:** Add `global.json` with pinned SDK version.
**Effort:** 15 minutes.

### R5 — Shared Workflow Reference Unpinned (LOW)
**File:** `.github/workflows/continuous-delivery-dotnet-blazor-github-pages.yml`
**Issue:** References `jpeckham/.github/...@main` — upstream changes deploy immediately without review.
**Fix:** Pin to a commit SHA or tag.
**Effort:** 15 minutes.

### R6 — Single Monolithic Test File (LOW)
**File:** `tests/RaidLoop.Core.Tests/RaidEngineTests.cs` (304 lines, 31 tests)
**Issue:** All tests co-mingled; will become unwieldy as coverage expands.
**Fix:** Split into `CombatBalanceTests.cs`, `EncounterLootTests.cs`, `RaidEngineTests.cs`.
**Effort:** Small.

---

## 6. Opportunities

### O1 — Loot Tiers System (READY TO IMPLEMENT — post-decomposition)
Fully designed and approved across ADR-008 through ADR-014. Zero architecture changes needed; extends existing `CombatBalance.cs` and `Models.cs` patterns. Clearest path to the next meaningful gameplay feature after decomposition lands.

### O2 — Home.razor Decomposition (IN PROGRESS)
ADR-015/016/017 have a concrete five-component plan with state-up/events-down ownership. Task 1 complete. Tasks 2–4 will reduce Home.razor to ≤150 lines, enabling bUnit testing of UI logic and eliminating R1.

### O3 — Save Migration Hardening + Versioned Schema Tests
`StashStorage` already has versioned migration infrastructure. Hardening error handling (R3) and adding bUnit coverage (R2) significantly reduces regression risk for the persistence layer — critical for player retention.

### O4 — SDK & Pipeline Pinning (QUICK WINS)
R4 and R5 together take ~30 minutes and eliminate two supply-chain risk vectors.

### O5 — version.json Runtime Embedding
`version.json` drives CI semver tagging but is not consumed at runtime. Embedding it as a generated C# constant would enable displaying a build version in the UI (useful for players reporting bugs).

### O6 — Procedural Encounter Variety
`EncounterLoot.cs` is intentionally thin (16 lines). The encounter table is an extension point for richer procedural content (multi-enemy rooms, trap events, environmental encounters) without touching the raid state machine.

---

## 7. Recommended Actions (Prioritized)

| Priority | Action | Risk/Opportunity | Effort |
|----------|--------|-----------------|--------|
| 1 | Add `global.json` to pin .NET SDK | R4 | 15 min |
| 2 | Pin shared workflow to commit SHA | R5 | 15 min |
| 3 | Harden `StashStorage` migration errors | R3 | Small |
| 4 | Implement Loot Tiers (spec approved) | O1 / ADR-008-014 | Medium |
| 5 | Add bUnit tests for StashStorage + UI | R2 / O3 | Medium |
| 6 | Decompose Home.razor | R1 / O2 / ADR-015-016 | Large |
| 7 | Split `RaidEngineTests.cs` by module | R6 | Small |
| 8 | Embed version.json as runtime constant | O5 | Small |

---

## 8. Architecture Highlights

- **Pure domain separation:** `RaidLoop.Core` is a dependency-free library usable in any .NET context.
- **Sealed records for immutability:** Domain types are sealed, enforcing safe copy-on-update patterns.
- **Injectable `IRng`:** Eliminates test flakiness; all randomness flows through one seam.
- **Static facades instead of DI:** Deliberate (ADR-002) to avoid DI container overhead in a single-user WASM app.
- **Versioned localStorage:** Save schema has an explicit version key; migration chain in `StashStorage.cs`.
- **Documentation-first process:** 17 ADRs + design plans precede implementation.

---

## 9. Known Limitations (By Design)

- **Single-device persistence** — localStorage only; no cloud sync.
- **No character progression** — XP/skills deferred post-MVP.
- **No procedural map** — Encounter sequences replace map exploration (by PRD design).
- **No multiplayer** — Single-player only; static hosting precludes server state.
- **No account system** — GitHub Pages static hosting; no authentication layer.
