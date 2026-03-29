create or replace function game.encounter_title(encounter_type text)
returns text
language sql
stable
as $$
    select case encounter_type
        when 'Combat' then 'Combat Encounter'
        when 'Loot' then 'Loot Encounter'
        when 'Extraction' then 'Extraction Opportunity'
        else 'Area Clear'
    end;
$$;

create or replace function game.raid_append_log(log_entries jsonb, entry_text text)
returns jsonb
language sql
stable
as $$
    select coalesce(log_entries, '[]'::jsonb) || jsonb_build_array(entry_text);
$$;

create or replace function game.raid_current_slots(carried_loot jsonb)
returns int
language sql
stable
as $$
    select coalesce(sum(coalesce((value->>'slots')::int, 1)), 0)
    from jsonb_array_elements(coalesce(carried_loot, '[]'::jsonb)) value;
$$;

create or replace function game.raid_find_equipped_item(equipped_items jsonb, slot_type int)
returns jsonb
language sql
stable
as $$
    select value
    from jsonb_array_elements(coalesce(equipped_items, '[]'::jsonb)) value
    where coalesce((value->>'type')::int, -1) = slot_type
    limit 1;
$$;

create or replace function game.raid_extractable_items(raid_payload jsonb)
returns jsonb
language sql
stable
as $$
    with base_items as (
        select value, ordinality
        from jsonb_array_elements(coalesce(raid_payload->'equippedItems', '[]'::jsonb)) with ordinality
        union all
        select value, 1000 + ordinality
        from jsonb_array_elements(coalesce(raid_payload->'carriedLoot', '[]'::jsonb)) with ordinality
        union all
        select jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 10, 'slots', 1, 'rarity', 0, 'displayRarity', 1), 2000 + ordinality
        from generate_series(1, greatest(coalesce((raid_payload->>'medkits')::int, 0), 0)) with ordinality
    )
    select coalesce(
        (
            select jsonb_agg(game.normalize_item(value) order by ordinality)
            from base_items
        ),
        '[]'::jsonb
    );
$$;

