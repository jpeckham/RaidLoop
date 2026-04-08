create or replace function game.payload_item_name(payload jsonb)
returns text
language sql
stable
as $function$
    select coalesce(
        payload->>'itemName',
        payload->>'item_name',
        payload->>'ItemName',
        payload->>'name',
        payload->>'Name',
        (
            select defs.name
            from game.item_defs defs
            where defs.item_def_id = coalesce(
                nullif(payload->>'itemDefId', '')::int,
                nullif(payload->>'item_def_id', '')::int,
                nullif(payload->>'ItemDefId', '')::int
            )
            limit 1
        ),
        (
            select defs.name
            from game.item_defs defs
            where defs.item_key = coalesce(
                payload->>'itemKey',
                payload->>'item_key',
                payload->>'ItemKey'
            )
            limit 1
        )
    );
$function$;

create or replace function game.perform_raid_action(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $function$
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
    enemy_constitution int;
    enemy_strength int;
    player_dexterity int;
    ammo int;
    medkits int;
    health int;
    backpack_capacity int;
    challenge int;
    distance_from_extract int;
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
    attack_roll int;
    attack_bonus int;
    attack_total int;
    attack_outcome text;
    damage int;
    incoming int;
    reduced_damage int;
    absorbed_damage int;
    player_attack_total int;
    enemy_attack_total int;
    enemy_armor_bonus int;
    player_armor_bonus int;
    current_slots int;
    dropped_item jsonb;
    loot_count int;
    enemy_dropped_items jsonb;
    player_max_health int;
    current_encumbrance int;
    player_encumbrance text;
    player_attack_penalty int;
    player_max_dex_bonus int;
    player_effective_dex_bonus int;
    player_strength int;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    player_max_health := greatest(coalesce((save_payload->>'playerMaxHealth')::int, 30), 1);
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
    enemy_constitution := greatest(coalesce((raid_payload->>'enemyConstitution')::int, 10), 0);
    enemy_strength := greatest(coalesce((raid_payload->>'enemyStrength')::int, 10), 0);
    ammo := greatest(coalesce((raid_payload->>'ammo')::int, 0), 0);
    medkits := greatest(coalesce((raid_payload->>'medkits')::int, 0), 0);
    health := greatest(coalesce((raid_payload->>'health')::int, player_max_health), 0);
    challenge := greatest(coalesce((raid_payload->>'challenge')::int, 0), 0);
    distance_from_extract := greatest(coalesce((raid_payload->>'distanceFromExtract')::int, 0), 0);
    extraction_combat := coalesce((raid_payload->>'extractionCombat')::boolean, false);
    equipped_weapon := game.raid_find_equipped_item(equipped_items, 0);
    equipped_weapon_name := coalesce(game.payload_item_name(equipped_weapon), 'Rusty Knife');
    equipped_armor := game.raid_find_equipped_item(equipped_items, 1);
    enemy_armor_name := coalesce(game.payload_item_name(game.raid_find_equipped_item(enemy_loadout, 1)), '');
    enemy_armor_bonus := game.armor_hit_bonus(enemy_armor_name);
    enemy_weapon_name := coalesce(game.payload_item_name(game.raid_find_equipped_item(enemy_loadout, 0)), 'Rusty Knife');
    player_armor_bonus := game.armor_hit_bonus(coalesce(game.payload_item_name(equipped_armor), ''));
    backpack_capacity := game.backpack_capacity(coalesce(game.payload_item_name(game.raid_find_equipped_item(equipped_items, 2)), ''));
    current_encumbrance := game.current_encumbrance(equipped_items || carried_loot, medkits);
    player_strength := coalesce((coalesce(raid_payload->'acceptedStats', save_payload->'acceptedStats')->>'strength')::int, 8);
    player_encumbrance := game.encumbrance_tier(player_strength, current_encumbrance);
    player_attack_penalty := game.encumbrance_attack_penalty(player_encumbrance);
    player_max_dex_bonus := game.encumbrance_max_dex_bonus(player_encumbrance);
    player_effective_dex_bonus := least(game.ability_modifier(player_dexterity), player_max_dex_bonus);

    if action in ('attack', 'burst-fire', 'full-auto') then
        if encounter_type <> 'Combat' then
            return save_payload;
        end if;

        uses_ammo := game.weapon_magazine_capacity(equipped_weapon_name) > 0;

        if action = 'attack' then
            if not game.weapon_supports_single_shot(equipped_weapon_name) then
                log_entries := game.raid_append_log(log_entries, 'Weapon does not support single fire.');
            elsif uses_ammo and ammo <= 0 then
                log_entries := game.raid_append_log(log_entries, 'No ammo.');
            else
                if uses_ammo then
                    ammo := ammo - 1;
                end if;

                attack_roll := floor(random() * 20)::int + 1;
                attack_bonus := player_effective_dex_bonus - player_attack_penalty;
                player_attack_total := attack_roll + attack_bonus;
                attack_total := player_attack_total;
                attack_outcome := game.classify_attack_outcome(
                    attack_roll,
                    player_attack_total,
                    game.ability_modifier(enemy_dexterity),
                    enemy_armor_bonus);

                if attack_outcome = 'hit' then
                    damage := game.roll_weapon_damage_d20(equipped_weapon_name, 'attack');
                    reduced_damage := game.apply_armor_damage_reduction(damage, enemy_armor_name, game.weapon_armor_penetration(equipped_weapon_name));
                    absorbed_damage := greatest(damage - reduced_damage, 0);
                    enemy_health := enemy_health - reduced_damage;
                    log_entries := game.raid_append_log(log_entries, game.describe_player_attack_outcome('attack', enemy_name, attack_outcome, reduced_damage, absorbed_damage));
                else
                    log_entries := game.raid_append_log(log_entries, game.describe_player_attack_outcome('attack', enemy_name, attack_outcome));
                end if;
            end if;
        elsif action = 'burst-fire' then
            if not game.weapon_supports_burst_fire(equipped_weapon_name) then
                log_entries := game.raid_append_log(log_entries, 'Weapon does not support burst fire.');
            elsif not uses_ammo or ammo < 3 then
                log_entries := game.raid_append_log(log_entries, 'Not enough ammo for Burst Fire.');
            else
                ammo := ammo - 3;
                attack_roll := floor(random() * 20)::int + 1;
                attack_bonus := player_effective_dex_bonus - game.weapon_burst_attack_penalty(equipped_weapon_name) - player_attack_penalty;
                player_attack_total := attack_roll + attack_bonus;
                attack_total := player_attack_total;
                attack_outcome := game.classify_attack_outcome(
                    attack_roll,
                    player_attack_total,
                    game.ability_modifier(enemy_dexterity),
                    enemy_armor_bonus);

                if attack_outcome = 'hit' then
                    damage := game.roll_weapon_damage_d20(equipped_weapon_name, 'burst-fire');
                    reduced_damage := game.apply_armor_damage_reduction(damage, enemy_armor_name, game.weapon_armor_penetration(equipped_weapon_name));
                    absorbed_damage := greatest(damage - reduced_damage, 0);
                    enemy_health := enemy_health - reduced_damage;
                    log_entries := game.raid_append_log(log_entries, game.describe_player_attack_outcome('burst-fire', enemy_name, attack_outcome, reduced_damage, absorbed_damage));
                else
                    log_entries := game.raid_append_log(log_entries, game.describe_player_attack_outcome('burst-fire', enemy_name, attack_outcome));
                end if;
            end if;
        else
            if not game.weapon_supports_full_auto(equipped_weapon_name) then
                log_entries := game.raid_append_log(log_entries, 'Weapon does not support full auto.');
            elsif not uses_ammo or ammo < 10 then
                log_entries := game.raid_append_log(log_entries, 'Not enough ammo for Full Auto.');
            else
                ammo := ammo - 10;
                attack_roll := floor(random() * 20)::int + 1;
                attack_bonus := player_effective_dex_bonus - 4 - player_attack_penalty;
                player_attack_total := attack_roll + attack_bonus;
                attack_total := player_attack_total;
                attack_outcome := game.classify_attack_outcome(
                    attack_roll,
                    player_attack_total,
                    game.ability_modifier(enemy_dexterity),
                    enemy_armor_bonus);

                if attack_outcome = 'hit' then
                    damage := game.roll_weapon_damage_d20(equipped_weapon_name, 'full-auto');
                    reduced_damage := game.apply_armor_damage_reduction(damage, enemy_armor_name, game.weapon_armor_penetration(equipped_weapon_name));
                    absorbed_damage := greatest(damage - reduced_damage, 0);
                    enemy_health := enemy_health - reduced_damage;
                    log_entries := game.raid_append_log(log_entries, game.describe_player_attack_outcome('full-auto', enemy_name, attack_outcome, reduced_damage, absorbed_damage));
                else
                    log_entries := game.raid_append_log(log_entries, game.describe_player_attack_outcome('full-auto', enemy_name, attack_outcome));
                end if;
            end if;
        end if;

        if enemy_health <= 0 then
            if extraction_combat then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Final guard defeated. Extraction successful.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, true, target_user_id);
            end if;

            enemy_dropped_items := enemy_loadout;
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
            raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
        else
            attack_roll := floor(random() * 20)::int + 1;
            attack_bonus := game.ability_modifier(enemy_dexterity);
            enemy_attack_total := attack_roll + attack_bonus;
            attack_total := enemy_attack_total;
            attack_outcome := game.classify_attack_outcome(
                attack_roll,
                enemy_attack_total,
                player_effective_dex_bonus,
                player_armor_bonus);

            if attack_outcome = 'hit' then
                incoming := 3 + floor(random() * 6)::int;
                reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(game.payload_item_name(equipped_armor), ''), game.weapon_armor_penetration(enemy_weapon_name));
                absorbed_damage := greatest(incoming - reduced_damage, 0);
                health := greatest(health - reduced_damage, 0);
                log_entries := game.raid_append_log(log_entries, game.describe_enemy_attack_outcome(enemy_name, attack_outcome, reduced_damage, absorbed_damage));
            else
                log_entries := game.raid_append_log(log_entries, game.describe_enemy_attack_outcome(enemy_name, attack_outcome));
            end if;

            if health <= 0 then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You were killed in raid. Loadout and loot lost.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, false, target_user_id);
            end if;

            raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(enemy_health), true);
            raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(health), true);
            raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
        end if;
    elsif action = 'use-medkit' then
        if medkits > 0 then
            medkits := medkits - 1;
            health := least(player_max_health, health + 10);
            log_entries := game.raid_append_log(log_entries, 'Medkit used (+10 HP).');

            if encounter_type = 'Combat' then
                attack_roll := floor(random() * 20)::int + 1;
                attack_bonus := game.ability_modifier(enemy_dexterity);
                enemy_attack_total := attack_roll + attack_bonus;
                attack_total := enemy_attack_total;
                attack_outcome := game.classify_attack_outcome(
                    attack_roll,
                    enemy_attack_total,
                    player_effective_dex_bonus,
                    player_armor_bonus);

                if attack_outcome = 'hit' then
                    incoming := 3 + floor(random() * 6)::int;
                    reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(game.payload_item_name(equipped_armor), ''), game.weapon_armor_penetration(enemy_weapon_name));
                    absorbed_damage := greatest(incoming - reduced_damage, 0);
                    health := greatest(health - reduced_damage, 0);
                    log_entries := game.raid_append_log(log_entries, game.describe_enemy_attack_outcome(enemy_name, attack_outcome, reduced_damage, absorbed_damage));
                else
                    log_entries := game.raid_append_log(log_entries, game.describe_enemy_attack_outcome(enemy_name, attack_outcome));
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
        uses_ammo := game.weapon_magazine_capacity(equipped_weapon_name) > 0;
        if not uses_ammo then
            log_entries := game.raid_append_log(log_entries, 'Knife doesn''t need reloading.');
        else
            ammo := game.weapon_magazine_capacity(equipped_weapon_name);
            log_entries := game.raid_append_log(log_entries, 'Weapon reloaded.');
        end if;

        if encounter_type = 'Combat' then
            attack_roll := floor(random() * 20)::int + 1;
            attack_bonus := game.ability_modifier(enemy_dexterity);
            enemy_attack_total := attack_roll + attack_bonus;
            attack_total := enemy_attack_total;
            attack_outcome := game.classify_attack_outcome(
                attack_roll,
                enemy_attack_total,
                player_effective_dex_bonus,
                player_armor_bonus);

            if attack_outcome = 'hit' then
                incoming := 3 + floor(random() * 6)::int;
                reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(game.payload_item_name(equipped_armor), ''), game.weapon_armor_penetration(enemy_weapon_name));
                absorbed_damage := greatest(incoming - reduced_damage, 0);
                health := greatest(health - reduced_damage, 0);
                log_entries := game.raid_append_log(log_entries, game.describe_enemy_attack_outcome(enemy_name, attack_outcome, reduced_damage, absorbed_damage));
            else
                log_entries := game.raid_append_log(log_entries, game.describe_enemy_attack_outcome(enemy_name, attack_outcome));
            end if;

            if health <= 0 then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You were killed in raid. Loadout and loot lost.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, false, target_user_id);
            end if;
        end if;

        raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
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
                attack_roll := floor(random() * 20)::int + 1;
                attack_bonus := game.ability_modifier(enemy_dexterity);
                enemy_attack_total := attack_roll + attack_bonus;
                attack_total := enemy_attack_total;
                attack_outcome := game.classify_attack_outcome(
                    attack_roll,
                    enemy_attack_total,
                    player_effective_dex_bonus,
                    player_armor_bonus);

                if attack_outcome = 'hit' then
                    incoming := 3 + floor(random() * 6)::int;
                    reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(game.payload_item_name(equipped_armor), ''), game.weapon_armor_penetration(enemy_weapon_name));
                    absorbed_damage := greatest(incoming - reduced_damage, 0);
                    health := greatest(health - reduced_damage, 0);
                    log_entries := game.raid_append_log(log_entries, game.describe_enemy_attack_outcome(enemy_name, attack_outcome, reduced_damage, absorbed_damage));
                else
                    log_entries := game.raid_append_log(log_entries, game.describe_enemy_attack_outcome(enemy_name, attack_outcome));
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
        item_name := game.payload_item_name(payload);
        selected_item := (
            select value
            from jsonb_array_elements(discovered_loot) value
            where game.item_matches_payload(value, payload)
            limit 1
        );

        if selected_item is not null then
            if game.payload_item_name(selected_item) = 'Medkit' then
                discovered_loot := game.jsonb_array_remove(
                    discovered_loot,
                    coalesce((
                        select ordinality::int - 1
                        from jsonb_array_elements(discovered_loot) with ordinality value
                        where game.item_matches_payload(value, payload)
                        limit 1
                    ), -1));
                medkits := medkits + 1;
                log_entries := game.raid_append_log(log_entries, format('Looted %s.', item_name));
            else
                current_slots := game.raid_current_slots(carried_loot);
                if current_slots + coalesce((selected_item->>'slots')::int, 1) <= backpack_capacity then
                    discovered_loot := game.jsonb_array_remove(
                        discovered_loot,
                        coalesce((
                            select ordinality::int - 1
                            from jsonb_array_elements(discovered_loot) with ordinality value
                            where game.item_matches_payload(value, payload)
                            limit 1
                        ), -1));
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
        item_name := game.payload_item_name(payload);
        selected_item := (
            select value
            from jsonb_array_elements(carried_loot) value
            where game.item_matches_payload(value, payload)
            limit 1
        );
        if selected_item is not null then
            carried_loot := game.jsonb_array_remove(
                carried_loot,
                coalesce((
                    select ordinality::int - 1
                    from jsonb_array_elements(carried_loot) with ordinality value
                    where game.item_matches_payload(value, payload)
                    limit 1
                ), -1));
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
                ammo := 0;
            end if;

            log_entries := game.raid_append_log(log_entries, format('Dropped equipped %s.', initcap(lower(payload->>'slotType'))));
        end if;

        raid_payload := jsonb_set(raid_payload, '{equippedItems}', equipped_items, true);
        raid_payload := jsonb_set(raid_payload, '{carriedLoot}', carried_loot, true);
        raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', discovered_loot, true);
        raid_payload := jsonb_set(raid_payload, '{backpackCapacity}', to_jsonb(backpack_capacity), true);
        raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action in ('equip-from-discovered', 'equip-from-carried') then
        item_name := game.payload_item_name(payload);
        if action = 'equip-from-discovered' then
            selected_item := (
                select value
                from jsonb_array_elements(discovered_loot) value
                where game.item_matches_payload(value, payload)
                limit 1
            );
        else
            selected_item := (
                select value
                from jsonb_array_elements(carried_loot) value
                where game.item_matches_payload(value, payload)
                limit 1
            );
        end if;

        if selected_item is not null and coalesce((selected_item->>'type')::int, -1) in (0, 1, 2) then
            slot_type := coalesce((selected_item->>'type')::int, -1);
            if action = 'equip-from-discovered' then
                discovered_loot := game.jsonb_array_remove(
                    discovered_loot,
                    coalesce((
                        select ordinality::int - 1
                        from jsonb_array_elements(discovered_loot) with ordinality value
                        where game.item_matches_payload(value, payload)
                        limit 1
                    ), -1));
            else
                carried_loot := game.jsonb_array_remove(
                    carried_loot,
                    coalesce((
                        select ordinality::int - 1
                        from jsonb_array_elements(carried_loot) with ordinality value
                        where game.item_matches_payload(value, payload)
                        limit 1
                    ), -1));
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
                backpack_capacity := game.backpack_capacity(coalesce(game.payload_item_name(selected_item), ''));
                while game.raid_current_slots(carried_loot) > backpack_capacity and jsonb_array_length(carried_loot) > 0 loop
                    dropped_item := game.jsonb_array_get(carried_loot, jsonb_array_length(carried_loot) - 1);
                    carried_loot := game.jsonb_array_remove(carried_loot, jsonb_array_length(carried_loot) - 1);
                    discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(dropped_item));
                end loop;
            end if;

            if slot_type = 0 then
                ammo := least(greatest(ammo, 0), game.weapon_magazine_capacity(coalesce(game.payload_item_name(selected_item), 'Rusty Knife')));
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
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action = 'go-deeper' then
        challenge := challenge + 1;
        distance_from_extract := distance_from_extract + 1;
        raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
        raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Moved deeper into the raid.'), true);
        raid_payload := game.generate_raid_encounter(raid_payload, false);
    elsif action = 'move-toward-extract' then
        loot_count := jsonb_array_length(discovered_loot);
        if encounter_type = 'Loot' and loot_count > 0 then
            log_entries := game.raid_append_log(log_entries, format('Moved on and left %s items behind.', loot_count));
            raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
        end if;

        distance_from_extract := greatest(distance_from_extract - 1, 0);
        raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);
        raid_payload := game.generate_raid_encounter(raid_payload, true);
    elsif action = 'stay-at-extract' then
        challenge := challenge + 1;
        raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
        raid_payload := game.generate_raid_encounter(raid_payload, false);
    elsif action = 'attempt-extract' then
        if distance_from_extract = 0 then
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Extraction completed. Loot secured.'), true);
            return game.finish_raid_session(save_payload, raid_payload, raid_profile, true, target_user_id);
        end if;
    end if;

    raid_payload := jsonb_set(raid_payload, '{equippedItems}', game.normalize_items(coalesce(raid_payload->'equippedItems', equipped_items)), true);
    raid_payload := jsonb_set(raid_payload, '{carriedLoot}', game.normalize_items(coalesce(raid_payload->'carriedLoot', carried_loot)), true);
    raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', game.normalize_items(coalesce(raid_payload->'discoveredLoot', discovered_loot)), true);
    raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', game.normalize_items(coalesce(raid_payload->'enemyLoadout', enemy_loadout)), true);
    raid_payload := jsonb_set(raid_payload, '{medkits}', to_jsonb(greatest(coalesce((raid_payload->>'medkits')::int, medkits), 0)), true);
    raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(greatest(coalesce((raid_payload->>'health')::int, health), 0)), true);
    raid_payload := jsonb_set(raid_payload, '{backpackCapacity}', to_jsonb(game.backpack_capacity(coalesce(game.payload_item_name(game.raid_find_equipped_item(coalesce(raid_payload->'equippedItems', equipped_items), 2)), ''))), true);
    raid_payload := jsonb_set(raid_payload, '{lootSlots}', to_jsonb(game.raid_current_slots(coalesce(raid_payload->'carriedLoot', carried_loot))), true);
    raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
    raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);

    current_encumbrance := game.current_encumbrance(
        coalesce(raid_payload->'equippedItems', equipped_items) || coalesce(raid_payload->'carriedLoot', carried_loot),
        coalesce((raid_payload->>'medkits')::int, medkits));
    player_strength := coalesce((coalesce(raid_payload->'acceptedStats', save_payload->'acceptedStats')->>'strength')::int, 8);
    player_encumbrance := game.encumbrance_tier(player_strength, current_encumbrance);
    raid_payload := jsonb_set(raid_payload, '{encumbrance}', to_jsonb(current_encumbrance), true);
    raid_payload := jsonb_set(raid_payload, '{maxEncumbrance}', to_jsonb(game.max_encumbrance(player_strength)), true);
    raid_payload := jsonb_set(raid_payload, '{encumbranceTier}', to_jsonb(player_encumbrance), true);

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
$function$;

