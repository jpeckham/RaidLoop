# Supabase Environment Strategy for RaidLoop

## Overview

This document defines a clean, scalable approach for managing **multiple environments** (local, dev, staging, prod) when using Supabase with schema migrations and CI/CD.

The key principle:

> **A Supabase Project = One Environment**

---

## Why This Matters

Using a single database for multiple environments leads to:

- Data leakage between environments
- Complex filtering logic (`environment` columns everywhere)
- Risky migrations
- Broken Row-Level Security (RLS)
- Difficult debugging

Instead, we isolate environments at the **project level**.

---

## Target Architecture

```
Local (Supabase CLI)
        ↓
Dev (Supabase Project)
        ↓
Staging (Supabase Project)
        ↓
Prod (Supabase Project)
```

Each environment has:
- Its own database
- Its own auth system
- Its own storage
- Its own configuration

---

## Repository Structure

```
/supabase
  /migrations
  seed.sql

/.github/workflows
  deploy.yml
```

You maintain **one set of migrations** across all environments.

---

## Local Development

Start local Supabase:

```
supabase start
```

Create migrations:

```
supabase migration new add_users_table
```

Apply locally:

```
supabase db reset
```

Generate diff:

```
supabase db diff
```

---

## Environment Setup

You will create separate Supabase projects:

- raidloop-dev
- raidloop-staging
- raidloop-prod

For each project, configure:

### Auth Settings
- Enable email auth
- Configure OAuth providers (optional)
- Set redirect URLs

### Redirect URLs

Dev:
```
http://localhost:3000
```

Staging:
```
https://staging.raidloop.com
```

Prod:
```
https://raidloop.com
```

---

## Environment Variables

Each environment uses different credentials.

### Example

```
SUPABASE_URL=...
SUPABASE_ANON_KEY=...
SUPABASE_SERVICE_ROLE_KEY=...
```

Store in:

- `.env.local` (local dev)
- GitHub Secrets (CI/CD)

---

## CI/CD Deployment

Example GitHub Actions workflow:

```yaml
name: Deploy Supabase

on:
  push:
    branches:
      - main

jobs:
  deploy-dev:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Install Supabase CLI
        run: npm install -g supabase

      - name: Link to Dev
        run: supabase link --project-ref $DEV_PROJECT_REF
        env:
          SUPABASE_ACCESS_TOKEN: ${{ secrets.SUPABASE_ACCESS_TOKEN }}

      - name: Push Migrations
        run: supabase db push

  deploy-prod:
    needs: deploy-dev
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Install Supabase CLI
        run: npm install -g supabase

      - name: Link to Prod
        run: supabase link --project-ref $PROD_PROJECT_REF
        env:
          SUPABASE_ACCESS_TOKEN: ${{ secrets.SUPABASE_ACCESS_TOKEN }}

      - name: Push Migrations
        run: supabase db push
```

---

## Promotion Model

You promote **schema changes**, not data:

```
local → dev → staging → prod
```

Each step applies the same migrations.

---

## Seeding Data

Use environment-specific seed files:

```
seed.dev.sql
seed.prod.sql
```

Apply in CI:

```
psql < seed.dev.sql
```

---

## Auth Strategy

Auth must be configured per environment.

### Options

#### Separate OAuth Apps (Recommended)
- Google Dev App
- Google Prod App

#### Shared OAuth App
- Multiple redirect URLs
- More fragile

---

## What Not to Do

Avoid:

- One database with `environment` column
- Schema-per-environment inside one DB
- Sharing Supabase project across environments

---

## Mental Model

```
Supabase Project = Environment
Database = State
Migrations = Schema
Auth = Configuration
```

---

## Summary

- Use one Supabase project per environment
- Keep a single migrations folder
- Use CI/CD to promote schema changes
- Configure auth once per environment
- Use local Supabase for development

---

## Future Enhancements

- Preview environments per PR
- Drift detection in CI
- Automated project provisioning via API

---

End of document.
