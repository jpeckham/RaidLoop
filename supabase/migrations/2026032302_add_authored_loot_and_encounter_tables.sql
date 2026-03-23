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
    ('enemy_makarov', 'default_enemy_loadout', 110, 10, true),
    ('enemy_bandage_6b2', 'default_enemy_loadout', 110, 20, true),
    ('enemy_6b2_only', 'default_enemy_loadout', 110, 30, true),
    ('enemy_ppsh_bandage', 'default_enemy_loadout', 60, 40, true),
    ('enemy_ak74_6b2', 'default_enemy_loadout', 6, 50, true),
    ('enemy_ak47_bandage', 'default_enemy_loadout', 6, 60, true),
    ('enemy_6b13_only', 'default_enemy_loadout', 6, 70, true),
    ('enemy_svds', 'default_enemy_loadout', 3, 80, true),
    ('enemy_fort_defender', 'default_enemy_loadout', 3, 90, true),
    ('enemy_pkp', 'default_enemy_loadout', 3, 100, true),
    ('enemy_nfm_thor', 'default_enemy_loadout', 3, 110, true)
on conflict (variant_key) do update
set table_key = excluded.table_key,
    weight = excluded.weight,
    sort_order = excluded.sort_order,
    enabled = excluded.enabled;

delete from game.enemy_loadout_variant_items
where variant_key in (
    'enemy_makarov',
    'enemy_bandage_6b2',
    'enemy_6b2_only',
    'enemy_ppsh_bandage',
    'enemy_ak74_6b2',
    'enemy_ak47_bandage',
    'enemy_6b13_only',
    'enemy_svds',
    'enemy_fort_defender',
    'enemy_pkp',
    'enemy_nfm_thor'
);

insert into game.enemy_loadout_variant_items (variant_key, item_key, item_order)
values
    ('enemy_makarov', 'makarov', 10),
    ('enemy_bandage_6b2', 'bandage', 10),
    ('enemy_bandage_6b2', '6b2_body_armor', 20),
    ('enemy_6b2_only', '6b2_body_armor', 10),
    ('enemy_ppsh_bandage', 'ppsh', 10),
    ('enemy_ppsh_bandage', 'bandage', 20),
    ('enemy_ak74_6b2', 'ak74', 10),
    ('enemy_ak74_6b2', '6b2_body_armor', 20),
    ('enemy_ak47_bandage', 'ak47', 10),
    ('enemy_ak47_bandage', 'bandage', 20),
    ('enemy_6b13_only', '6b13_assault_armor', 10),
    ('enemy_svds', 'svds', 10),
    ('enemy_fort_defender', 'fort_defender_2', 10),
    ('enemy_pkp', 'pkp', 10),
    ('enemy_nfm_thor', 'nfm_thor', 10);

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
    ('filing_ppsh', 'filing_cabinet', 36, 40, true),
    ('filing_rare_scope', 'filing_cabinet', 9, 50, true),
    ('filing_ak74', 'filing_cabinet', 9, 60, true),
    ('filing_svds', 'filing_cabinet', 9, 70, true),
    ('filing_legendary_trigger', 'filing_cabinet', 6, 80, true),
    ('weapons_makarov_ammo', 'weapons_crate', 40, 10, true),
    ('weapons_ppsh', 'weapons_crate', 12, 20, true),
    ('weapons_ak74', 'weapons_crate', 3, 30, true),
    ('weapons_ak47', 'weapons_crate', 3, 40, true),
    ('weapons_svds', 'weapons_crate', 3, 50, true),
    ('weapons_pkp', 'weapons_crate', 2, 60, true),
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
    'filing_ppsh',
    'filing_rare_scope',
    'filing_ak74',
    'filing_svds',
    'filing_legendary_trigger',
    'weapons_makarov_ammo',
    'weapons_ppsh',
    'weapons_ak74',
    'weapons_ak47',
    'weapons_svds',
    'weapons_pkp',
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
    ('filing_ppsh', 'ppsh', 10),
    ('filing_rare_scope', 'rare_scope', 10),
    ('filing_ak74', 'ak74', 10),
    ('filing_svds', 'svds', 10),
    ('filing_legendary_trigger', 'legendary_trigger_group', 10),
    ('weapons_makarov_ammo', 'makarov', 10),
    ('weapons_makarov_ammo', 'ammo_box', 20),
    ('weapons_ppsh', 'ppsh', 10),
    ('weapons_ak74', 'ak74', 10),
    ('weapons_ak47', 'ak47', 10),
    ('weapons_svds', 'svds', 10),
    ('weapons_pkp', 'pkp', 10),
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
    ('raid_combat_scav', 'default_raid', 'Combat', 650, 10, 'Scav', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'Enemy contact on your position.', true),
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
