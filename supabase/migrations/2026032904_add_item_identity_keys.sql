-- item_key text not null; unique (item_key) remains enforced by the existing primary key on game.item_defs.
-- item_def_id gives authored item definitions a stable surrogate identity while item_key remains the canonical app key.
-- add column if not exists item_def_id int generated always as identity
alter table game.item_defs
    add column if not exists item_def_id int;

with ordered_item_defs as (
    select
        ctid,
        row_number() over (order by sort_order, item_key) as deterministic_item_def_id
    from game.item_defs
)
update game.item_defs defs
set item_def_id = ordered_item_defs.deterministic_item_def_id
from ordered_item_defs
where defs.ctid = ordered_item_defs.ctid;

do $$
declare
    sequence_name text;
    max_item_def_id int;
begin
    execute 'alter table game.item_defs alter column item_def_id set not null';

    if exists (
        select 1
        from information_schema.columns
        where table_schema = 'game'
          and table_name = 'item_defs'
          and column_name = 'item_def_id'
          and is_identity = 'NO'
    ) then
        execute 'alter table game.item_defs alter column item_def_id add generated always as identity';
    end if;

    select coalesce(max(item_def_id), 0)
    into max_item_def_id
    from game.item_defs;

    sequence_name := pg_get_serial_sequence('game.item_defs', 'item_def_id');
    if sequence_name is not null then
        perform setval(sequence_name, greatest(max_item_def_id, 1), true);
    end if;
end;
$$;

create unique index if not exists item_defs_item_def_id_key
    on game.item_defs(item_def_id);

create or replace function game.authored_item(item_name text)
returns jsonb
language sql
stable
as $function$
    select case
        when resolved.item_def is null then null
        else jsonb_build_object(
            'itemKey', (resolved.item_def).item_key,
            'name', (resolved.item_def).name,
            'type', (resolved.item_def).item_type,
            'value', (resolved.item_def).value,
            'slots', (resolved.item_def).slots,
            'rarity', (resolved.item_def).rarity,
            'displayRarity', (resolved.item_def).display_rarity,
            'weight', (resolved.item_def).weight
        )
    end
    from (
        select coalesce(
            game.item_def_by_key(item_name),
            game.item_def_by_name(item_name)
        ) as item_def
    ) resolved;
$function$;

create or replace function game.normalize_item(item jsonb)
returns jsonb
language sql
stable
as $function$
    with normalized as (
        select
            coalesce(item->>'itemKey', item->>'item_key', item->>'ItemKey', item->>'Item_Key') as item_key,
            coalesce(item->>'name', item->>'Name') as item_name
    )
    select coalesce(
        game.authored_item(coalesce((select item_key from normalized), (select item_name from normalized))),
        jsonb_build_object(
            'itemKey', coalesce((select item_key from normalized), ''),
            'name', (select item_name from normalized),
            'type', coalesce((item->>'type')::int, (item->>'Type')::int, 0),
            'value', coalesce((item->>'value')::int, (item->>'Value')::int, 1),
            'slots', coalesce((item->>'slots')::int, (item->>'Slots')::int, 1),
            'rarity', coalesce((item->>'rarity')::int, (item->>'Rarity')::int, 0),
            'displayRarity', coalesce((item->>'displayRarity')::int, (item->>'DisplayRarity')::int, 1)
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
                game.authored_item('makarov'),
                game.authored_item('ppsh'),
                game.authored_item('ak74'),
                game.authored_item('6b2_body_armor'),
                game.authored_item('6b13_assault_armor'),
                game.authored_item('small_backpack'),
                game.authored_item('tactical_backpack'),
                game.authored_item('medkit'),
                game.authored_item('bandage'),
                game.authored_item('ammo_box')
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
            'playerDexterity', 8,
            'playerConstitution', 8,
            'playerMaxHealth', 26,
            'randomCharacterAvailableAt', '0001-01-01T00:00:00+00:00',
            'randomCharacter', null,
            'money', 500,
            'onPersonItems', jsonb_build_array(),
            'activeRaid', null
        )
    );
$function$;

create or replace function game.normalize_active_raid_payload(payload jsonb, accepted_stats jsonb default null)
returns jsonb
language plpgsql
volatile
as $function$
declare
    normalized_payload jsonb := case
        when jsonb_typeof(payload) = 'object' then payload
        else '{}'::jsonb
    end;
    resolved_stats jsonb := coalesce(
        accepted_stats,
        payload->'acceptedStats',
        payload->'AcceptedStats',
        jsonb_build_object(
            'strength', 8,
            'dexterity', 8,
            'constitution', 8,
            'intelligence', 8,
            'wisdom', 8,
            'charisma', 8
        )
    );
