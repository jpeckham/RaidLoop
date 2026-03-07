# SemVer Tags and Auto Release Notes Design

**Date:** 2026-03-06
**Status:** Approved

## Goal
Enhance GitHub Actions deploy pipeline to create semantic-versioned releases and generated release notes from diffs.

## Versioning Model
- Manual control of `major.minor` by editing `version.json`.
- Auto bump of patch/build number on each successful production build.
- Version tag format: `v<major>.<minor>.<patch>`.

## Source of Truth
- Add `version.json` at repo root, example:

```json
{
  "major": 1,
  "minor": 0
}
```

## Workflow Strategy
- Keep existing Pages deploy flow.
- Add release stage after successful deploy.
- Release stage computes next patch:
  - Find latest tag matching `v<major>.<minor>.*`.
  - If none exists, use patch `0`.
  - Else patch = latest + 1.
- Create and push tag for computed version.
- Create GitHub Release named by tag.

## Release Notes
- Generate from git diff since prior matching tag.
- Include:
  - Version header
  - Compare reference
  - Commit list (short SHA + subject)

## Guardrails
- Run tag/release only on `main`.
- Require `contents: write` permission.
- Fail with clear message if:
  - `version.json` is missing/invalid,
  - target tag already exists.
- Only tag/release after deploy success.

## Verification
- Add log step showing computed next version.
- Validate release notes are non-empty.
- Ensure no tag/release when deploy fails.

## Rollback Consideration
- If deployment fails, no release artifact is created.
- If release step fails after deployment, workflow clearly reports partial success and can be retried after correcting permissions/config.
