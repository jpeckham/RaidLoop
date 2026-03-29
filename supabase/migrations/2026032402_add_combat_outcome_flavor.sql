alter table game.item_defs
    add column if not exists armor_hit_bonus int not null default 0;

update game.item_defs
set armor_hit_bonus = case item_key
    when 'soft_armor_vest' then 1
    when 'light_plate_carrier' then 2
    when 'medium_plate_carrier' then 3
    when 'heavy_plate_carrier' then 4
    when 'assault_plate_carrier' then 5
    else armor_hit_bonus
end
where item_key in (
    'soft_armor_vest',
    'light_plate_carrier',
    'medium_plate_carrier',
    'heavy_plate_carrier',
    'assault_plate_carrier');

create or replace function game.weapon_armor_penetration(weapon_name text)
returns int
language sql
stable
as $$
    select coalesce((resolved.item_def).armor_penetration, 0)
    from (
        select game.item_def_by_name(weapon_name) as item_def
    ) resolved;
$$;

create or replace function game.armor_damage_reduction(armor_name text)
returns int
language sql
stable
as $$
    select coalesce((resolved.item_def).armor_damage_reduction, 0)
    from (
        select game.item_def_by_name(armor_name) as item_def
    ) resolved;
$$;

create or replace function game.armor_hit_bonus(armor_name text)
returns int
language sql
stable
as $$
    select coalesce((resolved.item_def).armor_hit_bonus, 0)
    from (
        select game.item_def_by_name(armor_name) as item_def
    ) resolved;
$$;

create or replace function game.apply_armor_damage_reduction(incoming_damage int, armor_name text, armor_penetration int default 0)
returns int
language plpgsql
stable
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
stable
as $$
    select coalesce((resolved.item_def).supports_single_shot, true)
    from (
        select game.item_def_by_name(weapon_name) as item_def
    ) resolved;
$$;

create or replace function game.weapon_supports_burst_fire(weapon_name text)
returns boolean
language sql
stable
as $$
    select coalesce((resolved.item_def).supports_burst_fire, false)
    from (
        select game.item_def_by_name(weapon_name) as item_def
    ) resolved;
$$;

create or replace function game.weapon_supports_full_auto(weapon_name text)
returns boolean
language sql
stable
as $$
    select coalesce((resolved.item_def).supports_full_auto, false)
    from (
        select game.item_def_by_name(weapon_name) as item_def
    ) resolved;
$$;

create or replace function game.weapon_burst_attack_penalty(weapon_name text)
returns int
language sql
stable
as $$
    select coalesce((resolved.item_def).burst_attack_penalty, 3)
    from (
        select game.item_def_by_name(weapon_name) as item_def
    ) resolved;
$$;

create or replace function game.roll_weapon_damage_d20(weapon_name text, attack_mode text)
returns int
language plpgsql
volatile
as $$
declare
    item_def game.item_defs%rowtype;
    die_size int;
    die_count int;
    roll int := 0;
begin
    select *
    from game.item_def_by_name(weapon_name)
    into item_def;

    die_size := coalesce(item_def.damage_die_size, 6);

    die_count := case coalesce(attack_mode, 'attack')
        when 'burst-fire' then 3
        when 'full-auto' then 4
        else 2
    end;

    for die_index in 1..die_count loop
        roll := roll + floor(random() * die_size)::int + 1;
    end loop;

    return roll;
end;
$$;

create or replace function game.classify_attack_outcome(
    attack_roll int,
    attack_total int,
    dodge_bonus int,
    armor_bonus int)
returns text
language plpgsql
immutable
as $$
begin
    dodge_bonus := greatest(coalesce(dodge_bonus, 0), 0);
    armor_bonus := greatest(coalesce(armor_bonus, 0), 0);

    if attack_roll = 1 then
        return 'miss';
    end if;

    if attack_roll = 20 then
        return 'hit';
    end if;

    if attack_total < 10 then
        return 'miss';
    end if;

    if attack_total < 10 + dodge_bonus then
        return 'evaded';
    end if;

    if attack_total < 10 + dodge_bonus + armor_bonus then
        return 'armor-absorbed';
    end if;

    return 'hit';
