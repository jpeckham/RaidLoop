create or replace function game.normalize_save_payload(payload jsonb)
returns jsonb
language plpgsql
volatile
as $$
declare
    settled_random_state jsonb := game.settle_random_character(
        coalesce(payload->'randomCharacter', payload->'RandomCharacter'),
        coalesce(payload->'randomCharacterAvailableAt', payload->'RandomCharacterAvailableAt', to_jsonb('0001-01-01T00:00:00+00:00'::text)));
    accepted_stats_source jsonb := coalesce(payload->'acceptedStats', payload->'AcceptedStats', '{}'::jsonb);
    draft_stats_source jsonb := coalesce(payload->'draftStats', payload->'DraftStats', '{}'::jsonb);
begin
    return jsonb_build_object(
        'money', greatest(coalesce((payload->>'money')::int, (payload->>'Money')::int, 0), 0),
        'acceptedStats', jsonb_build_object(
            'strength', coalesce((accepted_stats_source->>'strength')::int, (accepted_stats_source->>'Strength')::int, 8),
            'dexterity', coalesce((accepted_stats_source->>'dexterity')::int, (accepted_stats_source->>'Dexterity')::int, 8),
            'constitution', coalesce((accepted_stats_source->>'constitution')::int, (accepted_stats_source->>'Constitution')::int, 8),
            'intelligence', coalesce((accepted_stats_source->>'intelligence')::int, (accepted_stats_source->>'Intelligence')::int, 8),
            'wisdom', coalesce((accepted_stats_source->>'wisdom')::int, (accepted_stats_source->>'Wisdom')::int, 8),
            'charisma', coalesce((accepted_stats_source->>'charisma')::int, (accepted_stats_source->>'Charisma')::int, 8)
        ),
        'draftStats', jsonb_build_object(
            'strength', coalesce((draft_stats_source->>'strength')::int, (draft_stats_source->>'Strength')::int, 8),
            'dexterity', coalesce((draft_stats_source->>'dexterity')::int, (draft_stats_source->>'Dexterity')::int, 8),
            'constitution', coalesce((draft_stats_source->>'constitution')::int, (draft_stats_source->>'Constitution')::int, 8),
            'intelligence', coalesce((draft_stats_source->>'intelligence')::int, (draft_stats_source->>'Intelligence')::int, 8),
            'wisdom', coalesce((draft_stats_source->>'wisdom')::int, (draft_stats_source->>'Wisdom')::int, 8),
            'charisma', coalesce((draft_stats_source->>'charisma')::int, (draft_stats_source->>'Charisma')::int, 8)
        ),
        'availableStatPoints', coalesce((payload->>'availableStatPoints')::int, (payload->>'AvailableStatPoints')::int, 27),
        'statsAccepted', coalesce((payload->>'statsAccepted')::boolean, (payload->>'StatsAccepted')::boolean, false),
        'playerDexterity', coalesce(
            (accepted_stats_source->>'dexterity')::int,
            (accepted_stats_source->>'Dexterity')::int,
            (payload->>'playerDexterity')::int,
            (payload->>'PlayerDexterity')::int,
            8),
        'playerConstitution', coalesce(
            (accepted_stats_source->>'constitution')::int,
            (accepted_stats_source->>'Constitution')::int,
            (payload->>'playerConstitution')::int,
            (payload->>'PlayerConstitution')::int,
            8),
        'playerMaxHealth', coalesce(
            (payload->>'playerMaxHealth')::int,
            (payload->>'PlayerMaxHealth')::int,
            10 + (2 * coalesce(
                (accepted_stats_source->>'constitution')::int,
                (accepted_stats_source->>'Constitution')::int,
                (payload->>'playerConstitution')::int,
                (payload->>'PlayerConstitution')::int,
                8))),
        'mainStash', game.normalize_items(coalesce(payload->'mainStash', payload->'MainStash')),
        'onPersonItems', game.normalize_on_person_items(coalesce(payload->'onPersonItems', payload->'OnPersonItems')),
        'randomCharacterAvailableAt', settled_random_state->'randomCharacterAvailableAt',
        'randomCharacter', settled_random_state->'randomCharacter',
        'activeRaid', coalesce(payload->'activeRaid', payload->'ActiveRaid', 'null'::jsonb)
    );
end;
$$;

