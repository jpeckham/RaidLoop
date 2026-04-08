alter table game.enemy_loadout_variant_items
    add column if not exists item_def_id int;

update game.enemy_loadout_variant_items items
set item_def_id = defs.item_def_id
from game.item_defs defs
where defs.item_key = items.item_key
  and items.item_def_id is null;

alter table game.enemy_loadout_variant_items
    alter column item_def_id set not null;

alter table game.enemy_loadout_variant_items
    drop constraint if exists enemy_loadout_variant_items_item_key_fkey;

alter table game.enemy_loadout_variant_items
    drop constraint if exists enemy_loadout_variant_items_pkey;

alter table game.enemy_loadout_variant_items
    add constraint enemy_loadout_variant_items_item_def_id_fkey
    foreign key (item_def_id) references game.item_defs(item_def_id);

alter table game.enemy_loadout_variant_items
    add constraint enemy_loadout_variant_items_pkey
    primary key (variant_key, item_def_id, item_order);

alter table game.enemy_loadout_variant_items
    drop column item_key;

alter table game.loot_table_variant_items
    add column if not exists item_def_id int;

update game.loot_table_variant_items items
set item_def_id = defs.item_def_id
from game.item_defs defs
where defs.item_key = items.item_key
  and items.item_def_id is null;

alter table game.loot_table_variant_items
    alter column item_def_id set not null;

alter table game.loot_table_variant_items
    drop constraint if exists loot_table_variant_items_item_key_fkey;

alter table game.loot_table_variant_items
    drop constraint if exists loot_table_variant_items_pkey;

alter table game.loot_table_variant_items
    add constraint loot_table_variant_items_item_def_id_fkey
    foreign key (item_def_id) references game.item_defs(item_def_id);

alter table game.loot_table_variant_items
    add constraint loot_table_variant_items_pkey
    primary key (variant_key, item_def_id, item_order);

alter table game.loot_table_variant_items
    drop column item_key;

create or replace function game.payload_item_name(payload jsonb)
returns text
language sql
stable
as $function$
    select coalesce(
        payload->>'itemName',
        payload->>'item_name',
        payload->>'ItemName',
        payload->>'name',
        payload->>'Name',
        (
            select defs.name
            from game.item_defs defs
            where defs.item_def_id = coalesce(
                nullif(payload->>'itemDefId', '')::int,
                nullif(payload->>'item_def_id', '')::int,
                nullif(payload->>'ItemDefId', '')::int
            )
            limit 1
        )
    );
$function$;

create or replace function game.authored_item(item_name text)
returns jsonb
language sql
stable
as $function$
    select case
        when resolved.item_def is null then null
        else jsonb_build_object(
            'itemDefId', (resolved.item_def).item_def_id,
            'type', (resolved.item_def).item_type,
            'value', (resolved.item_def).value,
            'slots', (resolved.item_def).slots,
            'rarity', (resolved.item_def).rarity,
            'displayRarity', (resolved.item_def).display_rarity,
            'weight', (resolved.item_def).weight
        )
    end
    from (
        select game.item_def_by_name(item_name) as item_def
    ) resolved;
$function$;

create or replace function game.normalize_item(item jsonb)
returns jsonb
language sql
stable
as $function$
    with normalized as (
        select
            coalesce(item->>'name', item->>'Name') as item_name,
            coalesce(
                nullif(item->>'itemDefId', '')::int,
                nullif(item->>'item_def_id', '')::int,
                nullif(item->>'ItemDefId', '')::int
            ) as item_def_id
    )
    select coalesce(
        (
            select jsonb_build_object(
                'itemDefId', defs.item_def_id,
                'type', defs.item_type,
                'value', defs.value,
                'slots', defs.slots,
                'rarity', defs.rarity,
                'displayRarity', defs.display_rarity,
                'weight', defs.weight
            )
            from game.item_defs defs
            where defs.item_def_id = (select item_def_id from normalized)
            limit 1
        ),
        game.authored_item((select item_name from normalized)),
        jsonb_strip_nulls(
            jsonb_build_object(
                'itemDefId', coalesce((select item_def_id from normalized), 0),
                'name', nullif((select item_name from normalized), ''),
                'type', coalesce((item->>'type')::int, (item->>'Type')::int, 0),
                'value', coalesce((item->>'value')::int, (item->>'Value')::int, 1),
                'slots', coalesce((item->>'slots')::int, (item->>'Slots')::int, 1),
                'rarity', coalesce((item->>'rarity')::int, (item->>'Rarity')::int, 0),
                'displayRarity', coalesce((item->>'displayRarity')::int, (item->>'DisplayRarity')::int, 1),
                'weight', coalesce((item->>'weight')::int, (item->>'Weight')::int, 0)
            )
        )
    );
