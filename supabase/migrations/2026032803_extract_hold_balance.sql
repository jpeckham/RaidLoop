insert into game.encounter_tables (table_key, name, enabled)
values
    ('extract_hold', 'Extract Hold Encounter Table', true)
on conflict (table_key) do update
set name = excluded.name,
    enabled = excluded.enabled;

insert into game.encounter_table_entries (
    entry_key,
    table_key,
    encounter_type,
    contact_state,
    weight,
    sort_order,
    enemy_name,
    enemy_health_min,
    enemy_health_max_exclusive,
    loot_table_key,
    enemy_loadout_table_key,
    title,
    description,
    challenge_min,
    challenge_max_exclusive,
    enabled
)
values
    ('extract_hold_quiet_window', 'extract_hold', 'Extraction', 'MutualContact', 120, 10, null, null, null, null, null, 'Extraction Opportunity', 'The extract lane stays quiet. You have a clean window to leave.', 0, 2147483647, true),
    ('extract_hold_false_alarm', 'extract_hold', 'Extraction', 'MutualContact', 80, 20, null, null, null, null, null, 'Extraction Opportunity', 'You hold position through a false alarm and keep the route covered.', 0, 2147483647, true),
    ('extract_hold_medical_cache', 'extract_hold', 'Loot', 'MutualContact', 24, 30, null, null, null, 'medical_container', null, 'Loot Encounter', 'A hasty stash appears near the extract route while you hold your angle.', 0, 2147483647, true),
    ('extract_hold_player_spots_camper', 'extract_hold', 'Combat', 'PlayerAmbush', 28, 40, 'Extract Camper', 12, 21, null, null, 'Combat Encounter', 'You spot movement near extract before the camper notices you.', 0, 2147483647, true),
    ('extract_hold_enemy_pushes_position', 'extract_hold', 'Combat', 'EnemyAmbush', 28, 50, 'Extract Hunter', 12, 21, null, null, 'Combat Encounter', 'A hunter collapses on your hold position from an unexpected angle.', 0, 2147483647, true),
    ('extract_hold_mutual_contact', 'extract_hold', 'Combat', 'MutualContact', 36, 60, 'Final Guard', 12, 21, null, null, 'Combat Encounter', 'You and another survivor catch each other on the extract lane at the same time.', 0, 2147483647, true)
on conflict (entry_key) do update
set table_key = excluded.table_key,
    encounter_type = excluded.encounter_type,
    contact_state = excluded.contact_state,
    weight = excluded.weight,
    sort_order = excluded.sort_order,
    enemy_name = excluded.enemy_name,
    enemy_health_min = excluded.enemy_health_min,
    enemy_health_max_exclusive = excluded.enemy_health_max_exclusive,
    loot_table_key = excluded.loot_table_key,
    enemy_loadout_table_key = excluded.enemy_loadout_table_key,
    title = excluded.title,
    description = excluded.description,
    challenge_min = excluded.challenge_min,
    challenge_max_exclusive = excluded.challenge_max_exclusive,
    enabled = excluded.enabled;

create or replace function game.clear_extract_hold_state(raid_payload jsonb)
returns jsonb
language sql
stable
as $$
    select jsonb_set(
        jsonb_set(coalesce(raid_payload, '{}'::jsonb), '{extractHoldActive}', 'false'::jsonb, true),
        '{holdAtExtractUntil}',
        'null'::jsonb,
        true);
$$;

create or replace function game.generate_extract_hold_encounter(raid_payload jsonb)
returns jsonb
language plpgsql
volatile
as $$
declare
    updated_payload jsonb := game.clear_extract_hold_state(coalesce(raid_payload, '{}'::jsonb));
    challenge int := greatest(coalesce((updated_payload->>'challenge')::int, 0), 0);
    log_entries jsonb := coalesce(updated_payload->'logEntries', '[]'::jsonb);
    selected_entry game.encounter_table_entries%rowtype;
    container_name text;
    discovered_loot jsonb;
    enemy_loadout jsonb;
    enemy_health int;
    enemy_stats jsonb;
