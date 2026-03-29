create table if not exists game.enemy_loadout_tables (
    table_key text primary key,
    name text not null unique,
    enabled boolean not null default true
);

create table if not exists game.enemy_loadout_variants (
    variant_key text primary key,
    table_key text not null references game.enemy_loadout_tables(table_key) on delete cascade,
    weight int not null,
    sort_order int not null default 0,
    enabled boolean not null default true,
    constraint enemy_loadout_variants_weight_check check (weight > 0)
);

create table if not exists game.enemy_loadout_variant_items (
    variant_key text not null references game.enemy_loadout_variants(variant_key) on delete cascade,
    item_key text not null references game.item_defs(item_key),
    item_order int not null default 0,
    primary key (variant_key, item_key, item_order)
);

create table if not exists game.loot_tables (
    table_key text primary key,
    source_name text not null unique,
    derived_from_enemy_loadout boolean not null default false,
    enabled boolean not null default true
);

create table if not exists game.loot_table_variants (
    variant_key text primary key,
    table_key text not null references game.loot_tables(table_key) on delete cascade,
    weight int not null,
    sort_order int not null default 0,
    enabled boolean not null default true,
    constraint loot_table_variants_weight_check check (weight > 0)
);

create table if not exists game.loot_table_variant_items (
    variant_key text not null references game.loot_table_variants(variant_key) on delete cascade,
    item_key text not null references game.item_defs(item_key),
    item_order int not null default 0,
    primary key (variant_key, item_key, item_order)
);

create table if not exists game.encounter_tables (
    table_key text primary key,
    name text not null unique,
    enabled boolean not null default true
);

create table if not exists game.encounter_table_entries (
    entry_key text primary key,
    table_key text not null references game.encounter_tables(table_key) on delete cascade,
    encounter_type text not null,
    weight int not null,
    sort_order int not null default 0,
    enemy_name text,
    enemy_health_min int,
    enemy_health_max_exclusive int,
    loot_table_key text,
    enemy_loadout_table_key text,
    title text,
    description text,
    enabled boolean not null default true,
    constraint encounter_table_entries_weight_check check (weight > 0),
    constraint encounter_table_entries_enemy_health_bounds_check check (
        enemy_health_min is null
        or enemy_health_max_exclusive is null
        or enemy_health_max_exclusive > enemy_health_min
    ),
    constraint encounter_table_entries_loot_table_fkey foreign key (loot_table_key) references game.loot_tables(table_key),
    constraint encounter_table_entries_enemy_loadout_table_fkey foreign key (enemy_loadout_table_key) references game.enemy_loadout_tables(table_key)
);

insert into game.enemy_loadout_tables (table_key, name, enabled)
values
    ('default_enemy_loadout', 'Default Enemy Loadouts', true)
on conflict (table_key) do update
set name = excluded.name,
    enabled = excluded.enabled;

insert into game.enemy_loadout_variants (variant_key, table_key, weight, sort_order, enabled)
values
    ('enemy_light_pistol', 'default_enemy_loadout', 110, 10, true),
    ('enemy_bandage_6b2', 'default_enemy_loadout', 110, 20, true),
    ('enemy_6b2_only', 'default_enemy_loadout', 110, 30, true),
    ('enemy_drum_smg_bandage', 'default_enemy_loadout', 60, 40, true),
    ('enemy_field_carbine_6b2', 'default_enemy_loadout', 6, 50, true),
    ('enemy_battle_rifle_bandage', 'default_enemy_loadout', 6, 60, true),
    ('enemy_6b13_only', 'default_enemy_loadout', 6, 70, true),
    ('enemy_marksman_rifle', 'default_enemy_loadout', 3, 80, true),
    ('enemy_fort_defender', 'default_enemy_loadout', 3, 90, true),
    ('enemy_support_machine_gun', 'default_enemy_loadout', 3, 100, true),
    ('enemy_assault_plate_carrier', 'default_enemy_loadout', 3, 110, true)
on conflict (variant_key) do update
set table_key = excluded.table_key,
    weight = excluded.weight,
    sort_order = excluded.sort_order,
    enabled = excluded.enabled;

delete from game.enemy_loadout_variant_items
where variant_key in (
    'enemy_light_pistol',
    'enemy_bandage_6b2',
    'enemy_6b2_only',
    'enemy_drum_smg_bandage',
    'enemy_field_carbine_6b2',
    'enemy_battle_rifle_bandage',
    'enemy_6b13_only',
    'enemy_marksman_rifle',
    'enemy_fort_defender',
    'enemy_support_machine_gun',
    'enemy_assault_plate_carrier'
);

