# Supabase Backend Notes

## Migrations

Apply the SQL files in `supabase/migrations/` in timestamp order.

## CI/CD Configuration

GitHub Actions production deploy expects:

- Repository variables:
  - `SUPABASE_PROJECT_ID`
  - `SUPABASE_URL`
  - `SUPABASE_PUBLISHABLE_KEY`
- Repository secrets:
  - `SUPABASE_ACCESS_TOKEN`
  - `SUPABASE_DB_PASSWORD`

The project id is the Supabase project ref, for example `dblgbpzlrglcdwqyagnx`, not the project display name.

## Manual CLI Deploy

From the repo root:

```bash
supabase link --project-ref "$SUPABASE_PROJECT_ID" --password "$SUPABASE_DB_PASSWORD"
supabase db push --linked --include-all --password "$SUPABASE_DB_PASSWORD"
supabase functions deploy profile-bootstrap --project-ref "$SUPABASE_PROJECT_ID"
supabase functions deploy profile-save --project-ref "$SUPABASE_PROJECT_ID"
supabase functions deploy game-action --project-ref "$SUPABASE_PROJECT_ID"
```

## Function Tests

Run the Edge Function handler tests with:

```bash
deno test supabase/functions/profile-bootstrap/handler.test.mjs supabase/functions/profile-save/handler.test.mjs
deno test supabase/functions/game-action/handler.test.mjs
```

## Manual Verification

After applying migrations:

1. Verify tables exist:

```sql
select to_regclass('public.game_saves');
select to_regclass('public.raid_sessions');
```

2. Verify RLS is enabled:

```sql
select relname, relrowsecurity
from pg_class
where relname in ('game_saves', 'raid_sessions');
```

3. Verify bootstrap function exists:

```sql
select routine_name
from information_schema.routines
where routine_schema = 'game'
  and routine_name = 'bootstrap_player';
```

4. Verify bootstrap creates a starter save for an authenticated user:

```sql
select game.bootstrap_player(auth.uid());
```

5. Verify the created save row:

```sql
select user_id, save_version, payload, created_at, updated_at
from public.game_saves
where user_id = auth.uid();
```
