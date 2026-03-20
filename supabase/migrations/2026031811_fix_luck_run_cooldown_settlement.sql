create or replace function game.settle_random_character(random_character jsonb, random_available_at jsonb)
returns jsonb
language plpgsql
volatile
as $$
declare
    normalized_random_character jsonb := game.normalize_random_character(random_character);
    normalized_available_at jsonb := coalesce(random_available_at, to_jsonb('0001-01-01T00:00:00+00:00'::text));
begin
    if normalized_random_character is not null
       and jsonb_array_length(coalesce(normalized_random_character->'inventory', '[]'::jsonb)) = 0 then
        normalized_random_character := null;
        if trim(both '"' from normalized_available_at::text) = '0001-01-01T00:00:00+00:00' then
            normalized_available_at := to_jsonb((timezone('utc', now()) + interval '5 minutes')::text);
        end if;
    end if;

    return jsonb_build_object(
        'randomCharacterAvailableAt', normalized_available_at,
        'randomCharacter', normalized_random_character
    );
end;
$$;

create or replace function game.normalize_save_payload(payload jsonb)
returns jsonb
language plpgsql
volatile
as $$
declare
    settled_random_state jsonb := game.settle_random_character(
        coalesce(payload->'randomCharacter', payload->'RandomCharacter'),
        coalesce(payload->'randomCharacterAvailableAt', payload->'RandomCharacterAvailableAt', to_jsonb('0001-01-01T00:00:00+00:00'::text)));
begin
    return jsonb_build_object(
        'money', greatest(coalesce((payload->>'money')::int, (payload->>'Money')::int, 0), 0),
        'mainStash', game.normalize_items(coalesce(payload->'mainStash', payload->'MainStash')),
        'onPersonItems', game.normalize_on_person_items(coalesce(payload->'onPersonItems', payload->'OnPersonItems')),
        'randomCharacterAvailableAt', settled_random_state->'randomCharacterAvailableAt',
        'randomCharacter', settled_random_state->'randomCharacter',
        'activeRaid', coalesce(payload->'activeRaid', payload->'ActiveRaid', 'null'::jsonb)
    );
end;
$$;

create or replace function game.finish_raid_action(
    save_payload jsonb,
    raid_payload jsonb,
    raid_profile text,
    extracted boolean,
    target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    updated_save jsonb := game.normalize_save_payload(save_payload);
    stash jsonb := coalesce(updated_save->'mainStash', '[]'::jsonb);
    on_person_items jsonb := coalesce(updated_save->'onPersonItems', '[]'::jsonb);
    random_character jsonb := updated_save->'randomCharacter';
    random_available_at jsonb := updated_save->'randomCharacterAvailableAt';
    settled_random_state jsonb;
    extractable_items jsonb := case when extracted then game.raid_extractable_items(raid_payload) else '[]'::jsonb end;
begin
    if raid_profile = 'main' then
        if extracted then
            on_person_items := coalesce(
                (
                    select jsonb_agg(
                        jsonb_build_object(
                            'item', game.normalize_item(value),
                            'isEquipped', coalesce((value->>'type')::int, -1) in (0, 1, 2))
                        order by ordinality)
                    from jsonb_array_elements(extractable_items) with ordinality
                ),
                '[]'::jsonb
            );
        else
            on_person_items := '[]'::jsonb;
        end if;

        stash := game.ensure_knife_fallback(stash, on_person_items);
        updated_save := jsonb_set(updated_save, '{mainStash}', game.normalize_items(stash), true);
        updated_save := jsonb_set(updated_save, '{onPersonItems}', game.normalize_on_person_items(on_person_items), true);
    else
        if extracted and random_character is not null then
            random_character := jsonb_build_object(
                'name', coalesce(random_character->>'name', 'Unknown'),
                'inventory', game.normalize_items(extractable_items)
            );
        else
            random_character := null;
        end if;

        settled_random_state := game.settle_random_character(random_character, random_available_at);
        updated_save := jsonb_set(updated_save, '{randomCharacter}', coalesce(settled_random_state->'randomCharacter', 'null'::jsonb), true);
        updated_save := jsonb_set(updated_save, '{randomCharacterAvailableAt}', settled_random_state->'randomCharacterAvailableAt', true);
    end if;

    updated_save := jsonb_set(updated_save, '{activeRaid}', 'null'::jsonb, true);

    update public.game_saves
    set payload = updated_save,
        save_version = 1,
        updated_at = timezone('utc', now())
    where user_id = target_user_id;

    delete from public.raid_sessions
    where user_id = target_user_id;

    return updated_save;
end;
$$;

revoke all on function game.settle_random_character(jsonb, jsonb) from public;
