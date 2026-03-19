# Supabase Backend Notes

## Migrations

Apply the SQL files in `supabase/migrations/` in timestamp order.

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
