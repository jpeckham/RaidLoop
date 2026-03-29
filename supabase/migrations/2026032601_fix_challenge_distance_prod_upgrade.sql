alter table game.encounter_table_entries
    add column if not exists challenge_min int not null default 0,
    add column if not exists challenge_max_exclusive int not null default 2147483647,
    add column if not exists enemy_dexterity int not null default 10,
    add column if not exists enemy_constitution int not null default 10,
    add column if not exists enemy_strength int not null default 10;

insert into game.enemy_loadout_tables (table_key, name, enabled)
values
    ('challenge_0_enemy_loadout', 'Challenge 0 Enemy Loadouts', true)
on conflict (table_key) do update
set name = excluded.name,
    enabled = excluded.enabled;

delete from game.enemy_loadout_variants
where variant_key in (
    'challenge0_light_pistol',
    'challenge0_light_pistol_6b2',
    'challenge0_6b2_medkit',
    'challenge0_light_pistol_6b2_medkit'
);

insert into game.enemy_loadout_variants (variant_key, table_key, weight, sort_order, enabled)
values
    ('challenge0_light_pistol', 'challenge_0_enemy_loadout', 120, 10, true),
    ('challenge0_light_pistol_6b2', 'challenge_0_enemy_loadout', 90, 20, true),
    ('challenge0_6b2_medkit', 'challenge_0_enemy_loadout', 60, 30, true),
    ('challenge0_light_pistol_6b2_medkit', 'challenge_0_enemy_loadout', 30, 40, true)
on conflict (variant_key) do update
set table_key = excluded.table_key,
    weight = excluded.weight,
    sort_order = excluded.sort_order,
    enabled = excluded.enabled;

delete from game.enemy_loadout_variant_items
where variant_key in (
    'challenge0_light_pistol',
    'challenge0_light_pistol_6b2',
    'challenge0_6b2_medkit',
    'challenge0_light_pistol_6b2_medkit'
);

insert into game.enemy_loadout_variant_items (variant_key, item_key, item_order)
values
    ('challenge0_light_pistol', 'light_pistol', 10),
    ('challenge0_light_pistol_6b2', 'light_pistol', 10),
    ('challenge0_light_pistol_6b2', 'soft_armor_vest', 20),
    ('challenge0_6b2_medkit', 'soft_armor_vest', 10),
    ('challenge0_6b2_medkit', 'medkit', 20),
    ('challenge0_light_pistol_6b2_medkit', 'light_pistol', 10),
    ('challenge0_light_pistol_6b2_medkit', 'soft_armor_vest', 20),
    ('challenge0_light_pistol_6b2_medkit', 'medkit', 30);

