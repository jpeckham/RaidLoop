create or replace function game.payload_item_name(payload jsonb)
returns text
language sql
stable
as $function$
    select coalesce(
        payload->>'itemName',
        payload->>'item_name',
        payload->>'ItemName',
        (
            select defs.name
            from game.item_defs defs
            where defs.item_def_id = coalesce(
                nullif(payload->>'itemDefId', '')::int,
                nullif(payload->>'item_def_id', '')::int,
                nullif(payload->>'ItemDefId', '')::int
            )
            limit 1
        ),
        (
            select defs.name
            from game.item_defs defs
            where defs.item_key = coalesce(
                payload->>'itemKey',
                payload->>'item_key',
                payload->>'ItemKey'
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

create or replace function game.current_shop_charisma(target_user_id uuid default auth.uid())
returns int
language sql
stable
security definer
set search_path = public, auth, game
as $function$
    select coalesce(
        (
            select (game.normalize_save_payload(game.bootstrap_player(target_user_id))->'acceptedStats'->>'charisma')::int
        ),
        8
    );
$function$;

create or replace function game.shop_item(item_name text, target_user_id uuid default auth.uid())
returns jsonb
language sql
stable
as $function$
    select game.authored_item(item_defs.name)
    from game.item_defs
    where item_defs.enabled
      and item_defs.shop_enabled
      and item_defs.name = item_name
      and item_defs.rarity <= game.shop_max_rarity(game.current_shop_charisma(target_user_id))
    limit 1;
$function$;

create or replace function game.shop_item_price(item_name text, target_user_id uuid default auth.uid())
returns int
language sql
stable
as $function$
    select coalesce(
        (
            select greatest(
                1,
                round((item_defs.shop_price * (1 - (0.05 * greatest(game.ability_modifier(game.current_shop_charisma(target_user_id)), 0))))::numeric)::int
            )
            from game.item_defs
            where item_defs.enabled
              and item_defs.shop_enabled
              and item_defs.name = item_name
              and item_defs.rarity <= game.shop_max_rarity(game.current_shop_charisma(target_user_id))
            limit 1
        ),
        0
    );
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
                'itemKey', defs.item_key,
                'name', defs.name,
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
        jsonb_build_object(
            'itemDefId', coalesce((select item_def_id from normalized), 0),
            'itemKey', coalesce((select item_key from normalized), ''),
            'name', (select item_name from normalized),
            'type', coalesce((item->>'type')::int, (item->>'Type')::int, 0),
            'value', coalesce((item->>'value')::int, (item->>'Value')::int, 1),
            'slots', coalesce((item->>'slots')::int, (item->>'Slots')::int, 1),
            'rarity', coalesce((item->>'rarity')::int, (item->>'Rarity')::int, 0),
            'displayRarity', coalesce((item->>'displayRarity')::int, (item->>'DisplayRarity')::int, 1),
            'weight', coalesce((item->>'weight')::int, (item->>'Weight')::int, 0)
        )
    );
$function$;

create or replace function game.apply_profile_action(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $function$
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
    respec_cost int;
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
            item_name := game.payload_item_name(payload);
            price := game.shop_item_price(item_name, target_user_id);
            selected_item := game.shop_item(item_name, target_user_id);

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
            respec_cost := greatest((coalesce((save_payload->>'money')::int, 0) + 1) / 2, 0);
            if coalesce((save_payload->>'statsAccepted')::boolean, false) and active_raid = 'null'::jsonb and coalesce((save_payload->>'money')::int, 0) >= respec_cost then
                save_payload := jsonb_set(save_payload, '{money}', to_jsonb(coalesce((save_payload->>'money')::int, 0) - respec_cost));
                save_payload := jsonb_set(save_payload, '{acceptedStats}', jsonb_build_object(
                    'strength', 8,
                    'dexterity', 8,
                    'constitution', 8,
                    'intelligence', 8,
                    'wisdom', 8,
                    'charisma', 8
                ), true);
                save_payload := jsonb_set(save_payload, '{draftStats}', jsonb_build_object(
                    'strength', 8,
                    'dexterity', 8,
                    'constitution', 8,
                    'intelligence', 8,
                    'wisdom', 8,
                    'charisma', 8
                ), true);
                save_payload := jsonb_set(save_payload, '{availableStatPoints}', to_jsonb(27), true);
                save_payload := jsonb_set(save_payload, '{statsAccepted}', 'false'::jsonb, true);
                save_payload := jsonb_set(save_payload, '{playerDexterity}', to_jsonb(8), true);
                save_payload := jsonb_set(save_payload, '{playerConstitution}', to_jsonb(8), true);
                save_payload := jsonb_set(save_payload, '{playerMaxHealth}', to_jsonb(26), true);
            end if;

        else
            raise exception 'Unsupported profile action: %', action;
    end case;

    settled_random_state := game.settle_random_character(random_character, random_available_at);
    save_payload := jsonb_build_object(
        'money', coalesce((save_payload->>'money')::int, 0),
        'acceptedStats', coalesce(save_payload->'acceptedStats', accepted_stats),
        'draftStats', coalesce(save_payload->'draftStats', draft_stats),
        'availableStatPoints', coalesce((save_payload->>'availableStatPoints')::int, 27),
        'statsAccepted', coalesce((save_payload->>'statsAccepted')::boolean, false),
        'playerDexterity', coalesce((save_payload->>'playerDexterity')::int, 8),
        'playerConstitution', coalesce((save_payload->>'playerConstitution')::int, 8),
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
$function$;

create or replace function game.perform_raid_action_with_encumbrance(action text, payload jsonb, target_user_id uuid default auth.uid())
returns jsonb
language plpgsql
security definer
set search_path = public, auth, game
as $function$
declare
    request_payload jsonb := payload;
    save_payload jsonb;
    save_accepted_stats jsonb;
    raid_payload jsonb;
    equipped_items jsonb;
    carried_loot jsonb;
    discovered_loot jsonb;
    medkits int;
    selected_item jsonb;
    previous_item jsonb;
    selected_type int;
    item_name text;
    max_encumbrance int;
    current_encumbrance int;
    prospective_encumbrance int;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    save_accepted_stats := coalesce(
        save_payload->'acceptedStats',
        save_payload->'AcceptedStats',
        jsonb_build_object(
            'strength', 8,
            'dexterity', 8,
            'constitution', 8,
            'intelligence', 8,
            'wisdom', 8,
            'charisma', 8
        )
    );

    select sessions.payload
    into raid_payload
    from public.raid_sessions sessions
    where sessions.user_id = target_user_id;

    if raid_payload is null then
        return game.perform_raid_action(action, request_payload, target_user_id);
    end if;

    raid_payload := game.normalize_active_raid_payload(raid_payload, save_accepted_stats);

    update public.raid_sessions sessions
    set payload = raid_payload,
        updated_at = timezone('utc', now())
    where sessions.user_id = target_user_id;

    save_payload := jsonb_set(save_payload, '{activeRaid}', raid_payload, true);
    update public.game_saves saves
    set payload = save_payload,
        save_version = 1,
        updated_at = timezone('utc', now())
    where saves.user_id = target_user_id;

    max_encumbrance := game.max_encumbrance(coalesce((save_accepted_stats->>'strength')::int, 8));
    equipped_items := game.normalize_items(coalesce(raid_payload->'equippedItems', '[]'::jsonb));
    carried_loot := game.normalize_items(coalesce(raid_payload->'carriedLoot', '[]'::jsonb));
    discovered_loot := game.normalize_items(coalesce(raid_payload->'discoveredLoot', '[]'::jsonb));
    medkits := greatest(coalesce((raid_payload->>'medkits')::int, 0), 0);
    current_encumbrance := game.current_encumbrance(equipped_items || carried_loot, medkits);

    item_name := game.payload_item_name(request_payload);
    if item_name is not null then
        request_payload := jsonb_set(request_payload, '{itemName}', to_jsonb(item_name), true);
    end if;

    if action = 'take-loot' then
        selected_item := (
            select value
            from jsonb_array_elements(discovered_loot) value
            where value->>'name' = item_name
            limit 1
        );

        if selected_item is not null then
            prospective_encumbrance := current_encumbrance + game.item_weight(coalesce(selected_item->>'name', item_name));
            if prospective_encumbrance > max_encumbrance then
                return save_payload;
            end if;
        end if;
    elsif action in ('equip-from-discovered', 'equip-from-carried') then
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
            selected_type := coalesce((selected_item->>'type')::int, -1);
            previous_item := game.raid_find_equipped_item(equipped_items, selected_type);
            if action = 'equip-from-discovered' then
                prospective_encumbrance := current_encumbrance
                    + game.item_weight(coalesce(selected_item->>'name', item_name))
                    - coalesce(game.item_weight(previous_item->>'name'), 0);
            else
                prospective_encumbrance := current_encumbrance
                    - coalesce(game.item_weight(previous_item->>'name'), 0);
            end if;

            if prospective_encumbrance > max_encumbrance then
                return save_payload;
            end if;
        end if;
    end if;

    return game.perform_raid_action(action, request_payload, target_user_id);
end;
$function$;
