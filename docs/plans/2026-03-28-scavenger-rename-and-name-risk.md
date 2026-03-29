# Scavenger Rename And Name Risk Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rename the authored enemy shorthand to `Scavenger` everywhere in the repo and produce a conservative list of other names that should be considered for replacement.

**Architecture:** This is a content-consistency pass. Update the authoritative SQL encounter text and all dependent tests/docs to the new term, then verify with repo-wide search and targeted tests. In parallel, inventory current authored names and classify obvious trademark or competitor-term risks without changing them yet.

**Tech Stack:** C#, Blazor WebAssembly, PostgreSQL/Supabase SQL migrations, Node.js tests, ripgrep

---

### Task 1: Rename authored enemy shorthand

**Files:**
- Modify: `supabase/migrations/*.sql`
- Modify: `tests/RaidLoop.Core.Tests/*.cs`
- Modify: `supabase/functions/game-action/*.mjs`
- Modify: `docs/plans/*.md`

**Step 1: Write the failing search expectation**

Run: `rg -n --hidden -S "\b[Ss]cav\b" .`
Expected: existing authored occurrences are found.

**Step 2: Apply the minimal authored rename**

- Replace whole-word authored shorthand with `Scavenger`
- Replace lowercase whole-word shorthand with `scavenger`
- Skip third-party/vendor assets

**Step 3: Re-run search**

Run: `rg -n --hidden -S "\b[Ss]cav\b" .`
Expected: no authored occurrences remain.

### Task 2: Verify dependent behavior

**Files:**
- Test: `tests/RaidLoop.Core.Tests/RaidActionApiTests.cs`
- Test: `tests/RaidLoop.Core.Tests/RaidStartApiTests.cs`
- Test: `tests/RaidLoop.Core.Tests/ProfileMutationFlowTests.cs`
- Test: `supabase/functions/game-action/handler.test.mjs`
- Test: `supabase/functions/game-action/local-integration.test.mjs`

**Step 1: Run targeted tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "RaidActionApiTests|RaidStartApiTests|ProfileMutationFlowTests" -m:1`
Expected: pass.

**Step 2: Run edge-function tests**

Run: `node --test supabase/functions/game-action/handler.test.mjs supabase/functions/game-action/local-integration.test.mjs`
Expected: pass.

### Task 3: Inventory other risky names

**Files:**
- Review: `src/RaidLoop.Core/ItemCatalog.cs`
- Review: `supabase/migrations/*.sql`
- Review: `README.md`
- Review: `docs/plans/*.md`

**Step 1: Search authored names**

Run: `rg -n --hidden -S "Makarov|PPSH|AK74|AK47|SVDS|PKP|BNTI Kirasa-N|FORT Defender-2|6B43 Zabralo-Sh|NFM THOR|Tasmanian Tiger Trooper 35|6Sh118|Scavenger" .`
Expected: inventory of potentially risky names.

**Step 2: Classify and report**

- Mark exact brand/product/game terms as high-risk.
- Mark real-world model designations as medium-risk.
- Leave generic names as low-risk.

**Step 3: Commit**

```bash
git add docs/plans/2026-03-28-scavenger-rename-and-name-risk-design.md docs/plans/2026-03-28-scavenger-rename-and-name-risk.md supabase/migrations tests supabase/functions docs/plans
git commit -m "chore: rename enemy shorthand to scavenger"
```
