# App Deploy After Supabase Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Prevent the app deployment from getting ahead of Supabase on `main` pushes that modify `supabase/**`.

**Architecture:** Keep the workflows separate, but change the app deployment workflow so it behaves differently based on the triggering event. A direct `push` to `main` deploys immediately only when no Supabase files changed. A successful `workflow_run` completion from `Supabase Deploy` deploys the app for mixed pushes after the backend is already updated.

**Tech Stack:** GitHub Actions workflow YAML, reusable GitHub workflow invocation, `dorny/paths-filter`

---

### Task 1: Gate push-triggered app deploys by changed paths

**Files:**
- Modify: `.github/workflows/continuous-delivery-dotnet-blazor-github-pages.yml`

**Step 1: Add a change-detection job**

Use `dorny/paths-filter` to detect whether `supabase/**` changed on push and pull request events.

**Step 2: Verify push behavior is conditional**

Allow the app deployment job to run on push only when Supabase files did not change.

**Step 3: Verify PR behavior stays intact**

Keep pull request validation behavior unchanged by allowing the app workflow to run for PR events.

### Task 2: Deploy the app after successful Supabase workflow completion

**Files:**
- Modify: `.github/workflows/continuous-delivery-dotnet-blazor-github-pages.yml`

**Step 1: Add a `workflow_run` trigger**

Trigger on completed runs of the `Supabase Deploy` workflow.

**Step 2: Restrict workflow-run deployment to the intended source**

Only allow the app deployment to run when the completed workflow:
- is `Supabase Deploy`
- concluded with `success`
- came from a `push`
- targeted `main`

**Step 3: Preserve manual dispatch**

Keep `workflow_dispatch` available for manual recovery if needed.

### Task 3: Verify the resulting workflow

**Files:**
- Modify: `.github/workflows/continuous-delivery-dotnet-blazor-github-pages.yml`

**Step 1: Inspect the rendered YAML**

Run a local readback of the workflow file and verify the trigger and job conditions match the design.

**Step 2: Run repo-available YAML validation**

If a local parser/linter is available, run it against the workflow file. Otherwise document the gap and rely on file inspection plus Git diff review.
