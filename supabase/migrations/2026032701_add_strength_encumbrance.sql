alter table game.item_defs
    add column if not exists weight int not null default 0;

update game.item_defs
set weight = case name
    when 'Rusty Knife' then 2
    when 'Makarov' then 4
    when 'PPSH' then 8
    when 'AK74' then 9
    when 'AK47' then 10
    when 'SVDS' then 11
    when 'PKP' then 18
    when '6B2 body armor' then 8
    when 'BNTI Kirasa-N' then 12
    when '6B13 assault armor' then 18
    when 'FORT Defender-2' then 24
    when '6B43 Zabralo-Sh body armor' then 30
    when 'NFM THOR' then 22
    when 'Small Backpack' then 4
    when 'Large Backpack' then 6
    when 'Tactical Backpack' then 8
    when 'Tasmanian Tiger Trooper 35' then 10
    when '6Sh118' then 14
    when 'Medkit' then 3
    when 'Bandage' then 1
    when 'Ammo Box' then 2
    when 'Scrap Metal' then 3
    when 'Rare Scope' then 2
    when 'Legendary Trigger Group' then 4
    else weight
end;

create or replace function game.authored_item(item_name text)
returns jsonb
language sql
stable
as $$
    select case
        when resolved.item_def is null then null
        else jsonb_build_object(
            'name', (resolved.item_def).name,
            'type', (resolved.item_def).item_type,
            'value', (resolved.item_def).value,
            'slots', (resolved.item_def).slots,
            'rarity', (resolved.item_def).rarity,
            'displayRarity', (resolved.item_def).display_rarity,
            'weight', (resolved.item_def).weight
        )
    end
    from (
        select game.item_def_by_name(item_name) as item_def
    ) resolved;
$$;

create or replace function game.item_weight(item_name text)
returns int
language sql
stable
as $$
    select coalesce((game.authored_item(item_name))->>'weight', '0')::int;
$$;

create or replace function game.max_encumbrance(strength int)
returns int
language sql
stable
as $$
    select 40 + (5 * greatest(coalesce(strength, 0) - 8, 0));
$$;

create or replace function game.current_encumbrance(items jsonb, medkits int default 0)
returns int
language sql
stable
as $$
    select coalesce(
        (
            select sum(game.item_weight(coalesce(value->>'name', value->>'Name')))
            from jsonb_array_elements(coalesce(items, '[]'::jsonb)) value
        ),
        0
    ) + greatest(coalesce(medkits, 0), 0) * game.item_weight('Medkit');
$$;

create or replace function game.normalize_random_character(random_character jsonb)
returns jsonb
language sql
stable
as $$
    select case
        when random_character is null then null
        else jsonb_build_object(
            'name', coalesce(random_character->>'name', random_character->>'Name'),
            'stats', coalesce(
                random_character->'stats',
                random_character->'Stats',
                jsonb_build_object(
                    'strength', 8,
                    'dexterity', 8,
                    'constitution', 8,
                    'intelligence', 8,
                    'wisdom', 8,
                    'charisma', 8
                )
            ),
            'inventory', game.normalize_items(coalesce(random_character->'inventory', random_character->'Inventory'))
        )
    end;
$$;

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

create or replace function game.random_luck_run_stats()
returns jsonb
language sql
volatile
as $$
    select jsonb_build_object(
        'strength', 8 + floor(random() * 5)::int,
        'dexterity', 8 + floor(random() * 5)::int,
        'constitution', 8 + floor(random() * 5)::int,
        'intelligence', 8 + floor(random() * 5)::int,
        'wisdom', 8 + floor(random() * 5)::int,
        'charisma', 8 + floor(random() * 5)::int
    );
$$;

create or replace function game.random_luck_run_loadout_valid(loadout jsonb, stats jsonb)
returns boolean
language sql
stable
as $$
    select game.current_encumbrance(coalesce(loadout, '[]'::jsonb), 0)
        <= game.max_encumbrance(coalesce((stats->>'strength')::int, (stats->>'Strength')::int, 8));
$$;

create or replace function game.random_luck_run_character()
returns jsonb
language plpgsql
volatile
as $$
declare
    stats jsonb;
    loadout jsonb;
begin
    loop
        stats := game.random_luck_run_stats();
        loadout := game.random_luck_run_loadout();

        exit when game.random_luck_run_loadout_valid(loadout, stats);
    end loop;

    return jsonb_build_object(
        'name', game.random_raider_name(),
        'stats', stats,
        'inventory', loadout
    );
end;
$$;

