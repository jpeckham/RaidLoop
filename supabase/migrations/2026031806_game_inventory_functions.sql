create or replace function game.normalize_item(item jsonb)
returns jsonb
language sql
stable
as $$
    select jsonb_build_object(
        'name', coalesce(item->>'name', item->>'Name'),
        'type', coalesce((item->>'type')::int, (item->>'Type')::int, 0),
        'value', coalesce((item->>'value')::int, (item->>'Value')::int, 1),
        'slots', coalesce((item->>'slots')::int, (item->>'Slots')::int, 1),
        'rarity', coalesce((item->>'rarity')::int, (item->>'Rarity')::int, 0),
        'displayRarity', coalesce((item->>'displayRarity')::int, (item->>'DisplayRarity')::int, 1)
    );
$$;

create or replace function game.normalize_items(items jsonb)
returns jsonb
language sql
stable
as $$
    select coalesce(
        (
            select jsonb_agg(game.normalize_item(value) order by ordinality)
            from jsonb_array_elements(coalesce(items, '[]'::jsonb)) with ordinality
        ),
        '[]'::jsonb
    );
$$;

create or replace function game.normalize_on_person_items(items jsonb)
returns jsonb
language sql
stable
as $$
    select coalesce(
        (
            select jsonb_agg(
                jsonb_build_object(
                    'item', game.normalize_item(coalesce(value->'item', value->'Item')),
                    'isEquipped', coalesce((value->>'isEquipped')::boolean, (value->>'IsEquipped')::boolean, false)
                )
                order by ordinality
            )
            from jsonb_array_elements(coalesce(items, '[]'::jsonb)) with ordinality
        ),
        '[]'::jsonb
    );
$$;

create or replace function game.normalize_random_character(random_character jsonb)
returns jsonb
language sql
stable
as $$
    select case
        when random_character is null then null
        else jsonb_build_object(
            'name', coalesce(random_character->>'name', random_character->>'Name'),
            'inventory', game.normalize_items(coalesce(random_character->'inventory', random_character->'Inventory'))
        )
    end;
$$;

create or replace function game.normalize_save_payload(payload jsonb)
returns jsonb
language sql
stable
as $$
    select jsonb_build_object(
        'money', greatest(coalesce((payload->>'money')::int, (payload->>'Money')::int, 0), 0),
        'mainStash', game.normalize_items(coalesce(payload->'mainStash', payload->'MainStash')),
        'onPersonItems', game.normalize_on_person_items(coalesce(payload->'onPersonItems', payload->'OnPersonItems')),
        'randomCharacterAvailableAt', coalesce(payload->'randomCharacterAvailableAt', payload->'RandomCharacterAvailableAt', to_jsonb('0001-01-01T00:00:00+00:00'::text)),
        'randomCharacter', game.normalize_random_character(coalesce(payload->'randomCharacter', payload->'RandomCharacter')),
        'activeRaid', coalesce(payload->'activeRaid', payload->'ActiveRaid', 'null'::jsonb)
    );
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
            'randomCharacterAvailableAt', '0001-01-01T00:00:00+00:00',
            'randomCharacter', null,
            'money', 500,
            'onPersonItems', jsonb_build_array(),
            'activeRaid', null
        )
    );
$$;

create or replace function game.jsonb_array_get(items jsonb, zero_based_index int)
returns jsonb
language sql
stable
as $$
    select value
    from jsonb_array_elements(coalesce(items, '[]'::jsonb)) with ordinality
    where ordinality = zero_based_index + 1;
$$;

create or replace function game.jsonb_array_remove(items jsonb, zero_based_index int)
returns jsonb
language sql
stable
as $$
    select coalesce(
        (
            select jsonb_agg(value order by ordinality)
            from jsonb_array_elements(coalesce(items, '[]'::jsonb)) with ordinality
            where ordinality <> zero_based_index + 1
        ),
        '[]'::jsonb
    );
$$;

create or replace function game.has_weapon(stash jsonb, on_person_items jsonb)
returns boolean
language sql
stable
as $$
    select exists (
        select 1
        from jsonb_array_elements(coalesce(stash, '[]'::jsonb)) value
        where coalesce((value->>'type')::int, -1) = 0
    ) or exists (
        select 1
        from jsonb_array_elements(coalesce(on_person_items, '[]'::jsonb)) entry
        where coalesce((coalesce(entry->'item', entry->'Item')->>'type')::int, -1) = 0
    );
$$;