insert into game.enemy_loadout_variant_items (variant_key, item_key, item_order)
values
    ('enemy_light_pistol', 'light_pistol', 10),
    ('enemy_bandage_6b2', 'bandage', 10),
    ('enemy_bandage_6b2', 'soft_armor_vest', 20),
    ('enemy_6b2_only', 'soft_armor_vest', 10),
    ('enemy_drum_smg_bandage', 'drum_smg', 10),
    ('enemy_drum_smg_bandage', 'bandage', 20),
    ('enemy_field_carbine_6b2', 'field_carbine', 10),
    ('enemy_field_carbine_6b2', 'soft_armor_vest', 20),
    ('enemy_battle_rifle_bandage', 'battle_rifle', 10),
    ('enemy_battle_rifle_bandage', 'bandage', 20),
    ('enemy_6b13_only', 'light_plate_carrier', 10),
    ('enemy_marksman_rifle', 'marksman_rifle', 10),
    ('enemy_fort_defender', 'medium_plate_carrier', 10),
    ('enemy_support_machine_gun', 'support_machine_gun', 10),
    ('enemy_assault_plate_carrier', 'assault_plate_carrier', 10);

insert into game.loot_tables (table_key, source_name, derived_from_enemy_loadout, enabled)
values
    ('filing_cabinet', 'Filing Cabinet', false, true),
    ('weapons_crate', 'Weapons Crate', false, true),
    ('medical_container', 'Medical Container', false, true),
    ('dead_body', 'Dead Body', true, true)
on conflict (table_key) do update
set source_name = excluded.source_name,
    derived_from_enemy_loadout = excluded.derived_from_enemy_loadout,
    enabled = excluded.enabled;

insert into game.loot_table_variants (variant_key, table_key, weight, sort_order, enabled)
values
    ('filing_bandage_ammo', 'filing_cabinet', 40, 10, true),
    ('filing_scrap_metal', 'filing_cabinet', 40, 20, true),
    ('filing_medkit', 'filing_cabinet', 40, 30, true),
    ('filing_drum_smg', 'filing_cabinet', 36, 40, true),
    ('filing_rare_scope', 'filing_cabinet', 9, 50, true),
    ('filing_field_carbine', 'filing_cabinet', 9, 60, true),
    ('filing_marksman_rifle', 'filing_cabinet', 9, 70, true),
    ('filing_legendary_trigger', 'filing_cabinet', 6, 80, true),
    ('weapons_light_pistol_ammo', 'weapons_crate', 40, 10, true),
    ('weapons_drum_smg', 'weapons_crate', 12, 20, true),
    ('weapons_field_carbine', 'weapons_crate', 3, 30, true),
    ('weapons_battle_rifle', 'weapons_crate', 3, 40, true),
    ('weapons_marksman_rifle', 'weapons_crate', 3, 50, true),
    ('weapons_support_machine_gun', 'weapons_crate', 2, 60, true),
    ('medical_medkit_bandage', 'medical_container', 4, 10, true),
    ('medical_bandage_ammo', 'medical_container', 3, 20, true),
    ('medical_medkit', 'medical_container', 2, 30, true),
    ('medical_medkit_ammo', 'medical_container', 1, 40, true)
on conflict (variant_key) do update
set table_key = excluded.table_key,
    weight = excluded.weight,
    sort_order = excluded.sort_order,
    enabled = excluded.enabled;

delete from game.loot_table_variant_items
where variant_key in (
    'filing_bandage_ammo',
    'filing_scrap_metal',
    'filing_medkit',
    'filing_drum_smg',
    'filing_rare_scope',
    'filing_field_carbine',
    'filing_marksman_rifle',
    'filing_legendary_trigger',
    'weapons_light_pistol_ammo',
    'weapons_drum_smg',
    'weapons_field_carbine',
    'weapons_battle_rifle',
    'weapons_marksman_rifle',
    'weapons_support_machine_gun',
    'medical_medkit_bandage',
    'medical_bandage_ammo',
    'medical_medkit',
    'medical_medkit_ammo'
);