begin
    updated_payload := jsonb_set(updated_payload, '{distanceFromExtract}', to_jsonb(0), true);
    updated_payload := jsonb_set(updated_payload, '{discoveredLoot}', '[]'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{awaitingDecision}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{extractionCombat}', 'false'::jsonb, true);

    with weighted_entries as (
        select
            entries.*,
            sum(entries.weight) over (order by entries.sort_order, entries.entry_key) as running_weight,
            sum(entries.weight) over () as total_weight
        from game.encounter_table_entries entries
        join game.encounter_tables tables
            on tables.table_key = entries.table_key
        where entries.table_key = 'extract_hold'
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

    if selected_entry.entry_key is null or selected_entry.encounter_type = 'Extraction' then
        log_entries := game.raid_append_log(log_entries, 'You finish holding at extract without drawing contact.');
        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Extraction'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Extraction'))), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'The route settles down and extraction is available.'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
        updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
        return updated_payload;
    elsif selected_entry.encounter_type = 'Loot' then
        select tables.source_name
        into container_name
        from game.loot_tables tables
        where tables.table_key = selected_entry.loot_table_key
          and tables.enabled
        limit 1;

        discovered_loot := game.random_loot_items_from_table(selected_entry.loot_table_key);
        log_entries := game.raid_append_log(log_entries, format('Found %s with %s lootable items.', container_name, jsonb_array_length(discovered_loot)));
        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Loot'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Loot'))), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'A searchable container appears.'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(coalesce(container_name, 'Medical Container'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{discoveredLoot}', discovered_loot, true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
        updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
        return updated_payload;
    end if;

    enemy_health := selected_entry.enemy_health_min
        + floor(random() * (selected_entry.enemy_health_max_exclusive - selected_entry.enemy_health_min))::int;
    enemy_loadout := game.random_enemy_loadout_from_table(coalesce(selected_entry.enemy_loadout_table_key, game.challenge_enemy_loadout_table(challenge)));
    enemy_stats := game.challenge_enemy_stats(challenge);
    log_entries := game.raid_append_log(log_entries, format('Combat started vs %s.', selected_entry.enemy_name));
    updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Combat'::text), true);
    updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Combat'))), true);
    updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'Enemy contact on your position.'::text)), true);
    updated_payload := jsonb_set(updated_payload, '{contactState}', to_jsonb(coalesce(selected_entry.contact_state, 'MutualContact'::text)), true);
    updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(coalesce(selected_entry.enemy_name, ''::text)), true);
    updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(enemy_health), true);
    updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(coalesce((enemy_stats->>'dexterity')::int, 10)), true);
    updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(coalesce((enemy_stats->>'constitution')::int, 10)), true);
    updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(coalesce((enemy_stats->>'strength')::int, 10)), true);
    updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
    updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', enemy_loadout, true);
    updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
    return updated_payload;
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
    resolved_stats jsonb := coalesce(accepted_stats, jsonb_build_object('strength', 8, 'dexterity', 8, 'constitution', 8, 'intelligence', 8, 'wisdom', 8, 'charisma', 8));
begin
    for entry in select value from jsonb_array_elements(coalesce(loadout, '[]'::jsonb)) loop
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
        'encumbranceTier', game.encumbrance_tier(coalesce((resolved_stats->>'strength')::int, 8), game.current_encumbrance(equipped_items || carried_loot, medkits)),
        'challenge', 0,
        'distanceFromExtract', 3,
        'acceptedStats', resolved_stats,
        'extractHoldActive', false,
        'holdAtExtractUntil', null,
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

create or replace function game.generate_raid_encounter(raid_payload jsonb, moving_to_extract boolean default false)
returns jsonb
language plpgsql
volatile
as $$
declare
    updated_payload jsonb := game.clear_extract_hold_state(coalesce(raid_payload, '{}'::jsonb));
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
        selected_loot_table_key := game.challenge_encounter_loot_table(selected_entry.entry_key, selected_entry.loot_table_key, challenge);

        select tables.source_name
        into container_name
        from game.loot_tables tables
        where tables.table_key = selected_loot_table_key
          and tables.enabled
        limit 1;

        discovered_loot := game.random_loot_items_from_table(selected_loot_table_key);
        log_entries := game.raid_append_log(log_entries, format('Found %s with %s lootable items.', container_name, jsonb_array_length(discovered_loot)));

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

