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