create or replace function game.random_enemy_loadout()
returns jsonb
language sql
volatile
as $$
    select case floor(random() * 5)::int
        when 0 then jsonb_build_array(
            jsonb_build_object('name', 'Makarov', 'type', 0, 'value', 12, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        )
        when 1 then jsonb_build_array(
            jsonb_build_object('name', 'PPSH', 'type', 0, 'value', 20, 'slots', 1, 'rarity', 1, 'displayRarity', 2),
            jsonb_build_object('name', 'Bandage', 'type', 4, 'value', 4, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
        )
        when 2 then jsonb_build_array(
            jsonb_build_object('name', 'AK74', 'type', 0, 'value', 34, 'slots', 1, 'rarity', 2, 'displayRarity', 3),
            jsonb_build_object('name', '6B2 body armor', 'type', 1, 'value', 14, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        )
        when 3 then jsonb_build_array(
            jsonb_build_object('name', 'SVDS', 'type', 0, 'value', 44, 'slots', 1, 'rarity', 3, 'displayRarity', 4)
        )
        else jsonb_build_array(
            jsonb_build_object('name', 'AK47', 'type', 0, 'value', 38, 'slots', 1, 'rarity', 2, 'displayRarity', 3),
            jsonb_build_object('name', 'FORT Defender-2', 'type', 1, 'value', 40, 'slots', 1, 'rarity', 3, 'displayRarity', 4)
        )
    end;
$$;

create or replace function game.random_loot_items_for_container(container_name text)
returns jsonb
language sql
volatile
as $$
    select case container_name
        when 'Weapons Crate' then
            case floor(random() * 4)::int
                when 0 then jsonb_build_array(
                    jsonb_build_object('name', 'Makarov', 'type', 0, 'value', 12, 'slots', 1, 'rarity', 0, 'displayRarity', 1),
                    jsonb_build_object('name', 'Ammo Box', 'type', 4, 'value', 6, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
                )
                when 1 then jsonb_build_array(
                    jsonb_build_object('name', 'PPSH', 'type', 0, 'value', 20, 'slots', 1, 'rarity', 1, 'displayRarity', 2)
                )
                when 2 then jsonb_build_array(
                    jsonb_build_object('name', 'AK74', 'type', 0, 'value', 34, 'slots', 1, 'rarity', 2, 'displayRarity', 3)
                )
                else jsonb_build_array(
                    jsonb_build_object('name', 'SVDS', 'type', 0, 'value', 44, 'slots', 1, 'rarity', 3, 'displayRarity', 4)
                )
            end
        when 'Medical Container' then
            case floor(random() * 3)::int
                when 0 then jsonb_build_array(
                    jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 10, 'slots', 1, 'rarity', 0, 'displayRarity', 1),
                    jsonb_build_object('name', 'Bandage', 'type', 4, 'value', 4, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
                )
                when 1 then jsonb_build_array(
                    jsonb_build_object('name', 'Bandage', 'type', 4, 'value', 4, 'slots', 1, 'rarity', 0, 'displayRarity', 0),
                    jsonb_build_object('name', 'Ammo Box', 'type', 4, 'value', 6, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
                )
                else jsonb_build_array(
                    jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 10, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
                )
            end
        when 'Dead Body' then game.random_enemy_loadout()
        else
            case floor(random() * 4)::int
                when 0 then jsonb_build_array(
                    jsonb_build_object('name', 'Bandage', 'type', 4, 'value', 4, 'slots', 1, 'rarity', 0, 'displayRarity', 0),
                    jsonb_build_object('name', 'Ammo Box', 'type', 4, 'value', 6, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
                )
                when 1 then jsonb_build_array(
                    jsonb_build_object('name', 'Scrap Metal', 'type', 5, 'value', 5, 'slots', 1, 'rarity', 0, 'displayRarity', 0),
                    jsonb_build_object('name', 'Rare Scope', 'type', 5, 'value', 16, 'slots', 1, 'rarity', 2, 'displayRarity', 0)
                )
                when 2 then jsonb_build_array(
                    jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 10, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
                )
                else jsonb_build_array(
                    jsonb_build_object('name', 'Makarov', 'type', 0, 'value', 12, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
                )
            end
    end;
$$;

create or replace function game.roll_weapon_damage(weapon_name text, attack_mode text)
returns int
language sql
volatile
as $$
    with bounds as (
        select case
            when weapon_name = 'PPSH' and attack_mode = 'standard' then 6
            when weapon_name = 'AK74' and attack_mode = 'standard' then 8
            when weapon_name = 'SVDS' and attack_mode = 'standard' then 11
            when weapon_name = 'AK47' and attack_mode = 'standard' then 9
            when weapon_name = 'PKP' and attack_mode = 'standard' then 12
            when weapon_name = 'Makarov' and attack_mode = 'burst' then 8
            when weapon_name = 'PPSH' and attack_mode = 'burst' then 10
            when weapon_name = 'AK74' and attack_mode = 'burst' then 12
            when weapon_name = 'SVDS' and attack_mode = 'burst' then 15
            when weapon_name = 'AK47' and attack_mode = 'burst' then 13
            when weapon_name = 'PKP' and attack_mode = 'burst' then 16
            else 5
        end as damage_min,
        case
            when weapon_name = 'PPSH' and attack_mode = 'standard' then 10
            when weapon_name = 'AK74' and attack_mode = 'standard' then 12
            when weapon_name = 'SVDS' and attack_mode = 'standard' then 16
            when weapon_name = 'AK47' and attack_mode = 'standard' then 14
            when weapon_name = 'PKP' and attack_mode = 'standard' then 18
            when weapon_name = 'Makarov' and attack_mode = 'burst' then 12
            when weapon_name = 'PPSH' and attack_mode = 'burst' then 15
            when weapon_name = 'AK74' and attack_mode = 'burst' then 17
            when weapon_name = 'SVDS' and attack_mode = 'burst' then 21
            when weapon_name = 'AK47' and attack_mode = 'burst' then 19
            when weapon_name = 'PKP' and attack_mode = 'burst' then 24
            else 8
        end as damage_max
    )
    select damage_min + floor(random() * (damage_max - damage_min + 1))::int
    from bounds;
$$;

create or replace function game.generate_raid_encounter(raid_payload jsonb, moving_to_extract boolean default false)
returns jsonb
language plpgsql
volatile
as $$
declare
    updated_payload jsonb := coalesce(raid_payload, '{}'::jsonb);
    challenge int := greatest(coalesce((updated_payload->>'challenge')::int, 0), 0);
    distance_from_extract int := greatest(coalesce((updated_payload->>'distanceFromExtract')::int, 0), 0);
    roll int;
    container_name text;
    discovered_loot jsonb;
    enemy_loadout jsonb;
    enemy_name text;
    enemy_health int;
    log_entries jsonb := coalesce(updated_payload->'logEntries', '[]'::jsonb);
begin
    if moving_to_extract then
        distance_from_extract := greatest(distance_from_extract - 1, 0);
    end if;

    updated_payload := jsonb_set(updated_payload, '{discoveredLoot}', '[]'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{awaitingDecision}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{extractionCombat}', 'false'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{challenge}', to_jsonb(challenge), true);
    updated_payload := jsonb_set(updated_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);

    if distance_from_extract = 0 and random() < 0.35 then
        log_entries := game.raid_append_log(log_entries, 'Extraction point located.');
        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Extraction'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Extraction')), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb('You are near the extraction route.'::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
        updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
        return updated_payload;
    end if;

    roll := floor(random() * 100)::int;
    if roll < 50 then
        enemy_name := case when random() < 0.65 then 'Scav' else 'Patrol Guard' end;
        enemy_health := 12 + floor(random() * 9)::int;
        enemy_loadout := game.random_enemy_loadout();
        log_entries := game.raid_append_log(log_entries, format('Combat started vs %s.', enemy_name));

        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Combat'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Combat')), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb('Enemy contact on your position.'::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(enemy_name), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(enemy_health), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', enemy_loadout, true);
        updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
        return updated_payload;
    end if;

    if roll < 80 then
        container_name := (array['Filing Cabinet', 'Weapons Crate', 'Medical Container', 'Dead Body'])[1 + floor(random() * 4)::int];
        discovered_loot := game.random_loot_items_for_container(container_name);
        log_entries := game.raid_append_log(
            log_entries,
            format('Found %s with %s lootable items.', container_name, jsonb_array_length(discovered_loot)));

        updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Loot'::text), true);
        updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Loot')), true);
        updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb('A searchable container appears.'::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
        updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
        updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(container_name), true);
        updated_payload := jsonb_set(updated_payload, '{discoveredLoot}', discovered_loot, true);
        updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
        updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
        return updated_payload;
    end if;

    log_entries := game.raid_append_log(log_entries, 'No enemies or loot found.');
    updated_payload := jsonb_set(updated_payload, '{encounterType}', to_jsonb('Neutral'::text), true);
    updated_payload := jsonb_set(updated_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Neutral')), true);
    updated_payload := jsonb_set(updated_payload, '{encounterDescription}', to_jsonb('Area looks quiet. Nothing useful here.'::text), true);
    updated_payload := jsonb_set(updated_payload, '{enemyName}', to_jsonb(''::text), true);
    updated_payload := jsonb_set(updated_payload, '{enemyHealth}', to_jsonb(0), true);
    updated_payload := jsonb_set(updated_payload, '{lootContainer}', to_jsonb(''::text), true);
    updated_payload := jsonb_set(updated_payload, '{enemyLoadout}', '[]'::jsonb, true);
    updated_payload := jsonb_set(updated_payload, '{logEntries}', log_entries, true);
    return updated_payload;
end;
$$;

create or replace function game.finish_raid_session(
    save_payload jsonb,
    raid_payload jsonb,
    raid_profile text,
    extracted boolean,
    target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    updated_save jsonb := game.normalize_save_payload(save_payload);
    stash jsonb := coalesce(updated_save->'mainStash', '[]'::jsonb);
    on_person_items jsonb := coalesce(updated_save->'onPersonItems', '[]'::jsonb);
    random_character jsonb := updated_save->'randomCharacter';
    random_available_at jsonb := updated_save->'randomCharacterAvailableAt';
    settled_random_state jsonb;
    extractable_items jsonb := case when extracted then game.raid_extractable_items(raid_payload) else '[]'::jsonb end;
begin
    if raid_profile = 'main' then
        if extracted then
            on_person_items := coalesce(
                (
                    select jsonb_agg(
                        jsonb_build_object(
                            'item', game.normalize_item(value),
                            'isEquipped', coalesce((value->>'type')::int, -1) in (0, 1, 2))
                        order by ordinality)
                    from jsonb_array_elements(extractable_items) with ordinality
                ),
                '[]'::jsonb
            );
        else
            on_person_items := '[]'::jsonb;
        end if;

        stash := game.ensure_knife_fallback(stash, on_person_items);
        updated_save := jsonb_set(updated_save, '{mainStash}', game.normalize_items(stash), true);
        updated_save := jsonb_set(updated_save, '{onPersonItems}', game.normalize_on_person_items(on_person_items), true);
    else
        if extracted and random_character is not null then
            random_character := jsonb_build_object(
                'name', coalesce(random_character->>'name', 'Unknown'),
                'inventory', game.normalize_items(extractable_items)
            );
        else
            random_character := null;
        end if;

        settled_random_state := game.settle_random_character(random_character, random_available_at);
        updated_save := jsonb_set(updated_save, '{randomCharacter}', coalesce(settled_random_state->'randomCharacter', 'null'::jsonb), true);
        updated_save := jsonb_set(updated_save, '{randomCharacterAvailableAt}', settled_random_state->'randomCharacterAvailableAt', true);
    end if;

    updated_save := jsonb_set(updated_save, '{activeRaid}', 'null'::jsonb, true);

    update public.game_saves
    set payload = updated_save,
        save_version = 1,
        updated_at = timezone('utc', now())
    where user_id = target_user_id;

    delete from public.raid_sessions
    where user_id = target_user_id;

    return updated_save;
end;
$$;

create or replace function game.start_raid_action(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    save_payload jsonb;
    on_person_items jsonb;
    random_character jsonb;
    loadout jsonb := '[]'::jsonb;
    raid_snapshot jsonb;
    raider_name text := 'Main Character';
    raid_profile text := 'main';
    equipped_entry jsonb;
    player_max_health int;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    on_person_items := coalesce(save_payload->'onPersonItems', '[]'::jsonb);
    random_character := save_payload->'randomCharacter';
    player_max_health := greatest(coalesce((save_payload->>'playerMaxHealth')::int, 30), 1);

    if action = 'start-main-raid' then
        if random_character is not null and jsonb_array_length(coalesce(random_character->'inventory', '[]'::jsonb)) > 0 then
            return save_payload;
        end if;

        if exists (
            select 1
            from jsonb_array_elements(on_person_items) entry
            where coalesce((coalesce(entry->'item', entry->'Item')->>'type')::int, -1) in (0, 1, 2)
              and not coalesce((entry->>'isEquipped')::boolean, (entry->>'IsEquipped')::boolean, false)
        ) then
            return save_payload;
        end if;

        if not exists (
            select 1
            from jsonb_array_elements(on_person_items) entry
            where coalesce((coalesce(entry->'item', entry->'Item')->>'type')::int, -1) = 0
              and coalesce((entry->>'isEquipped')::boolean, (entry->>'IsEquipped')::boolean, false)
        ) then
            return save_payload;
        end if;

        for equipped_entry in
            select coalesce(value->'item', value->'Item')
            from jsonb_array_elements(on_person_items) value
        loop
            loadout := loadout || jsonb_build_array(game.normalize_item(equipped_entry));
        end loop;
    elsif action = 'start-random-raid' then
        if random_character is not null and jsonb_array_length(coalesce(random_character->'inventory', '[]'::jsonb)) > 0 then
            return save_payload;
        end if;

        if (save_payload->>'randomCharacterAvailableAt')::timestamptz > timezone('utc', now()) then
            return save_payload;
        end if;

        random_character := jsonb_build_object(
            'name', game.random_raider_name(),
            'inventory', game.random_luck_run_loadout()
        );
        raid_profile := 'random';
        raider_name := random_character->>'name';
        loadout := coalesce(random_character->'inventory', '[]'::jsonb);
        save_payload := jsonb_set(save_payload, '{randomCharacter}', random_character, true);
    else
        return save_payload;
    end if;

    raid_snapshot := game.build_raid_snapshot(loadout, raider_name, player_max_health);
    raid_snapshot := jsonb_set(raid_snapshot, '{enemyLoadout}', '[]'::jsonb, true);
    raid_snapshot := jsonb_set(raid_snapshot, '{extractionCombat}', 'false'::jsonb, true);

    insert into public.raid_sessions (user_id, profile, payload)
    values (target_user_id, raid_profile, raid_snapshot)
    on conflict (user_id) do update
        set profile = excluded.profile,
            payload = excluded.payload,
            updated_at = timezone('utc', now());

    save_payload := jsonb_set(save_payload, '{activeRaid}', raid_snapshot, true);
    update public.game_saves
    set payload = save_payload,
        save_version = 1,
        updated_at = timezone('utc', now())
    where user_id = target_user_id;

    return save_payload;
end;
$$;

create or replace function game.perform_raid_action(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    save_payload jsonb;
    raid_payload jsonb;
    raid_profile text;
    equipped_items jsonb;
    carried_loot jsonb;
    discovered_loot jsonb;
    enemy_loadout jsonb;
    log_entries jsonb;
    encounter_type text;
    enemy_name text;
    enemy_health int;
    ammo int;
    medkits int;
    health int;
    backpack_capacity int;
    challenge int;
    distance_from_extract int;
    weapon_malfunction boolean;
    extraction_combat boolean;
    equipped_weapon jsonb;
    equipped_armor jsonb;
    selected_item jsonb;
    previous_item jsonb;
    item_name text;
    slot_type int;
    uses_ammo boolean;
    damage int;
    incoming int;
    reduced_damage int;
    current_slots int;
    dropped_item jsonb;
    loot_count int;
    enemy_dropped_items jsonb;
    player_max_health int;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    player_max_health := greatest(coalesce((save_payload->>'playerMaxHealth')::int, 30), 1);

    select raid_sessions.profile, raid_sessions.payload
    into raid_profile, raid_payload
    from public.raid_sessions
    where user_id = target_user_id;

    if raid_payload is null then
        return save_payload;
    end if;

    equipped_items := game.normalize_items(coalesce(raid_payload->'equippedItems', '[]'::jsonb));
    carried_loot := game.normalize_items(coalesce(raid_payload->'carriedLoot', '[]'::jsonb));
    discovered_loot := game.normalize_items(coalesce(raid_payload->'discoveredLoot', '[]'::jsonb));
    enemy_loadout := game.normalize_items(coalesce(raid_payload->'enemyLoadout', '[]'::jsonb));
    log_entries := coalesce(raid_payload->'logEntries', '[]'::jsonb);
    encounter_type := coalesce(raid_payload->>'encounterType', 'Neutral');
    enemy_name := coalesce(raid_payload->>'enemyName', '');
    enemy_health := greatest(coalesce((raid_payload->>'enemyHealth')::int, 0), 0);
    ammo := greatest(coalesce((raid_payload->>'ammo')::int, 0), 0);
    medkits := greatest(coalesce((raid_payload->>'medkits')::int, 0), 0);
    health := greatest(coalesce((raid_payload->>'health')::int, player_max_health), 0);
    challenge := greatest(coalesce((raid_payload->>'challenge')::int, 0), 0);
    distance_from_extract := greatest(coalesce((raid_payload->>'distanceFromExtract')::int, 0), 0);
    weapon_malfunction := coalesce((raid_payload->>'weaponMalfunction')::boolean, false);
    extraction_combat := coalesce((raid_payload->>'extractionCombat')::boolean, false);
    equipped_weapon := game.raid_find_equipped_item(equipped_items, 0);
    equipped_armor := game.raid_find_equipped_item(equipped_items, 1);
    backpack_capacity := game.backpack_capacity(coalesce(game.raid_find_equipped_item(equipped_items, 2)->>'name', ''));

    if action in ('attack', 'burst-fire') then
        if encounter_type <> 'Combat' then
            return save_payload;
        end if;

        uses_ammo := game.weapon_magazine_capacity(coalesce(equipped_weapon->>'name', 'Rusty Knife')) > 0;

        if action = 'attack' then
            if weapon_malfunction then
                log_entries := game.raid_append_log(log_entries, 'Weapon is malfunctioned. Reload to clear it.');
            elsif uses_ammo and ammo <= 0 then
                log_entries := game.raid_append_log(log_entries, 'No ammo.');
            else
                if not weapon_malfunction and random() < 0.10 then
                    weapon_malfunction := true;
                    log_entries := game.raid_append_log(log_entries, 'Weapon malfunctioned. Reload to clear it.');
                else
                    if uses_ammo then
                        ammo := ammo - 1;
                    end if;

                    damage := game.roll_weapon_damage(coalesce(equipped_weapon->>'name', 'Rusty Knife'), 'standard');
                    enemy_health := enemy_health - damage;
                    log_entries := game.raid_append_log(log_entries, format('You hit %s for %s.', enemy_name, damage));
                end if;
            end if;
        else
            if weapon_malfunction then
                log_entries := game.raid_append_log(log_entries, 'Weapon is malfunctioned. Reload to clear it.');
            elsif not uses_ammo or ammo < 2 then
                log_entries := game.raid_append_log(log_entries, 'Not enough ammo for Burst Fire.');
            else
                if not weapon_malfunction and random() < 0.10 then
                    weapon_malfunction := true;
                    log_entries := game.raid_append_log(log_entries, 'Weapon malfunctioned during Burst Fire. Reload to clear it.');
                else
                    ammo := ammo - 2;
                    damage := game.roll_weapon_damage(coalesce(equipped_weapon->>'name', 'Rusty Knife'), 'burst');
                    enemy_health := enemy_health - damage;
                    log_entries := game.raid_append_log(log_entries, format('Burst Fire deals %s.', damage));
                end if;
            end if;
        end if;

        if enemy_health <= 0 then
            if extraction_combat then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Final guard defeated. Extraction successful.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, true, target_user_id);
            end if;

            enemy_dropped_items := case
                when jsonb_array_length(enemy_loadout) > 0 then enemy_loadout
                else game.random_enemy_loadout()
            end;

            log_entries := game.raid_append_log(log_entries, format('Found Dead Body with %s lootable items.', jsonb_array_length(enemy_dropped_items)));
            raid_payload := jsonb_set(raid_payload, '{encounterType}', to_jsonb('Loot'::text), true);
            raid_payload := jsonb_set(raid_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Loot')), true);
            raid_payload := jsonb_set(raid_payload, '{encounterDescription}', to_jsonb('Enemy down. Check the body for loot.'::text), true);
            raid_payload := jsonb_set(raid_payload, '{enemyName}', to_jsonb(''::text), true);
            raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(0), true);
            raid_payload := jsonb_set(raid_payload, '{lootContainer}', to_jsonb('Dead Body'::text), true);
            raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', enemy_dropped_items, true);
            raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', '[]'::jsonb, true);
            raid_payload := jsonb_set(raid_payload, '{awaitingDecision}', 'false'::jsonb, true);
            raid_payload := jsonb_set(raid_payload, '{weaponMalfunction}', to_jsonb(weapon_malfunction), true);
            raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
        else
            incoming := 3 + floor(random() * 6)::int;
            reduced_damage := greatest(1, incoming - case
                when coalesce(equipped_armor->>'name', '') = 'NFM THOR' then 6
                when coalesce(equipped_armor->>'name', '') = '6B43 Zabralo-Sh body armor' then 5
                when coalesce(equipped_armor->>'name', '') = 'FORT Defender-2' then 4
                when coalesce(equipped_armor->>'name', '') = '6B13 assault armor' then 3
                when coalesce(equipped_armor->>'name', '') = '6B2 body armor' then 1
                else 0
            end);
            health := greatest(health - reduced_damage, 0);
            log_entries := game.raid_append_log(log_entries, format('%s hits you for %s.', enemy_name, reduced_damage));

            if health <= 0 then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You were killed in raid. Loadout and loot lost.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, false, target_user_id);
            end if;

            raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(enemy_health), true);
            raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(health), true);
            raid_payload := jsonb_set(raid_payload, '{weaponMalfunction}', to_jsonb(weapon_malfunction), true);
            raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
            raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
        end if;
    elsif action = 'use-medkit' then
        if medkits > 0 then
            medkits := medkits - 1;
            health := least(player_max_health, health + 10);
            log_entries := game.raid_append_log(log_entries, 'Medkit used (+10 HP).');

            if encounter_type = 'Combat' then
                incoming := 3 + floor(random() * 6)::int;
                reduced_damage := greatest(1, incoming - case
                    when coalesce(equipped_armor->>'name', '') = 'NFM THOR' then 6
                    when coalesce(equipped_armor->>'name', '') = '6B43 Zabralo-Sh body armor' then 5
                    when coalesce(equipped_armor->>'name', '') = 'FORT Defender-2' then 4
                    when coalesce(equipped_armor->>'name', '') = '6B13 assault armor' then 3
                    when coalesce(equipped_armor->>'name', '') = '6B2 body armor' then 1
                    else 0
                end);
                health := greatest(health - reduced_damage, 0);
                log_entries := game.raid_append_log(log_entries, format('%s hits you for %s.', enemy_name, reduced_damage));

                if health <= 0 then
                    raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You were killed in raid. Loadout and loot lost.'), true);
                    return game.finish_raid_session(save_payload, raid_payload, raid_profile, false, target_user_id);
                end if;
            end if;
        end if;

        raid_payload := jsonb_set(raid_payload, '{medkits}', to_jsonb(medkits), true);
        raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(health), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action = 'reload' then
        if encounter_type = 'Combat' then
            uses_ammo := game.weapon_magazine_capacity(coalesce(equipped_weapon->>'name', 'Rusty Knife')) > 0;
            if not uses_ammo then
                log_entries := game.raid_append_log(log_entries, 'Knife doesn''t need reloading.');
            else
                ammo := game.weapon_magazine_capacity(coalesce(equipped_weapon->>'name', 'Rusty Knife'));
                weapon_malfunction := false;
                log_entries := game.raid_append_log(log_entries, 'Weapon reloaded and cleared.');
            end if;

            incoming := 3 + floor(random() * 6)::int;
            reduced_damage := greatest(1, incoming - case
                when coalesce(equipped_armor->>'name', '') = 'NFM THOR' then 6
                when coalesce(equipped_armor->>'name', '') = '6B43 Zabralo-Sh body armor' then 5
                when coalesce(equipped_armor->>'name', '') = 'FORT Defender-2' then 4
                when coalesce(equipped_armor->>'name', '') = '6B13 assault armor' then 3
                when coalesce(equipped_armor->>'name', '') = '6B2 body armor' then 1
                else 0
            end);
            health := greatest(health - reduced_damage, 0);
            log_entries := game.raid_append_log(log_entries, format('%s hits you for %s.', enemy_name, reduced_damage));

            if health <= 0 then
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You were killed in raid. Loadout and loot lost.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, false, target_user_id);
            end if;
        end if;

        raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
        raid_payload := jsonb_set(raid_payload, '{weaponMalfunction}', to_jsonb(weapon_malfunction), true);
        raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(health), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action = 'flee' then
        if encounter_type = 'Combat' then
            if random() < 0.15 then
                log_entries := game.raid_append_log(log_entries, 'Flee succeeded.');
                raid_payload := jsonb_set(raid_payload, '{encounterType}', to_jsonb('Neutral'::text), true);
                raid_payload := jsonb_set(raid_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Neutral')), true);
                raid_payload := jsonb_set(raid_payload, '{encounterDescription}', to_jsonb('Choose your next move.'::text), true);
                raid_payload := jsonb_set(raid_payload, '{enemyName}', to_jsonb(''::text), true);
                raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(0), true);
                raid_payload := jsonb_set(raid_payload, '{awaitingDecision}', 'true'::jsonb, true);
                raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', '[]'::jsonb, true);
                raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
            else
                log_entries := game.raid_append_log(log_entries, 'Flee failed.');
                incoming := 3 + floor(random() * 6)::int;
                reduced_damage := greatest(1, incoming - case
                    when coalesce(equipped_armor->>'name', '') = 'NFM THOR' then 6
                    when coalesce(equipped_armor->>'name', '') = '6B43 Zabralo-Sh body armor' then 5
                    when coalesce(equipped_armor->>'name', '') = 'FORT Defender-2' then 4
                    when coalesce(equipped_armor->>'name', '') = '6B13 assault armor' then 3
                    when coalesce(equipped_armor->>'name', '') = '6B2 body armor' then 1
                    else 0
                end);
                health := greatest(health - reduced_damage, 0);
                log_entries := game.raid_append_log(log_entries, format('%s hits you for %s.', enemy_name, reduced_damage));

                if health <= 0 then
                    raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'You were killed in raid. Loadout and loot lost.'), true);
                    return game.finish_raid_session(save_payload, raid_payload, raid_profile, false, target_user_id);
                end if;

                raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(health), true);
                raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
            end if;
        end if;
    elsif action = 'take-loot' then
        item_name := payload->>'itemName';
        selected_item := (
            select value
            from jsonb_array_elements(discovered_loot) value
            where value->>'name' = item_name
            limit 1
        );

        if selected_item is not null then
            if item_name = 'Medkit' then
                discovered_loot := game.jsonb_array_remove(
                    discovered_loot,
                    coalesce((select ordinality::int - 1 from jsonb_array_elements(discovered_loot) with ordinality value where value->>'name' = item_name limit 1), -1));
                medkits := medkits + 1;
                log_entries := game.raid_append_log(log_entries, format('Looted %s.', item_name));
            else
                current_slots := game.raid_current_slots(carried_loot);
                if current_slots + coalesce((selected_item->>'slots')::int, 1) <= backpack_capacity then
                    discovered_loot := game.jsonb_array_remove(
                        discovered_loot,
                        coalesce((select ordinality::int - 1 from jsonb_array_elements(discovered_loot) with ordinality value where value->>'name' = item_name limit 1), -1));
                    carried_loot := carried_loot || jsonb_build_array(game.normalize_item(selected_item));
                    log_entries := game.raid_append_log(log_entries, format('Looted %s.', item_name));
                else
                    log_entries := game.raid_append_log(log_entries, format('Could not loot %s: backpack full.', item_name));
                end if;
            end if;
        end if;

        raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', discovered_loot, true);
        raid_payload := jsonb_set(raid_payload, '{carriedLoot}', carried_loot, true);
        raid_payload := jsonb_set(raid_payload, '{medkits}', to_jsonb(medkits), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action = 'drop-carried' then
        item_name := payload->>'itemName';
        selected_item := (
            select value
            from jsonb_array_elements(carried_loot) value
            where value->>'name' = item_name
            limit 1
        );
        if selected_item is not null then
            carried_loot := game.jsonb_array_remove(
                carried_loot,
                coalesce((select ordinality::int - 1 from jsonb_array_elements(carried_loot) with ordinality value where value->>'name' = item_name limit 1), -1));
            discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(selected_item));
            log_entries := game.raid_append_log(log_entries, format('Dropped %s.', item_name));
        end if;

        raid_payload := jsonb_set(raid_payload, '{carriedLoot}', carried_loot, true);
        raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', discovered_loot, true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action = 'drop-equipped' then
        slot_type := case upper(coalesce(payload->>'slotType', ''))
            when 'WEAPON' then 0
            when 'ARMOR' then 1
            when 'BACKPACK' then 2
            else -1
        end;
        selected_item := game.raid_find_equipped_item(equipped_items, slot_type);

        if selected_item is not null then
            equipped_items := coalesce(
                (
                    select jsonb_agg(value order by ordinality)
                    from jsonb_array_elements(equipped_items) with ordinality
                    where coalesce((value->>'type')::int, -1) <> slot_type
                ),
                '[]'::jsonb
            );

            if slot_type = 2 then
                discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(selected_item)) || carried_loot;
                carried_loot := '[]'::jsonb;
                backpack_capacity := game.backpack_capacity('');
            else
                discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(selected_item));
            end if;

            if slot_type = 0 then
                weapon_malfunction := false;
                ammo := 0;
            end if;

            log_entries := game.raid_append_log(log_entries, format('Dropped equipped %s.', initcap(lower(payload->>'slotType'))));
        end if;

        raid_payload := jsonb_set(raid_payload, '{equippedItems}', equipped_items, true);
        raid_payload := jsonb_set(raid_payload, '{carriedLoot}', carried_loot, true);
        raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', discovered_loot, true);
        raid_payload := jsonb_set(raid_payload, '{backpackCapacity}', to_jsonb(backpack_capacity), true);
        raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
        raid_payload := jsonb_set(raid_payload, '{weaponMalfunction}', to_jsonb(weapon_malfunction), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action in ('equip-from-discovered', 'equip-from-carried') then
        item_name := payload->>'itemName';
        if action = 'equip-from-discovered' then
            selected_item := (
                select value
                from jsonb_array_elements(discovered_loot) value
                where value->>'name' = item_name
                limit 1
            );
        else
            selected_item := (
                select value
                from jsonb_array_elements(carried_loot) value
                where value->>'name' = item_name
                limit 1
            );
        end if;

        if selected_item is not null and coalesce((selected_item->>'type')::int, -1) in (0, 1, 2) then
            slot_type := coalesce((selected_item->>'type')::int, -1);
            if action = 'equip-from-discovered' then
                discovered_loot := game.jsonb_array_remove(
                    discovered_loot,
                    coalesce((select ordinality::int - 1 from jsonb_array_elements(discovered_loot) with ordinality value where value->>'name' = item_name limit 1), -1));
            else
                carried_loot := game.jsonb_array_remove(
                    carried_loot,
                    coalesce((select ordinality::int - 1 from jsonb_array_elements(carried_loot) with ordinality value where value->>'name' = item_name limit 1), -1));
            end if;

            previous_item := game.raid_find_equipped_item(equipped_items, slot_type);
            if previous_item is not null then
                discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(previous_item));
                equipped_items := coalesce(
                    (
                        select jsonb_agg(value order by ordinality)
                        from jsonb_array_elements(equipped_items) with ordinality
                        where coalesce((value->>'type')::int, -1) <> slot_type
                    ),
                    '[]'::jsonb
                );
            end if;

            equipped_items := equipped_items || jsonb_build_array(game.normalize_item(selected_item));

            if slot_type = 2 then
                backpack_capacity := game.backpack_capacity(selected_item->>'name');
                while game.raid_current_slots(carried_loot) > backpack_capacity and jsonb_array_length(carried_loot) > 0 loop
                    dropped_item := game.jsonb_array_get(carried_loot, jsonb_array_length(carried_loot) - 1);
                    carried_loot := game.jsonb_array_remove(carried_loot, jsonb_array_length(carried_loot) - 1);
                    discovered_loot := discovered_loot || jsonb_build_array(game.normalize_item(dropped_item));
                end loop;
            end if;

            if slot_type = 0 then
                ammo := least(greatest(ammo, 0), game.weapon_magazine_capacity(selected_item->>'name'));
                weapon_malfunction := false;
            end if;

            log_entries := game.raid_append_log(
                log_entries,
                format('Equipped %s from %s loot.', item_name, case when action = 'equip-from-discovered' then 'discovered' else 'carried' end));
        end if;

        raid_payload := jsonb_set(raid_payload, '{equippedItems}', equipped_items, true);
        raid_payload := jsonb_set(raid_payload, '{carriedLoot}', carried_loot, true);
        raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', discovered_loot, true);
        raid_payload := jsonb_set(raid_payload, '{backpackCapacity}', to_jsonb(backpack_capacity), true);
        raid_payload := jsonb_set(raid_payload, '{ammo}', to_jsonb(greatest(ammo, 0)), true);
        raid_payload := jsonb_set(raid_payload, '{weaponMalfunction}', to_jsonb(weapon_malfunction), true);
        raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
    elsif action in ('go-deeper', 'move-toward-extract', 'stay-at-extract') then
        loot_count := jsonb_array_length(discovered_loot);
        if encounter_type = 'Loot' and loot_count > 0 then
            log_entries := game.raid_append_log(log_entries, format('Moved on and left %s items behind.', loot_count));
            raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
        end if;

        raid_payload := game.generate_raid_encounter(raid_payload, action = 'move-toward-extract');
    elsif action = 'attempt-extract' then
        if encounter_type = 'Extraction' then
            if random() < 0.50 then
                log_entries := game.raid_append_log(log_entries, 'Extraction ambush.');
                raid_payload := jsonb_set(raid_payload, '{encounterType}', to_jsonb('Combat'::text), true);
                raid_payload := jsonb_set(raid_payload, '{encounterTitle}', to_jsonb(game.encounter_title('Combat')), true);
                raid_payload := jsonb_set(raid_payload, '{encounterDescription}', to_jsonb('Extraction ambush engaged.'::text), true);
                raid_payload := jsonb_set(raid_payload, '{enemyName}', to_jsonb('Final Guard'::text), true);
                raid_payload := jsonb_set(raid_payload, '{enemyHealth}', to_jsonb(22), true);
                raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', game.random_enemy_loadout(), true);
                raid_payload := jsonb_set(raid_payload, '{extractionCombat}', 'true'::jsonb, true);
                raid_payload := jsonb_set(raid_payload, '{logEntries}', log_entries, true);
            else
                raid_payload := jsonb_set(raid_payload, '{logEntries}', game.raid_append_log(log_entries, 'Extraction completed. Loot secured.'), true);
                return game.finish_raid_session(save_payload, raid_payload, raid_profile, true, target_user_id);
            end if;
        end if;
    end if;

    raid_payload := jsonb_set(raid_payload, '{equippedItems}', game.normalize_items(coalesce(raid_payload->'equippedItems', equipped_items)), true);
    raid_payload := jsonb_set(raid_payload, '{carriedLoot}', game.normalize_items(coalesce(raid_payload->'carriedLoot', carried_loot)), true);
    raid_payload := jsonb_set(raid_payload, '{discoveredLoot}', game.normalize_items(coalesce(raid_payload->'discoveredLoot', discovered_loot)), true);
    raid_payload := jsonb_set(raid_payload, '{enemyLoadout}', game.normalize_items(coalesce(raid_payload->'enemyLoadout', enemy_loadout)), true);
    raid_payload := jsonb_set(raid_payload, '{medkits}', to_jsonb(greatest(coalesce((raid_payload->>'medkits')::int, medkits), 0)), true);
    raid_payload := jsonb_set(raid_payload, '{health}', to_jsonb(greatest(coalesce((raid_payload->>'health')::int, health), 0)), true);
    raid_payload := jsonb_set(raid_payload, '{backpackCapacity}', to_jsonb(game.backpack_capacity(coalesce(game.raid_find_equipped_item(coalesce(raid_payload->'equippedItems', equipped_items), 2)->>'name', ''))), true);
    raid_payload := jsonb_set(raid_payload, '{lootSlots}', to_jsonb(game.raid_current_slots(coalesce(raid_payload->'carriedLoot', carried_loot))), true);
    challenge := greatest(coalesce((raid_payload->>'challenge')::int, challenge), 0);
    distance_from_extract := greatest(coalesce((raid_payload->>'distanceFromExtract')::int, distance_from_extract), 0);
    raid_payload := jsonb_set(raid_payload, '{challenge}', to_jsonb(challenge), true);
    raid_payload := jsonb_set(raid_payload, '{distanceFromExtract}', to_jsonb(distance_from_extract), true);

    update public.raid_sessions
    set payload = raid_payload,
        updated_at = timezone('utc', now())
    where user_id = target_user_id;

    save_payload := jsonb_set(save_payload, '{activeRaid}', raid_payload, true);
    update public.game_saves
    set payload = save_payload,
        save_version = 1,
        updated_at = timezone('utc', now())
    where user_id = target_user_id;

    return save_payload;
