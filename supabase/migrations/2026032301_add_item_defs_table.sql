-- Verification query:
-- select count(*) = 22 as has_expected_item_count,
--        count(distinct item_key) = 22 as item_keys_unique,
--        count(distinct name) = 22 as item_names_unique
-- from game.item_defs;

create table if not exists game.item_defs (
    item_key text primary key,
    name text not null unique,
    item_type int not null,
    value int not null,
    slots int not null,
    rarity int not null,
    display_rarity int not null,
    magazine_capacity int not null default 0,
    backpack_capacity int not null default 0,
    armor_damage_reduction int not null default 0,
    armor_penetration int not null default 0,
    supports_single_shot boolean not null default true,
    supports_burst_fire boolean not null default false,
    supports_full_auto boolean not null default false,
    burst_attack_penalty int not null default 3,
    damage_die_size int not null default 6,
    enabled boolean not null default true,
    sort_order int not null default 0,
    notes text,
    constraint item_defs_item_type_check check (item_type >= 0),
    constraint item_defs_value_check check (value >= 0),
    constraint item_defs_slots_check check (slots >= 0),
    constraint item_defs_rarity_check check (rarity >= 0),
    constraint item_defs_display_rarity_check check (display_rarity >= 0),
    constraint item_defs_magazine_capacity_check check (magazine_capacity >= 0),
    constraint item_defs_backpack_capacity_check check (backpack_capacity >= 0),
    constraint item_defs_armor_damage_reduction_check check (armor_damage_reduction >= 0),
    constraint item_defs_armor_penetration_check check (armor_penetration >= 0),
    constraint item_defs_burst_attack_penalty_check check (burst_attack_penalty >= 0),
    constraint item_defs_damage_die_size_check check (damage_die_size > 0)
);

insert into game.item_defs (
    item_key,
    name,
    item_type,
    value,
    slots,
    rarity,
    display_rarity,
    magazine_capacity,
    backpack_capacity,
    armor_damage_reduction,
    armor_penetration,
    supports_single_shot,
    supports_burst_fire,
    supports_full_auto,
    burst_attack_penalty,
    damage_die_size,
    enabled,
    sort_order,
    notes
)
values
    ('rusty_knife', 'Rusty Knife', 0, 1, 1, 0, 1, 0, 0, 0, 0, true, false, false, 3, 6, true, 10, 'Fallback melee weapon'),
    ('light_pistol', 'Light Pistol', 0, 60, 1, 0, 1, 8, 0, 0, 1, true, true, false, 3, 6, true, 20, null),
    ('drum_smg', 'Drum SMG', 0, 160, 1, 1, 2, 35, 0, 0, 1, true, true, true, 2, 4, true, 30, null),
    ('field_carbine', 'Field Carbine', 0, 320, 1, 2, 3, 30, 0, 0, 2, true, true, true, 2, 8, true, 40, null),
    ('battle_rifle', 'Battle Rifle', 0, 375, 1, 2, 3, 30, 0, 0, 2, true, true, true, 2, 10, true, 50, null),
    ('marksman_rifle', 'Marksman Rifle', 0, 550, 1, 3, 4, 20, 0, 0, 3, true, true, false, 2, 12, true, 60, null),
    ('support_machine_gun', 'Support Machine Gun', 0, 800, 1, 4, 5, 100, 0, 0, 0, false, true, true, 2, 12, true, 70, null),
    ('soft_armor_vest', 'Soft Armor Vest', 1, 95, 1, 0, 1, 0, 0, 1, 0, true, false, false, 3, 6, true, 80, null),
    ('light_plate_carrier', 'Light Plate Carrier', 1, 225, 1, 2, 3, 0, 0, 3, 0, true, false, false, 3, 6, true, 90, null),
    ('medium_plate_carrier', 'Medium Plate Carrier', 1, 375, 1, 3, 4, 0, 0, 4, 0, true, false, false, 3, 6, true, 100, null),
    ('heavy_plate_carrier', 'Heavy Plate Carrier', 1, 450, 1, 4, 5, 0, 0, 5, 0, true, false, false, 3, 6, true, 110, null),
    ('assault_plate_carrier', 'Assault Plate Carrier', 1, 650, 1, 4, 5, 0, 0, 6, 0, true, false, false, 3, 6, true, 120, null),
    ('small_backpack', 'Small Backpack', 2, 25, 1, 1, 2, 0, 3, 0, 0, true, false, false, 3, 6, true, 130, null),
    ('tactical_backpack', 'Tactical Backpack', 2, 75, 2, 2, 3, 0, 6, 0, 0, true, false, false, 3, 6, true, 140, null),
    ('hiking_backpack', 'Hiking Backpack', 2, 400, 3, 3, 4, 0, 8, 0, 0, true, false, false, 3, 6, true, 150, null),
    ('raid_backpack', 'Raid Backpack', 2, 600, 4, 4, 5, 0, 10, 0, 0, true, false, false, 3, 6, true, 160, null),
    ('medkit', 'Medkit', 3, 30, 1, 0, 1, 0, 0, 0, 0, true, false, false, 3, 6, true, 170, null),
    ('bandage', 'Bandage', 4, 15, 1, 0, 0, 0, 0, 0, 0, true, false, false, 3, 6, true, 180, null),
    ('ammo_box', 'Ammo Box', 4, 20, 1, 0, 0, 0, 0, 0, 0, true, false, false, 3, 6, true, 190, null),
    ('scrap_metal', 'Scrap Metal', 5, 18, 1, 0, 0, 0, 0, 0, 0, true, false, false, 3, 6, true, 200, null),
    ('rare_scope', 'Rare Scope', 5, 80, 1, 2, 0, 0, 0, 0, 0, true, false, false, 3, 6, true, 210, null),
    ('legendary_trigger_group', 'Legendary Trigger Group', 5, 150, 1, 4, 0, 0, 0, 0, 0, true, false, false, 3, 6, true, 220, null)
