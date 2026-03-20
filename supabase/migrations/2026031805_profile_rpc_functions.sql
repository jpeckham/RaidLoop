create or replace function public.profile_bootstrap()
returns jsonb
language sql
security definer
set search_path = public, auth, game
as $$
    select game.bootstrap_player(auth.uid());
$$;

create or replace function public.profile_save(snapshot jsonb)
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    target_user_id uuid := auth.uid();
    payload_result jsonb;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    insert into public.game_saves (user_id, save_version, payload)
    values (target_user_id, 1, snapshot)
    on conflict (user_id) do update
        set payload = excluded.payload,
            save_version = excluded.save_version,
            updated_at = timezone('utc', now())
    returning payload into payload_result;

    return payload_result;
end;
$$;

revoke all on function public.profile_bootstrap() from public;
revoke all on function public.profile_save(jsonb) from public;

grant execute on function public.profile_bootstrap() to authenticated;
grant execute on function public.profile_save(jsonb) to authenticated;
