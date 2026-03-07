# SemVer Releases and Auto Notes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add automated semantic version tagging (`vX.Y.Z`) and diff-based GitHub release notes to the existing Pages deployment pipeline.

**Architecture:** Extend the current GitHub Actions workflow with a post-deploy release job. The job reads manual `major.minor` from `version.json`, computes next patch from existing tags, creates/pushes the new tag, then creates a GitHub release with generated notes from commit diffs.

**Tech Stack:** GitHub Actions YAML, PowerShell/Bash in workflow, Git tags, GitHub Releases API

---

### Task 1: Add version source file

**Files:**
- Create: `version.json`

**Step 1: Create minimal version file**

```json
{
  "major": 1,
  "minor": 0
}
```

**Step 2: Validate JSON format locally**

Run: `Get-Content version.json | ConvertFrom-Json | Format-List`
Expected: object with `major` and `minor` values.

**Step 3: Commit**

```bash
git add version.json
git commit -m "chore: add manual major-minor version source"
```

### Task 2: Extend workflow permissions and add release job skeleton

**Files:**
- Modify: `.github/workflows/deploy-pages.yml`

**Step 1: Add required permission**

Set top-level permissions:
```yaml
permissions:
  contents: write
  pages: write
  id-token: write
```

**Step 2: Add `release` job dependent on `deploy`**

```yaml
release:
  runs-on: ubuntu-latest
  needs: deploy
  if: github.ref == 'refs/heads/main'
  steps:
    - uses: actions/checkout@v4
```

**Step 3: Validate workflow syntax**

Run: `Get-Content .github/workflows/deploy-pages.yml`
Expected: valid YAML structure with new job.

**Step 4: Commit**

```bash
git add .github/workflows/deploy-pages.yml
git commit -m "ci: add release job scaffold for semver tagging"
```

### Task 3: Compute next semantic version from tags

**Files:**
- Modify: `.github/workflows/deploy-pages.yml`

**Step 1: Add compute-version step**

In release job, add a shell step that:
- reads `version.json` major/minor,
- finds latest matching tag `v<major>.<minor>.*`,
- calculates `patch` (`0` if none),
- exports `next_tag` and `previous_tag` to outputs.

Example script block:
```bash
major=$(jq -r '.major' version.json)
minor=$(jq -r '.minor' version.json)
latest=$(git tag -l "v${major}.${minor}.*" --sort=-v:refname | head -n 1)
if [ -z "$latest" ]; then patch=0; prev=""; else patch=$(( ${latest##*.} + 1 )); prev="$latest"; fi
next="v${major}.${minor}.${patch}"
echo "next_tag=$next" >> "$GITHUB_OUTPUT"
echo "previous_tag=$prev" >> "$GITHUB_OUTPUT"
```

**Step 2: Add guard step for existing tag**

```bash
if git rev-parse "${next_tag}" >/dev/null 2>&1; then
  echo "Tag already exists: ${next_tag}" >&2
  exit 1
fi
```

**Step 3: Validate locally (logic read-through + dry run output in workflow logs)**

Expected: logs show computed next tag.

**Step 4: Commit**

```bash
git add .github/workflows/deploy-pages.yml
git commit -m "ci: compute next semver tag from manual major-minor"
```

### Task 4: Create and push tag in workflow

**Files:**
- Modify: `.github/workflows/deploy-pages.yml`

**Step 1: Add tag creation step**

```bash
git config user.name "github-actions[bot]"
git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
git tag "${next_tag}"
git push origin "${next_tag}"
```

Use `${{ steps.compute.outputs.next_tag }}` for references.

**Step 2: Add failure clarity**

Ensure command fails loudly with explicit message when push denied.

**Step 3: Commit**

```bash
git add .github/workflows/deploy-pages.yml
git commit -m "ci: create and push semver release tag after deploy"
```

### Task 5: Generate release notes from diff and publish GitHub Release

**Files:**
- Modify: `.github/workflows/deploy-pages.yml`

**Step 1: Build release notes text from git log range**

If `previous_tag` exists:
```bash
range="${previous_tag}..HEAD"
```
Else:
```bash
range="HEAD"
```

Generate notes:
```bash
echo "## Changes" > release-notes.md
git log --pretty=format:'- %h %s' $range >> release-notes.md
```

**Step 2: Create GitHub release**

Use `gh` CLI or `actions/create-release` equivalent. Preferred with `gh`:
```bash
gh release create "${next_tag}" --title "${next_tag}" --notes-file release-notes.md
```

**Step 3: Add compare reference when previous tag exists**

Append line:
```bash
echo "" >> release-notes.md
echo "Compare: ${previous_tag}...${next_tag}" >> release-notes.md
```

**Step 4: Commit**

```bash
git add .github/workflows/deploy-pages.yml
git commit -m "ci: publish github release with diff-based notes"
```

### Task 6: Verification and documentation check

**Files:**
- Verify output only

**Step 1: Validate workflow file and version file present**

Run: `Get-ChildItem .github/workflows; Get-Content version.json`
Expected: release job and valid version file.

**Step 2: Push and observe Actions dry-run logs**

Run:
```bash
git push origin main
```
Expected in workflow logs:
- computed next tag,
- tag pushed,
- release created.

**Step 3: Manual repository verification**

- Tags page contains latest `vX.Y.Z`.
- Releases page contains release with commit-based notes.

**Step 4: Final commit (if verification-only changes were made)**

```bash
# only if any final edits occurred
```
