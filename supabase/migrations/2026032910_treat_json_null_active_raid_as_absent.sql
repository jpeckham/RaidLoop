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
    accepted_stats_source jsonb := coalesce(save_payload->'acceptedStats', save_payload->'AcceptedStats', '{}'::jsonb);
    draft_stats_source jsonb := coalesce(save_payload->'draftStats', save_payload->'DraftStats', '{}'::jsonb);
    normalized_player_constitution int := greatest(coalesce((accepted_stats_source->>'constitution')::int, (accepted_stats_source->>'Constitution')::int, 8), 0);
    derived_player_max_health int := 10 + (2 * normalized_player_constitution);
    settled_random_state jsonb := game.settle_random_character(
        coalesce(save_payload->'randomCharacter', save_payload->'RandomCharacter'),
        coalesce(save_payload->'randomCharacterAvailableAt', save_payload->'RandomCharacterAvailableAt', to_jsonb('0001-01-01T00:00:00+00:00'::text))
    );
    active_raid_source jsonb := coalesce(save_payload->'activeRaid', save_payload->'ActiveRaid');
    normalized_active_raid jsonb := case
        when active_raid_source is null or jsonb_typeof(active_raid_source) = 'null' then null
        else game.normalize_active_raid_payload(
            active_raid_source,
            coalesce(
                save_payload->'acceptedStats',
                save_payload->'AcceptedStats',
                coalesce(active_raid_source->'acceptedStats', active_raid_source->'AcceptedStats', '{}'::jsonb)
            )
        )
    end;
begin
    return jsonb_build_object(
        'money', greatest(coalesce((save_payload->>'money')::int, (save_payload->>'Money')::int, 0), 0),
        'acceptedStats', jsonb_build_object(
            'strength', coalesce((accepted_stats_source->>'strength')::int, (accepted_stats_source->>'Strength')::int, 8),
            'dexterity', coalesce((accepted_stats_source->>'dexterity')::int, (accepted_stats_source->>'Dexterity')::int, 8),
            'constitution', coalesce((accepted_stats_source->>'constitution')::int, (accepted_stats_source->>'Constitution')::int, 8),
            'intelligence', coalesce((accepted_stats_source->>'intelligence')::int, (accepted_stats_source->>'Intelligence')::int, 8),
            'wisdom', coalesce((accepted_stats_source->>'wisdom')::int, (accepted_stats_source->>'Wisdom')::int, 8),
            'charisma', coalesce((accepted_stats_source->>'charisma')::int, (accepted_stats_source->>'Charisma')::int, 8)
        ),
        'draftStats', jsonb_build_object(
            'strength', coalesce((draft_stats_source->>'strength')::int, (draft_stats_source->>'Strength')::int, coalesce((accepted_stats_source->>'strength')::int, (accepted_stats_source->>'Strength')::int, 8)),
            'dexterity', coalesce((draft_stats_source->>'dexterity')::int, (draft_stats_source->>'Dexterity')::int, coalesce((accepted_stats_source->>'dexterity')::int, (accepted_stats_source->>'Dexterity')::int, 8)),
            'constitution', coalesce((draft_stats_source->>'constitution')::int, (draft_stats_source->>'Constitution')::int, coalesce((accepted_stats_source->>'constitution')::int, (accepted_stats_source->>'Constitution')::int, 8)),
            'intelligence', coalesce((draft_stats_source->>'intelligence')::int, (draft_stats_source->>'Intelligence')::int, coalesce((accepted_stats_source->>'intelligence')::int, (accepted_stats_source->>'Intelligence')::int, 8)),
            'wisdom', coalesce((draft_stats_source->>'wisdom')::int, (draft_stats_source->>'Wisdom')::int, coalesce((accepted_stats_source->>'wisdom')::int, (accepted_stats_source->>'Wisdom')::int, 8)),
            'charisma', coalesce((draft_stats_source->>'charisma')::int, (draft_stats_source->>'Charisma')::int, coalesce((accepted_stats_source->>'charisma')::int, (accepted_stats_source->>'Charisma')::int, 8))
        ),
        'availableStatPoints', coalesce((save_payload->>'availableStatPoints')::int, (save_payload->>'AvailableStatPoints')::int, 27),
        'statsAccepted', coalesce((save_payload->>'statsAccepted')::boolean, (save_payload->>'StatsAccepted')::boolean, false),
        'playerDexterity', coalesce((coalesce(save_payload->'acceptedStats', accepted_stats_source)->>'dexterity')::int, (coalesce(save_payload->'acceptedStats', accepted_stats_source)->>'Dexterity')::int, (save_payload->>'playerDexterity')::int, (save_payload->>'PlayerDexterity')::int, 8),
        'playerConstitution', coalesce((coalesce(save_payload->'acceptedStats', accepted_stats_source)->>'constitution')::int, (coalesce(save_payload->'acceptedStats', accepted_stats_source)->>'Constitution')::int, (save_payload->>'playerConstitution')::int, (save_payload->>'PlayerConstitution')::int, 8),
        'playerMaxHealth', derived_player_max_health,
        'mainStash', game.normalize_items(coalesce(save_payload->'mainStash', save_payload->'MainStash')),
        'onPersonItems', game.normalize_on_person_items(coalesce(save_payload->'onPersonItems', save_payload->'OnPersonItems')),
        'randomCharacterAvailableAt', settled_random_state->'randomCharacterAvailableAt',
        'randomCharacter', settled_random_state->'randomCharacter',
        'activeRaid', normalized_active_raid
    );
end;
$function$;

with normalized_game_saves as (
    select
        user_id,
        game.normalize_save_payload(payload) as normalized_payload
    from public.game_saves
)
update public.game_saves saves
set payload = normalized_game_saves.normalized_payload,
    save_version = 1,
    updated_at = timezone('utc', now())
from normalized_game_saves
where saves.user_id = normalized_game_saves.user_id
  and saves.payload is distinct from normalized_game_saves.normalized_payload;
