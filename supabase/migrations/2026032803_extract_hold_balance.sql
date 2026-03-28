insert into game.encounter_tables (table_key, name, enabled)
values
    ('extract_hold', 'Extract Hold Encounter Table', true)
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
    ('extract_hold_quiet_window', 'extract_hold', 'Extraction', 'MutualContact', 120, 10, null, null, null, null, null, 'Extraction Opportunity', 'The extract lane stays quiet. You have a clean window to leave.', true),
    ('extract_hold_false_alarm', 'extract_hold', 'Extraction', 'MutualContact', 80, 20, null, null, null, null, null, 'Extraction Opportunity', 'You hold position through a false alarm and keep the route covered.', true),
    ('extract_hold_medical_cache', 'extract_hold', 'Loot', 'MutualContact', 24, 30, null, null, null, 'medical_container', null, 'Loot Encounter', 'A hasty stash appears near the extract route while you hold your angle.', true),
    ('extract_hold_player_spots_camper', 'extract_hold', 'Combat', 'PlayerAmbush', 28, 40, 'Extract Camper', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You spot movement near extract before the camper notices you.', true),
    ('extract_hold_enemy_pushes_position', 'extract_hold', 'Combat', 'EnemyAmbush', 28, 50, 'Extract Hunter', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'A hunter collapses on your hold position from an unexpected angle.', true),
    ('extract_hold_mutual_contact', 'extract_hold', 'Combat', 'MutualContact', 36, 60, 'Final Guard', 12, 21, null, 'default_enemy_loadout', 'Combat Encounter', 'You and another survivor catch each other on the extract lane at the same time.', true)
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

create or replace function game.clear_extract_hold_state(raid_payload jsonb)
returns jsonb
language sql
stable
as $$
    select jsonb_set(
        jsonb_set(coalesce(raid_payload, '{}'::jsonb), '{extractHoldActive}', 'false'::jsonb, true),
        '{holdAtExtractUntil}',
        'null'::jsonb,
        true);
$$;

create or replace function game.generate_extract_hold_encounter(raid_payload jsonb)
returns jsonb
language plpgsql
volatile
as $$
declare
    updated_payload jsonb := game.clear_extract_hold_state(coalesce(raid_payload, '{}'::jsonb));
    log_entries jsonb := coalesce(updated_payload->'logEntries', '[]'::jsonb);
    selected_entry game.encounter_table_entries%rowtype;
    container_name text;
    discovered_loot jsonb;
    enemy_loadout jsonb;
    enemy_health int;
begin
    updated_payload := jsonb_set(updated_payload, '{distanceFromExtract}', to_jsonb(0), true);
    updated_payload := jsonb_set(updated_payload, '{discoveredLoot}', '[]'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{awaitingDecision}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{extractionCombat}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{contactState}', to_jsonb('None'::text), true);
    updated_payload := jsonb_set(updated_payload, '{surpriseSide}', to_jsonb('None'::text), true);
    updated_payload := jsonb_set(updated_payload, '{initiativeWinner}', to_jsonb('None'::text), true);
    updated_payload := jsonb_set(updated_payload, '{openingActionsRemaining}', to_jsonb(0), true);
    updated_payload := jsonb_set(updated_payload, '{surprisePersistenceEligible}', to_jsonb(false), true);

    with weighted_entries as (
        select
            entries.*,
            sum(entries.weight) over (order by entries.sort_order, entries.entry_key) as running_weight,
            sum(entries.weight) over () as total_weight
        from game.encounter_table_entries entries
        join game.encounter_tables tables
            on tables.table_key = entries.table_key
        where entries.table_key = 'extract_hold'
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

    if selected_entry.entry_key is null or selected_entry.encounter_type = 'Extraction' then
        log_entries := game.raid_append_log(log_entries, 'You finish holding at extract without drawing contact.');
        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Extraction'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Extraction'))), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'The route settles down and extraction is available.'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
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
            format(
                'Found %s with %s lootable items.',
                coalesce(container_name, 'Medical Container'),
                jsonb_array_length(discovered_loot)));
        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Loot'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Loot'))), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'A searchable container appears.'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(coalesce(container_name, 'Medical Container'::text)), true);
        updated_payload := jsonb_set(updated_payload, '{discoveredLoot}', discovered_loot, true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
        updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
        return updated_payload;
    end if;

    enemy_health := selected_entry.enemy_health_min
        + floor(random() * (selected_entry.enemy_health_max_exclusive - selected_entry.enemy_health_min))::int;
    enemy_loadout := game.random_enemy_loadout_from_table(coalesce(selected_entry.enemy_loadout_table_key, 'default_enemy_loadout'));
    log_entries := game.raid_append_log(log_entries, format('Combat started vs %s.', selected_entry.enemy_name));
    updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Combat'::text), true);
    updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(coalesce(selected_entry.title, game.encounter_title('Combat'))), true);
    updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb(coalesce(selected_entry.description, 'Enemy contact on your position.'::text)), true);
    updated_payload := jsonb_set(updated_payload, '{contactState}', to_jsonb(coalesce(selected_entry.contact_state, 'MutualContact'::text)), true);
    updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(coalesce(selected_entry.enemy_name, ''::text)), true);
    updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(enemy_health), true);
    updated_payload := jsonb_set(updated_payload, '{enemyDexterity}', to_jsonb(10), true);
    updated_payload := jsonb_set(updated_payload, '{enemyConstitution}', to_jsonb(10), true);
    updated_payload := jsonb_set(updated_payload, '{enemyStrength}', to_jsonb(10), true);
    updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
    updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', enemy_loadout, true);

    if selected_entry.contact_state = 'PlayerAmbush' then
        updated_payload := jsonb_set(updated_payload, '{surpriseSide}', to_jsonb('Player'::text), true);
        updated_payload := jsonb_set(updated_payload, '{initiativeWinner}', to_jsonb('None'::text), true);
        updated_payload := jsonb_set(updated_payload, '{openingActionsRemaining}', to_jsonb(1), true);
    elsif selected_entry.contact_state = 'EnemyAmbush' then
        updated_payload := jsonb_set(updated_payload, '{surpriseSide}', to_jsonb('Enemy'::text), true);
        updated_payload := jsonb_set(updated_payload, '{initiativeWinner}', to_jsonb('None'::text), true);
        updated_payload := jsonb_set(updated_payload, '{openingActionsRemaining}', to_jsonb(1), true);
    else
        updated_payload := jsonb_set(updated_payload, '{surpriseSide}', to_jsonb('None'::text), true);
        updated_payload := jsonb_set(updated_payload, '{initiativeWinner}', to_jsonb(case when random() < 0.5 then 'Player'::text else 'Enemy'::text end), true);
        updated_payload := jsonb_set(updated_payload, '{openingActionsRemaining}', to_jsonb(0), true);
    end if;

    updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
    return updated_payload;