$function$;

create or replace function game.default_save_payload()
returns jsonb
language sql
stable
as $function$
    select game.normalize_save_payload(
        jsonb_build_object(
            'mainStash', jsonb_build_array(
                game.authored_item('Makarov'),
                game.authored_item('PPSH'),
                game.authored_item('AK74'),
                game.authored_item('6B2 body armor'),
                game.authored_item('6B13 assault armor'),
                game.authored_item('Small Backpack'),
                game.authored_item('Tactical Backpack'),
                game.authored_item('Medkit'),
                game.authored_item('Bandage'),
                game.authored_item('Ammo Box')
            ),
            'acceptedStats', jsonb_build_object(
                'strength', 8,
                'dexterity', 8,
                'constitution', 8,
                'intelligence', 8,
                'wisdom', 8,
                'charisma', 8
            ),
            'draftStats', jsonb_build_object(
                'strength', 8,
                'dexterity', 8,
                'constitution', 8,
                'intelligence', 8,
                'wisdom', 8,
                'charisma', 8
            ),
            'availableStatPoints', 27,
            'statsAccepted', false,
            'money', 0,
            'onPersonItems', '[]'::jsonb,
            'randomCharacterAvailableAt', to_jsonb('0001-01-01T00:00:00+00:00'::text),
            'randomCharacter', null,
            'activeRaid', null
        )
    );
$function$;

create or replace function game.random_loot_items()
returns jsonb
language sql
as $function$
    select case floor(random() * 4)::int
        when 0 then jsonb_build_array(
            game.authored_item('Bandage'),
            game.authored_item('Ammo Box')
        )
        when 1 then jsonb_build_array(
            game.authored_item('Medkit')
        )
        when 2 then jsonb_build_array(
            game.authored_item('Scrap Metal'),
            game.authored_item('Rare Scope')
        )
        else jsonb_build_array(
            game.authored_item('Makarov')
        )
    end;
$function$;

create or replace function game.random_enemy_loadout_from_table(loadout_table_key text)
returns jsonb
language plpgsql
volatile
as $function$
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
            select jsonb_agg(game.authored_item(game.item_defs.name) order by items.item_order)
            from game.enemy_loadout_variant_items items
            join game.item_defs
                on game.item_defs.item_def_id = items.item_def_id
            where items.variant_key = selected_variant_key
              and game.item_defs.enabled
        ),
        '[]'::jsonb
    );
end;
$function$;

create or replace function game.random_loot_items_from_table(loot_table_key text)
returns jsonb
language plpgsql
volatile
as $function$
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
            select jsonb_agg(game.authored_item(game.item_defs.name) order by items.item_order)
            from game.loot_table_variant_items items
            join game.item_defs
                on game.item_defs.item_def_id = items.item_def_id
            where items.variant_key = selected_variant_key
              and game.item_defs.enabled
        ),
        '[]'::jsonb
    );
end;
$function$;

create or replace function game.random_luck_run_loadout()
returns jsonb
language sql
volatile
as $function$
    select jsonb_build_array(
        game.authored_item('Makarov'),
        case when random() < 0.5
            then game.authored_item('Small Backpack')
            else game.authored_item('Tactical Backpack')
        end,
        game.authored_item('Medkit'),
        game.authored_item('Bandage'),
        game.authored_item('Ammo Box')
    );
$function$;

drop function if exists game.item_def_by_key(text);
drop function if exists game.item_key_for_name(text);

alter table game.item_defs drop constraint if exists item_defs_pkey;
drop index if exists item_defs_item_def_id_key;
alter table game.item_defs add constraint item_defs_pkey primary key (item_def_id);
alter table game.item_defs drop column item_key;