create or replace function game.ensure_knife_fallback(stash jsonb, on_person_items jsonb)
returns jsonb
language sql
stable
as $$
    select case
        when game.has_weapon(stash, on_person_items) then coalesce(stash, '[]'::jsonb)
        else coalesce(stash, '[]'::jsonb) || jsonb_build_array(
            jsonb_build_object('name', 'Rusty Knife', 'type', 0, 'value', 1, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        )
    end;
$$;

create or replace function game.has_equipped_slot(on_person_items jsonb, slot_type int)
returns boolean
language sql
stable
as $$
    select exists (
        select 1
        from jsonb_array_elements(coalesce(on_person_items, '[]'::jsonb)) entry
        where coalesce((coalesce(entry->'item', entry->'Item')->>'type')::int, -1) = slot_type
          and coalesce((entry->>'isEquipped')::boolean, (entry->>'IsEquipped')::boolean, false)
    );
$$;

create or replace function game.set_equipped_on_person(on_person_items jsonb, selected_index int, should_equip boolean)
returns jsonb
language sql
stable
as $$
    with selected as (
        select coalesce((coalesce(value->'item', value->'Item')->>'type')::int, -1) as slot_type
        from jsonb_array_elements(coalesce(on_person_items, '[]'::jsonb)) with ordinality
        where ordinality = selected_index + 1
    )
    select coalesce(
        (
            select jsonb_agg(
                jsonb_build_object(
                    'item', game.normalize_item(coalesce(value->'item', value->'Item')),
                    'isEquipped',
                        case
                            when ordinality = selected_index + 1 then should_equip
                            when should_equip
                                and coalesce((coalesce(value->'item', value->'Item')->>'type')::int, -1) = (select slot_type from selected)
                                then false
                            else coalesce((value->>'isEquipped')::boolean, (value->>'IsEquipped')::boolean, false)
                        end
                )
                order by ordinality
            )
            from jsonb_array_elements(coalesce(on_person_items, '[]'::jsonb)) with ordinality
        ),
        '[]'::jsonb
    );
$$;

create or replace function game.shop_item(item_name text)
returns jsonb
language sql
stable
as $$
    select case item_name
        when 'Medkit' then jsonb_build_object('name', 'Medkit', 'type', 3, 'value', 10, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        when 'Makarov' then jsonb_build_object('name', 'Makarov', 'type', 0, 'value', 12, 'slots', 1, 'rarity', 0, 'displayRarity', 1)
        when 'Small Backpack' then jsonb_build_object('name', 'Small Backpack', 'type', 2, 'value', 18, 'slots', 1, 'rarity', 1, 'displayRarity', 2)
        else null
    end;
$$;

create or replace function game.shop_item_price(item_name text)
returns int
language sql
stable
as $$
    select case item_name
        when 'Medkit' then 120
        when 'Makarov' then 240
        when 'Small Backpack' then 100
        else 0
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
    active_raid jsonb;
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
    end case;

    save_payload := jsonb_build_object(
        'money', coalesce((save_payload->>'money')::int, 0),
        'mainStash', game.normalize_items(stash),
        'onPersonItems', game.normalize_on_person_items(on_person_items),
        'randomCharacterAvailableAt', random_available_at,
        'randomCharacter', game.normalize_random_character(random_character),
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

create or replace function public.game_action(action text, payload jsonb)
returns jsonb
language sql
security definer
set search_path = public, auth, game
as $$
    select game.apply_profile_action(action, payload, auth.uid());
$$;

revoke all on function game.normalize_item(jsonb) from public;
revoke all on function game.normalize_items(jsonb) from public;
revoke all on function game.normalize_on_person_items(jsonb) from public;
revoke all on function game.normalize_random_character(jsonb) from public;
revoke all on function game.normalize_save_payload(jsonb) from public;
revoke all on function game.jsonb_array_get(jsonb, int) from public;
revoke all on function game.jsonb_array_remove(jsonb, int) from public;
revoke all on function game.has_weapon(jsonb, jsonb) from public;
revoke all on function game.ensure_knife_fallback(jsonb, jsonb) from public;
revoke all on function game.has_equipped_slot(jsonb, int) from public;
revoke all on function game.set_equipped_on_person(jsonb, int, boolean) from public;
revoke all on function game.shop_item(text) from public;
revoke all on function game.shop_item_price(text) from public;
revoke all on function game.apply_profile_action(text, jsonb, uuid) from public;
revoke all on function public.game_action(text, jsonb) from public;

grant execute on function public.game_action(text, jsonb) to authenticated;
