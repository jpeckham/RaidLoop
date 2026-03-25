alter table game.encounter_table_entries
    add column if not exists contact_state text not null default 'MutualContact';

do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where conname = 'encounter_table_entries_contact_state_check'
          and conrelid = 'game.encounter_table_entries'::regclass
    ) then
        alter table game.encounter_table_entries
            add constraint encounter_table_entries_contact_state_check
            check (contact_state in ('PlayerAmbush', 'EnemyAmbush', 'MutualContact'));
    end if;
end;
$$;

update game.encounter_table_entries
set contact_state = 'MutualContact'
where encounter_type = 'Combat';

insert into game.encounter_tables (table_key, name, enabled)
values
    ('default_raid_travel', 'Default Raid Travel Encounter Table', true),
    ('loot_interruption', 'Loot Interruption Encounter Table', true),
    ('extract_approach', 'Extract Approach Encounter Table', true)
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
    enabled
)
values
    ('raid_combat_travel_player_spots_camp', 'default_raid_travel', 'Combat', 'PlayerAmbush', 33, 10, 'Scav', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You spot an enemy camp before they see you.', true),
    ('raid_combat_travel_enemy_ambush', 'default_raid_travel', 'Combat', 'EnemyAmbush', 33, 20, 'Scav', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You are ambushed while moving through the raid.', true),
    ('raid_combat_travel_mutual_contact', 'default_raid_travel', 'Combat', 'MutualContact', 34, 30, 'Patrol Guard', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You and a patrol notice each other at nearly the same moment.', true),
    ('raid_combat_loot_player_hears_movement', 'loot_interruption', 'Combat', 'PlayerAmbush', 33, 10, 'Scav', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You hear movement while looting and catch them before they spot you.', true),
    ('raid_combat_loot_enemy_pushes_camp', 'loot_interruption', 'Combat', 'EnemyAmbush', 33, 20, 'Patrol Guard', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You are ambushed while looting.', true),
    ('raid_combat_loot_mutual_contact', 'loot_interruption', 'Combat', 'MutualContact', 34, 30, 'Scav', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You and a scav team spot each other at the container at the same time.', true),
    ('raid_combat_extract_player_spots_guard', 'extract_approach', 'Combat', 'PlayerAmbush', 33, 10, 'Final Guard', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You spot an enemy camp near extract before they notice you.', true),
    ('raid_combat_extract_enemy_ambush', 'extract_approach', 'Combat', 'EnemyAmbush', 33, 20, 'Final Guard', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You are ambushed while pushing toward extract.', true),
    ('raid_combat_extract_mutual_contact', 'extract_approach', 'Combat', 'MutualContact', 34, 30, 'Final Guard', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You and a guard on the extraction route notice each other at the same time.', true)
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
    enabled = excluded.enabled;

create or replace function game.generate_raid_encounter(raid_payload jsonb, moving_to_extract boolean default false)
returns jsonb
language plpgsql
volatile
as $$
declare
    updated_payload jsonb := coalesce(raid_payload, '{}'::jsonb);
    extract_progress int := greatest(coalesce((updated_payload->>'extractProgress')::int, 0), 0);
    extract_required int := greatest(coalesce((updated_payload->>'extractRequired')::int, 3), 1);
    log_entries jsonb := coalesce(updated_payload->'logEntries', '[]'::jsonb);
    selected_entry game.encounter_table_entries%rowtype;
    selected_combat_table_key text := 'default_raid_travel';
    container_name text;
    discovered_loot jsonb;
    enemy_loadout jsonb;
    enemy_health int;
begin
    if moving_to_extract then
        extract_progress := extract_progress + 1;
    end if;

    updated_payload := jsonb_set(updated_payload, '{discoveredLoot}', '[]'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{awaitingDecision}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{extractionCombat}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{extractProgress}', to_jsonb(extract_progress), true);
    updated_payload := jsonb_set(updated_payload, '{contactState}', to_jsonb('None'::text), true);
    updated_payload := jsonb_set(updated_payload, '{surpriseSide}', to_jsonb('None'::text), true);
    updated_payload := jsonb_set(updated_payload, '{initiativeWinner}', to_jsonb('None'::text), true);
    updated_payload := jsonb_set(updated_payload, '{openingActionsRemaining}', to_jsonb(0), true);
    updated_payload := jsonb_set(updated_payload, '{surprisePersistenceEligible}', to_jsonb(false), true);

    if extract_progress >= extract_required then
        with weighted_entries as (
            select
                entries.*,
                sum(entries.weight) over (order by entries.sort_order, entries.entry_key) as running_weight,
                sum(entries.weight) over () as total_weight
            from game.encounter_table_entries entries
            join game.encounter_tables tables
                on tables.table_key = entries.table_key
            where entries.table_key = 'extraction_check'
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

        if selected_entry.encounter_type = 'Extraction' then
            log_entries := game.raid_append_log(log_entries, 'Extraction point located.');
            updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Extraction'::text), true);
            updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Extraction'))), true);
            updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'You are near the extraction route.'::text)), true);
            updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
            updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
            updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
            updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
            updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
            return updated_payload;
        end if;
    end if;

    with weighted_entries as (
        select
            entries.*,
            sum(entries.weight) over (order by entries.sort_order, entries.entry_key) as running_weight,
            sum(entries.weight) over () as total_weight
        from game.encounter_table_entries entries
        join game.encounter_tables tables
            on tables.table_key = entries.table_key
        where entries.table_key = 'default_raid'
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
        log_entries := game.raid_append_log(log_entries, format('Combat started vs %s.', selected_entry.enemy_name));

        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Combat'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Combat'))), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'Enemy contact on your position.'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{contactState}', to_jsonb(coalesce(selected_entry.contact_state, 'MutualContact'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(coalesce(selected_entry.enemy_name, ''::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(enemy_health), true);
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
    updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
    updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
    return updated_payload;
end;
$$;
