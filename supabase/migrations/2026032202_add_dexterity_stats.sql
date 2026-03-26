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
            'randomCharacterAvailableAt', '0001-01-01T00:00:00+00:00',
            'randomCharacter', null,
            'money', 500,
            'onPersonItems', jsonb_build_array(),
            'activeRaid', null
        )
    );
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
    enemy_dexterity int := 10;
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
        'challenge', 0,
        'distanceFromExtract', 3,
        'encounterType', encounter_type,
        'encounterTitle', encounter_title,
        'encounterDescription', encounter_description,
        'enemyName', enemy_name,
        'enemyHealth', enemy_health,
        'enemyDexterity', enemy_dexterity,
        'lootContainer', loot_container,
        'awaitingDecision', false,
        'discoveredLoot', discovered_loot,
        'carriedLoot', carried_loot,
        'equippedItems', equipped_items,
        'logEntries', jsonb_build_array(format('Raid started as %s.', raider_name))
    );
end;
$$;

revoke all on function game.normalize_save_payload(jsonb) from public;
revoke all on function game.default_save_payload() from public;
revoke all on function game.build_raid_snapshot(jsonb, text) from public;

create or replace function game.ability_modifier(score int)
returns int
language plpgsql
immutable
as $$
begin
    return floor((score - 10) / 2.0)::int;
end;
$$;

create or replace function game.roll_attack_d20(attack_bonus int default 0, defense int default 10)
returns boolean
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    roll int;
begin
    roll := floor(random() * 20)::int + 1;

    if roll = 1 then
        return false;
    end if;

    if roll = 20 then
        return true;
    end if;

    return roll + coalesce(attack_bonus, 0) >= coalesce(defense, 10);
end;
$$;

