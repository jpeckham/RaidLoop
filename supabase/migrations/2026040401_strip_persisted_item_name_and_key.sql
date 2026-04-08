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
        game.authored_item(coalesce((select item_key from normalized), (select item_name from normalized))),
        jsonb_strip_nulls(
            jsonb_build_object(
                'itemDefId', coalesce((select item_def_id from normalized), 0),
                'itemKey', nullif(coalesce((select item_key from normalized), ''), ''),
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

with normalized_game_saves as (
    select
        user_id,
        game.normalize_save_payload(payload) as normalized_payload
    from public.game_saves
)
update public.game_saves saves
set payload = normalized_game_saves.normalized_payload,
    updated_at = timezone('utc', now())
from normalized_game_saves
where saves.user_id = normalized_game_saves.user_id
  and saves.payload is distinct from normalized_game_saves.normalized_payload;

with normalized_raid_sessions as (
    select
        sessions.user_id,
        game.normalize_active_raid_payload(
            sessions.payload,
            coalesce(
                saves.payload->'acceptedStats',
                saves.payload->'AcceptedStats',
                sessions.payload->'acceptedStats',
                sessions.payload->'AcceptedStats',
                '{}'::jsonb
            )
        ) as normalized_payload
    from public.raid_sessions sessions
    left join public.game_saves saves
        on saves.user_id = sessions.user_id
)
update public.raid_sessions sessions
set payload = normalized_raid_sessions.normalized_payload,
    updated_at = timezone('utc', now())
from normalized_raid_sessions
where sessions.user_id = normalized_raid_sessions.user_id
  and sessions.payload is distinct from normalized_raid_sessions.normalized_payload;