end;
$$;

do $$
declare
    function_sql text;
    search_text text := $patch$
        'challenge', 0,
        'distanceFromExtract', 3,
        'acceptedStats', resolved_stats,
        'encounterType', 'Neutral',
$patch$;
    replace_text text := $patch$
        'challenge', 0,
        'distanceFromExtract', 3,
        'acceptedStats', resolved_stats,
        'extractHoldActive', false,
        'holdAtExtractUntil', null,
        'encounterType', 'Neutral',
$patch$;
begin
    select pg_get_functiondef(to_regprocedure('game.build_raid_snapshot(jsonb, text, integer, jsonb)'))
    into function_sql;

    if function_sql is null then
        raise exception 'game.build_raid_snapshot(jsonb, text, integer, jsonb) not found';
    end if;

    if position('''extractHoldActive'', false' in function_sql) > 0 then
        return;
    end if;

    if position(search_text in function_sql) = 0 then
        raise exception 'Unable to apply extract-hold patch to game.build_raid_snapshot';
    end if;

    function_sql := replace(function_sql, search_text, replace_text);
    execute function_sql;
end;
$$;

do $$
declare
    function_sql text;
    declaration_search text := $patch$
    extraction_combat boolean;
    equipped_weapon jsonb;
$patch$;
    declaration_replace text := $patch$
    extraction_combat boolean;
    extract_hold_active boolean;
    hold_at_extract_until text;
    requested_hold_at_extract_until text;
    hold_deadline timestamptz;
    equipped_weapon jsonb;
$patch$;
    assignment_search text := $patch$
    extraction_combat := coalesce((raid_payload->>'extractionCombat')::boolean, false);
    equipped_weapon := game.raid_find_equipped_item(equipped_items, 0);
$patch$;
    assignment_replace text := $patch$
    extraction_combat := coalesce((raid_payload->>'extractionCombat')::boolean, false);
    extract_hold_active := coalesce((raid_payload->>'extractHoldActive')::boolean, false);
    hold_at_extract_until := nullif(coalesce(raid_payload->>'holdAtExtractUntil', ''), '');
    requested_hold_at_extract_until := nullif(coalesce(payload->>'holdAtExtractUntil', ''), '');
    equipped_weapon := game.raid_find_equipped_item(equipped_items, 0);
$patch$;
    action_search text := $patch$
    elsif action = 'go-deeper' then
        challenge := challenge + 1;
        distance_from_extract := distance_from_extract + 1;
        raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
        raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Moved deeper into the raid.'), true);
        raid_payload := game.generate_raid_encounter(raid_payload, false);
    elsif action = 'move-toward-extract' then
        loot_count := jsonb_array_length(discovered_loot);
        if encounter_type = 'Loot' and loot_count > 0 then
            log_entries := game.raid_append_log(log_entries, format('Moved on and left %s items behind.', loot_count));
            raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
        end if;

        distance_from_extract := greatest(distance_from_extract - 1, 0);
        raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);
        raid_payload := game.generate_raid_encounter(raid_payload, true);
    elsif action = 'stay-at-extract' then
        challenge := challenge + 1;
        raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
        raid_payload := game.generate_raid_encounter(raid_payload, false);
    elsif action = 'attempt-extract' then
        if distance_from_extract = 0 then
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Extraction completed. Loot secured.'), true);
            return game.finish_raid_session(save_payload, raid_payload, raid_profile, true, target_user_id);
        end if;
    end if;
$patch$;
    action_replace text := $patch$
    elsif action = 'go-deeper' then
        if extract_hold_active then
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Cancel the extract hold before moving.'), true);
        else
            challenge := challenge + 1;
            distance_from_extract := distance_from_extract + 1;
            raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
            raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Moved deeper into the raid.'), true);
            raid_payload := game.generate_raid_encounter(raid_payload, false);
        end if;
    elsif action = 'move-toward-extract' then
        if extract_hold_active then
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Cancel the extract hold before moving.'), true);
        else
            loot_count := jsonb_array_length(discovered_loot);
            if encounter_type = 'Loot' and loot_count > 0 then
                log_entries := game.raid_append_log(log_entries, format('Moved on and left %s items behind.', loot_count));
                raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
            end if;

            distance_from_extract := greatest(distance_from_extract - 1, 0);
            raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);
            raid_payload := game.generate_raid_encounter(raid_payload, true);
        end if;
    elsif action in ('stay-at-extract', 'start-extract-hold') then
        if encounter_type = 'Extraction' and distance_from_extract = 0 and not extract_hold_active then
            hold_at_extract_until := (timezone('utc', now()) + interval '30 seconds')::text;
            raid_payload := jsonb_set(raid_payload, '{extractHoldActive}', 'true'::jsonb, true);
            raid_payload := jsonb_set(raid_payload, '{holdAtExtractUntil}', to_jsonb(hold_at_extract_until), true);
            raid_payload := jsonb_set(raid_payload, '{encounterDescription}', to_jsonb('Holding at extract. Stay alert.'::text), true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You begin holding at extract.'), true);
        end if;
    elsif action = 'resolve-extract-hold' then
        if extract_hold_active and hold_at_extract_until is not null then
            if requested_hold_at_extract_until is null or requested_hold_at_extract_until <> hold_at_extract_until then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Hold resolution ignored because the request is stale.'), true);
            else
                hold_deadline := hold_at_extract_until::timestamptz;
                if timezone('utc', now()) < hold_deadline then
                    raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Hold is still in progress.'), true);
                else
                    challenge := challenge + 1;
                    raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
                    raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You finish holding at extract.'), true);
                    raid_payload := game.generate_extract_hold_encounter(raid_payload);
                end if;
            end if;
        end if;
    elsif action = 'cancel-extract-hold' then
        if extract_hold_active then
            raid_payload := game.clear_extract_hold_state(raid_payload);
            raid_payload := jsonb_set(raid_payload, '{encounterType}', to_jsonb('Extraction'::text), true);
            raid_payload := jsonb_set(raid_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Extraction')), true);
            raid_payload := jsonb_set(raid_payload, '{encounterDescription}', to_jsonb('You are near the extraction route.'::text), true);
            raid_payload := jsonb_set(raid_payload, '{enemyName}', to_jsonb(''::text), true);
            raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(0), true);
            raid_payload := jsonb_set(raid_payload, '{enemyDexterity}', to_jsonb(0), true);
            raid_payload := jsonb_set(raid_payload, '{enemyConstitution}', to_jsonb(0), true);
            raid_payload := jsonb_set(raid_payload, '{enemyStrength}', to_jsonb(0), true);
            raid_payload := jsonb_set(raid_payload, '{lootContainer}', to_jsonb(''::text), true);
            raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', '[]'::jsonb, true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You stop holding at extract.'), true);
        end if;
    elsif action = 'attempt-extract' then
        if extract_hold_active then
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Resolve or cancel the extract hold before extracting.'), true);
        elsif distance_from_extract = 0 then
            raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Extraction completed. Loot secured.'), true);
            return game.finish_raid_session(save_payload, raid_payload, raid_profile, true, target_user_id);
        end if;
    end if;
$patch$;
    final_state_search text := $patch$
    raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
    raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);

    current_encumbrance := game.current_encumbrance(
$patch$;
    final_state_replace text := $patch$
    raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
    raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);
    raid_payload := jsonb_set(raid_payload, '{extractHoldActive}', to_jsonb(coalesce((raid_payload->>'extractHoldActive')::boolean, false)), true);
    raid_payload := jsonb_set(raid_payload, '{holdAtExtractUntil}', coalesce(raid_payload->'holdAtExtractUntil', 'null'::jsonb), true);

    current_encumbrance := game.current_encumbrance(
$patch$;
begin
    select pg_get_functiondef(to_regprocedure('game.perform_raid_action(text, jsonb, uuid)'))
    into function_sql;

    if function_sql is null then
        raise exception 'game.perform_raid_action(text, jsonb, uuid) not found';
    end if;

    if position('start-extract-hold' in function_sql) > 0 then
        return;
    end if;

    if position(declaration_search in function_sql) = 0 then
        raise exception 'Unable to apply declaration patch to game.perform_raid_action';
    end if;

    if position(assignment_search in function_sql) = 0 then
        raise exception 'Unable to apply assignment patch to game.perform_raid_action';
    end if;

    if position(action_search in function_sql) = 0 then
        raise exception 'Unable to apply action patch to game.perform_raid_action';
    end if;

    if position(final_state_search in function_sql) = 0 then
        raise exception 'Unable to apply final-state patch to game.perform_raid_action';
    end if;

    function_sql := replace(function_sql, declaration_search, declaration_replace);
    function_sql := replace(function_sql, assignment_search, assignment_replace);
    function_sql := replace(function_sql, action_search, action_replace);
    function_sql := replace(function_sql, final_state_search, final_state_replace);
    execute function_sql;
end;
$$;

do $$
declare
    function_sql text;
    search_text text := $patch$
            'go-deeper',
            'move-toward-extract',
            'stay-at-extract',
            'attempt-extract')
$patch$;
    replace_text text := $patch$
            'go-deeper',
            'move-toward-extract',
            'stay-at-extract',
            'start-extract-hold',
            'resolve-extract-hold',
            'cancel-extract-hold',
            'attempt-extract')
$patch$;
begin
    select pg_get_functiondef(to_regprocedure('public.game_action(text, jsonb)'))
    into function_sql;

    if function_sql is null then
        raise exception 'public.game_action(text, jsonb) not found';
    end if;

    if position('''start-extract-hold''' in function_sql) > 0 then
        return;
    end if;

    if position(search_text in function_sql) = 0 then
        raise exception 'Unable to apply extract-hold patch to public.game_action';
    end if;

    function_sql := replace(function_sql, search_text, replace_text);
    execute function_sql;
end;
$$;

revoke all on function game.clear_extract_hold_state(jsonb) from public;
revoke all on function game.generate_extract_hold_encounter(jsonb) from public;