create or replace function game.perform_raid_action(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    save_payload jsonb;
    raid_payload jsonb;
    raid_profile text;
    equipped_items jsonb;
    carried_loot jsonb;
    discovered_loot jsonb;
    enemy_loadout jsonb;
    log_entries jsonb;
    encounter_type text;
    enemy_name text;
    enemy_health int;
    enemy_dexterity int;
    player_dexterity int;
    ammo int;
    medkits int;
    health int;
    backpack_capacity int;
    challenge int;
    distance_from_extract int;
    weapon_malfunction boolean;
    extraction_combat boolean;
    equipped_weapon jsonb;
    equipped_armor jsonb;
    selected_item jsonb;
    previous_item jsonb;
    item_name text;
    slot_type int;
    uses_ammo boolean;
    attack_hit boolean;
    damage int;
    incoming int;
    reduced_damage int;
    current_slots int;
    dropped_item jsonb;
    loot_count int;
    enemy_dropped_items jsonb;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    player_dexterity := greatest(coalesce((save_payload->>'playerDexterity')::int, 10), 0);

    select raid_sessions.profile, raid_sessions.payload
    into raid_profile, raid_payload
    from public.raid_sessions
    where user_id = target_user_id;

    if raid_payload is null then
        return save_payload;
    end if;

    equipped_items := game.normalize_items(coalesce(raid_payload->'equippedItems', '[]'::jsonb));
    carried_loot := game.normalize_items(coalesce(raid_payload->'carriedLoot', '[]'::jsonb));
    discovered_loot := game.normalize_items(coalesce(raid_payload->'discoveredLoot', '[]'::jsonb));
    enemy_loadout := game.normalize_items(coalesce(raid_payload->'enemyLoadout', '[]'::jsonb));
    log_entries := coalesce(raid_payload->'logEntries', '[]'::jsonb);
    encounter_type := coalesce(raid_payload->>'encounterType', 'Neutral');
    enemy_name := coalesce(raid_payload->>'enemyName', '');
    enemy_health := greatest(coalesce((raid_payload->>'enemyHealth')::int, 0), 0);
    enemy_dexterity := greatest(coalesce((raid_payload->>'enemyDexterity')::int, 10), 0);
    ammo := greatest(coalesce((raid_payload->>'ammo')::int, 0), 0);
    medkits := greatest(coalesce((raid_payload->>'medkits')::int, 0), 0);
    health := greatest(coalesce((raid_payload->>'health')::int, 30), 0);
    challenge := greatest(coalesce((raid_payload->>'challenge')::int, 0), 0);
    distance_from_extract := greatest(coalesce((raid_payload->>'distanceFromExtract')::int, 0), 0);
    weapon_malfunction := coalesce((raid_payload->>'weaponMalfunction')::boolean, false);
    extraction_combat := coalesce((raid_payload->>'extractionCombat')::boolean, false);
    equipped_weapon := game.raid_find_equipped_item(equipped_items, 0);
    equipped_armor := game.raid_find_equipped_item(equipped_items, 1);
    backpack_capacity := game.backpack_capacity(coalesce(game.raid_find_equipped_item(equipped_items, 2)->>'name', ''));

    if action in ('attack', 'burst-fire') then
        if encounter_type <> 'Combat' then
            return save_payload;
        end if;

        uses_ammo := game.weapon_magazine_capacity(coalesce(equipped_weapon->>'name', 'Rusty Knife')) > 0;

        if action = 'attack' then
            if weapon_malfunction then
                log_entries := game.raid_append_log(log_entries, 'Weapon is malfunctioned. Reload to clear it.');
            elsif uses_ammo and ammo <= 0 then
                log_entries := game.raid_append_log(log_entries, 'No ammo.');
            else
                if not weapon_malfunction and random() < 0.10 then
                    weapon_malfunction := true;
                    log_entries := game.raid_append_log(log_entries, 'Weapon malfunctioned. Reload to clear it.');
                else
                    if uses_ammo then
                        ammo := ammo - 1;
                    end if;

                    attack_hit := game.roll_attack_d20(game.ability_modifier(player_dexterity), 10 + game.ability_modifier(enemy_dexterity));
                    if attack_hit then
                        damage := game.roll_weapon_damage(coalesce(equipped_weapon->>'name', 'Rusty Knife'), 'standard');
                        enemy_health := enemy_health - damage;
                        log_entries := game.raid_append_log(log_entries, format('You hit %s for %s.', enemy_name, damage));
                    else
                        log_entries := game.raid_append_log(log_entries, format('You miss %s.', enemy_name));
                    end if;
                end if;
            end if;
        else
            if weapon_malfunction then
                log_entries := game.raid_append_log(log_entries, 'Weapon is malfunctioned. Reload to clear it.');
            elsif not uses_ammo or ammo < 2 then
                log_entries := game.raid_append_log(log_entries, 'Not enough ammo for Burst Fire.');
            else
                if not weapon_malfunction and random() < 0.10 then
                    weapon_malfunction := true;
                    log_entries := game.raid_append_log(log_entries, 'Weapon malfunctioned during Burst Fire. Reload to clear it.');
                else
                    ammo := ammo - 2;
                    attack_hit := game.roll_attack_d20(game.ability_modifier(player_dexterity), 10 + game.ability_modifier(enemy_dexterity));
                    if attack_hit then
                        damage := game.roll_weapon_damage(coalesce(equipped_weapon->>'name', 'Rusty Knife'), 'burst');
                        enemy_health := enemy_health - damage;
                        log_entries := game.raid_append_log(log_entries, format('Burst Fire deals %s.', damage));
                    else
                        log_entries := game.raid_append_log(log_entries, format('Burst Fire misses %s.', enemy_name));
                    end if;
                end if;
            end if;
        end if;

        if enemy_health <= 0 then
            if extraction_combat then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Final guard defeated. Extraction successful.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, true, target_user_id);
            end if;

            enemy_dropped_items := case
                when jsonb_array_length(enemy_loadout) > 0 then enemy_loadout
                else game.random_enemy_loadout()
            end;

            log_entries := game.raid_append_log(log_entries, format('Found Dead Body with %s lootable items.', jsonb_array_length(enemy_dropped_items)));
            raid_payload := jsonb_set(raid_payload, '{encounterType}', to_jsonb('Loot'::text), true);
            raid_payload := jsonb_set(raid_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Loot')), true);
            raid_payload := jsonb_set(raid_payload, '{encounterDescription}', to_jsonb('Enemy down. Check the body for loot.'::text), true);
            raid_payload := jsonb_set(raid_payload, '{enemyName}', to_jsonb(''::text), true);
            raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(0), true);
            raid_payload := jsonb_set(raid_payload, '{lootContainer}', to_jsonb('Dead Body'::text), true);
            raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', enemy_dropped_items, true);
            raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', '[]'::jsonb, true);
            raid_payload := jsonb_set(raid_payload, '{awaitingDecision}', 'false'::jsonb, true);
            raid_payload := jsonb_set(raid_payload, '{weaponMalfunction}', to_jsonb(weapon_malfunction), true);
            raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
        else
            attack_hit := game.roll_attack_d20(
                game.ability_modifier(enemy_dexterity),
                10 + game.ability_modifier(player_dexterity));
            if attack_hit then
                incoming := 3 + floor(random() * 6)::int;
                reduced_damage := greatest(1, incoming - case
                    when coalesce(equipped_armor->>'name', '') = 'NFM THOR' then 6
                    when coalesce(equipped_armor->>'name', '') = '6B43 Zabralo-Sh body armor' then 5
                    when coalesce(equipped_armor->>'name', '') = 'FORT Defender-2' then 4
                    when coalesce(equipped_armor->>'name', '') = '6B13 assault armor' then 3
                    when coalesce(equipped_armor->>'name', '') = '6B2 body armor' then 1
                    else 0
                end);
                health := greatest(health - reduced_damage, 0);
                log_entries := game.raid_append_log(log_entries, format('%s hits you for %s.', enemy_name, reduced_damage));
            else
                log_entries := game.raid_append_log(log_entries, format('%s misses you.', enemy_name));
            end if;

            if health <= 0 then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You were killed in raid. Loadout and loot lost.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, false, target_user_id);
            end if;

            raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(enemy_health), true);
            raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(health), true);
            raid_payload := jsonb_set(raid_payload, '{weaponMalfunction}', to_jsonb(weapon_malfunction), true);
            raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
        end if;
    elsif action = 'use-medkit' then
        if medkits > 0 then
            medkits := medkits - 1;
            health := least(30, health + 10);
            log_entries := game.raid_append_log(log_entries, 'Medkit used (+10 HP).');

            if encounter_type = 'Combat' then
                attack_hit := game.roll_attack_d20(
                    game.ability_modifier(enemy_dexterity),
                    10 + game.ability_modifier(player_dexterity));
                if attack_hit then
                    incoming := 3 + floor(random() * 6)::int;
                    reduced_damage := greatest(1, incoming - case
                        when coalesce(equipped_armor->>'name', '') = 'NFM THOR' then 6
                        when coalesce(equipped_armor->>'name', '') = '6B43 Zabralo-Sh body armor' then 5
                        when coalesce(equipped_armor->>'name', '') = 'FORT Defender-2' then 4
                        when coalesce(equipped_armor->>'name', '') = '6B13 assault armor' then 3
                        when coalesce(equipped_armor->>'name', '') = '6B2 body armor' then 1
                        else 0
                    end);
                    health := greatest(health - reduced_damage, 0);
                    log_entries := game.raid_append_log(log_entries, format('%s hits you for %s.', enemy_name, reduced_damage));
                else
                    log_entries := game.raid_append_log(log_entries, format('%s misses you.', enemy_name));
                end if;

                if health <= 0 then
                    raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You were killed in raid. Loadout and loot lost.'), true);
                    return game.finish_raid_session(save_payload, raid_payload, raid_profile, false, target_user_id);
                end if;
            end if;
        end if;

        raid_payload := jsonb_set(raid_payload, '{medkits}', to_jsonb(medkits), true);
        raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(health), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action = 'reload' then
        if encounter_type = 'Combat' then
            uses_ammo := game.weapon_magazine_capacity(coalesce(equipped_weapon->>'name', 'Rusty Knife')) > 0;
            if not uses_ammo then
                log_entries := game.raid_append_log(log_entries, 'Knife doesn''t need reloading.');
            else
                ammo := game.weapon_magazine_capacity(coalesce(equipped_weapon->>'name', 'Rusty Knife'));
                weapon_malfunction := false;
                log_entries := game.raid_append_log(log_entries, 'Weapon reloaded and cleared.');
            end if;

            attack_hit := game.roll_attack_d20(
                game.ability_modifier(enemy_dexterity),
                10 + game.ability_modifier(player_dexterity));
            if attack_hit then
                incoming := 3 + floor(random() * 6)::int;
                reduced_damage := greatest(1, incoming - case
                    when coalesce(equipped_armor->>'name', '') = 'NFM THOR' then 6
                    when coalesce(equipped_armor->>'name', '') = '6B43 Zabralo-Sh body armor' then 5
                    when coalesce(equipped_armor->>'name', '') = 'FORT Defender-2' then 4
                    when coalesce(equipped_armor->>'name', '') = '6B13 assault armor' then 3
                    when coalesce(equipped_armor->>'name', '') = '6B2 body armor' then 1
                    else 0
                end);
                health := greatest(health - reduced_damage, 0);
                log_entries := game.raid_append_log(log_entries, format('%s hits you for %s.', enemy_name, reduced_damage));
            else
                log_entries := game.raid_append_log(log_entries, format('%s misses you.', enemy_name));
            end if;

            if health <= 0 then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You were killed in raid. Loadout and loot lost.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, false, target_user_id);
            end if;
        end if;

        raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
        raid_payload := jsonb_set(raid_payload, '{weaponMalfunction}', to_jsonb(weapon_malfunction), true);
        raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(health), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action = 'flee' then
        if encounter_type = 'Combat' then
            if random() < 0.15 then
                log_entries := game.raid_append_log(log_entries, 'Flee succeeded.');
                raid_payload := jsonb_set(raid_payload, '{encounterType}', to_jsonb('Neutral'::text), true);
                raid_payload := jsonb_set(raid_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Neutral')), true);
                raid_payload := jsonb_set(raid_payload, '{encounterDescription}', to_jsonb('Choose your next move.'::text), true);
                raid_payload := jsonb_set(raid_payload, '{enemyName}', to_jsonb(''::text), true);
                raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(0), true);
                raid_payload := jsonb_set(raid_payload, '{awaitingDecision}', 'true'::jsonb, true);
                raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', '[]'::jsonb, true);
                raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
            else
                log_entries := game.raid_append_log(log_entries, 'Flee failed.');
                attack_hit := game.roll_attack_d20(
                    game.ability_modifier(enemy_dexterity),
                    10 + game.ability_modifier(player_dexterity));
                if attack_hit then
                    incoming := 3 + floor(random() * 6)::int;
                    reduced_damage := greatest(1, incoming - case
                        when coalesce(equipped_armor->>'name', '') = 'NFM THOR' then 6
                        when coalesce(equipped_armor->>'name', '') = '6B43 Zabralo-Sh body armor' then 5
                        when coalesce(equipped_armor->>'name', '') = 'FORT Defender-2' then 4
                        when coalesce(equipped_armor->>'name', '') = '6B13 assault armor' then 3
                        when coalesce(equipped_armor->>'name', '') = '6B2 body armor' then 1
                        else 0
                    end);
                    health := greatest(health - reduced_damage, 0);
                    log_entries := game.raid_append_log(log_entries, format('%s hits you for %s.', enemy_name, reduced_damage));
                else
                    log_entries := game.raid_append_log(log_entries, format('%s misses you.', enemy_name));
                end if;

                if health <= 0 then
                    raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You were killed in raid. Loadout and loot lost.'), true);
                    return game.finish_raid_session(save_payload, raid_payload, raid_profile, false, target_user_id);
                end if;

                raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(health), true);
                raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
            end if;
        end if;
    elsif action = 'take-loot' then
        item_name := payload->>'itemName';
        selected_item := (
            select value
            from jsonb_array_elements(discovered_loot) value
            where value->>'name' = item_name
            limit 1
        );

        if selected_item is not null then
            if item_name = 'Medkit' then
                discovered_loot := game.jsonb_array_remove(
                    discovered_loot,
                    coalesce((select ordinality::int - 1 from jsonb_array_elements(discovered_loot) with ordinality value where value->>'name' = item_name limit 1), -1));
                medkits := medkits + 1;
                log_entries := game.raid_append_log(log_entries, format('Looted %s.', item_name));
            else
                current_slots := game.raid_current_slots(carried_loot);
                if current_slots + coalesce((selected_item->>'slots')::int, 1) <= backpack_capacity then
                    discovered_loot := game.jsonb_array_remove(
                        discovered_loot,
                        coalesce((select ordinality::int - 1 from jsonb_array_elements(discovered_loot) with ordinality value where value->>'name' = item_name limit 1), -1));
                    carried_loot := carried_loot || jsonb_build_array(game.normalize_item(selected_item));
                    log_entries := game.raid_append_log(log_entries, format('Looted %s.', item_name));
                else
                    log_entries := game.raid_append_log(log_entries, format('Could not loot %s: backpack full.', item_name));
                end if;
            end if;
        end if;

        raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', discovered_loot, true);
        raid_payload := jsonb_set(raid_payload, '{carriedLoot}', carried_loot, true);
        raid_payload := jsonb_set(raid_payload, '{medkits}', to_jsonb(medkits), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action = 'drop-carried' then
        item_name := payload->>'itemName';
        selected_item := (
            select value
            from jsonb_array_elements(carried_loot) value
            where value->>'name' = item_name
            limit 1
        );
        if selected_item is not null then
            carried_loot := game.jsonb_array_remove(
                carried_loot,
                coalesce((select ordinality::int - 1 from jsonb_array_elements(carried_loot) with ordinality value where value->>'name' = item_name limit 1), -1));
            discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(selected_item));
            log_entries := game.raid_append_log(log_entries, format('Dropped %s.', item_name));
        end if;

        raid_payload := jsonb_set(raid_payload, '{carriedLoot}', carried_loot, true);
        raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', discovered_loot, true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action = 'drop-equipped' then
        slot_type := case upper(coalesce(payload->>'slotType', ''))
            when 'WEAPON' then 0
            when 'ARMOR' then 1
            when 'BACKPACK' then 2
            else -1
        end;
        selected_item := game.raid_find_equipped_item(equipped_items, slot_type);

        if selected_item is not null then
            equipped_items := coalesce(
                (
                    select jsonb_agg(value order by ordinality)
                    from jsonb_array_elements(equipped_items) with ordinality
                    where coalesce((value->>'type')::int, -1) <> slot_type
                ),
                '[]'::jsonb
            );

            if slot_type = 2 then
                discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(selected_item)) || carried_loot;
                carried_loot := '[]'::jsonb;
                backpack_capacity := game.backpack_capacity('');
            else
                discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(selected_item));
            end if;

            if slot_type = 0 then
                weapon_malfunction := false;
                ammo := 0;
            end if;

            log_entries := game.raid_append_log(log_entries, format('Dropped equipped %s.', initcap(lower(payload->>'slotType'))));
        end if;

        raid_payload := jsonb_set(raid_payload, '{equippedItems}', equipped_items, true);
        raid_payload := jsonb_set(raid_payload, '{carriedLoot}', carried_loot, true);
        raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', discovered_loot, true);
        raid_payload := jsonb_set(raid_payload, '{backpackCapacity}', to_jsonb(backpack_capacity), true);
        raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
        raid_payload := jsonb_set(raid_payload, '{weaponMalfunction}', to_jsonb(weapon_malfunction), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
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
            slot_type := coalesce((selected_item->>'type')::int, -1);
            if action = 'equip-from-discovered' then
                discovered_loot := game.jsonb_array_remove(
                    discovered_loot,
                    coalesce((select ordinality::int - 1 from jsonb_array_elements(discovered_loot) with ordinality value where value->>'name' = item_name limit 1), -1));
            else
                carried_loot := game.jsonb_array_remove(
                    carried_loot,
                    coalesce((select ordinality::int - 1 from jsonb_array_elements(carried_loot) with ordinality value where value->>'name' = item_name limit 1), -1));
            end if;

            previous_item := game.raid_find_equipped_item(equipped_items, slot_type);
            if previous_item is not null then
                discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(previous_item));
                equipped_items := coalesce(
                    (
                        select jsonb_agg(value order by ordinality)
                        from jsonb_array_elements(equipped_items) with ordinality
                        where coalesce((value->>'type')::int, -1) <> slot_type
                    ),
                    '[]'::jsonb
                );
            end if;

            equipped_items := equipped_items || jsonb_build_array(game.normalize_item(selected_item));

            if slot_type = 2 then
                backpack_capacity := game.backpack_capacity(selected_item->>'name');
                while game.raid_current_slots(carried_loot) > backpack_capacity and jsonb_array_length(carried_loot) > 0 loop
                    dropped_item := game.jsonb_array_get(carried_loot, jsonb_array_length(carried_loot) - 1);
                    carried_loot := game.jsonb_array_remove(carried_loot, jsonb_array_length(carried_loot) - 1);
                    discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(dropped_item));
                end loop;
            end if;

            if slot_type = 0 then
                ammo := least(greatest(ammo, 0), game.weapon_magazine_capacity(selected_item->>'name'));
                weapon_malfunction := false;
            end if;

            log_entries := game.raid_append_log(
                log_entries,
                format('Equipped %s from %s loot.', item_name, case when action = 'equip-from-discovered' then 'discovered' else 'carried' end));
        end if;

        raid_payload := jsonb_set(raid_payload, '{equippedItems}', equipped_items, true);
        raid_payload := jsonb_set(raid_payload, '{carriedLoot}', carried_loot, true);
        raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', discovered_loot, true);
        raid_payload := jsonb_set(raid_payload, '{backpackCapacity}', to_jsonb(backpack_capacity), true);
        raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
        raid_payload := jsonb_set(raid_payload, '{weaponMalfunction}', to_jsonb(weapon_malfunction), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action in ('go-deeper', 'move-toward-extract', 'stay-at-extract') then
        loot_count := jsonb_array_length(discovered_loot);
        if encounter_type = 'Loot' and loot_count > 0 then
            log_entries := game.raid_append_log(log_entries, format('Moved on and left %s items behind.', loot_count));
            raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
        end if;

        raid_payload := game.generate_raid_encounter(raid_payload, action = 'move-toward-extract');
    elsif action = 'attempt-extract' then
        if encounter_type = 'Extraction' then
            if random() < 0.50 then
                log_entries := game.raid_append_log(log_entries, 'Extraction ambush.');
                raid_payload := jsonb_set(raid_payload, '{encounterType}', to_jsonb('Combat'::text), true);
                raid_payload := jsonb_set(raid_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Combat')), true);
                raid_payload := jsonb_set(raid_payload, '{encounterDescription}', to_jsonb('Extraction ambush engaged.'::text), true);
                raid_payload := jsonb_set(raid_payload, '{enemyName}', to_jsonb('Final Guard'::text), true);
                raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(22), true);
                raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', game.random_enemy_loadout(), true);
                raid_payload := jsonb_set(raid_payload, '{extractionCombat}', 'true'::jsonb, true);
                raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
            else
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Extraction completed. Loot secured.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, true, target_user_id);
            end if;
        end if;
    end if;

    raid_payload := jsonb_set(raid_payload, '{equippedItems}', game.normalize_items(coalesce(raid_payload->'equippedItems', equipped_items)), true);
    raid_payload := jsonb_set(raid_payload, '{carriedLoot}', game.normalize_items(coalesce(raid_payload->'carriedLoot', carried_loot)), true);
    raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', game.normalize_items(coalesce(raid_payload->'discoveredLoot', discovered_loot)), true);
    raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', game.normalize_items(coalesce(raid_payload->'enemyLoadout', enemy_loadout)), true);
    raid_payload := jsonb_set(raid_payload, '{medkits}', to_jsonb(greatest(coalesce((raid_payload->>'medkits')::int, medkits), 0)), true);
    raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(greatest(coalesce((raid_payload->>'health')::int, health), 0)), true);
    raid_payload := jsonb_set(raid_payload, '{backpackCapacity}', to_jsonb(game.backpack_capacity(coalesce(game.raid_find_equipped_item(coalesce(raid_payload->'equippedItems', equipped_items), 2)->>'name', ''))), true);
    raid_payload := jsonb_set(raid_payload, '{lootSlots}', to_jsonb(game.raid_current_slots(coalesce(raid_payload->'carriedLoot', carried_loot))), true);
    challenge := greatest(coalesce((raid_payload->>'challenge')::int, challenge), 0);
    distance_from_extract := greatest(coalesce((raid_payload->>'distanceFromExtract')::int, distance_from_extract), 0);
    raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
    raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);

    update public.raid_sessions
    set payload = raid_payload,
        updated_at = timezone('utc', now())
    where user_id = target_user_id;

    save_payload := jsonb_set(save_payload, '{activeRaid}', raid_payload, true);
    update public.game_saves
    set payload = save_payload,
        save_version = 1,
        updated_at = timezone('utc', now())
    where user_id = target_user_id;

    return save_payload;
end;
$$;

revoke all on function game.ability_modifier(int) from public;
revoke all on function game.roll_attack_d20(int, int) from public;
revoke all on function game.perform_raid_action(text, jsonb, uuid) from public;
grant execute on function game.ability_modifier(int) to authenticated;
grant execute on function game.roll_attack_d20(int, int) to authenticated;
grant execute on function game.perform_raid_action(text, jsonb, uuid) to authenticated;