end;
$$;

create or replace function public.profile_save(snapshot jsonb)
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
begin
    raise exception 'Profile saving is no longer supported. Use action endpoints.';
end;
$$;

create or replace function public.game_action(action text, payload jsonb)
returns jsonb
language sql
security definer
set search_path = public, auth, game
as $$
    select case
        when action in ('start-main-raid', 'start-random-raid')
            then game.start_raid_action(action, payload, auth.uid())
        when action in (
            'attack',
            'burst-fire',
            'reload',
            'flee',
            'use-medkit',
            'take-loot',
            'drop-carried',
            'drop-equipped',
            'equip-from-discovered',
            'equip-from-carried',
            'go-deeper',
            'move-toward-extract',
            'stay-at-extract',
            'attempt-extract')
            then game.perform_raid_action(action, payload, auth.uid())
        else game.apply_profile_action(action, payload, auth.uid())
    end;
$$;

-- go-deeper
-- move-toward-extract
-- stay-at-extract
-- attempt-extract
-- challenge := challenge + 1
-- distanceFromExtract := greatest(distanceFromExtract - 1, 0)
-- if distanceFromExtract = 0 then
-- distanceFromExtract := distanceFromExtract + 1
-- select raid_sessions.profile, raid_sessions.payload

revoke all on function game.encounter_title(text) from public;
revoke all on function game.raid_append_log(jsonb, text) from public;
revoke all on function game.raid_current_slots(jsonb) from public;
revoke all on function game.raid_find_equipped_item(jsonb, int) from public;
revoke all on function game.raid_extractable_items(jsonb) from public;
revoke all on function game.random_enemy_loadout() from public;
revoke all on function game.random_loot_items_for_container(text) from public;
revoke all on function game.roll_weapon_damage(text, text) from public;
revoke all on function game.generate_raid_encounter(jsonb, boolean) from public;
revoke all on function game.finish_raid_session(jsonb, jsonb, text, boolean, uuid) from public;
revoke all on function game.perform_raid_action(text, jsonb, uuid) from public;
revoke all on function public.profile_save(jsonb) from public;
revoke execute on function public.profile_save(jsonb) from authenticated;
