create or replace function game.authored_item(item_name text)
returns jsonb
language sql
stable
as $$
    select case item_name
        when 'Rusty Knife' then jsonb_build_object('name', 'Rusty Knife', 'type', 0, 'value', 1, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        when 'Light Pistol' then jsonb_build_object('name', 'Light Pistol', 'type', 0, 'value', 60, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        when 'Drum SMG' then jsonb_build_object('name', 'Drum SMG', 'type', 0, 'value', 160, 'slots', 1, 'rarity', 1, 'displayRarity', 2)
        when 'Field Carbine' then jsonb_build_object('name', 'Field Carbine', 'type', 0, 'value', 320, 'slots', 1, 'rarity', 2, 'displayRarity', 3)
        when 'Marksman Rifle' then jsonb_build_object('name', 'Marksman Rifle', 'type', 0, 'value', 550, 'slots', 1, 'rarity', 3, 'displayRarity', 4)
        when 'Battle Rifle' then jsonb_build_object('name', 'Battle Rifle', 'type', 0, 'value', 375, 'slots', 1, 'rarity', 2, 'displayRarity', 3)
        when 'Support Machine Gun' then jsonb_build_object('name', 'Support Machine Gun', 'type', 0, 'value', 800, 'slots', 1, 'rarity', 4, 'displayRarity', 5)
        when 'Soft Armor Vest' then jsonb_build_object('name', 'Soft Armor Vest', 'type', 1, 'value', 95, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        when 'Light Plate Carrier' then jsonb_build_object('name', 'Light Plate Carrier', 'type', 1, 'value', 225, 'slots', 1, 'rarity', 2, 'displayRarity', 3)
        when 'Medium Plate Carrier' then jsonb_build_object('name', 'Medium Plate Carrier', 'type', 1, 'value', 375, 'slots', 1, 'rarity', 3, 'displayRarity', 4)
        when 'Heavy Plate Carrier' then jsonb_build_object('name', 'Heavy Plate Carrier', 'type', 1, 'value', 450, 'slots', 1, 'rarity', 4, 'displayRarity', 5)
        when 'Assault Plate Carrier' then jsonb_build_object('name', 'Assault Plate Carrier', 'type', 1, 'value', 650, 'slots', 1, 'rarity', 4, 'displayRarity', 5)
        when 'Small Backpack' then jsonb_build_object('name', 'Small Backpack', 'type', 2, 'value', 25, 'slots', 1, 'rarity', 1, 'displayRarity', 2)
        when 'Tactical Backpack' then jsonb_build_object('name', 'Tactical Backpack', 'type', 2, 'value', 75, 'slots', 2, 'rarity', 2, 'displayRarity', 3)
        when 'Hiking Backpack' then jsonb_build_object('name', 'Hiking Backpack', 'type', 2, 'value', 400, 'slots', 3, 'rarity', 3, 'displayRarity', 4)
        when 'Raid Backpack' then jsonb_build_object('name', 'Raid Backpack', 'type', 2, 'value', 600, 'slots', 4, 'rarity', 4, 'displayRarity', 5)
        when 'Medkit' then jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 30, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        when 'Bandage' then jsonb_build_object('name', 'Bandage', 'type', 4, 'value', 15, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
        when 'Ammo Box' then jsonb_build_object('name', 'Ammo Box', 'type', 4, 'value', 20, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
        when 'Scrap Metal' then jsonb_build_object('name', 'Scrap Metal', 'type', 5, 'value', 18, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
        when 'Rare Scope' then jsonb_build_object('name', 'Rare Scope', 'type', 5, 'value', 80, 'slots', 1, 'rarity', 2, 'displayRarity', 0)
        when 'Legendary Trigger Group' then jsonb_build_object('name', 'Legendary Trigger Group', 'type', 5, 'value', 150, 'slots', 1, 'rarity', 4, 'displayRarity', 0)
        else null
    end;
$$;

create or replace function game.normalize_item(item jsonb)
returns jsonb
language sql
stable
as $$
    with normalized_name as (
        select coalesce(item->>'name', item->>'Name') as item_name
    )
    select coalesce(
        game.authored_item((select item_name from normalized_name)),
        jsonb_build_object(
            'name', (select item_name from normalized_name),
            'type', coalesce((item->>'type')::int, (item->>'Type')::int, 0),
            'value', coalesce((item->>'value')::int, (item->>'Value')::int, 1),
            'slots', coalesce((item->>'slots')::int, (item->>'Slots')::int, 1),
            'rarity', coalesce((item->>'rarity')::int, (item->>'Rarity')::int, 0),
            'displayRarity', coalesce((item->>'displayRarity')::int, (item->>'DisplayRarity')::int, 1)
        )
    );
$$;

create or replace function game.default_save_payload()
returns jsonb
language sql
stable
as $$
    select game.normalize_save_payload(
        jsonb_build_object(
            'mainStash', jsonb_build_array(
                game.authored_item('Light Pistol'),
                game.authored_item('Drum SMG'),
                game.authored_item('Field Carbine'),
                game.authored_item('Soft Armor Vest'),
                game.authored_item('Light Plate Carrier'),
                game.authored_item('Small Backpack'),
                game.authored_item('Tactical Backpack'),
                game.authored_item('Medkit'),
                game.authored_item('Bandage'),
                game.authored_item('Ammo Box')
            ),
            'randomCharacterAvailableAt', '0001-01-01T00:00:00+00:00',
            'randomCharacter', null,
            'money', 500,
            'onPersonItems', jsonb_build_array(),
            'activeRaid', null
        )
    );
$$;

create or replace function game.ensure_knife_fallback(stash jsonb, on_person_items jsonb)
returns jsonb
language sql
stable
as $$
    select case
        when game.has_weapon(stash, on_person_items) then coalesce(stash, '[]'::jsonb)
        else coalesce(stash, '[]'::jsonb) || jsonb_build_array(game.authored_item('Rusty Knife'))
    end;
$$;

create or replace function game.shop_item(item_name text)
returns jsonb
language sql
stable
as $$
    select case item_name
        when 'Medkit' then game.authored_item('Medkit')
        when 'Light Pistol' then game.authored_item('Light Pistol')
        when 'Small Backpack' then game.authored_item('Small Backpack')
        else null
    end;
$$;

create or replace function game.random_luck_run_loadout()
returns jsonb
language sql
volatile
as $$
    select jsonb_build_array(
        game.authored_item('Light Pistol'),
        case when random() < 0.5
            then game.authored_item('Small Backpack')
            else game.authored_item('Tactical Backpack')
        end,
        game.authored_item('Medkit'),
        game.authored_item('Bandage'),
        game.authored_item('Ammo Box')
    );
$$;

create or replace function game.raid_extractable_items(raid_payload jsonb)
returns jsonb
language sql
stable
as $$
    with base_items as (
        select value, ordinality
        from jsonb_array_elements(coalesce(raid_payload->'equippedItems', '[]'::jsonb)) with ordinality
        union all
        select value, 1000 + ordinality
        from jsonb_array_elements(coalesce(raid_payload->'carriedLoot', '[]'::jsonb)) with ordinality
        union all
        select game.authored_item('Medkit'), 2000 + ordinality
        from generate_series(1, greatest(coalesce((raid_payload->>'medkits')::int, 0), 0)) with ordinality
    )
    select coalesce(
        (
            select jsonb_agg(game.normalize_item(value) order by ordinality)
            from base_items
        ),
        '[]'::jsonb
    );
$$;

create or replace function game.random_enemy_loadout()
returns jsonb
language sql
volatile
as $$
    select case floor(random() * 5)::int
        when 0 then jsonb_build_array(game.authored_item('Light Pistol'))
        when 1 then jsonb_build_array(
            game.authored_item('Drum SMG'),
            game.authored_item('Bandage')
        )
        when 2 then jsonb_build_array(
            game.authored_item('Field Carbine'),
            game.authored_item('Soft Armor Vest')
        )
        when 3 then jsonb_build_array(game.authored_item('Marksman Rifle'))
        else jsonb_build_array(
            game.authored_item('Battle Rifle'),
            game.authored_item('Medium Plate Carrier')
        )
    end;
$$;

create or replace function game.random_loot_items_for_container(container_name text)
returns jsonb
language sql
volatile
as $$
    select case container_name
        when 'Weapons Crate' then
            case floor(random() * 4)::int
                when 0 then jsonb_build_array(
                    game.authored_item('Light Pistol'),
                    game.authored_item('Ammo Box')
                )
                when 1 then jsonb_build_array(game.authored_item('Drum SMG'))
                when 2 then jsonb_build_array(game.authored_item('Field Carbine'))
                else jsonb_build_array(game.authored_item('Marksman Rifle'))
            end
        when 'Medical Container' then
            case floor(random() * 3)::int
                when 0 then jsonb_build_array(
                    game.authored_item('Medkit'),
                    game.authored_item('Bandage')
                )
                when 1 then jsonb_build_array(
                    game.authored_item('Bandage'),
                    game.authored_item('Ammo Box')
                )
                else jsonb_build_array(game.authored_item('Medkit'))
            end
        when 'Dead Body' then game.random_enemy_loadout()
        else
            case floor(random() * 4)::int
                when 0 then jsonb_build_array(
                    game.authored_item('Bandage'),
                    game.authored_item('Ammo Box')
                )
                when 1 then jsonb_build_array(
                    game.authored_item('Scrap Metal'),
                    game.authored_item('Rare Scope')
                )
                when 2 then jsonb_build_array(game.authored_item('Medkit'))
                else jsonb_build_array(game.authored_item('Light Pistol'))
            end
    end;
$$;

revoke all on function game.authored_item(text) from public;
