create or replace function game.normalize_save_payload(payload jsonb)
returns jsonb
language plpgsql
volatile
as $$
declare
    settled_random_state jsonb := game.settle_random_character(
        coalesce(payload->'randomCharacter', payload->'RandomCharacter'),
        coalesce(payload->'randomCharacterAvailableAt', payload->'RandomCharacterAvailableAt', to_jsonb('0001-01-01T00:00:00+00:00'::text)));
    normalized_player_constitution int := greatest(coalesce((payload->>'playerConstitution')::int, (payload->>'PlayerConstitution')::int, 10), 0);
    derived_player_max_health int := 10 + (2 * normalized_player_constitution);
    normalized_player_max_health int := greatest(
        coalesce(nullif(greatest(coalesce((payload->>'playerMaxHealth')::int, (payload->>'PlayerMaxHealth')::int, 0), 0), 0), derived_player_max_health),
        1);
begin
    return jsonb_build_object(
        'money', greatest(coalesce((payload->>'money')::int, (payload->>'Money')::int, 0), 0),
        'playerDexterity', coalesce((payload->>'playerDexterity')::int, (payload->>'PlayerDexterity')::int, 10),
        'playerConstitution', normalized_player_constitution,
        'playerMaxHealth', normalized_player_max_health,
        'mainStash', game.normalize_items(coalesce(payload->'mainStash', payload->'MainStash')),
        'onPersonItems', game.normalize_on_person_items(coalesce(payload->'onPersonItems', payload->'OnPersonItems')),
        'randomCharacterAvailableAt', settled_random_state->'randomCharacterAvailableAt',
        'randomCharacter', settled_random_state->'randomCharacter',
        'activeRaid', coalesce(payload->'activeRaid', payload->'ActiveRaid', 'null'::jsonb)
    );
end;
$$;

update public.game_saves
set payload = game.normalize_save_payload(payload),
    updated_at = timezone('utc', now())
where payload is distinct from game.normalize_save_payload(payload);
