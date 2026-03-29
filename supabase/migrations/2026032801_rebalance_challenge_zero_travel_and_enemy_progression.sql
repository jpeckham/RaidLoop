alter table game.encounter_table_entries
    add column if not exists challenge_min int not null default 0,
    add column if not exists challenge_max_exclusive int not null default 2147483647,
    add column if not exists enemy_dexterity int not null default 10,
    add column if not exists enemy_constitution int not null default 10,
    add column if not exists enemy_strength int not null default 10;

insert into game.enemy_loadout_tables (table_key, name, enabled)
values
    ('challenge_0_enemy_loadout', 'Challenge 0 Enemy Loadouts', true),
    ('challenge_1_enemy_loadout', 'Challenge 1 Enemy Loadouts', true),
    ('challenge_2_enemy_loadout', 'Challenge 2 Enemy Loadouts', true),
    ('challenge_3_enemy_loadout', 'Challenge 3 Enemy Loadouts', true),
    ('challenge_4_enemy_loadout', 'Challenge 4 Enemy Loadouts', true),
    ('challenge_5_enemy_loadout', 'Challenge 5 Enemy Loadouts', true)
on conflict (table_key) do update
set name = excluded.name,
    enabled = excluded.enabled;

delete from game.enemy_loadout_variants
where table_key in (
    'challenge_0_enemy_loadout',
    'challenge_1_enemy_loadout',
    'challenge_2_enemy_loadout',
    'challenge_3_enemy_loadout',
    'challenge_4_enemy_loadout',
    'challenge_5_enemy_loadout'
);

insert into game.enemy_loadout_variants (variant_key, table_key, weight, sort_order, enabled)
values
    ('challenge0_light_pistol', 'challenge_0_enemy_loadout', 140, 10, true),
    ('challenge0_light_pistol_bandage', 'challenge_0_enemy_loadout', 60, 20, true),
    ('challenge1_light_pistol_6b2', 'challenge_1_enemy_loadout', 120, 10, true),
    ('challenge1_light_pistol_bandage_6b2', 'challenge_1_enemy_loadout', 80, 20, true),
    ('challenge2_drum_smg', 'challenge_2_enemy_loadout', 120, 10, true),
    ('challenge2_drum_smg_kirasa', 'challenge_2_enemy_loadout', 80, 20, true),
    ('challenge3_field_carbine', 'challenge_3_enemy_loadout', 110, 10, true),
    ('challenge3_battle_rifle_6b13', 'challenge_3_enemy_loadout', 90, 20, true),
    ('challenge4_marksman_rifle', 'challenge_4_enemy_loadout', 100, 10, true),
    ('challenge4_marksman_rifle_fort', 'challenge_4_enemy_loadout', 100, 20, true),
    ('challenge5_support_machine_gun', 'challenge_5_enemy_loadout', 100, 10, true),
    ('challenge5_support_machine_gun_thor', 'challenge_5_enemy_loadout', 100, 20, true)
on conflict (variant_key) do update
set table_key = excluded.table_key,
    weight = excluded.weight,
    sort_order = excluded.sort_order,
    enabled = excluded.enabled;

insert into game.enemy_loadout_variant_items (variant_key, item_key, item_order)
values
    ('challenge0_light_pistol', 'light_pistol', 10),
    ('challenge0_light_pistol_bandage', 'light_pistol', 10),
    ('challenge0_light_pistol_bandage', 'bandage', 20),
    ('challenge1_light_pistol_6b2', 'light_pistol', 10),
    ('challenge1_light_pistol_6b2', 'soft_armor_vest', 20),
    ('challenge1_light_pistol_bandage_6b2', 'light_pistol', 10),
    ('challenge1_light_pistol_bandage_6b2', 'bandage', 20),
    ('challenge1_light_pistol_bandage_6b2', 'soft_armor_vest', 30),
    ('challenge2_drum_smg', 'drum_smg', 10),
    ('challenge2_drum_smg_kirasa', 'drum_smg', 10),
    ('challenge2_drum_smg_kirasa', 'reinforced_vest', 20),
    ('challenge3_field_carbine', 'field_carbine', 10),
    ('challenge3_battle_rifle_6b13', 'battle_rifle', 10),
    ('challenge3_battle_rifle_6b13', 'light_plate_carrier', 20),
    ('challenge4_marksman_rifle', 'marksman_rifle', 10),
    ('challenge4_marksman_rifle_fort', 'marksman_rifle', 10),
    ('challenge4_marksman_rifle_fort', 'medium_plate_carrier', 20),
    ('challenge5_support_machine_gun', 'support_machine_gun', 10),
    ('challenge5_support_machine_gun_thor', 'support_machine_gun', 10),
    ('challenge5_support_machine_gun_thor', 'assault_plate_carrier', 20);

