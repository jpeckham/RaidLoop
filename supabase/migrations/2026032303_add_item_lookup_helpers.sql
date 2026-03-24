-- Verification queries:
-- select (game.item_def_by_name('AK74')).name = 'AK74' as name_lookup_matches,
--        (game.item_def_by_name('AK74')).enabled as name_lookup_enabled;
-- select (game.item_def_by_key('ak74')).name = 'AK74' as key_lookup_matches,
--        (game.item_def_by_key('ak74')).enabled as key_lookup_enabled;
-- select game.item_key_for_name('AK74') = 'ak74' as item_key_matches_name_lookup;
--
-- Parity checks:
-- select game.weapon_magazine_capacity('PKP') = 100 as pkp_magazine_capacity_ok;
-- select game.backpack_capacity('6Sh118') = 10 as backpack_capacity_ok;
-- select game.weapon_supports_single_shot('PKP') = false as pkp_single_shot_ok;
-- select game.weapon_burst_attack_penalty('Makarov') = 3 as makarov_burst_penalty_ok;

create or replace function game.item_def_by_name(item_name text)
returns game.item_defs
language sql
stable
as $$
    select item_defs
    from game.item_defs
    where item_defs.name = item_name
      and item_defs.enabled
    limit 1;
$$;

create or replace function game.item_def_by_key(def_item_key text)
returns game.item_defs
language sql
stable
as $$
    select item_defs
    from game.item_defs
    where item_defs.item_key = def_item_key
      and item_defs.enabled
    limit 1;
$$;

create or replace function game.item_key_for_name(item_name text)
returns text
language sql
stable
as $$
    select (game.item_def_by_name(item_name)).item_key;
$$;

create or replace function game.authored_item(item_name text)
returns jsonb
language sql
stable
as $$
    select case
        when resolved.item_def is null then null
        else jsonb_build_object(
            'name', (resolved.item_def).name,
            'type', (resolved.item_def).item_type,
            'value', (resolved.item_def).value,
            'slots', (resolved.item_def).slots,
            'rarity', (resolved.item_def).rarity,
            'displayRarity', (resolved.item_def).display_rarity
        )
    end
    from (
        select game.item_def_by_name(item_name) as item_def
    ) resolved;
$$;

create or replace function game.weapon_magazine_capacity(weapon_name text)
returns int
language sql
stable
as $$
    select coalesce((resolved.item_def).magazine_capacity, 8)
    from (
        select game.item_def_by_name(weapon_name) as item_def
    ) resolved;
$$;

create or replace function game.backpack_capacity(backpack_name text)
returns int
language sql
stable
as $$
    select coalesce((resolved.item_def).backpack_capacity, 2)
    from (
        select game.item_def_by_name(backpack_name) as item_def
    ) resolved;
$$;

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

revoke all on function game.item_def_by_name(text) from public;
revoke all on function game.item_def_by_key(text) from public;
revoke all on function game.item_key_for_name(text) from public;
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
