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
        '{logEntries}',
        coalesce(payload->'logEntries', payload->'LogEntries', '[]'::jsonb),
        true
    );
    normalized_payload := jsonb_set(
        normalized_payload,
        '{acceptedStats}',
        coalesce(resolved_stats, '{}'::jsonb),
        true
    );
    normalized_payload := jsonb_set(
        normalized_payload,
        '{maxEncumbrance}',
        to_jsonb(game.max_encumbrance(coalesce((resolved_stats->>'strength')::int, (resolved_stats->>'Strength')::int, 8))),
        true
    );
    normalized_payload := jsonb_set(
        normalized_payload,
        '{encumbranceTier}',
        to_jsonb(game.encumbrance_tier(
            coalesce((resolved_stats->>'strength')::int, (resolved_stats->>'Strength')::int, 8),
            game.current_encumbrance(
                coalesce(normalized_payload->'equippedItems', '[]'::jsonb) || coalesce(normalized_payload->'carriedLoot', '[]'::jsonb),
                greatest(coalesce((normalized_payload->>'medkits')::int, 0), 0)
            )
        )),
        true
    );

    return normalized_payload;
end;
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
set payload = normalized_raid_sessions.normalized_payload
from normalized_raid_sessions
where sessions.user_id = normalized_raid_sessions.user_id
  and sessions.payload is distinct from normalized_raid_sessions.normalized_payload;
