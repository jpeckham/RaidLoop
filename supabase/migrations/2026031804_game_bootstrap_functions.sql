create schema if not exists game;

create or replace function game.default_save_payload()
returns jsonb
language sql
stable
as $$
    select jsonb_build_object(
        'MainStash', jsonb_build_array(
            jsonb_build_object('Name', 'Light Pistol', 'Type', 0, 'Value', 12, 'Slots', 1, 'Rarity', 0, 'DisplayRarity', 1),
            jsonb_build_object('Name', 'Drum SMG', 'Type', 0, 'Value', 20, 'Slots', 1, 'Rarity', 1, 'DisplayRarity', 2),
            jsonb_build_object('Name', 'Field Carbine', 'Type', 0, 'Value', 34, 'Slots', 1, 'Rarity', 2, 'DisplayRarity', 3),
            jsonb_build_object('Name', 'Soft Armor Vest', 'Type', 1, 'Value', 14, 'Slots', 1, 'Rarity', 0, 'DisplayRarity', 1),
            jsonb_build_object('Name', 'Light Plate Carrier', 'Type', 1, 'Value', 30, 'Slots', 1, 'Rarity', 2, 'DisplayRarity', 3),
            jsonb_build_object('Name', 'Small Backpack', 'Type', 2, 'Value', 18, 'Slots', 1, 'Rarity', 1, 'DisplayRarity', 2),
            jsonb_build_object('Name', 'Tactical Backpack', 'Type', 2, 'Value', 28, 'Slots', 2, 'Rarity', 2, 'DisplayRarity', 3),
            jsonb_build_object('Name', 'Medkit', 'Type', 3, 'Value', 10, 'Slots', 1, 'Rarity', 0, 'DisplayRarity', 1),
            jsonb_build_object('Name', 'Bandage', 'Type', 4, 'Value', 4, 'Slots', 1, 'Rarity', 0, 'DisplayRarity', 0),
            jsonb_build_object('Name', 'Ammo Box', 'Type', 4, 'Value', 6, 'Slots', 1, 'Rarity', 0, 'DisplayRarity', 0)
        ),
        'RandomCharacterAvailableAt', '0001-01-01T00:00:00+00:00',
        'RandomCharacter', null,
        'Money', 500,
        'OnPersonItems', jsonb_build_array()
    );
$$;

create or replace function game.bootstrap_player(target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    payload_result jsonb;
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

    return payload_result;
end;
$$;

revoke all on function game.default_save_payload() from public;
revoke all on function game.bootstrap_player(uuid) from public;