update game.encounter_table_entries
set challenge_min = case entry_key
        when 'raid_combat_travel_player_spots_camp' then 0
        when 'raid_combat_travel_enemy_ambush' then 3
        when 'raid_combat_travel_mutual_contact' then 6
        when 'raid_combat_loot_player_hears_movement' then 0
        when 'raid_combat_loot_enemy_pushes_camp' then 3
        when 'raid_combat_loot_mutual_contact' then 6
        when 'raid_combat_extract_player_spots_guard' then 0
        when 'raid_combat_extract_enemy_ambush' then 3
        when 'raid_combat_extract_mutual_contact' then 6
        else challenge_min
    end,
    challenge_max_exclusive = case entry_key
        when 'raid_combat_travel_player_spots_camp' then 3
        when 'raid_combat_travel_enemy_ambush' then 6
        when 'raid_combat_travel_mutual_contact' then 2147483647
        when 'raid_combat_loot_player_hears_movement' then 3
        when 'raid_combat_loot_enemy_pushes_camp' then 6
        when 'raid_combat_loot_mutual_contact' then 2147483647
        when 'raid_combat_extract_player_spots_guard' then 3
        when 'raid_combat_extract_enemy_ambush' then 6
        when 'raid_combat_extract_mutual_contact' then 2147483647
        else challenge_max_exclusive
    end,
    enemy_loadout_table_key = case entry_key
        when 'raid_combat_travel_player_spots_camp' then 'challenge_0_enemy_loadout'
        when 'raid_combat_loot_player_hears_movement' then 'challenge_0_enemy_loadout'
        when 'raid_combat_extract_player_spots_guard' then 'challenge_0_enemy_loadout'
        else 'default_enemy_loadout'
    end,
    enemy_health_min = case entry_key
        when 'raid_combat_travel_player_spots_camp' then 8
        when 'raid_combat_travel_enemy_ambush' then 12
        when 'raid_combat_travel_mutual_contact' then 16
        when 'raid_combat_loot_player_hears_movement' then 8
        when 'raid_combat_loot_enemy_pushes_camp' then 12
        when 'raid_combat_loot_mutual_contact' then 16
        when 'raid_combat_extract_player_spots_guard' then 8
        when 'raid_combat_extract_enemy_ambush' then 12
        when 'raid_combat_extract_mutual_contact' then 16
        else enemy_health_min
    end,
    enemy_health_max_exclusive = case entry_key
        when 'raid_combat_travel_player_spots_camp' then 14
        when 'raid_combat_travel_enemy_ambush' then 18
        when 'raid_combat_travel_mutual_contact' then 24
        when 'raid_combat_loot_player_hears_movement' then 14
        when 'raid_combat_loot_enemy_pushes_camp' then 18
        when 'raid_combat_loot_mutual_contact' then 24
        when 'raid_combat_extract_player_spots_guard' then 14
        when 'raid_combat_extract_enemy_ambush' then 18
        when 'raid_combat_extract_mutual_contact' then 24
        else enemy_health_max_exclusive
    end,
    enemy_dexterity = case entry_key
        when 'raid_combat_travel_player_spots_camp' then 9
        when 'raid_combat_travel_enemy_ambush' then 10
        when 'raid_combat_travel_mutual_contact' then 11
        when 'raid_combat_loot_player_hears_movement' then 9
        when 'raid_combat_loot_enemy_pushes_camp' then 10
        when 'raid_combat_loot_mutual_contact' then 11
        when 'raid_combat_extract_player_spots_guard' then 9
        when 'raid_combat_extract_enemy_ambush' then 10
        when 'raid_combat_extract_mutual_contact' then 11
        else enemy_dexterity
    end,
    enemy_constitution = case entry_key
        when 'raid_combat_travel_player_spots_camp' then 9
        when 'raid_combat_travel_enemy_ambush' then 10
        when 'raid_combat_travel_mutual_contact' then 11
        when 'raid_combat_loot_player_hears_movement' then 9
        when 'raid_combat_loot_enemy_pushes_camp' then 10
        when 'raid_combat_loot_mutual_contact' then 11
        when 'raid_combat_extract_player_spots_guard' then 9
        when 'raid_combat_extract_enemy_ambush' then 10
        when 'raid_combat_extract_mutual_contact' then 11
        else enemy_constitution
    end,
    enemy_strength = case entry_key
        when 'raid_combat_travel_player_spots_camp' then 9
        when 'raid_combat_travel_enemy_ambush' then 10
        when 'raid_combat_travel_mutual_contact' then 11
        when 'raid_combat_loot_player_hears_movement' then 9
        when 'raid_combat_loot_enemy_pushes_camp' then 10
        when 'raid_combat_loot_mutual_contact' then 11
        when 'raid_combat_extract_player_spots_guard' then 9
        when 'raid_combat_extract_enemy_ambush' then 10
        when 'raid_combat_extract_mutual_contact' then 11
        else enemy_strength
    end
where entry_key in (
    'raid_combat_travel_player_spots_camp',
    'raid_combat_travel_enemy_ambush',
    'raid_combat_travel_mutual_contact',
    'raid_combat_loot_player_hears_movement',
    'raid_combat_loot_enemy_pushes_camp',
    'raid_combat_loot_mutual_contact',
    'raid_combat_extract_player_spots_guard',
    'raid_combat_extract_enemy_ambush',
    'raid_combat_extract_mutual_contact'
);

create or replace function game.build_raid_snapshot(loadout jsonb, raider_name text, player_max_health int)
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
        'health', greatest(coalesce(player_max_health, 30), 1),
        'backpackCapacity', game.backpack_capacity(equipped_backpack_name),
        'ammo', game.weapon_magazine_capacity(equipped_weapon_name),
        'weaponMalfunction', false,
        'medkits', medkits,
        'lootSlots', 0,
        'challenge', 0,
        'distanceFromExtract', 3,
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
            'go-deeper',
            'move-toward-extract',
            'stay-at-extract',
            'attempt-extract')
            then game.perform_raid_action(action, payload, auth.uid())
        else game.apply_profile_action(action, payload, auth.uid())
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
    entry jsonb;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    on_person_items := coalesce(save_payload->'onPersonItems', '[]'::jsonb);
    random_character := save_payload->'randomCharacter';
    player_max_health := greatest(coalesce((save_payload->>'playerMaxHealth')::int, 30), 1);

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

        raid_snapshot := game.build_raid_snapshot(loadout, raider_name, player_max_health);

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
        raid_snapshot := game.build_raid_snapshot(loadout, raider_name, player_max_health);

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