create or replace function game.default_save_payload()
returns jsonb
language sql
stable
as $$
    select game.normalize_save_payload(
        jsonb_build_object(
            'mainStash', jsonb_build_array(
                jsonb_build_object('name', 'Makarov', 'type', 0, 'value', 12, 'slots', 1, 'rarity', 0, 'displayRarity', 1),
                jsonb_build_object('name', 'PPSH', 'type', 0, 'value', 20, 'slots', 1, 'rarity', 1, 'displayRarity', 2),
                jsonb_build_object('name', 'AK74', 'type', 0, 'value', 34, 'slots', 1, 'rarity', 2, 'displayRarity', 3),
                jsonb_build_object('name', '6B2 body armor', 'type', 1, 'value', 14, 'slots', 1, 'rarity', 0, 'displayRarity', 1),
                jsonb_build_object('name', '6B13 assault armor', 'type', 1, 'value', 30, 'slots', 1, 'rarity', 2, 'displayRarity', 3),
                jsonb_build_object('name', 'Small Backpack', 'type', 2, 'value', 18, 'slots', 1, 'rarity', 1, 'displayRarity', 2),
                jsonb_build_object('name', 'Tactical Backpack', 'type', 2, 'value', 28, 'slots', 2, 'rarity', 2, 'displayRarity', 3),
                jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 10, 'slots', 1, 'rarity', 0, 'displayRarity', 1),
                jsonb_build_object('name', 'Bandage', 'type', 4, 'value', 4, 'slots', 1, 'rarity', 0, 'displayRarity', 0),
                jsonb_build_object('name', 'Ammo Box', 'type', 4, 'value', 6, 'slots', 1, 'rarity', 0, 'displayRarity', 0)
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
$$;

create or replace function game.build_raid_snapshot(loadout jsonb, raider_name text, player_max_health int, accepted_stats jsonb default null)
returns jsonb
language plpgsql
volatile
as $$
declare
    equipped_items jsonb := '[]'::jsonb;
    carried_loot jsonb := '[]'::jsonb;
    discovered_loot jsonb := '[]'::jsonb;
    medkits int := 0;
    equipped_weapon_name text := 'Rusty Knife';
    equipped_backpack_name text := '';
    raid_payload jsonb;
    entry jsonb;
    accepted_stats jsonb := coalesce(accepted_stats, jsonb_build_object(
        'strength', 8,
        'dexterity', 8,
        'constitution', 8,
        'intelligence', 8,
        'wisdom', 8,
        'charisma', 8
    ));
begin
    for entry in
        select value
        from jsonb_array_elements(coalesce(loadout, '[]'::jsonb))
    loop
        if coalesce((entry->>'type')::int, -1) in (0, 1, 2) then
            equipped_items := equipped_items || jsonb_build_array(entry);
            if coalesce((entry->>'type')::int, -1) = 0 then
                equipped_weapon_name := entry->>'name';
            elsif coalesce((entry->>'type')::int, -1) = 2 then
                equipped_backpack_name := entry->>'name';
            end if;
        elsif entry->>'name' = 'Medkit' then
            medkits := medkits + 1;
        else
            carried_loot := carried_loot || jsonb_build_array(entry);
        end if;
    end loop;

    raid_payload := jsonb_build_object(
        'health', greatest(coalesce(player_max_health, 26), 1),
        'backpackCapacity', game.backpack_capacity(equipped_backpack_name),
        'ammo', game.weapon_magazine_capacity(equipped_weapon_name),
        'weaponMalfunction', false,
        'medkits', medkits,
        'lootSlots', 0,
        'challenge', 0,
        'distanceFromExtract', 3,
        'acceptedStats', accepted_stats,
        'encounterType', 'Neutral',
        'encounterTitle', 'Area Clear',
        'encounterDescription', 'Area looks quiet. Nothing useful here.',
        'enemyName', '',
        'enemyHealth', 0,
        'enemyDexterity', 0,
        'enemyConstitution', 0,
        'enemyStrength', 0,
        'lootContainer', '',
        'enemyLoadout', '[]'::jsonb,
        'awaitingDecision', false,
        'discoveredLoot', discovered_loot,
        'carriedLoot', carried_loot,
        'equippedItems', equipped_items,
        'logEntries', jsonb_build_array(format('Raid started as %s.', raider_name))
    );

    return game.generate_raid_encounter(raid_payload, false);
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
    player_max_health int;
    accepted_stats jsonb;
    entry jsonb;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    on_person_items := coalesce(save_payload->'onPersonItems', '[]'::jsonb);
    random_character := save_payload->'randomCharacter';
    player_max_health := greatest(coalesce((save_payload->>'playerMaxHealth')::int, 26), 1);
    accepted_stats := coalesce(save_payload->'acceptedStats', jsonb_build_object(
        'strength', 8,
        'dexterity', 8,
        'constitution', 8,
        'intelligence', 8,
        'wisdom', 8,
        'charisma', 8
    ));

    if not coalesce((save_payload->>'statsAccepted')::boolean, false) then
        return save_payload;
    end if;

    if action = 'start-main-raid' then
        if random_character is not null and jsonb_array_length(coalesce(random_character->'inventory', '[]'::jsonb)) > 0 then
            return save_payload;
        end if;

        for entry in
            select coalesce(value->'item', value->'Item')
            from jsonb_array_elements(on_person_items) value
        loop
            loadout := loadout || jsonb_build_array(game.normalize_item(entry));
        end loop;

        raid_snapshot := game.build_raid_snapshot(loadout, raider_name, player_max_health, accepted_stats);
        return jsonb_set(save_payload, '{activeRaid}', raid_snapshot, true);
    elsif action = 'start-random-raid' then
        if (save_payload->>'randomCharacterAvailableAt')::timestamptz > timezone('utc', now()) then
            return save_payload;
        end if;

        random_character := jsonb_build_object(
            'name', game.random_raider_name(),
            'inventory', game.random_luck_run_loadout()
        );
        raider_name := random_character->>'name';
        loadout := coalesce(random_character->'inventory', '[]'::jsonb);
        raid_snapshot := game.build_raid_snapshot(loadout, raider_name, player_max_health, accepted_stats);
        save_payload := jsonb_set(save_payload, '{randomCharacter}', random_character, true);
        return jsonb_set(save_payload, '{activeRaid}', raid_snapshot, true);
    end if;

    return save_payload;
end;
$$;

create or replace function game.apply_profile_action(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $$
declare
    save_payload jsonb;
    stash jsonb;
    on_person_items jsonb;
    random_character jsonb;
    random_inventory jsonb;
    random_available_at jsonb;
    settled_random_state jsonb;
    active_raid jsonb;
    accepted_stats jsonb;
    draft_stats jsonb;
    normalized_draft_stats jsonb;
    selected_item jsonb;
    selected_entry jsonb;
    selected_name text;
    selected_type int;
    selected_value int;
    idx int;
    item_name text;
    price int;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    stash := coalesce(save_payload->'mainStash', '[]'::jsonb);
    on_person_items := coalesce(save_payload->'onPersonItems', '[]'::jsonb);
    random_character := save_payload->'randomCharacter';
    random_available_at := save_payload->'randomCharacterAvailableAt';
    active_raid := save_payload->'activeRaid';
    accepted_stats := coalesce(save_payload->'acceptedStats', jsonb_build_object(
        'strength', 8,
        'dexterity', 8,
        'constitution', 8,
        'intelligence', 8,
        'wisdom', 8,
        'charisma', 8
    ));
    draft_stats := coalesce(save_payload->'draftStats', accepted_stats);

    case action
        when 'sell-stash-item' then
            idx := coalesce((payload->>'stashIndex')::int, -1);
            selected_item := game.jsonb_array_get(stash, idx);
            selected_name := coalesce(selected_item->>'name', '');
            selected_value := coalesce((selected_item->>'value')::int, 0);

            if selected_item is not null and selected_name <> 'Rusty Knife' and selected_value > 0 then
                stash := game.jsonb_array_remove(stash, idx);
                save_payload := jsonb_set(save_payload, '{money}', to_jsonb(coalesce((save_payload->>'money')::int, 0) + selected_value));
                stash := game.ensure_knife_fallback(stash, on_person_items);
            end if;

        when 'move-stash-to-on-person' then
            idx := coalesce((payload->>'stashIndex')::int, -1);
            selected_item := game.jsonb_array_get(stash, idx);

            if selected_item is not null then
                selected_type := coalesce((selected_item->>'type')::int, -1);
                stash := game.jsonb_array_remove(stash, idx);
                if selected_type in (0, 1, 2) and not game.has_equipped_slot(on_person_items, selected_type) then
                    on_person_items := game.set_equipped_on_person(
                        on_person_items || jsonb_build_array(jsonb_build_object('item', selected_item, 'isEquipped', false)),
                        jsonb_array_length(on_person_items),
                        true);
                else
                    on_person_items := on_person_items || jsonb_build_array(jsonb_build_object('item', selected_item, 'isEquipped', false));
                end if;
            end if;

        when 'sell-on-person-item' then
            idx := coalesce((payload->>'onPersonIndex')::int, -1);
            selected_entry := game.jsonb_array_get(on_person_items, idx);
            selected_item := game.normalize_item(coalesce(selected_entry->'item', selected_entry->'Item'));
            selected_name := coalesce(selected_item->>'name', '');
            selected_value := coalesce((selected_item->>'value')::int, 0);

            if selected_entry is not null and selected_name <> 'Rusty Knife' and selected_value > 0 then
                on_person_items := game.jsonb_array_remove(on_person_items, idx);
                save_payload := jsonb_set(save_payload, '{money}', to_jsonb(coalesce((save_payload->>'money')::int, 0) + selected_value));
                stash := game.ensure_knife_fallback(stash, on_person_items);
            end if;

        when 'stash-on-person-item' then
            idx := coalesce((payload->>'onPersonIndex')::int, -1);
            if jsonb_array_length(stash) < 30 then
                selected_entry := game.jsonb_array_get(on_person_items, idx);
                selected_item := game.normalize_item(coalesce(selected_entry->'item', selected_entry->'Item'));
                if selected_entry is not null then
                    on_person_items := game.jsonb_array_remove(on_person_items, idx);
                    stash := stash || jsonb_build_array(selected_item);
                end if;
            end if;

        when 'equip-on-person-item' then
            idx := coalesce((payload->>'onPersonIndex')::int, -1);
            selected_entry := game.jsonb_array_get(on_person_items, idx);
            selected_item := game.normalize_item(coalesce(selected_entry->'item', selected_entry->'Item'));
            selected_type := coalesce((selected_item->>'type')::int, -1);

            if selected_entry is not null and selected_type in (0, 1, 2) then
                on_person_items := game.set_equipped_on_person(on_person_items, idx, true);
            end if;

        when 'unequip-on-person-item' then
            idx := coalesce((payload->>'onPersonIndex')::int, -1);
            selected_entry := game.jsonb_array_get(on_person_items, idx);
            selected_item := game.normalize_item(coalesce(selected_entry->'item', selected_entry->'Item'));
            selected_type := coalesce((selected_item->>'type')::int, -1);

            if selected_entry is not null and selected_type in (0, 1, 2) then
                on_person_items := game.set_equipped_on_person(on_person_items, idx, false);
            end if;

        when 'buy-from-shop' then
            item_name := payload->>'itemName';
            price := game.shop_item_price(item_name);
            selected_item := game.shop_item(item_name);

            if selected_item is not null and price > 0 and coalesce((save_payload->>'money')::int, 0) >= price then
                save_payload := jsonb_set(save_payload, '{money}', to_jsonb(coalesce((save_payload->>'money')::int, 0) - price));
                on_person_items := on_person_items || jsonb_build_array(jsonb_build_object('item', selected_item, 'isEquipped', false));
            end if;

        when 'store-luck-run-item' then
            idx := coalesce((payload->>'luckIndex')::int, -1);
            if random_character is not null and jsonb_array_length(stash) < 30 then
                random_inventory := coalesce(random_character->'inventory', '[]'::jsonb);
                selected_item := game.jsonb_array_get(random_inventory, idx);
                if selected_item is not null then
                    random_inventory := game.jsonb_array_remove(random_inventory, idx);
                    stash := stash || jsonb_build_array(selected_item);
                    if jsonb_array_length(random_inventory) = 0 then
                        random_character := null;
                        random_available_at := to_jsonb((timezone('utc', now()) + interval '5 minutes')::text);
                    else
                        random_character := jsonb_build_object('name', random_character->>'name', 'inventory', random_inventory);
                    end if;
                    stash := game.ensure_knife_fallback(stash, on_person_items);
                end if;
            end if;

        when 'move-luck-run-item-to-on-person' then
            idx := coalesce((payload->>'luckIndex')::int, -1);
            if random_character is not null then
                random_inventory := coalesce(random_character->'inventory', '[]'::jsonb);
                selected_item := game.jsonb_array_get(random_inventory, idx);
                if selected_item is not null then
                    random_inventory := game.jsonb_array_remove(random_inventory, idx);
                    selected_type := coalesce((selected_item->>'type')::int, -1);
                    if selected_type in (0, 1, 2) and not game.has_equipped_slot(on_person_items, selected_type) then
                        on_person_items := game.set_equipped_on_person(
                            on_person_items || jsonb_build_array(jsonb_build_object('item', selected_item, 'isEquipped', false)),
                            jsonb_array_length(on_person_items),
                            true);
                    else
                        on_person_items := on_person_items || jsonb_build_array(jsonb_build_object('item', selected_item, 'isEquipped', false));
                    end if;

                    if jsonb_array_length(random_inventory) = 0 then
                        random_character := null;
                        random_available_at := to_jsonb((timezone('utc', now()) + interval '5 minutes')::text);
                    else
                        random_character := jsonb_build_object('name', random_character->>'name', 'inventory', random_inventory);
                    end if;
                end if;
            end if;

        when 'sell-luck-run-item' then
            idx := coalesce((payload->>'luckIndex')::int, -1);
            if random_character is not null then
                random_inventory := coalesce(random_character->'inventory', '[]'::jsonb);
                selected_item := game.jsonb_array_get(random_inventory, idx);
                selected_name := coalesce(selected_item->>'name', '');
                selected_value := coalesce((selected_item->>'value')::int, 0);
                if selected_item is not null and selected_name <> 'Rusty Knife' and selected_value > 0 then
                    random_inventory := game.jsonb_array_remove(random_inventory, idx);
                    save_payload := jsonb_set(save_payload, '{money}', to_jsonb(coalesce((save_payload->>'money')::int, 0) + selected_value));
                    if jsonb_array_length(random_inventory) = 0 then
                        random_character := null;
                        random_available_at := to_jsonb((timezone('utc', now()) + interval '5 minutes')::text);
                    else
                        random_character := jsonb_build_object('name', random_character->>'name', 'inventory', random_inventory);
                    end if;
                end if;
            end if;

        when 'accept-stats' then
            normalized_draft_stats := game.normalize_save_payload(
                jsonb_build_object('draftStats', coalesce(payload->'draftStats', payload->'DraftStats', draft_stats))
            )->'draftStats';
            save_payload := jsonb_set(save_payload, '{draftStats}', normalized_draft_stats, true);
            save_payload := jsonb_set(save_payload, '{acceptedStats}', normalized_draft_stats, true);
            save_payload := jsonb_set(save_payload, '{availableStatPoints}', to_jsonb(0), true);
            save_payload := jsonb_set(save_payload, '{statsAccepted}', 'true'::jsonb, true);

        when 'reallocate-stats' then
            if active_raid is null and coalesce((save_payload->>'money')::int, 0) >= 5000 then
                normalized_draft_stats := jsonb_build_object(
                    'strength', 8,
                    'dexterity', 8,
                    'constitution', 8,
                    'intelligence', 8,
                    'wisdom', 8,
                    'charisma', 8
                );
                save_payload := jsonb_set(save_payload, '{money}', to_jsonb(coalesce((save_payload->>'money')::int, 0) - 5000), true);
                save_payload := jsonb_set(save_payload, '{draftStats}', normalized_draft_stats, true);
                save_payload := jsonb_set(save_payload, '{availableStatPoints}', to_jsonb(27), true);
                save_payload := jsonb_set(save_payload, '{statsAccepted}', 'false'::jsonb, true);
            end if;
    end case;

    settled_random_state := game.settle_random_character(random_character, random_available_at);

    save_payload := jsonb_build_object(
        'money', coalesce((save_payload->>'money')::int, 0),
        'acceptedStats', coalesce(save_payload->'acceptedStats', accepted_stats),
        'draftStats', coalesce(save_payload->'draftStats', draft_stats),
        'availableStatPoints', coalesce((save_payload->>'availableStatPoints')::int, 27),
        'statsAccepted', coalesce((save_payload->>'statsAccepted')::boolean, false),
        'playerDexterity', coalesce((coalesce(save_payload->'acceptedStats', accepted_stats)->>'dexterity')::int, 8),
        'playerConstitution', coalesce((coalesce(save_payload->'acceptedStats', accepted_stats)->>'constitution')::int, 8),
        'playerMaxHealth', coalesce((save_payload->>'playerMaxHealth')::int, 26),
        'mainStash', game.normalize_items(stash),
        'onPersonItems', game.normalize_on_person_items(on_person_items),
        'randomCharacterAvailableAt', settled_random_state->'randomCharacterAvailableAt',
        'randomCharacter', settled_random_state->'randomCharacter',
        'activeRaid', active_raid
    );

    update public.game_saves
    set payload = save_payload,
        save_version = 1,
        updated_at = timezone('utc', now())
    where user_id = target_user_id;

    return save_payload;
end;
$$;

update public.game_saves
set payload = game.normalize_save_payload(payload),
    updated_at = timezone('utc', now())
where payload is distinct from game.normalize_save_payload(payload);

revoke all on function game.normalize_save_payload(jsonb) from public;
revoke all on function game.default_save_payload() from public;
revoke all on function game.build_raid_snapshot(jsonb, text, int, jsonb) from public;
revoke all on function game.start_raid_action(text, jsonb, uuid) from public;