create or replace function game.perform_raid_action_with_encumbrance(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $function$
declare
    request_payload jsonb := payload;
    save_payload jsonb;
    save_accepted_stats jsonb;
    raid_payload jsonb;
    equipped_items jsonb;
    carried_loot jsonb;
    discovered_loot jsonb;
    medkits int;
    selected_item jsonb;
    previous_item jsonb;
    selected_type int;
    item_name text;
    max_encumbrance int;
    current_encumbrance int;
    prospective_encumbrance int;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    save_accepted_stats := coalesce(
        save_payload->'acceptedStats',
        save_payload->'AcceptedStats',
        jsonb_build_object(
            'strength', 8,
            'dexterity', 8,
            'constitution', 8,
            'intelligence', 8,
            'wisdom', 8,
            'charisma', 8
        )
    );

    select sessions.payload
    into raid_payload
    from public.raid_sessions sessions
    where sessions.user_id = target_user_id;

    if raid_payload is null then
        return game.perform_raid_action(action, request_payload, target_user_id);
    end if;

    raid_payload := game.normalize_active_raid_payload(raid_payload, save_accepted_stats);

    update public.raid_sessions sessions
    set payload = raid_payload,
        updated_at = timezone('utc', now())
    where sessions.user_id = target_user_id;

    save_payload := jsonb_set(save_payload, '{activeRaid}', raid_payload, true);
    update public.game_saves saves
    set payload = save_payload,
        save_version = 1,
        updated_at = timezone('utc', now())
    where saves.user_id = target_user_id;

    max_encumbrance := game.max_encumbrance(coalesce((save_accepted_stats->>'strength')::int, 8));
    equipped_items := game.normalize_items(coalesce(raid_payload->'equippedItems', '[]'::jsonb));
    carried_loot := game.normalize_items(coalesce(raid_payload->'carriedLoot', '[]'::jsonb));
    discovered_loot := game.normalize_items(coalesce(raid_payload->'discoveredLoot', '[]'::jsonb));
    medkits := greatest(coalesce((raid_payload->>'medkits')::int, 0), 0);
    current_encumbrance := game.current_encumbrance(equipped_items || carried_loot, medkits);

    item_name := game.payload_item_name(request_payload);
    if item_name is not null then
        request_payload := jsonb_set(request_payload, '{itemName}', to_jsonb(item_name), true);
    end if;

    if action = 'take-loot' then
        selected_item := (
            select value
            from jsonb_array_elements(discovered_loot) value
            where game.item_matches_payload(value, request_payload)
            limit 1
        );

        if selected_item is not null then
            prospective_encumbrance := current_encumbrance + game.item_weight(game.payload_item_name(selected_item));
            if prospective_encumbrance > max_encumbrance then
                return save_payload;
            end if;
        end if;
    elsif action in ('equip-from-discovered', 'equip-from-carried') then
        if action = 'equip-from-discovered' then
            selected_item := (
                select value
                from jsonb_array_elements(discovered_loot) value
                where game.item_matches_payload(value, request_payload)
                limit 1
            );
        else
            selected_item := (
                select value
                from jsonb_array_elements(carried_loot) value
                where game.item_matches_payload(value, request_payload)
                limit 1
            );
        end if;

        if selected_item is not null and coalesce((selected_item->>'type')::int, -1) in (0, 1, 2) then
            selected_type := coalesce((selected_item->>'type')::int, -1);
            previous_item := game.raid_find_equipped_item(equipped_items, selected_type);
            if action = 'equip-from-discovered' then
                prospective_encumbrance := current_encumbrance
                    + game.item_weight(game.payload_item_name(selected_item))
                    - coalesce(game.item_weight(game.payload_item_name(previous_item)), 0);
            else
                prospective_encumbrance := current_encumbrance
                    - coalesce(game.item_weight(game.payload_item_name(previous_item)), 0);
            end if;

            if prospective_encumbrance > max_encumbrance then
                return save_payload;
            end if;
        end if;
    end if;

    return game.perform_raid_action(action, request_payload, target_user_id);
end;
$function$;

create or replace function game.item_matches_payload(item jsonb, payload jsonb)
returns boolean
language sql
stable
as $function$
    with item_norm as (
        select
            coalesce(
                nullif(item->>'itemDefId', '')::int,
                nullif(item->>'item_def_id', '')::int,
                nullif(item->>'ItemDefId', '')::int
            ) as item_def_id,
            game.payload_item_name(item) as item_name
    ),
    payload_norm as (
        select
            coalesce(
                nullif(payload->>'itemDefId', '')::int,
                nullif(payload->>'item_def_id', '')::int,
                nullif(payload->>'ItemDefId', '')::int
            ) as payload_def_id,
            game.payload_item_name(payload) as payload_name
    )
    select
        (
            (select payload_def_id from payload_norm) is not null
            and (select payload_def_id from payload_norm) > 0
            and (select item_def_id from item_norm) = (select payload_def_id from payload_norm)
        )
        or (
            coalesce(nullif((select payload_name from payload_norm), ''), '') <> ''
            and (select item_name from item_norm) = (select payload_name from payload_norm)
        );
$function$;

create or replace function game.current_encumbrance(items jsonb, medkits int default 0)
returns int
language sql
stable
as $function$
    select coalesce(
        (
            select sum(
                coalesce(
                    nullif(value->>'weight', '')::int,
                    game.item_weight(game.payload_item_name(value))
                )
            )
            from jsonb_array_elements(coalesce(items, '[]'::jsonb)) value
        ),
        0
    ) + greatest(coalesce(medkits, 0), 0) * game.item_weight('Medkit');
$function$;