on conflict (item_key) do update
set name = excluded.name,
    item_type = excluded.item_type,
    value = excluded.value,
    slots = excluded.slots,
    rarity = excluded.rarity,
    display_rarity = excluded.display_rarity,
    magazine_capacity = excluded.magazine_capacity,
    backpack_capacity = excluded.backpack_capacity,
    armor_damage_reduction = excluded.armor_damage_reduction,
    armor_penetration = excluded.armor_penetration,
    supports_single_shot = excluded.supports_single_shot,
    supports_burst_fire = excluded.supports_burst_fire,
    supports_full_auto = excluded.supports_full_auto,
    burst_attack_penalty = excluded.burst_attack_penalty,
    damage_die_size = excluded.damage_die_size,
    enabled = excluded.enabled,
    sort_order = excluded.sort_order,
    notes = excluded.notes;

revoke all on table game.item_defs from public;

create or replace function game.authored_item(item_name text)
returns jsonb
language sql
stable
as $$
    select jsonb_build_object(
        'name', item_defs.name,
        'type', item_defs.item_type,
        'value', item_defs.value,
        'slots', item_defs.slots,
        'rarity', item_defs.rarity,
        'displayRarity', item_defs.display_rarity
    )
    from game.item_defs
    where item_defs.name = item_name
      and item_defs.enabled
    limit 1;
$$;

create or replace function game.weapon_magazine_capacity(weapon_name text)
returns int
language sql
stable
as $$
    select coalesce(
        (
            select item_defs.magazine_capacity
            from game.item_defs
            where item_defs.name = weapon_name
              and item_defs.enabled
            limit 1
        ),
        8
    );
$$;

create or replace function game.backpack_capacity(backpack_name text)
returns int
language sql
stable
as $$
    select coalesce(
        (
            select item_defs.backpack_capacity
            from game.item_defs
            where item_defs.name = backpack_name
              and item_defs.enabled
            limit 1
        ),
        2
    );
$$;

create or replace function game.weapon_armor_penetration(weapon_name text)
returns int
language sql
stable
as $$
    select coalesce(
        (
            select item_defs.armor_penetration
            from game.item_defs
            where item_defs.name = weapon_name
              and item_defs.enabled
            limit 1
        ),
        0
    );
$$;

create or replace function game.armor_damage_reduction(armor_name text)
returns int
language sql
stable
as $$
    select coalesce(
        (
            select item_defs.armor_damage_reduction
            from game.item_defs
            where item_defs.name = armor_name
              and item_defs.enabled
            limit 1
        ),
        0
    );
$$;

create or replace function game.weapon_supports_single_shot(weapon_name text)
returns boolean
language sql
stable
as $$
    select coalesce(
        (
            select item_defs.supports_single_shot
            from game.item_defs
            where item_defs.name = weapon_name
              and item_defs.enabled
            limit 1
        ),
        true
    );
$$;

create or replace function game.weapon_supports_burst_fire(weapon_name text)
returns boolean
language sql
stable
as $$
    select coalesce(
        (
            select item_defs.supports_burst_fire
            from game.item_defs
            where item_defs.name = weapon_name
              and item_defs.enabled
            limit 1
        ),
        false
    );
$$;

create or replace function game.weapon_supports_full_auto(weapon_name text)
returns boolean
language sql
stable
as $$
    select coalesce(
        (
            select item_defs.supports_full_auto
            from game.item_defs
            where item_defs.name = weapon_name
              and item_defs.enabled
            limit 1
        ),
        false
    );
$$;

create or replace function game.weapon_burst_attack_penalty(weapon_name text)
returns int
language sql
stable
as $$
    select coalesce(
        (
            select item_defs.burst_attack_penalty
            from game.item_defs
            where item_defs.name = weapon_name
              and item_defs.enabled
            limit 1
        ),
        3
    );
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
begin
    select coalesce(item_defs.damage_die_size, 6)
    into die_size
    from game.item_defs
    where item_defs.name = weapon_name
      and item_defs.enabled
    limit 1;

    die_size := coalesce(die_size, 6);

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

revoke all on function game.authored_item(text) from public;
revoke all on function game.weapon_magazine_capacity(text) from public;
revoke all on function game.backpack_capacity(text) from public;
revoke all on function game.weapon_armor_penetration(text) from public;
revoke all on function game.armor_damage_reduction(text) from public;
revoke all on function game.weapon_supports_single_shot(text) from public;
revoke all on function game.weapon_supports_burst_fire(text) from public;
revoke all on function game.weapon_supports_full_auto(text) from public;
revoke all on function game.weapon_burst_attack_penalty(text) from public;
revoke all on function game.roll_weapon_damage_d20(text, text) from public;
