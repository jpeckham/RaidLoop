create or replace function game.weapon_armor_penetration(weapon_name text)
returns int
language plpgsql
immutable
as $$
begin
    return case
        when weapon_name = 'Makarov' then 1
        when weapon_name = 'PPSH' then 1
        when weapon_name = 'AK74' then 2
        when weapon_name = 'AK47' then 2
        when weapon_name = 'SVDS' then 3
        else 0
    end;
end;
$$;

create or replace function game.armor_damage_reduction(armor_name text)
returns int
language plpgsql
immutable
as $$
begin
    return case
        when armor_name = 'NFM THOR' then 6
        when armor_name = '6B43 Zabralo-Sh body armor' then 5
        when armor_name = 'FORT Defender-2' then 4
        when armor_name = '6B13 assault armor' then 3
        when armor_name = '6B2 body armor' then 1
        else 0
    end;
end;
$$;

create or replace function game.apply_armor_damage_reduction(incoming_damage int, armor_name text, armor_penetration int default 0)
returns int
language plpgsql
immutable
as $$
declare
    effective_armor_dr int;
begin
    effective_armor_dr := greatest(0, game.armor_damage_reduction(armor_name) - coalesce(armor_penetration, 0));
    return greatest(1, coalesce(incoming_damage, 0) - effective_armor_dr);
end;
$$;

create or replace function game.weapon_supports_single_shot(weapon_name text)
returns boolean
language sql
immutable
as $$
    select case coalesce(weapon_name, '')
        when 'PKP' then false
        else true
    end;
$$;

create or replace function game.weapon_supports_burst_fire(weapon_name text)
returns boolean
language sql
immutable
as $$
    select case coalesce(weapon_name, '')
        when 'Makarov' then true
        when 'PPSH' then true
        when 'AK74' then true
        when 'AK47' then true
        when 'SVDS' then true
        when 'PKP' then true
        else false
    end;
$$;

create or replace function game.weapon_supports_full_auto(weapon_name text)
returns boolean
language sql
immutable
as $$
    select case coalesce(weapon_name, '')
        when 'PPSH' then true
        when 'AK74' then true
        when 'AK47' then true
        when 'PKP' then true
        else false
    end;
$$;

create or replace function game.weapon_burst_attack_penalty(weapon_name text)
returns int
language sql
immutable
as $$
    select case coalesce(weapon_name, '')
        when 'Makarov' then 3
        when 'PPSH' then 2
        when 'AK74' then 2
        when 'AK47' then 2
        when 'SVDS' then 2
        when 'PKP' then 2
        else 3
    end;
$$;

create or replace function game.roll_weapon_damage_d20(weapon_name text, attack_mode text)
returns int
language plpgsql
volatile
as $$
declare
    die_size int;
    die_count int;
    roll int := 0;
    current_die int;
