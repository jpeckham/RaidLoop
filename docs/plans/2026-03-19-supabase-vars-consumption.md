# Supabase Vars Consumption Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Consume `SUPABASE_URL` and `SUPABASE_PUBLISHABLE_KEY` from GitHub repository variables in the Supabase deploy workflow.

**Architecture:** Keep the workflow command surface stable, but expand the job-level environment to source all non-secret Supabase metadata from `vars`. Add validation steps so the workflow fails early if those repository variables are missing, even before future reusable workflow extraction.

**Tech Stack:** GitHub Actions workflow YAML, Markdown docs

---

### Task 1: Update workflow env and validation

**Files:**
- Modify: `.github/workflows/supabase-deploy.yml`

**Step 1: Add repo variable-backed env entries**

Source these values from GitHub Actions repository variables:
- `SUPABASE_PROJECT_ID`
- `SUPABASE_URL`
- `SUPABASE_PUBLISHABLE_KEY`

**Step 2: Validate them in workflow steps**

Extend the existing secret validation so function deploys and migrations fail fast when required non-secret metadata is not configured.

### Task 2: Keep docs aligned

**Files:**
- Modify: `supabase/README.md`

**Step 1: Keep CI/CD configuration section current**

Ensure the docs list all three repository variables and the two repository secrets.

### Task 3: Verify references

**Files:**
- Modify: `.github/workflows/supabase-deploy.yml`
- Modify: `supabase/README.md`

**Step 1: Inspect the diff**

Run:
- `git diff -- .github/workflows/supabase-deploy.yml supabase/README.md`

**Step 2: Search for variable usage**

Run:
- `rg -n "vars\\.SUPABASE_PROJECT_ID|vars\\.SUPABASE_URL|vars\\.SUPABASE_PUBLISHABLE_KEY|secrets\\.SUPABASE_" -S .github supabase`
