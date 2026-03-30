create or replace function game.bootstrap_player(target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $function$
declare
    payload_result jsonb;
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

    normalized_payload := game.normalize_save_payload(payload_result);

    update public.game_saves
    set payload = normalized_payload,
        save_version = 1,
        updated_at = timezone('utc', now())
    where user_id = target_user_id
      and payload is distinct from normalized_payload;

    return normalized_payload;
end;
$function$;

with normalized_game_saves as (
    select
        user_id,
        game.normalize_save_payload(payload) as normalized_payload
    from public.game_saves
)
update public.game_saves saves
set payload = normalized_game_saves.normalized_payload,
    save_version = 1,
    updated_at = timezone('utc', now())
from normalized_game_saves
where saves.user_id = normalized_game_saves.user_id
  and saves.payload is distinct from normalized_game_saves.normalized_payload;