end;
$$;

create or replace function game.describe_player_attack_outcome(
    attack_label text,
    target_name text,
    outcome text,
    final_damage int default 0,
    absorbed_damage int default 0)
returns text
language plpgsql
immutable
as $$
begin
    return case outcome
        when 'miss' then
            case coalesce(attack_label, 'attack')
                when 'attack' then format('You miss %s.', target_name)
                when 'burst-fire' then format('Burst Fire misses %s.', target_name)
                when 'full-auto' then format('Full Auto misses %s.', target_name)
                else format('You miss %s.', target_name)
            end
        when 'evaded' then
            case coalesce(attack_label, 'attack')
                when 'attack' then format('%s evades your attack.', target_name)
                when 'burst-fire' then format('%s evades your Burst Fire.', target_name)
                when 'full-auto' then format('%s evades your Full Auto.', target_name)
                else format('%s evades your attack.', target_name)
            end
        when 'armor-absorbed' then
            case coalesce(attack_label, 'attack')
                when 'attack' then format('Your attack on %s is absorbed by armor.', target_name)
                when 'burst-fire' then format('Burst Fire on %s is absorbed by armor.', target_name)
                when 'full-auto' then format('Full Auto on %s is absorbed by armor.', target_name)
                else format('Your attack on %s is absorbed by armor.', target_name)
            end
        when 'hit' then
            case
                when coalesce(absorbed_damage, 0) > 0 and coalesce(attack_label, 'attack') = 'attack'
                    then format('You hit %s for %s. Enemy armor absorbs %s.', target_name, final_damage, absorbed_damage)
                when coalesce(absorbed_damage, 0) > 0 and attack_label = 'burst-fire'
                    then format('Burst Fire hits %s for %s. Enemy armor absorbs %s.', target_name, final_damage, absorbed_damage)
                when coalesce(absorbed_damage, 0) > 0 and attack_label = 'full-auto'
                    then format('Full Auto hits %s for %s. Enemy armor absorbs %s.', target_name, final_damage, absorbed_damage)
                when coalesce(attack_label, 'attack') = 'burst-fire'
                    then format('Burst Fire hits %s for %s.', target_name, final_damage)
                when coalesce(attack_label, 'attack') = 'full-auto'
                    then format('Full Auto hits %s for %s.', target_name, final_damage)
                else format('You hit %s for %s.', target_name, final_damage)
            end
        else format('You miss %s.', target_name)
    end;
end;
$$;

create or replace function game.describe_enemy_attack_outcome(
    attacker_name text,
    outcome text,
    final_damage int default 0,
    absorbed_damage int default 0)
