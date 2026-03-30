create or replace function game.bootstrap_player(target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $function$
declare
    payload_result jsonb;
    raid_session_payload jsonb;
    reconciled_payload jsonb;
    normalized_payload jsonb;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    insert into public.game_saves (user_id, save_version, payload)
    values (target_user_id, 1, game.default_save_payload())
    on conflict (user_id) do nothing;

    select payload
    into payload_result
    from public.game_saves
    where user_id = target_user_id;

    select payload
    into raid_session_payload
    from public.raid_sessions
    where user_id = target_user_id;

    if raid_session_payload is null then
        reconciled_payload := jsonb_set(payload_result, '{activeRaid}', 'null'::jsonb, true);
    else
        reconciled_payload := jsonb_set(payload_result, '{activeRaid}', game.normalize_active_raid_payload(raid_session_payload, coalesce(payload_result->'acceptedStats', payload_result->'AcceptedStats', '{}'::jsonb)), true);
    end if;

    normalized_payload := game.normalize_save_payload(reconciled_payload);

    update public.game_saves
    set payload = normalized_payload,
        save_version = 1,
        updated_at = timezone('utc', now())
    where user_id = target_user_id
      and payload is distinct from normalized_payload;

    return normalized_payload;
end;
$function$;

with reconciled_game_saves as (
    select
        saves.user_id,
        game.normalize_save_payload(
            case
                when sessions.payload is null then jsonb_set(saves.payload, '{activeRaid}', 'null'::jsonb, true)
                else jsonb_set(
                    saves.payload,
                    '{activeRaid}',
                    game.normalize_active_raid_payload(
                        sessions.payload,
                        coalesce(saves.payload->'acceptedStats', saves.payload->'AcceptedStats', '{}'::jsonb)
                    ),
                    true
                )
            end
        ) as normalized_payload
    from public.game_saves saves
    left join public.raid_sessions sessions
        on sessions.user_id = saves.user_id
)
update public.game_saves saves
set payload = reconciled_game_saves.normalized_payload,
    save_version = 1,
    updated_at = timezone('utc', now())
from reconciled_game_saves
where saves.user_id = reconciled_game_saves.user_id
  and saves.payload is distinct from reconciled_game_saves.normalized_payload;