create or replace function game.random_luck_run_loadout()
returns jsonb
language sql
volatile
as $$
    select jsonb_build_array(
        game.authored_item('Makarov'),
        case when random() < 0.5
            then game.authored_item('Small Backpack')
            else game.authored_item('Tactical Backpack')
        end,
        game.authored_item('Medkit'),
        game.authored_item('Bandage'),
        game.authored_item('Ammo Box')
    );
$$;

create or replace function game.build_raid_snapshot(loadout jsonb, raider_name text, player_max_health int, accepted_stats jsonb default null)
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
    raid_payload jsonb;
    entry jsonb;
    resolved_stats jsonb := coalesce(accepted_stats, jsonb_build_object(
        'strength', 8,
        'dexterity', 8,
        'constitution', 8,
        'intelligence', 8,
        'wisdom', 8,
        'charisma', 8
    ));
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

    raid_payload := jsonb_build_object(
        'health', greatest(coalesce(player_max_health, 26), 1),
        'backpackCapacity', game.backpack_capacity(equipped_backpack_name),
        'ammo', game.weapon_magazine_capacity(equipped_weapon_name),
        'weaponMalfunction', false,
        'medkits', medkits,
        'lootSlots', 0,
        'encumbrance', game.current_encumbrance(equipped_items || carried_loot, medkits),
        'maxEncumbrance', game.max_encumbrance(coalesce((resolved_stats->>'strength')::int, 8)),
        'challenge', 0,
        'distanceFromExtract', 3,
        'acceptedStats', resolved_stats,
        'encounterType', 'Neutral',
        'encounterTitle', 'Area Clear',
        'encounterDescription', 'Area looks quiet. Nothing useful here.',
        'enemyName', '',
        'enemyHealth', 0,
        'enemyDexterity', 0,
        'enemyConstitution', 0,
        'enemyStrength', 0,
        'lootContainer', '',
        'enemyLoadout', '[]'::jsonb,
        'awaitingDecision', false,
        'discoveredLoot', discovered_loot,
        'carriedLoot', carried_loot,
        'equippedItems', equipped_items,
        'logEntries', jsonb_build_array(format('Raid started as %s.', raider_name))
    );

    return game.generate_raid_encounter(raid_payload, false);
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
    player_max_health int;
    accepted_stats jsonb;
    entry jsonb;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    on_person_items := coalesce(save_payload->'onPersonItems', '[]'::jsonb);
    random_character := save_payload->'randomCharacter';
    player_max_health := greatest(coalesce((save_payload->>'playerMaxHealth')::int, 26), 1);
    accepted_stats := coalesce(save_payload->'acceptedStats', jsonb_build_object(
        'strength', 8,
        'dexterity', 8,
        'constitution', 8,
        'intelligence', 8,
        'wisdom', 8,
        'charisma', 8
    ));

    if not coalesce((save_payload->>'statsAccepted')::boolean, false) then
        return save_payload;
    end if;

    if action = 'start-main-raid' then
        if random_character is not null and jsonb_array_length(coalesce(random_character->'inventory', '[]'::jsonb)) > 0 then
            return save_payload;
        end if;

        if exists (
            select 1
            from jsonb_array_elements(on_person_items) inventory_entry
            where coalesce((coalesce(inventory_entry->'item', inventory_entry->'Item')->>'type')::int, -1) in (0, 1, 2)
              and not coalesce((inventory_entry->>'isEquipped')::boolean, (inventory_entry->>'IsEquipped')::boolean, false)
        ) then
            return save_payload;
        end if;

        if not exists (
            select 1
            from jsonb_array_elements(on_person_items) inventory_entry
            where coalesce((coalesce(inventory_entry->'item', inventory_entry->'Item')->>'type')::int, -1) = 0
              and coalesce((inventory_entry->>'isEquipped')::boolean, (inventory_entry->>'IsEquipped')::boolean, false)
        ) then
            return save_payload;
        end if;

        for entry in
            select coalesce(value->'item', value->'Item')
            from jsonb_array_elements(on_person_items) value
        loop
            loadout := loadout || jsonb_build_array(game.normalize_item(entry));
        end loop;

        raid_snapshot := game.build_raid_snapshot(loadout, raider_name, player_max_health, accepted_stats);

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

        random_character := game.random_luck_run_character();
        raider_name := random_character->>'name';
        accepted_stats := coalesce(random_character->'stats', accepted_stats);
        loadout := coalesce(random_character->'inventory', '[]'::jsonb);
        raid_snapshot := game.build_raid_snapshot(loadout, raider_name, player_max_health, accepted_stats);

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