returns text
language plpgsql
immutable
as $$
begin
    return case outcome
        when 'miss' then format('%s misses you.', attacker_name)
        when 'evaded' then format('You evade %s''s attack.', attacker_name)
        when 'armor-absorbed' then format('%s''s attack is absorbed by armor.', attacker_name)
        when 'hit' then
            case
                when coalesce(absorbed_damage, 0) > 0
                    then format('%s hits you for %s. Your armor absorbs %s.', attacker_name, final_damage, absorbed_damage)
                else format('%s hits you for %s.', attacker_name, final_damage)
            end
        else format('%s misses you.', attacker_name)
    end;
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
    ammo := greatest(coalesce((raid_payload->>'ammo')::int, 0), 0);
    medkits := greatest(coalesce((raid_payload->>'medkits')::int, 0), 0);
    health := greatest(coalesce((raid_payload->>'health')::int, player_max_health), 0);
    challenge := greatest(coalesce((raid_payload->>'challenge')::int, 0), 0);
    distance_from_extract := greatest(coalesce((raid_payload->>'distanceFromExtract')::int, 0), 0);
    extraction_combat := coalesce((raid_payload->>'extractionCombat')::boolean, false);
    equipped_weapon := game.raid_find_equipped_item(equipped_items, 0);
    equipped_weapon_name := coalesce(equipped_weapon->>'name', 'Rusty Knife');
    equipped_armor := game.raid_find_equipped_item(equipped_items, 1);
    enemy_armor_name := coalesce(game.raid_find_equipped_item(enemy_loadout, 1)->>'name', '');
    enemy_armor_bonus := game.armor_hit_bonus(enemy_armor_name);
    enemy_weapon_name := coalesce(game.raid_find_equipped_item(enemy_loadout, 0)->>'name', 'Rusty Knife');
    player_armor_bonus := game.armor_hit_bonus(coalesce(equipped_armor->>'name', ''));
    backpack_capacity := game.backpack_capacity(coalesce(game.raid_find_equipped_item(equipped_items, 2)->>'name', ''));

    if action in ('attack', 'burst-fire', 'full-auto') then
        if encounter_type <> 'Combat' then
            return save_payload;
        end if;

        uses_ammo := game.weapon_magazine_capacity(coalesce(equipped_weapon->>'name', 'Rusty Knife')) > 0;

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
                attack_bonus := game.ability_modifier(player_dexterity);
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
                attack_bonus := game.ability_modifier(player_dexterity) - game.weapon_burst_attack_penalty(equipped_weapon_name);
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
                attack_bonus := game.ability_modifier(player_dexterity) - 4;
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
                game.ability_modifier(player_dexterity),
                player_armor_bonus);

            if attack_outcome = 'hit' then
                incoming := 3 + floor(random() * 6)::int;
                reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(equipped_armor->>'name', ''), game.weapon_armor_penetration(enemy_weapon_name));
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
                    game.ability_modifier(player_dexterity),
                    player_armor_bonus);

                if attack_outcome = 'hit' then
                    incoming := 3 + floor(random() * 6)::int;
                    reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(equipped_armor->>'name', ''), game.weapon_armor_penetration(enemy_weapon_name));
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
        uses_ammo := game.weapon_magazine_capacity(coalesce(equipped_weapon->>'name', 'Rusty Knife')) > 0;
        if not uses_ammo then
            log_entries := game.raid_append_log(log_entries, 'Knife doesn''t need reloading.');
        else
            ammo := game.weapon_magazine_capacity(coalesce(equipped_weapon->>'name', 'Rusty Knife'));
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
                game.ability_modifier(player_dexterity),
                player_armor_bonus);

            if attack_outcome = 'hit' then
                incoming := 3 + floor(random() * 6)::int;
                reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(equipped_armor->>'name', ''), game.weapon_armor_penetration(enemy_weapon_name));
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
                    game.ability_modifier(player_dexterity),
                    player_armor_bonus);

                if attack_outcome = 'hit' then
                    incoming := 3 + floor(random() * 6)::int;
                    reduced_damage := game.apply_armor_damage_reduction(incoming, coalesce(equipped_armor->>'name', ''), game.weapon_armor_penetration(enemy_weapon_name));
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
    raid_payload := jsonb_set(raid_payload, '{backpackCapacity}', to_jsonb(game.backpack_capacity(coalesce(game.raid_find_equipped_item(coalesce(raid_payload->'equippedItems', equipped_items), 2)->>'name', ''))), true);
    raid_payload := jsonb_set(raid_payload, '{lootSlots}', to_jsonb(game.raid_current_slots(coalesce(raid_payload->'carriedLoot', carried_loot))), true);
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

update public.raid_sessions
set payload = case
        when payload ? 'weaponMalfunction' then payload #- '{weaponMalfunction}'
        else payload
    end,
    updated_at = timezone('utc', now())
where payload ? 'weaponMalfunction';

update public.game_saves
set payload = case
        when payload ? 'activeRaid'
            and jsonb_typeof(payload->'activeRaid') = 'object'
            and (payload->'activeRaid') ? 'weaponMalfunction'
            then payload #- '{activeRaid,weaponMalfunction}'
        else payload
    end,
    updated_at = timezone('utc', now())
where payload ? 'activeRaid'
  and jsonb_typeof(payload->'activeRaid') = 'object'
  and (payload->'activeRaid') ? 'weaponMalfunction';
