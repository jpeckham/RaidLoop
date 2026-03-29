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
        'playerDexterity', coalesce((payload->>'playerDexterity')::int, (payload->>'PlayerDexterity')::int, 10),
        'playerConstitution', coalesce((payload->>'playerConstitution')::int, (payload->>'PlayerConstitution')::int, 10),
        'playerMaxHealth', coalesce((payload->>'playerMaxHealth')::int, (payload->>'PlayerMaxHealth')::int, 10 + (2 * coalesce((payload->>'playerConstitution')::int, (payload->>'PlayerConstitution')::int, 10))),
        'mainStash', game.normalize_items(coalesce(payload->'mainStash', payload->'MainStash')),
        'onPersonItems', game.normalize_on_person_items(coalesce(payload->'onPersonItems', payload->'OnPersonItems')),
        'randomCharacterAvailableAt', settled_random_state->'randomCharacterAvailableAt',
        'randomCharacter', settled_random_state->'randomCharacter',
        'activeRaid', coalesce(payload->'activeRaid', payload->'ActiveRaid', 'null'::jsonb)
    );
end;
$$;

create or replace function game.default_save_payload()
returns jsonb
language sql
stable
as $$
    select game.normalize_save_payload(
        jsonb_build_object(
            'mainStash', jsonb_build_array(
                jsonb_build_object('name', 'Makarov', 'type', 0, 'value', 12, 'slots', 1, 'rarity', 0, 'displayRarity', 1),
                jsonb_build_object('name', 'PPSH', 'type', 0, 'value', 20, 'slots', 1, 'rarity', 1, 'displayRarity', 2),
                jsonb_build_object('name', 'AK74', 'type', 0, 'value', 34, 'slots', 1, 'rarity', 2, 'displayRarity', 3),
                jsonb_build_object('name', '6B2 body armor', 'type', 1, 'value', 14, 'slots', 1, 'rarity', 0, 'displayRarity', 1),
                jsonb_build_object('name', '6B13 assault armor', 'type', 1, 'value', 30, 'slots', 1, 'rarity', 2, 'displayRarity', 3),
                jsonb_build_object('name', 'Small Backpack', 'type', 2, 'value', 18, 'slots', 1, 'rarity', 1, 'displayRarity', 2),
                jsonb_build_object('name', 'Tactical Backpack', 'type', 2, 'value', 28, 'slots', 2, 'rarity', 2, 'displayRarity', 3),
                jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 10, 'slots', 1, 'rarity', 0, 'displayRarity', 1),
                jsonb_build_object('name', 'Bandage', 'type', 4, 'value', 4, 'slots', 1, 'rarity', 0, 'displayRarity', 0),
                jsonb_build_object('name', 'Ammo Box', 'type', 4, 'value', 6, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
            ),
            'playerDexterity', 10,
            'playerConstitution', 10,
            'playerMaxHealth', 30,
            'randomCharacterAvailableAt', '0001-01-01T00:00:00+00:00',
            'randomCharacter', null,
            'money', 500,
            'onPersonItems', jsonb_build_array(),
            'activeRaid', null
        )
    );
$$;

revoke all on function game.normalize_save_payload(jsonb) from public;
revoke all on function game.default_save_payload() from public;
