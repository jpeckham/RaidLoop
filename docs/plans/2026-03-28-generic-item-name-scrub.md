# Generic Item Name Scrub Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace branded and real-world item names with generic flavored names and align their stats to the intended tier order.

**Architecture:** Lock the desired progression in tests first, then update the shared C# catalog and authoritative SQL/authored content to the new names. Finally, sweep dependent tests and docs so the repo no longer depends on the old terms except in historical design notes where explicitly preserved.

**Tech Stack:** C#, Blazor WebAssembly, PostgreSQL/Supabase SQL migrations, Node.js, Deno, ripgrep

---

### Task 1: Lock progression with failing tests

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/ItemCatalogTests.cs`
- Modify: `tests/RaidLoop.Core.Tests/RaidEngineTests.cs`

**Step 1: Write the failing tests**

- Add assertions for the new item names in the weapon, armor, and backpack progression tests.
- Add assertions that `Battle Rifle < Marksman Rifle < Support Machine Gun` by value.
- Add assertions that `Heavy Plate Carrier < Assault Plate Carrier` by value and weight.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemCatalogTests|RaidEngineTests" -m:1`
Expected: FAIL because the catalog and SQL still use old names and old ordering.

### Task 2: Update authoritative item names and stats

**Files:**
- Modify: `src/RaidLoop.Core/ItemCatalog.cs`
- Modify: `supabase/migrations/*.sql`

**Step 1: Write minimal implementation**

- Replace the old weapon, armor, and backpack names with the approved generic names.
- Update authored values and weights only where needed to satisfy the approved progression.
- Keep item counts, item types, rarities, and gameplay roles intact.

**Step 2: Run focused tests**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemCatalogTests|RaidEngineTests" -m:1`
Expected: PASS

### Task 3: Sweep dependent tests and edge-function fixtures

**Files:**
- Modify: `tests/RaidLoop.Core.Tests/*.cs`
- Modify: `supabase/functions/**/*.mjs`
- Modify: `README.md`
- Modify: `docs/plans/*.md`

**Step 1: Update dependent references**

- Rename all remaining authored references to the new item names.
- Leave historical/legal-risk notes only where explicitly needed.

**Step 2: Run broader verification**

Run: `dotnet test tests/RaidLoop.Core.Tests/RaidLoop.Core.Tests.csproj --filter "ItemCatalogTests|RaidEngineTests|RaidActionApiTests|RaidStartApiTests|ProfileMutationFlowTests" -m:1`
Expected: PASS

Run: `node --test supabase/functions/game-action/handler.test.mjs`
Expected: PASS

### Task 4: Final search and commit

**Files:**
- Review: repo-wide search output

**Step 1: Verify old names are gone from authored content**

Run: `rg -n --hidden -S "Makarov|PPSH|AK74|AK47|SVDS|PKP|BNTI Kirasa-N|FORT Defender-2|6B43 Zabralo-Sh body armor|NFM THOR|Tasmanian Tiger Trooper 35|6Sh118" .`
Expected: no authored content remains beyond intentional historical references.

**Step 2: Commit**

```bash
git add src/RaidLoop.Core/ItemCatalog.cs supabase/migrations tests supabase/functions README.md docs/plans
git commit -m "chore: scrub branded item names"
```
