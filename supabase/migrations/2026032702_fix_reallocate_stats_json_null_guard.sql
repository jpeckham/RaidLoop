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
    active_raid := coalesce(save_payload->'activeRaid', 'null'::jsonb);
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
            if active_raid = 'null'::jsonb and coalesce((save_payload->>'money')::int, 0) >= 5000 then
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

revoke all on function game.apply_profile_action(text, jsonb, uuid) from public;