begin
    normalized_payload := jsonb_set(
        normalized_payload,
        '{equippedItems}',
        game.normalize_items(coalesce(payload->'equippedItems', payload->'EquippedItems')),
        true
    );
    normalized_payload := jsonb_set(
        normalized_payload,
        '{carriedLoot}',
        game.normalize_items(coalesce(payload->'carriedLoot', payload->'CarriedLoot')),
        true
    );
    normalized_payload := jsonb_set(
        normalized_payload,
        '{discoveredLoot}',
        game.normalize_items(coalesce(payload->'discoveredLoot', payload->'DiscoveredLoot')),
        true
    );
    normalized_payload := jsonb_set(
        normalized_payload,
        '{enemyLoadout}',
        game.normalize_items(coalesce(payload->'enemyLoadout', payload->'EnemyLoadout')),
        true
    );
    normalized_payload := jsonb_set(
        normalized_payload,
        '{acceptedStats}',
        coalesce(resolved_stats, '{}'::jsonb),
        true
    );

    return normalized_payload;
end;
$function$;

create or replace function game.normalize_save_payload(payload jsonb)
returns jsonb
language plpgsql
volatile
as $function$
declare
    save_payload jsonb := case
        when jsonb_typeof(payload) = 'object' then payload
        else '{}'::jsonb
    end;
    settled_random_state jsonb := game.settle_random_character(
        coalesce(save_payload->'randomCharacter', save_payload->'RandomCharacter'),
        coalesce(save_payload->'randomCharacterAvailableAt', save_payload->'RandomCharacterAvailableAt', to_jsonb('0001-01-01T00:00:00+00:00'::text)));
    normalized_active_raid jsonb := case
        when coalesce(save_payload->'activeRaid', save_payload->'ActiveRaid') is null then null
        else game.normalize_active_raid_payload(
            coalesce(save_payload->'activeRaid', save_payload->'ActiveRaid'),
            coalesce(
                save_payload->'acceptedStats',
                save_payload->'AcceptedStats',
                coalesce(save_payload->'activeRaid'->'acceptedStats', save_payload->'activeRaid'->'AcceptedStats', '{}'::jsonb)
            )
        )
    end;
begin
    return jsonb_build_object(
        'money', greatest(coalesce((save_payload->>'money')::int, (save_payload->>'Money')::int, 0), 0),
        'playerDexterity', coalesce((save_payload->>'playerDexterity')::int, (save_payload->>'PlayerDexterity')::int, 10),
        'playerConstitution', coalesce((save_payload->>'playerConstitution')::int, (save_payload->>'PlayerConstitution')::int, 10),
        'playerMaxHealth', coalesce((save_payload->>'playerMaxHealth')::int, (save_payload->>'PlayerMaxHealth')::int, 10 + (2 * coalesce((save_payload->>'playerConstitution')::int, (save_payload->>'PlayerConstitution')::int, 10))),
        'mainStash', game.normalize_items(coalesce(save_payload->'mainStash', save_payload->'MainStash')),
        'onPersonItems', game.normalize_on_person_items(coalesce(save_payload->'onPersonItems', save_payload->'OnPersonItems')),
        'randomCharacterAvailableAt', settled_random_state->'randomCharacterAvailableAt',
        'randomCharacter', settled_random_state->'randomCharacter',
        'activeRaid', normalized_active_raid
    );
end;
$function$;

create or replace function game.random_loot_items()
returns jsonb
language sql
as $function$
    select case floor(random() * 4)::int
        when 0 then jsonb_build_array(
            game.authored_item('bandage'),
            game.authored_item('ammo_box')
        )
        when 1 then jsonb_build_array(
            game.authored_item('medkit')
        )
        when 2 then jsonb_build_array(
            game.authored_item('scrap_metal'),
            game.authored_item('rare_scope')
        )
        else jsonb_build_array(
            game.authored_item('makarov')
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
            select jsonb_agg(game.authored_item(game.item_defs.item_key) order by items.item_order)
            from game.enemy_loadout_variant_items items
            join game.item_defs
                on game.item_defs.item_key = items.item_key
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
            select jsonb_agg(game.authored_item(game.item_defs.item_key) order by items.item_order)
            from game.loot_table_variant_items items
            join game.item_defs
                on game.item_defs.item_key = items.item_key
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
        game.authored_item('makarov'),
        case when random() < 0.5
            then game.authored_item('small_backpack')
            else game.authored_item('tactical_backpack')
        end,
        game.authored_item('medkit'),
        game.authored_item('bandage'),
        game.authored_item('ammo_box')
    );
$function$;

with normalized_game_saves as (
    select
        user_id,
        game.normalize_save_payload(payload) as normalized_payload
    from public.game_saves
)
update public.game_saves saves
set payload = normalized_game_saves.normalized_payload
from normalized_game_saves
where saves.user_id = normalized_game_saves.user_id
  and saves.payload is distinct from normalized_game_saves.normalized_payload;

with normalized_raid_sessions as (
    select
        user_id,
        game.normalize_active_raid_payload(
            payload,
            coalesce(payload->'acceptedStats', payload->'AcceptedStats', '{}'::jsonb)
        ) as normalized_payload
    from public.raid_sessions
)
update public.raid_sessions sessions
set payload = normalized_raid_sessions.normalized_payload
from normalized_raid_sessions
where sessions.user_id = normalized_raid_sessions.user_id
  and sessions.payload is distinct from normalized_raid_sessions.normalized_payload;
