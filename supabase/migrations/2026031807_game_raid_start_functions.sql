create or replace function game.weapon_magazine_capacity(weapon_name text)
returns int
language sql
stable
as $$
    select case weapon_name
        when 'PPSH' then 35
        when 'AK74' then 30
        when 'SVDS' then 20
        when 'AK47' then 30
        when 'PKP' then 100
        when 'Rusty Knife' then 0
        else 8
    end;
$$;

create or replace function game.backpack_capacity(backpack_name text)
returns int
language sql
stable
as $$
    select case backpack_name
        when '6Sh118' then 10
        when 'Tasmanian Tiger Trooper 35' then 8
        when 'Tactical Backpack' then 6
        when 'Small Backpack' then 3
        else 2
    end;
$$;

create or replace function game.random_raider_name()
returns text
language sql
volatile
as $$
    select (array['Ghost', 'Moth', 'Brick', 'Vex', 'Nail', 'Echo'])[1 + floor(random() * 6)::int]
        || '-' ||
        (100 + floor(random() * 900)::int)::text;
$$;

create or replace function game.random_luck_run_loadout()
returns jsonb
language sql
volatile
as $$
    select jsonb_build_array(
        jsonb_build_object('name', 'Makarov', 'type', 0, 'value', 12, 'slots', 1, 'rarity', 0, 'displayRarity', 1),
        case when random() < 0.5
            then jsonb_build_object('name', 'Small Backpack', 'type', 2, 'value', 18, 'slots', 1, 'rarity', 1, 'displayRarity', 2)
            else jsonb_build_object('name', 'Tactical Backpack', 'type', 2, 'value', 28, 'slots', 2, 'rarity', 2, 'displayRarity', 3)
        end,
        jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 10, 'slots', 1, 'rarity', 0, 'displayRarity', 1),
        jsonb_build_object('name', 'Bandage', 'type', 4, 'value', 4, 'slots', 1, 'rarity', 0, 'displayRarity', 0),
        jsonb_build_object('name', 'Ammo Box', 'type', 4, 'value', 6, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
    );
$$;

create or replace function game.random_container_name()
returns text
language sql
volatile
as $$
    select (array['Filing Cabinet', 'Weapons Crate', 'Medical Container', 'Dead Body'])[1 + floor(random() * 4)::int];
$$;