begin
    die_size := case coalesce(weapon_name, '')
        when 'PPSH' then 4
        when 'AK74' then 8
        when 'AK47' then 10
        when 'SVDS' then 12
        when 'PKP' then 12
        when 'Makarov' then 6
        else 6
    end;

    die_count := case coalesce(attack_mode, 'attack')
        when 'burst-fire' then 3
        when 'full-auto' then 4
        else 2
    end;

    for current_die in 1..die_count loop
        roll := roll + floor(random() * die_size)::int + 1;
    end loop;

    return roll;
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
    extract_required int;
    weapon_malfunction boolean;
    extraction_combat boolean;
    equipped_weapon jsonb;
    equipped_weapon_name text;
    equipped_armor jsonb;
    enemy_armor_name text;
    enemy_weapon_name text;
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
    extract_required := greatest(coalesce((raid_payload->>'extractRequired')::int, 3), 1);
    weapon_malfunction := coalesce((raid_payload->>'weaponMalfunction')::boolean, false);
    extraction_combat := coalesce((raid_payload->>'extractionCombat')::boolean, false);
    equipped_weapon := game.raid_find_equipped_item(equipped_items, 0);
    equipped_weapon_name := coalesce(equipped_weapon->>'name', 'Rusty Knife');
    equipped_armor := game.raid_find_equipped_item(equipped_items, 1);
    enemy_armor_name := coalesce(game.raid_find_equipped_item(enemy_loadout, 1)->>'name', '');
    enemy_weapon_name := coalesce(game.raid_find_equipped_item(enemy_loadout, 0)->>'name', 'Rusty Knife');
    backpack_capacity := game.backpack_capacity(coalesce(game.raid_find_equipped_item(equipped_items, 2)->>'name', ''));

    if action in ('attack', 'burst-fire', 'full-auto') then
        if encounter_type <> 'Combat' then
            return save_payload;
        end if;

        uses_ammo := game.weapon_magazine_capacity(coalesce(equipped_weapon->>'name', 'Rusty Knife')) > 0;

        if action = 'attack' then
            if weapon_malfunction then
                log_entries := game.raid_append_log(log_entries, 'Weapon is malfunctioned. Reload to clear it.');
            elsif not game.weapon_supports_single_shot(equipped_weapon_name) then
                log_entries := game.raid_append_log(log_entries, 'Weapon does not support single fire.');
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
                        damage := game.roll_weapon_damage_d20(equipped_weapon_name, 'attack');
                        damage := game.apply_armor_damage_reduction(damage, enemy_armor_name, game.weapon_armor_penetration(equipped_weapon_name));
                        enemy_health := enemy_health - damage;
                        log_entries := game.raid_append_log(log_entries, format('You hit %s for %s.', enemy_name, damage));
                    else
                        log_entries := game.raid_append_log(log_entries, format('You miss %s.', enemy_name));
                    end if;
                end if;
            end if;
        elsif action = 'burst-fire' then
            if weapon_malfunction then
                log_entries := game.raid_append_log(log_entries, 'Weapon is malfunctioned. Reload to clear it.');
            elsif not game.weapon_supports_burst_fire(equipped_weapon_name) then
                log_entries := game.raid_append_log(log_entries, 'Weapon does not support burst fire.');
            elsif not uses_ammo or ammo < 3 then
                log_entries := game.raid_append_log(log_entries, 'Not enough ammo for Burst Fire.');
            else
                if not weapon_malfunction and random() < 0.10 then
                    weapon_malfunction := true;
                    log_entries := game.raid_append_log(log_entries, 'Weapon malfunctioned during Burst Fire. Reload to clear it.');
                else
                    ammo := ammo - 3;
                    attack_hit := game.roll_attack_d20(
                        game.ability_modifier(player_dexterity) - game.weapon_burst_attack_penalty(equipped_weapon_name),
                        10 + game.ability_modifier(enemy_dexterity));
                    if attack_hit then
                        damage := game.roll_weapon_damage_d20(equipped_weapon_name, 'burst-fire');
                        damage := game.apply_armor_damage_reduction(damage, enemy_armor_name, game.weapon_armor_penetration(equipped_weapon_name));
                        enemy_health := enemy_health - damage;
                        log_entries := game.raid_append_log(log_entries, format('Burst Fire deals %s.', damage));
                    else
                        log_entries := game.raid_append_log(log_entries, format('Burst Fire misses %s.', enemy_name));
                    end if;
                end if;
            end if;
        else
            if weapon_malfunction then
                log_entries := game.raid_append_log(log_entries, 'Weapon is malfunctioned. Reload to clear it.');
            elsif not game.weapon_supports_full_auto(equipped_weapon_name) then
                log_entries := game.raid_append_log(log_entries, 'Weapon does not support full auto.');
            elsif not uses_ammo or ammo < 10 then
                log_entries := game.raid_append_log(log_entries, 'Not enough ammo for Full Auto.');
            else
                if not weapon_malfunction and random() < 0.10 then
                    weapon_malfunction := true;
                    log_entries := game.raid_append_log(log_entries, 'Weapon malfunctioned during Full Auto. Reload to clear it.');
                else
                    ammo := ammo - 10;
                    attack_hit := game.roll_attack_d20(game.ability_modifier(player_dexterity) - 4, 10 + game.ability_modifier(enemy_dexterity));
                    if attack_hit then
                        damage := game.roll_weapon_damage_d20(equipped_weapon_name, 'full-auto');
                        damage := game.apply_armor_damage_reduction(damage, enemy_armor_name, game.weapon_armor_penetration(equipped_weapon_name));
                        enemy_health := enemy_health - damage;
                        log_entries := game.raid_append_log(log_entries, format('Full Auto deals %s.', damage));
                    else
                        log_entries := game.raid_append_log(log_entries, format('Full Auto misses %s.', enemy_name));
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
                reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(equipped_armor->>'name', ''), game.weapon_armor_penetration(enemy_weapon_name));
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
                    reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(equipped_armor->>'name', ''), game.weapon_armor_penetration(enemy_weapon_name));
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
                reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(equipped_armor->>'name', ''), game.weapon_armor_penetration(enemy_weapon_name));
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
                    reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(equipped_armor->>'name', ''), game.weapon_armor_penetration(enemy_weapon_name));
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
    elsif action in ('continue-searching', 'move-toward-extract') then
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
    raid_payload := jsonb_set(raid_payload, '{extractRequired}', to_jsonb(extract_required), true);

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
            'continue-searching',
            'move-toward-extract',
            'attempt-extract')
            then game.perform_raid_action(action, payload, auth.uid())
        else game.apply_profile_action(action, payload, auth.uid())
    end;
$$;
