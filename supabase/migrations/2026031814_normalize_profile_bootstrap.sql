create or replace function public.profile_bootstrap()
returns jsonb
language sql
security definer
set search_path = public, auth, game
as $$
    select game.normalize_save_payload(game.bootstrap_player(auth.uid()));
$$;

revoke all on function public.profile_bootstrap() from public;
grant execute on function public.profile_bootstrap() to authenticated;