insert into game.loot_table_variant_items (variant_key, item_key, item_order)
values
    ('filing_bandage_ammo', 'bandage', 10),
    ('filing_bandage_ammo', 'ammo_box', 20),
    ('filing_scrap_metal', 'scrap_metal', 10),
    ('filing_medkit', 'medkit', 10),
    ('filing_drum_smg', 'drum_smg', 10),
    ('filing_rare_scope', 'rare_scope', 10),
    ('filing_field_carbine', 'field_carbine', 10),
    ('filing_marksman_rifle', 'marksman_rifle', 10),
    ('filing_legendary_trigger', 'legendary_trigger_group', 10),
    ('weapons_light_pistol_ammo', 'light_pistol', 10),
    ('weapons_light_pistol_ammo', 'ammo_box', 20),
    ('weapons_drum_smg', 'drum_smg', 10),
    ('weapons_field_carbine', 'field_carbine', 10),
    ('weapons_battle_rifle', 'battle_rifle', 10),
    ('weapons_marksman_rifle', 'marksman_rifle', 10),
    ('weapons_support_machine_gun', 'support_machine_gun', 10),
    ('medical_medkit_bandage', 'medkit', 10),
    ('medical_medkit_bandage', 'bandage', 20),
    ('medical_bandage_ammo', 'bandage', 10),
    ('medical_bandage_ammo', 'ammo_box', 20),
    ('medical_medkit', 'medkit', 10),
    ('medical_medkit_ammo', 'medkit', 10),
    ('medical_medkit_ammo', 'ammo_box', 20);

insert into game.encounter_tables (table_key, name, enabled)
values
    ('default_raid', 'Default Raid Encounter Table', true),
    ('extraction_check', 'Extraction Check Encounter Table', true)
on conflict (table_key) do update
set name = excluded.name,
    enabled = excluded.enabled;