insert into game.loot_tables (table_key, source_name, derived_from_enemy_loadout, enabled)
values
    ('challenge_0_travel_cache', 'Challenge 0 Travel Cache', false, true),
    ('challenge_0_extract_cache', 'Challenge 0 Extract Cache', false, true)
on conflict (table_key) do update
set source_name = excluded.source_name,
    derived_from_enemy_loadout = excluded.derived_from_enemy_loadout,
    enabled = excluded.enabled;

delete from game.loot_table_variants
where variant_key in (
    'challenge0_travel_bandage_ammo',
    'challenge0_travel_medkit',
    'challenge0_travel_scrap',
    'challenge0_travel_small_backpack',
    'challenge0_travel_large_backpack',
    'challenge0_travel_light_pistol',
    'challenge0_extract_bandage_ammo',
    'challenge0_extract_medkit',
    'challenge0_extract_scrap',
    'challenge0_extract_small_backpack',
    'challenge0_extract_large_backpack',
    'challenge0_extract_light_pistol'
);

insert into game.loot_table_variants (variant_key, table_key, weight, sort_order, enabled)
values
    ('challenge0_travel_bandage_ammo', 'challenge_0_travel_cache', 60, 10, true),
    ('challenge0_travel_medkit', 'challenge_0_travel_cache', 40, 20, true),
    ('challenge0_travel_scrap', 'challenge_0_travel_cache', 45, 30, true),
    ('challenge0_travel_small_backpack', 'challenge_0_travel_cache', 18, 40, true),
    ('challenge0_travel_large_backpack', 'challenge_0_travel_cache', 12, 50, true),
    ('challenge0_travel_light_pistol', 'challenge_0_travel_cache', 25, 60, true),
    ('challenge0_extract_bandage_ammo', 'challenge_0_extract_cache', 55, 10, true),
    ('challenge0_extract_medkit', 'challenge_0_extract_cache', 45, 20, true),
    ('challenge0_extract_scrap', 'challenge_0_extract_cache', 35, 30, true),
    ('challenge0_extract_small_backpack', 'challenge_0_extract_cache', 20, 40, true),
    ('challenge0_extract_large_backpack', 'challenge_0_extract_cache', 15, 50, true),
    ('challenge0_extract_light_pistol', 'challenge_0_extract_cache', 20, 60, true)
on conflict (variant_key) do update
set table_key = excluded.table_key,
    weight = excluded.weight,
    sort_order = excluded.sort_order,
    enabled = excluded.enabled;

delete from game.loot_table_variant_items
where variant_key in (
    'challenge0_travel_bandage_ammo',
    'challenge0_travel_medkit',
    'challenge0_travel_scrap',
    'challenge0_travel_small_backpack',
    'challenge0_travel_large_backpack',
    'challenge0_travel_light_pistol',
    'challenge0_extract_bandage_ammo',
    'challenge0_extract_medkit',
    'challenge0_extract_scrap',
    'challenge0_extract_small_backpack',
    'challenge0_extract_large_backpack',
    'challenge0_extract_light_pistol'
);

insert into game.loot_table_variant_items (variant_key, item_key, item_order)
values
    ('challenge0_travel_bandage_ammo', 'bandage', 10),
    ('challenge0_travel_bandage_ammo', 'ammo_box', 20),
    ('challenge0_travel_medkit', 'medkit', 10),
    ('challenge0_travel_scrap', 'scrap_metal', 10),
    ('challenge0_travel_small_backpack', 'small_backpack', 10),
    ('challenge0_travel_large_backpack', 'large_backpack', 10),
    ('challenge0_travel_light_pistol', 'light_pistol', 10),
    ('challenge0_extract_bandage_ammo', 'bandage', 10),
    ('challenge0_extract_bandage_ammo', 'ammo_box', 20),
    ('challenge0_extract_medkit', 'medkit', 10),
    ('challenge0_extract_scrap', 'scrap_metal', 10),
    ('challenge0_extract_small_backpack', 'small_backpack', 10),
    ('challenge0_extract_large_backpack', 'large_backpack', 10),
    ('challenge0_extract_light_pistol', 'light_pistol', 10);

