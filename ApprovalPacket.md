# Approval Packet — Home.razor Component Decomposition

**Generated**: 2026-03-15
**Increment**: Planning & specification (pre-implementation)
**Decision required**: Approve to begin implementation | Request changes | Defer

---

## What Was Done This Session

Five planning artifacts were produced. No production code was changed.

| Artifact | Purpose | Status |
|----------|---------|--------|
| `ProjectSnapshot.md` | Full health-check: architecture, risks, opportunities, next actions | Complete |
| `FeatureProposal.md` | Proposal for Home.razor decomposition (O1 / R1 mitigation) | Complete |
| `FeatureSpec.md` | Full implementation spec with 7 BRs, 8 FR groups, 7 NFRs, 8 AC groups | Ready for development |
| `ArchitectureDecision.md` | Added ADR-015 (decomposition) and ADR-016 (no GameStateService) | Complete |
| `ApprovalPacket.md` | This document | — |

---

## The Increment in One Paragraph

The session diagnosed the project's single HIGH-severity risk (R1: monolithic `Home.razor` at ~1,100 lines) and produced a fully-specced plan to resolve it by extracting five focused Blazor sub-components (`StashPanel`, `LoadoutPanel`, `ShopPanel`, `PreRaidPanel`, `RaidHUD`) while reducing `Home.razor` to a ≤150-line orchestration shell. The plan also resolves the open architectural question TBD-A (state ownership during decomposition), choosing parameter drilling over a DI service for the current scope. No code was written; all production files are unchanged.

---

## Proposed Feature: Home.razor Component Decomposition

### What it is
A pure structural refactor of `RaidLoop.Client`. Five new Blazor components replace the display markup currently embedded in `Home.razor`. State stays in the shell; sub-components receive parameters and emit `EventCallback<T>` upward. Zero behaviour change.

### Why now
- R1 is the **only HIGH-severity risk** in the project. Every feature added to the current monolith compounds the refactor cost.
- The Loot Tiers feature (fully specced, ready to implement) will add ~25 lines to `Home.razor`. Procedural encounter expansion will add ~50–100 more. The decomposition cost is approximately constant; the benefit increases with delay.
- Completing this unlocks bUnit UI tests (R2 mitigation) as a free follow-on — one refactor closes two risks simultaneously.

### What changes
| File | Change |
|------|--------|
| `src/RaidLoop.Client/Pages/Home.razor` | Reduced from ~1,100 lines to ≤150 lines |
| `src/RaidLoop.Client/Components/StashPanel.razor` | New |
| `src/RaidLoop.Client/Components/LoadoutPanel.razor` | New |
| `src/RaidLoop.Client/Components/ShopPanel.razor` | New |
| `src/RaidLoop.Client/Components/PreRaidPanel.razor` | New |
| `src/RaidLoop.Client/Components/RaidHUD.razor` | New |

**Nothing else changes.** `RaidLoop.Core`, all tests, `StashStorage.cs`, `Program.cs`, and every `.csproj` file are untouched.

---

## Risks of the Proposed Work

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Missed state reference during split | Medium | Medium | Smoke-test each component before PR merge |
| Blazor render cycle edge cases (`StateHasChanged`) | Low | Low | No state is moved; only markup and callbacks change |
| Conflict with in-flight Loot Tiers PR | Low | Low | Either complete Loot Tiers first or coordinate branch order; features touch different lines |
| PR review burden | Medium | Low | Deliver as 5 sequential PRs (one component per PR) |

---

## Risks of NOT Doing This Work

| Risk | Current Severity | Trend |
|------|-----------------|-------|
| R1 — Monolithic Home.razor | **HIGH** | Worsening with each feature |
| R2 — No bUnit UI tests | MEDIUM | Blocked until decomposition exists |
| Parallel PR merge conflicts | Ongoing | Each new feature increases probability |

---

## Acceptance Criteria Summary

The feature is complete when all of the following are true:

1. All five component files exist and are non-empty.
2. `Home.razor` is ≤150 lines; no sub-component exceeds 300 lines.
3. No sub-component owns or mutates shared game state.
4. All parameters and callbacks match the contracts defined in the spec (FR-2 through FR-6).
5. Full game loop (prepare → raid → combat → loot → extract/die → repeat) is manually verified unchanged.
6. `dotnet build` succeeds with zero new warnings; all 31 existing tests pass.
7. No Core, test, infrastructure, or `.csproj` files are modified.
8. `RaidHUD.razor` and `StashPanel.razor` contain all item-name rendering so Loot Tiers can apply `rarity-*` CSS classes without a second structural pass.

---

## Delivery Plan

5 sequential PRs, each leaving the game in a working state:

| PR | Content |
|----|---------|
| PR-1 | Extract `StashPanel.razor` |
| PR-2 | Extract `LoadoutPanel.razor` |
| PR-3 | Extract `ShopPanel.razor` |
| PR-4 | Extract `PreRaidPanel.razor` |
| PR-5 | Extract `RaidHUD.razor`; final `Home.razor` shell |

---

## Sequencing Relative to Loot Tiers

Either order is valid:

- **Decompose first, then Loot Tiers**: rarity CSS classes land directly in the right sub-components on the first pass.
- **Loot Tiers first, then decompose**: rarity classes move into sub-components during PR-5; AC-8 enforces they end up in the right place.

The current recommendation (from ProjectSnapshot) is: implement Loot Tiers first (spec is already complete), then decompose. Either order passes all acceptance criteria.

---

## Decision

- [ ] **Approve** — proceed to implementation as specified
- [ ] **Approve with changes** — implement with the following modifications: ___
- [ ] **Defer** — revisit after the following condition is met: ___
- [ ] **Reject** — do not proceed; reason: ___