insert into game.encounter_table_entries (
    entry_key,
    table_key,
    encounter_type,
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
    ('raid_combat_scav', 'default_raid', 'Combat', 650, 10, 'Scavenger', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'Enemy contact on your position.', true),
    ('raid_combat_patrol_guard', 'default_raid', 'Combat', 350, 20, 'Patrol Guard', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'Enemy contact on your position.', true),
    ('raid_loot_filing_cabinet', 'default_raid', 'Loot', 150, 30, null, null, null, 'filing_cabinet', null, 'Loot Encounter', 'A searchable container appears.', true),
    ('raid_loot_weapons_crate', 'default_raid', 'Loot', 150, 40, null, null, null, 'weapons_crate', null, 'Loot Encounter', 'A searchable container appears.', true),
    ('raid_loot_medical_container', 'default_raid', 'Loot', 150, 50, null, null, null, 'medical_container', null, 'Loot Encounter', 'A searchable container appears.', true),
    ('raid_loot_dead_body', 'default_raid', 'Loot', 150, 60, null, null, null, 'dead_body', null, 'Loot Encounter', 'A searchable container appears.', true),
    ('raid_neutral', 'default_raid', 'Neutral', 400, 70, null, null, null, null, null, 'Area Clear', 'Area looks quiet. Nothing useful here.', true),
    ('extraction_found', 'extraction_check', 'Extraction', 35, 10, null, null, null, null, null, 'Extraction Opportunity', 'You are near the extraction route.', true),
    ('extraction_continue', 'extraction_check', 'Continue', 65, 20, null, null, null, null, null, null, null, true)
on conflict (entry_key) do update
set table_key = excluded.table_key,
    encounter_type = excluded.encounter_type,
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

revoke all on table game.enemy_loadout_tables from public;
revoke all on table game.enemy_loadout_variants from public;
revoke all on table game.enemy_loadout_variant_items from public;
revoke all on table game.loot_tables from public;
revoke all on table game.loot_table_variants from public;
revoke all on table game.loot_table_variant_items from public;
revoke all on table game.encounter_tables from public;
revoke all on table game.encounter_table_entries from public;

create or replace function game.random_enemy_loadout_from_table(loadout_table_key text)
returns jsonb
language plpgsql
volatile
as $$
declare
    selected_variant_key text;
begin
    with weighted_variants as (
        select
            variants.variant_key,
            sum(variants.weight) over (order by variants.sort_order, variants.variant_key) as running_weight,
            sum(variants.weight) over () as total_weight
        from game.enemy_loadout_variants variants
        join game.enemy_loadout_tables tables
            on tables.table_key = variants.table_key
        where variants.table_key = loadout_table_key
          and variants.enabled
          and tables.enabled
    ),
    target_roll as (
        select floor(random() * max(weighted_variants.total_weight))::int + 1 as target
        from weighted_variants
    )
    select weighted_variants.variant_key
    into selected_variant_key
    from weighted_variants
    cross join target_roll
    where weighted_variants.running_weight >= target_roll.target
    order by weighted_variants.running_weight
    limit 1;

    return coalesce(
        (
            select jsonb_agg(game.authored_item(item_defs.name) order by items.item_order)
            from game.enemy_loadout_variant_items items
            join game.item_defs
                on game.item_defs.item_key = items.item_key
            where items.variant_key = selected_variant_key
              and game.item_defs.enabled
        ),
        '[]'::jsonb
    );
end;
$$;

create or replace function game.random_enemy_loadout()
returns jsonb
language sql
volatile
as $$
    select game.random_enemy_loadout_from_table('default_enemy_loadout');
$$;

create or replace function game.random_loot_items_from_table(loot_table_key text)
returns jsonb
language plpgsql
volatile
as $$
declare
    selected_variant_key text;
    derives_from_enemy_loadout boolean := false;
begin
    select tables.derived_from_enemy_loadout
    into derives_from_enemy_loadout
    from game.loot_tables tables
    where tables.table_key = loot_table_key
      and tables.enabled
    limit 1;

    if coalesce(derives_from_enemy_loadout, false) then
        return game.random_enemy_loadout();
    end if;

    with weighted_variants as (
        select
            variants.variant_key,
            sum(variants.weight) over (order by variants.sort_order, variants.variant_key) as running_weight,
            sum(variants.weight) over () as total_weight
        from game.loot_table_variants variants
        join game.loot_tables tables
            on tables.table_key = variants.table_key
        where variants.table_key = loot_table_key
          and variants.enabled
          and tables.enabled
    ),
    target_roll as (
        select floor(random() * max(weighted_variants.total_weight))::int + 1 as target
        from weighted_variants
    )
    select weighted_variants.variant_key
    into selected_variant_key
    from weighted_variants
    cross join target_roll
    where weighted_variants.running_weight >= target_roll.target
    order by weighted_variants.running_weight
    limit 1;

    return coalesce(
        (
            select jsonb_agg(game.authored_item(item_defs.name) order by items.item_order)
            from game.loot_table_variant_items items
            join game.item_defs
                on game.item_defs.item_key = items.item_key
            where items.variant_key = selected_variant_key
              and game.item_defs.enabled
        ),
        '[]'::jsonb
    );
end;
$$;

create or replace function game.random_loot_items_for_container(container_name text)
returns jsonb
language plpgsql
volatile
as $$
declare
    selected_table_key text;
begin
    select tables.table_key
    into selected_table_key
    from game.loot_tables tables
    where tables.source_name = container_name
      and tables.enabled
    limit 1;

    if selected_table_key is null then
        select tables.table_key
        into selected_table_key
        from game.loot_tables tables
        where tables.source_name = 'Filing Cabinet'
          and tables.enabled
        limit 1;
    end if;

    return game.random_loot_items_from_table(selected_table_key);
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
    container_name text;
    discovered_loot jsonb;
    enemy_loadout jsonb;
    enemy_health int;
begin
    if moving_to_extract then
        distance_from_extract := greatest(distance_from_extract - 1, 0);
    end if;

    updated_payload := jsonb_set(updated_payload, '{discoveredLoot}', '[]'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{awaitingDecision}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{extractionCombat}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{challenge}', to_jsonb(challenge), true);
    updated_payload := jsonb_set(updated_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);

    if distance_from_extract = 0 then
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
        enemy_health := selected_entry.enemy_health_min
            + floor(random() * (selected_entry.enemy_health_max_exclusive - selected_entry.enemy_health_min))::int;
        enemy_loadout := game.random_enemy_loadout_from_table(coalesce(selected_entry.enemy_loadout_table_key, 'default_enemy_loadout'));
        log_entries := game.raid_append_log(log_entries, format('Combat started vs %s.', selected_entry.enemy_name));

        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Combat'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Combat'))), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'Enemy contact on your position.'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(coalesce(selected_entry.enemy_name, ''::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(enemy_health), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', enemy_loadout, true);
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

revoke all on function game.random_enemy_loadout_from_table(text) from public;
revoke all on function game.random_enemy_loadout() from public;
revoke all on function game.random_loot_items_from_table(text) from public;
revoke all on function game.random_loot_items_for_container(text) from public;
revoke all on function game.generate_raid_encounter(jsonb, boolean) from public;