create or replace function game.perform_raid_action_with_encumbrance(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    save_payload jsonb;
    raid_payload jsonb;
    equipped_items jsonb;
    carried_loot jsonb;
    discovered_loot jsonb;
    medkits int;
    selected_item jsonb;
    previous_item jsonb;
    selected_type int;
    item_name text;
    player_stats jsonb;
    max_encumbrance int;
    current_encumbrance int;
    prospective_encumbrance int;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    raid_payload := coalesce(save_payload->'activeRaid', 'null'::jsonb);

    if raid_payload is null then
        return game.perform_raid_action(action, payload, target_user_id);
    end if;

    player_stats := coalesce(raid_payload->'acceptedStats', save_payload->'acceptedStats', jsonb_build_object(
        'strength', 8,
        'dexterity', 8,
        'constitution', 8,
        'intelligence', 8,
        'wisdom', 8,
        'charisma', 8
    ));
    max_encumbrance := game.max_encumbrance(coalesce((player_stats->>'strength')::int, 8));
    equipped_items := game.normalize_items(coalesce(raid_payload->'equippedItems', '[]'::jsonb));
    carried_loot := game.normalize_items(coalesce(raid_payload->'carriedLoot', '[]'::jsonb));
    discovered_loot := game.normalize_items(coalesce(raid_payload->'discoveredLoot', '[]'::jsonb));
    medkits := greatest(coalesce((raid_payload->>'medkits')::int, 0), 0);
    current_encumbrance := game.current_encumbrance(equipped_items || carried_loot, medkits);

    if action = 'take-loot' then
        item_name := payload->>'itemName';
        selected_item := (
            select value
            from jsonb_array_elements(discovered_loot) value
            where value->>'name' = item_name
            limit 1
        );

        if selected_item is not null then
            prospective_encumbrance := current_encumbrance + game.item_weight(coalesce(selected_item->>'name', item_name));
            if prospective_encumbrance > max_encumbrance then
                return save_payload;
            end if;
        end if;
    elsif action in ('equip-from-discovered', 'equip-from-carried') then
        item_name := payload->>'itemName';
        if action = 'equip-from-discovered' then
            selected_item := (
                select value
                from jsonb_array_elements(discovered_loot) value
                where value->>'name' = item_name
                limit 1
            );
        else
            selected_item := (
                select value
                from jsonb_array_elements(carried_loot) value
                where value->>'name' = item_name
                limit 1
            );
        end if;

        if selected_item is not null and coalesce((selected_item->>'type')::int, -1) in (0, 1, 2) then
            selected_type := coalesce((selected_item->>'type')::int, -1);
            previous_item := game.raid_find_equipped_item(equipped_items, selected_type);
            if action = 'equip-from-discovered' then
                prospective_encumbrance := current_encumbrance
                    + game.item_weight(coalesce(selected_item->>'name', item_name))
                    - coalesce(game.item_weight(previous_item->>'name'), 0);
            else
                prospective_encumbrance := current_encumbrance
                    - coalesce(game.item_weight(previous_item->>'name'), 0);
            end if;

            if prospective_encumbrance > max_encumbrance then
                return save_payload;
            end if;
        end if;
    end if;

    return game.perform_raid_action(action, payload, target_user_id);
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
        when action in (
            'attack',
            'burst-fire',
            'full-auto',
            'reload',
            'flee',
            'use-medkit',
            'take-loot',
            'drop-carried',
            'drop-equipped',
            'equip-from-discovered',
            'equip-from-carried',
            'go-deeper',
            'move-toward-extract',
            'stay-at-extract',
            'attempt-extract')
            then game.perform_raid_action_with_encumbrance(action, payload, auth.uid())
        else game.apply_profile_action(action, payload, auth.uid())
    end;
$$;

revoke all on function game.authored_item(text) from public;
revoke all on function game.item_weight(text) from public;
revoke all on function game.max_encumbrance(int) from public;
revoke all on function game.current_encumbrance(jsonb, int) from public;
revoke all on function game.normalize_random_character(jsonb) from public;
revoke all on function game.settle_random_character(jsonb, jsonb) from public;
revoke all on function game.random_luck_run_stats() from public;
revoke all on function game.random_luck_run_loadout_valid(jsonb, jsonb) from public;
revoke all on function game.random_luck_run_character() from public;
revoke all on function game.random_luck_run_loadout() from public;
revoke all on function game.build_raid_snapshot(jsonb, text, int, jsonb) from public;
revoke all on function game.start_raid_action(text, jsonb, uuid) from public;
revoke all on function game.perform_raid_action_with_encumbrance(text, jsonb, uuid) from public;
revoke all on function public.game_action(text, jsonb) from public;
