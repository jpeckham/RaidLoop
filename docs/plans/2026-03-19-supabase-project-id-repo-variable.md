# Supabase Project Id Repo Variable Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Move `SUPABASE_PROJECT_ID` from a GitHub secret to a repository variable in the Supabase deploy workflow and docs.

**Architecture:** Keep `SUPABASE_PROJECT_ID` as a job-level environment variable so the shell commands do not change, but source it from `vars.SUPABASE_PROJECT_ID` instead of `secrets.SUPABASE_PROJECT_ID`. Update the Supabase README so CI setup instructions match the workflow while preserving the local CLI env var examples.

**Tech Stack:** GitHub Actions workflow YAML, Markdown docs

---

### Task 1: Update workflow configuration

**Files:**
- Modify: `.github/workflows/supabase-deploy.yml`

**Step 1: Update the env source**

Change the workflow so `SUPABASE_PROJECT_ID` comes from `vars.SUPABASE_PROJECT_ID`.

**Step 2: Keep command surface stable**

Leave the `supabase link` and `supabase functions deploy` commands unchanged so the workflow still uses the same environment variable name internally.

### Task 2: Update setup documentation

**Files:**
- Modify: `supabase/README.md`

**Step 1: Clarify CI configuration**

Document that:
- `SUPABASE_PROJECT_ID` is a repository variable in GitHub Actions
- `SUPABASE_ACCESS_TOKEN` and `SUPABASE_DB_PASSWORD` remain secrets

**Step 2: Preserve local CLI guidance**

Keep the shell examples using `SUPABASE_PROJECT_ID` for local/manual use.

### Task 3: Verify the change

**Files:**
- Modify: `.github/workflows/supabase-deploy.yml`
- Modify: `supabase/README.md`

**Step 1: Inspect the workflow and docs diff**

Run:
- `git diff -- .github/workflows/supabase-deploy.yml supabase/README.md`

Expected:
- workflow reads `vars.SUPABASE_PROJECT_ID`
- docs distinguish repo variable vs secrets

**Step 2: Check for stale references**

Run:
- `rg -n "secrets\\.SUPABASE_PROJECT_ID|SUPABASE_PROJECT_ID" -S .github supabase`

Expected:
- no remaining `secrets.SUPABASE_PROJECT_ID` references
- remaining `SUPABASE_PROJECT_ID` references are either `vars.SUPABASE_PROJECT_ID` in workflow or manual CLI/docs references