create or replace function game.random_loot_items()
returns jsonb
language sql
volatile
as $$
    select case floor(random() * 4)::int
        when 0 then jsonb_build_array(
            jsonb_build_object('name', 'Bandage', 'type', 4, 'value', 4, 'slots', 1, 'rarity', 0, 'displayRarity', 0),
            jsonb_build_object('name', 'Ammo Box', 'type', 4, 'value', 6, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
        )
        when 1 then jsonb_build_array(
            jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 10, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        )
        when 2 then jsonb_build_array(
            jsonb_build_object('name', 'Scrap Metal', 'type', 5, 'value', 5, 'slots', 1, 'rarity', 0, 'displayRarity', 0),
            jsonb_build_object('name', 'Rare Scope', 'type', 5, 'value', 16, 'slots', 1, 'rarity', 2, 'displayRarity', 0)
        )
        else jsonb_build_array(
            jsonb_build_object('name', 'Makarov', 'type', 0, 'value', 12, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        )
    end;
$$;

create or replace function game.build_raid_snapshot(loadout jsonb, raider_name text)
returns jsonb
language plpgsql
volatile
as $$
declare
    equipped_items jsonb := '[]'::jsonb;
    carried_loot jsonb := '[]'::jsonb;
    discovered_loot jsonb := '[]'::jsonb;
    medkits int := 0;
    equipped_weapon_name text := 'Rusty Knife';
    equipped_backpack_name text := '';
    encounter_roll float8 := random();
    encounter_type text := 'Neutral';
    encounter_title text := 'Area Clear';
    encounter_description text := 'Area looks quiet. Nothing useful here.';
    enemy_name text := '';
    enemy_health int := 0;
    loot_container text := '';
    entry jsonb;
begin
    for entry in
        select value
        from jsonb_array_elements(coalesce(loadout, '[]'::jsonb))
    loop
        if coalesce((entry->>'type')::int, -1) in (0, 1, 2) then
            equipped_items := equipped_items || jsonb_build_array(entry);
            if coalesce((entry->>'type')::int, -1) = 0 then
                equipped_weapon_name := entry->>'name';
            elsif coalesce((entry->>'type')::int, -1) = 2 then
                equipped_backpack_name := entry->>'name';
            end if;
        elsif entry->>'name' = 'Medkit' then
            medkits := medkits + 1;
        else
            carried_loot := carried_loot || jsonb_build_array(entry);
        end if;
    end loop;

    if encounter_roll < 0.5 then
        encounter_type := 'Combat';
        encounter_title := 'Combat Encounter';
        encounter_description := 'Enemy contact on your position.';
        enemy_name := case when random() < 0.65 then 'Scav' else 'Patrol Guard' end;
        enemy_health := 12 + floor(random() * 9)::int;
    elsif encounter_roll < 0.8 then
        encounter_type := 'Loot';
        encounter_title := 'Loot Encounter';
        encounter_description := 'A searchable container appears.';
        loot_container := game.random_container_name();
        discovered_loot := game.random_loot_items();
    end if;

    return jsonb_build_object(
        'health', 30,
        'backpackCapacity', game.backpack_capacity(equipped_backpack_name),
        'ammo', game.weapon_magazine_capacity(equipped_weapon_name),
        'weaponMalfunction', false,
        'medkits', medkits,
        'lootSlots', 0,
        'extractProgress', 0,
        'extractRequired', 3,
        'encounterType', encounter_type,
        'encounterTitle', encounter_title,
        'encounterDescription', encounter_description,
        'enemyName', enemy_name,
        'enemyHealth', enemy_health,
        'lootContainer', loot_container,
        'awaitingDecision', false,
        'discoveredLoot', discovered_loot,
        'carriedLoot', carried_loot,
        'equippedItems', equipped_items,
        'logEntries', jsonb_build_array(format('Raid started as %s.', raider_name))
    );
end;
$$;

create or replace function game.start_raid_action(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    save_payload jsonb;
    on_person_items jsonb;
    random_character jsonb;
    loadout jsonb := '[]'::jsonb;
    raid_snapshot jsonb;
    raider_name text := 'Main Character';
    idx int;
    entry jsonb;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    on_person_items := coalesce(save_payload->'onPersonItems', '[]'::jsonb);
    random_character := save_payload->'randomCharacter';

    if action = 'start-main-raid' then
        if random_character is not null and jsonb_array_length(coalesce(random_character->'inventory', '[]'::jsonb)) > 0 then
            return save_payload;
        end if;

        if exists (
            select 1
            from jsonb_array_elements(on_person_items) entry
            where coalesce((coalesce(entry->'item', entry->'Item')->>'type')::int, -1) in (0, 1, 2)
              and not coalesce((entry->>'isEquipped')::boolean, (entry->>'IsEquipped')::boolean, false)
        ) then
            return save_payload;
        end if;

        if not exists (
            select 1
            from jsonb_array_elements(on_person_items) entry
            where coalesce((coalesce(entry->'item', entry->'Item')->>'type')::int, -1) = 0
              and coalesce((entry->>'isEquipped')::boolean, (entry->>'IsEquipped')::boolean, false)
        ) then
            return save_payload;
        end if;

        for entry in
            select coalesce(value->'item', value->'Item')
            from jsonb_array_elements(on_person_items) value
        loop
            loadout := loadout || jsonb_build_array(game.normalize_item(entry));
        end loop;

        raid_snapshot := game.build_raid_snapshot(loadout, raider_name);

        insert into public.raid_sessions (user_id, profile, payload)
        values (target_user_id, 'main', raid_snapshot)
        on conflict (user_id) do update
            set profile = excluded.profile,
                payload = excluded.payload,
                updated_at = timezone('utc', now());

        save_payload := jsonb_set(save_payload, '{activeRaid}', raid_snapshot, true);
        update public.game_saves
        set payload = save_payload,
            save_version = 1,
            updated_at = timezone('utc', now())
        where user_id = target_user_id;

        return save_payload;
    elsif action = 'start-random-raid' then
        if random_character is not null and jsonb_array_length(coalesce(random_character->'inventory', '[]'::jsonb)) > 0 then
            return save_payload;
        end if;

        if (save_payload->>'randomCharacterAvailableAt')::timestamptz > timezone('utc', now()) then
            return save_payload;
        end if;

        random_character := jsonb_build_object(
            'name', game.random_raider_name(),
            'inventory', game.random_luck_run_loadout()
        );
        raider_name := random_character->>'name';
        loadout := coalesce(random_character->'inventory', '[]'::jsonb);
        raid_snapshot := game.build_raid_snapshot(loadout, raider_name);

        insert into public.raid_sessions (user_id, profile, payload)
        values (target_user_id, 'random', raid_snapshot)
        on conflict (user_id) do update
            set profile = excluded.profile,
                payload = excluded.payload,
                updated_at = timezone('utc', now());

        save_payload := jsonb_set(save_payload, '{randomCharacter}', random_character, true);
        save_payload := jsonb_set(save_payload, '{activeRaid}', raid_snapshot, true);
        update public.game_saves
        set payload = save_payload,
            save_version = 1,
            updated_at = timezone('utc', now())
        where user_id = target_user_id;

        return save_payload;
    end if;

    return save_payload;
end;
$$;

create or replace function public.game_action(action text, payload jsonb)
returns jsonb
language sql
security definer
set search_path = public, auth, game
as $$
    select case
        when action in ('start-main-raid', 'start-random-raid')
            then game.start_raid_action(action, payload, auth.uid())
        else game.apply_profile_action(action, payload, auth.uid())
    end;
$$;

revoke all on function game.weapon_magazine_capacity(text) from public;
revoke all on function game.backpack_capacity(text) from public;
revoke all on function game.random_raider_name() from public;
revoke all on function game.random_luck_run_loadout() from public;
revoke all on function game.random_container_name() from public;
revoke all on function game.random_loot_items() from public;
revoke all on function game.build_raid_snapshot(jsonb, text) from public;
revoke all on function game.start_raid_action(text, jsonb, uuid) from public;