create or replace function game.perform_extract_hold_action(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
-- perform_raid_action branches for start-extract-hold, resolve-extract-hold,
-- cancel-extract-hold, and guarded attempt-extract are routed here first.
declare
    save_payload jsonb;
    raid_payload jsonb;
    raid_profile text;
    log_entries jsonb;
    encounter_type text;
    distance_from_extract int;
    challenge int;
    extract_hold_active boolean;
    hold_at_extract_until text;
    requested_hold_at_extract_until text;
    hold_deadline timestamptz;
begin
    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));

    select raid_sessions.profile, raid_sessions.payload
    into raid_profile, raid_payload
    from public.raid_sessions
    where user_id = target_user_id;

    if raid_payload is null then
        return save_payload;
    end if;

    log_entries := coalesce(raid_payload->'logEntries', '[]'::jsonb);
    encounter_type := coalesce(raid_payload->>'encounterType', 'Neutral');
    distance_from_extract := greatest(coalesce((raid_payload->>'distanceFromExtract')::int, 0), 0);
    challenge := greatest(coalesce((raid_payload->>'challenge')::int, 0), 0);
    extract_hold_active := coalesce((raid_payload->>'extractHoldActive')::boolean, false);
    hold_at_extract_until := nullif(coalesce(raid_payload->>'holdAtExtractUntil', ''), '');
    requested_hold_at_extract_until := nullif(coalesce(payload->>'holdAtExtractUntil', ''), '');

    if action in ('stay-at-extract', 'start-extract-hold') then
        if encounter_type = 'Extraction' and distance_from_extract = 0 and not extract_hold_active then
            hold_at_extract_until := (timezone('utc', now()) + interval '30 seconds')::text;
            raid_payload := jsonb_set(raid_payload, '{extractHoldActive}', 'true'::jsonb, true);
            raid_payload := jsonb_set(raid_payload, '{holdAtExtractUntil}', to_jsonb(hold_at_extract_until), true);
            raid_payload := jsonb_set(raid_payload, '{encounterDescription}', to_jsonb('Holding at extract. Stay alert.'::text), true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You begin holding at extract.'), true);
        end if;
    elsif action = 'resolve-extract-hold' then
        if extract_hold_active and hold_at_extract_until is not null then
            if requested_hold_at_extract_until is not null and requested_hold_at_extract_until <> hold_at_extract_until then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Hold resolution ignored because the request is stale.'), true);
            else
                hold_deadline := hold_at_extract_until::timestamptz;
                if timezone('utc', now()) < hold_deadline then
                    raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Hold is still in progress.'), true);
                else
                    raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge + 1), true);
                    raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You finish holding at extract.'), true);
                    raid_payload := game.generate_extract_hold_encounter(raid_payload);
                end if;
            end if;
        end if;
    elsif action = 'cancel-extract-hold' then
        if extract_hold_active then
            raid_payload := game.clear_extract_hold_state(raid_payload);
            raid_payload := jsonb_set(raid_payload, '{encounterType}', to_jsonb('Extraction'::text), true);
            raid_payload := jsonb_set(raid_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Extraction')), true);
            raid_payload := jsonb_set(raid_payload, '{encounterDescription}', to_jsonb('You are near the extraction route.'::text), true);
            raid_payload := jsonb_set(raid_payload, '{enemyName}', to_jsonb(''::text), true);
            raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(0), true);
            raid_payload := jsonb_set(raid_payload, '{enemyDexterity}', to_jsonb(0), true);
            raid_payload := jsonb_set(raid_payload, '{enemyConstitution}', to_jsonb(0), true);
            raid_payload := jsonb_set(raid_payload, '{enemyStrength}', to_jsonb(0), true);
            raid_payload := jsonb_set(raid_payload, '{lootContainer}', to_jsonb(''::text), true);
            raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', '[]'::jsonb, true);
            raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', '[]'::jsonb, true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You stop holding at extract.'), true);
        end if;
    elsif action = 'attempt-extract' then
        if extract_hold_active then
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Resolve or cancel the extract hold before extracting.'), true);
        elsif encounter_type = 'Extraction' and distance_from_extract = 0 then
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Extraction completed. Loot secured.'), true);
            return game.finish_raid_session(save_payload, raid_payload, raid_profile, true, target_user_id);
        else
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Extraction is not available right now.'), true);
        end if;
    else
        raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Cancel the extract hold before taking another extract action.'), true);
    end if;

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
    extract_hold_active boolean;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    raid_payload := coalesce(save_payload->'activeRaid', 'null'::jsonb);

    if raid_payload is null then
        return game.perform_raid_action(action, payload, target_user_id);
    end if;

    extract_hold_active := coalesce((raid_payload->>'extractHoldActive')::boolean, false);

    if action in ('stay-at-extract', 'start-extract-hold', 'resolve-extract-hold', 'cancel-extract-hold', 'attempt-extract')
        or (extract_hold_active and action in ('go-deeper', 'move-toward-extract')) then
        return game.perform_extract_hold_action(action, payload, target_user_id);
    end if;

    player_stats := coalesce(raid_payload->'acceptedStats', save_payload->'acceptedStats', jsonb_build_object('strength', 8, 'dexterity', 8, 'constitution', 8, 'intelligence', 8, 'wisdom', 8, 'charisma', 8));
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
                prospective_encumbrance := current_encumbrance + game.item_weight(coalesce(selected_item->>'name', item_name)) - coalesce(game.item_weight(previous_item->>'name'), 0);
            else
                prospective_encumbrance := current_encumbrance - coalesce(game.item_weight(previous_item->>'name'), 0);
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
            'start-extract-hold',
            'resolve-extract-hold',
            'cancel-extract-hold',
            'attempt-extract')
            then game.perform_raid_action_with_encumbrance(action, payload, auth.uid())
        else game.apply_profile_action(action, payload, auth.uid())
    end;
$$;

revoke all on function game.clear_extract_hold_state(jsonb) from public;
revoke all on function game.generate_extract_hold_encounter(jsonb) from public;
revoke all on function game.perform_extract_hold_action(text, jsonb, uuid) from public;
revoke all on function game.build_raid_snapshot(jsonb, text, int, jsonb) from public;
revoke all on function game.generate_raid_encounter(jsonb, boolean) from public;
revoke all on function game.perform_raid_action_with_encumbrance(text, jsonb, uuid) from public;
revoke all on function public.game_action(text, jsonb) from public;