create or replace function game.challenge_enemy_loadout_table(challenge int)
returns text
language sql
stable
as $$
    select case greatest(coalesce(challenge, 0), 0)
        when 0 then 'challenge_0_enemy_loadout'
        when 1 then 'challenge_1_enemy_loadout'
        when 2 then 'challenge_2_enemy_loadout'
        when 3 then 'challenge_3_enemy_loadout'
        when 4 then 'challenge_4_enemy_loadout'
        else 'challenge_5_enemy_loadout'
    end;
$$;

create or replace function game.challenge_enemy_stats(challenge int)
returns jsonb
language sql
stable
as $$
    select case greatest(coalesce(challenge, 0), 0)
        when 0 then jsonb_build_object('strength', 10, 'dexterity', 13, 'constitution', 12, 'intelligence', 8, 'wisdom', 14, 'charisma', 15)
        when 1 then jsonb_build_object('strength', 12, 'dexterity', 14, 'constitution', 12, 'intelligence', 10, 'wisdom', 11, 'charisma', 14)
        when 2 then jsonb_build_object('strength', 12, 'dexterity', 14, 'constitution', 14, 'intelligence', 10, 'wisdom', 10, 'charisma', 13)
        when 3 then jsonb_build_object('strength', 13, 'dexterity', 14, 'constitution', 14, 'intelligence', 10, 'wisdom', 9, 'charisma', 13)
        when 4 then jsonb_build_object('strength', 13, 'dexterity', 15, 'constitution', 14, 'intelligence', 10, 'wisdom', 8, 'charisma', 12)
        else jsonb_build_object('strength', 14, 'dexterity', 15, 'constitution', 13, 'intelligence', 10, 'wisdom', 9, 'charisma', 11)
    end;
$$;

create or replace function game.challenge_encounter_loot_table(entry_key text, loot_table_key text, challenge int)
returns text
language sql
stable
as $$
    select case
        when greatest(coalesce(challenge, 0), 0) = 0 and entry_key = 'raid_loot_travel_filing_cache' then 'challenge_0_travel_cache'
        when greatest(coalesce(challenge, 0), 0) = 0 and entry_key = 'raid_loot_extract_abandoned_cache' then 'challenge_0_extract_cache'
        else loot_table_key
    end;
$$;

update game.encounter_table_entries
set weight = case entry_key
        when 'raid_combat_travel_player_spots_camp' then 60
        when 'raid_combat_travel_enemy_ambush' then 60
        when 'raid_combat_travel_mutual_contact' then 60
        when 'raid_neutral_travel_area_clear' then 80
        when 'raid_loot_travel_filing_cache' then 140
        when 'raid_combat_extract_player_spots_guard' then 70
        when 'raid_combat_extract_enemy_ambush' then 70
        when 'raid_combat_extract_mutual_contact' then 70
        when 'raid_neutral_extract_route_clear' then 80
        when 'raid_loot_extract_abandoned_cache' then 120
        else weight
    end,
    challenge_min = 0,
    challenge_max_exclusive = 2147483647
where entry_key in (
    'raid_combat_travel_player_spots_camp',
    'raid_combat_travel_enemy_ambush',
    'raid_combat_travel_mutual_contact',
    'raid_neutral_travel_area_clear',
    'raid_loot_travel_filing_cache',
    'raid_combat_extract_player_spots_guard',
    'raid_combat_extract_enemy_ambush',
    'raid_combat_extract_mutual_contact',
    'raid_neutral_extract_route_clear',
    'raid_loot_extract_abandoned_cache'
);

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
    selected_loot_table_key text;
    container_name text;
    discovered_loot jsonb;
    enemy_loadout jsonb;
    enemy_health int;
    enemy_stats jsonb;
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
        enemy_loadout := game.random_enemy_loadout_from_table(game.challenge_enemy_loadout_table(challenge));
        enemy_stats := game.challenge_enemy_stats(challenge);
        enemy_dexterity := coalesce((enemy_stats->>'dexterity')::int, 10);
        enemy_constitution := coalesce((enemy_stats->>'constitution')::int, 10);
        enemy_strength := coalesce((enemy_stats->>'strength')::int, 10);
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
        selected_loot_table_key := game.challenge_encounter_loot_table(
            selected_entry.entry_key,
            selected_entry.loot_table_key,
            challenge);

        select tables.source_name
        into container_name
        from game.loot_tables tables
        where tables.table_key = selected_loot_table_key
          and tables.enabled
        limit 1;

        discovered_loot := game.random_loot_items_from_table(selected_loot_table_key);
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