create or replace function game.generate_raid_encounter(raid_payload jsonb, moving_to_extract boolean default false)
returns jsonb
language plpgsql
volatile
as $$
declare
    updated_payload jsonb := coalesce(raid_payload, '{}'::jsonb);
    challenge int := greatest(coalesce((updated_payload->>'challenge')::int, 0), 0);
    distance_from_extract int := greatest(coalesce((updated_payload->>'distanceFromExtract')::int, 0), 0);
    log_entries jsonb := coalesce(updated_payload->'logEntries', '[]'::jsonb);
    selected_entry game.encounter_table_entries%rowtype;
    selected_combat_table_key text := 'default_raid_travel';
    container_name text;
    discovered_loot jsonb;
    enemy_loadout jsonb;
    enemy_health int;
    enemy_dexterity int;
    enemy_constitution int;
    enemy_strength int;
begin
    updated_payload := jsonb_set(updated_payload, '{discoveredLoot}', '[]'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{awaitingDecision}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{extractionCombat}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{challenge}', to_jsonb(challenge), true);
    updated_payload := jsonb_set(updated_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);
    updated_payload := jsonb_set(updated_payload, '{contactState}', to_jsonb('None'::text), true);
    updated_payload := jsonb_set(updated_payload, '{surpriseSide}', to_jsonb('None'::text), true);
    updated_payload := jsonb_set(updated_payload, '{initiativeWinner}', to_jsonb('None'::text), true);
    updated_payload := jsonb_set(updated_payload, '{openingActionsRemaining}', to_jsonb(0), true);
    updated_payload := jsonb_set(updated_payload, '{surprisePersistenceEligible}', to_jsonb(false), true);

    if not moving_to_extract and distance_from_extract = 0 then
        if random() < 0.1 then
            distance_from_extract := distance_from_extract + 1;
            log_entries := game.raid_append_log(log_entries, 'You drifted one step away from extract.');
            updated_payload := jsonb_set(updated_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);
        else
            log_entries := game.raid_append_log(log_entries, 'Extraction point located.');
            updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Extraction'::text), true);
            updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Extraction')), true);
            updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb('You are near the extraction route.'::text), true);
            updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
            updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
            updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(0), true);
            updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(0), true);
            updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(0), true);
            updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
            updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
            updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
            return updated_payload;
        end if;
    elsif distance_from_extract = 0 then
        log_entries := game.raid_append_log(log_entries, 'Extraction point located.');
        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Extraction'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Extraction')), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb('You are near the extraction route.'::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
        updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
        return updated_payload;
    end if;

    with weighted_entries as (
        select
            entries.*,
            sum(entries.weight) over (order by entries.sort_order, entries.entry_key) as running_weight,
            sum(entries.weight) over () as total_weight
        from game.encounter_table_entries entries
        join game.encounter_tables tables
            on tables.table_key = entries.table_key
        where entries.table_key = 'default_raid_travel'
          and entries.enabled
          and tables.enabled
    ),
    target_roll as (
        select floor(random() * max(weighted_entries.total_weight))::int + 1 as target
        from weighted_entries
    )
    select weighted_entries.*
    into selected_entry
    from weighted_entries
    cross join target_roll
    where weighted_entries.running_weight >= target_roll.target
    order by weighted_entries.running_weight
    limit 1;

    if selected_entry.encounter_type = 'Combat' then
        if moving_to_extract then
            selected_combat_table_key := 'extract_approach';
        elsif coalesce(updated_payload->>'encounterType', 'Neutral') = 'Loot' then
            selected_combat_table_key := 'loot_interruption';
        else
            selected_combat_table_key := 'default_raid_travel';
        end if;

        with weighted_entries as (
            select
                entries.*,
                sum(entries.weight) over (order by entries.sort_order, entries.entry_key) as running_weight,
                sum(entries.weight) over () as total_weight
            from game.encounter_table_entries entries
            join game.encounter_tables tables
                on tables.table_key = entries.table_key
        where entries.table_key = selected_combat_table_key
          and entries.enabled
          and tables.enabled
          and challenge >= coalesce(entries.challenge_min, 0)
          and challenge < coalesce(entries.challenge_max_exclusive, 2147483647)
    ),
    target_roll as (
        select floor(random() * max(weighted_entries.total_weight))::int + 1 as target
        from weighted_entries
        )
        select weighted_entries.*
        into selected_entry
        from weighted_entries
        cross join target_roll
        where weighted_entries.running_weight >= target_roll.target
        order by weighted_entries.running_weight
        limit 1;
    end if;

    if selected_entry.encounter_type = 'Combat' then
        enemy_health := selected_entry.enemy_health_min
            + floor(random() * (selected_entry.enemy_health_max_exclusive - selected_entry.enemy_health_min))::int;
        enemy_loadout := game.random_enemy_loadout_from_table(coalesce(selected_entry.enemy_loadout_table_key, 'default_enemy_loadout'));
        enemy_dexterity := coalesce(selected_entry.enemy_dexterity, 10);
        enemy_constitution := coalesce(selected_entry.enemy_constitution, 10);
        enemy_strength := coalesce(selected_entry.enemy_strength, 10);
        log_entries := game.raid_append_log(log_entries, format('Combat started vs %s.', selected_entry.enemy_name));

        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Combat'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Combat'))), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'Enemy contact on your position.'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{contactState}', to_jsonb(coalesce(selected_entry.contact_state, 'MutualContact'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(coalesce(selected_entry.enemy_name, ''::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(enemy_health), true);
        updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(enemy_dexterity), true);
        updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(enemy_constitution), true);
        updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(enemy_strength), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', enemy_loadout, true);
        if selected_entry.contact_state = 'PlayerAmbush' then
            updated_payload := jsonb_set(updated_payload, '{surpriseSide}', to_jsonb('Player'::text), true);
            updated_payload := jsonb_set(updated_payload, '{initiativeWinner}', to_jsonb('None'::text), true);
            updated_payload := jsonb_set(updated_payload, '{openingActionsRemaining}', to_jsonb(1), true);
        elsif selected_entry.contact_state = 'EnemyAmbush' then
            updated_payload := jsonb_set(updated_payload, '{surpriseSide}', to_jsonb('Enemy'::text), true);
            updated_payload := jsonb_set(updated_payload, '{initiativeWinner}', to_jsonb('None'::text), true);
            updated_payload := jsonb_set(updated_payload, '{openingActionsRemaining}', to_jsonb(1), true);
        else
            updated_payload := jsonb_set(updated_payload, '{surpriseSide}', to_jsonb('None'::text), true);
            updated_payload := jsonb_set(updated_payload, '{initiativeWinner}', to_jsonb(case when random() < 0.5 then 'Player'::text else 'Enemy'::text end), true);
            updated_payload := jsonb_set(updated_payload, '{openingActionsRemaining}', to_jsonb(0), true);
        end if;
        updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
        return updated_payload;
    end if;

    if selected_entry.encounter_type = 'Loot' then
        select tables.source_name
        into container_name
        from game.loot_tables tables
        where tables.table_key = selected_entry.loot_table_key
          and tables.enabled
        limit 1;

        discovered_loot := game.random_loot_items_from_table(selected_entry.loot_table_key);
        log_entries := game.raid_append_log(
            log_entries,
            format('Found %s with %s lootable items.', container_name, jsonb_array_length(discovered_loot)));

        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Loot'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Loot'))), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'A searchable container appears.'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(coalesce(container_name, 'Filing Cabinet'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{discoveredLoot}', discovered_loot, true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
        updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
        return updated_payload;
    end if;

    log_entries := game.raid_append_log(log_entries, 'No enemies or loot found.');
    updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Neutral'::text), true);
    updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Neutral'))), true);
    updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'Area looks quiet. Nothing useful here.'::text)), true);
    updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
    updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
    updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(0), true);
    updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(0), true);
    updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(0), true);
    updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
    updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
    return updated_payload;
end;
$$;
