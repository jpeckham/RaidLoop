create or replace function game.max_encumbrance(strength int)
returns int
language plpgsql
stable
as $$
declare
    heavy_loads int[] := array[10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 115, 130, 150, 175, 200, 230, 260, 300, 350, 400, 460, 520, 600, 700, 800, 920, 1040, 1200, 1400];
begin
    strength := greatest(coalesce(strength, 1), 1);

    if strength <= array_length(heavy_loads, 1) then
        return heavy_loads[strength];
    end if;

    return game.max_encumbrance(strength - 10) * 4;
end;
$$;

create or replace function game.light_encumbrance(strength int)
returns int
language plpgsql
stable
as $$
begin
    return game.max_encumbrance(strength) / 3;
end;
$$;

create or replace function game.medium_encumbrance(strength int)
returns int
language plpgsql
stable
as $$
begin
    return (game.max_encumbrance(strength) * 2) / 3;
end;
$$;

create or replace function game.encumbrance_tier(strength int, carried_weight int)
returns text
language plpgsql
stable
as $$
begin
    if greatest(coalesce(carried_weight, 0), 0) <= game.light_encumbrance(strength) then
        return 'Light';
    end if;

    if greatest(coalesce(carried_weight, 0), 0) <= game.medium_encumbrance(strength) then
        return 'Medium';
    end if;

    return 'Heavy';
end;
$$;

create or replace function game.encumbrance_attack_penalty(encumbrance text)
returns int
language sql
stable
as $$
    select case
        when encumbrance = 'Heavy' then 6
        when encumbrance = 'Medium' then 3
        else 0
    end;
$$;

create or replace function game.encumbrance_max_dex_bonus(encumbrance text)
returns int
language sql
stable
as $$
    select case
        when encumbrance = 'Heavy' then 1
        when encumbrance = 'Medium' then 3
        else 100
    end;
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
        'encumbranceTier', game.encumbrance_tier(
            coalesce((resolved_stats->>'strength')::int, 8),
            game.current_encumbrance(equipped_items || carried_loot, medkits)),
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

/*
Authoritative combat integration for perform_raid_action(action text, payload jsonb, target_user_id uuid default auth.uid()):

declare
    player_encumbrance text;
    player_attack_penalty int;
    player_max_dex_bonus int;
begin
    player_encumbrance := game.encumbrance_tier(
        coalesce((coalesce(raid_payload->'acceptedStats', save_payload->'acceptedStats')->>'strength')::int, 8),
        game.current_encumbrance(equipped_items || carried_loot, medkits));
    player_attack_penalty := game.encumbrance_attack_penalty(player_encumbrance);
    player_max_dex_bonus := game.encumbrance_max_dex_bonus(player_encumbrance);

    -- player attack examples
    attack_bonus := least(game.ability_modifier(player_dexterity), player_max_dex_bonus) - player_attack_penalty;
    attack_bonus := least(game.ability_modifier(player_dexterity), player_max_dex_bonus) - game.weapon_burst_attack_penalty(equipped_weapon_name) - player_attack_penalty;
    attack_bonus := least(game.ability_modifier(player_dexterity), player_max_dex_bonus) - 4 - player_attack_penalty;

    -- enemy attack vs player defense example
    attack_outcome := game.classify_attack_outcome(
        attack_roll,
        enemy_attack_total,
        least(game.ability_modifier(player_dexterity), player_max_dex_bonus),
        player_armor_bonus);
end;
*/

revoke all on function game.max_encumbrance(int) from public;
revoke all on function game.light_encumbrance(int) from public;
revoke all on function game.medium_encumbrance(int) from public;
revoke all on function game.encumbrance_tier(int, int) from public;
revoke all on function game.encumbrance_attack_penalty(text) from public;
revoke all on function game.encumbrance_max_dex_bonus(text) from public;
revoke all on function game.build_raid_snapshot(jsonb, text, int, jsonb) from public;
revoke all on function game.perform_raid_action_with_encumbrance(text, jsonb, uuid) from public;
