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

        if exists (
            select 1
            from jsonb_array_elements(on_person_items) inventory_entry
            where coalesce((coalesce(inventory_entry->'item', inventory_entry->'Item')->>'type')::int, -1) in (0, 1, 2)
              and not coalesce((inventory_entry->>'isEquipped')::boolean, (inventory_entry->>'IsEquipped')::boolean, false)
        ) then
            return save_payload;
        end if;

        if not exists (
            select 1
            from jsonb_array_elements(on_person_items) inventory_entry
            where coalesce((coalesce(inventory_entry->'item', inventory_entry->'Item')->>'type')::int, -1) = 0
              and coalesce((inventory_entry->>'isEquipped')::boolean, (inventory_entry->>'IsEquipped')::boolean, false)
        ) then
            return save_payload;
        end if;

        for entry in
            select coalesce(value->'item', value->'Item')
            from jsonb_array_elements(on_person_items) value
        loop
            loadout := loadout || jsonb_build_array(game.normalize_item(entry));
        end loop;

        raid_snapshot := game.build_raid_snapshot(loadout, raider_name, player_max_health, accepted_stats);

        insert into public.raid_sessions (user_id, profile, payload)
        values (target_user_id, 'main', raid_snapshot)
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

        insert into public.raid_sessions (user_id, profile, payload)
        values (target_user_id, 'random', raid_snapshot)
        on conflict (user_id) do update
            set profile = excluded.profile,
                payload = excluded.payload,
                updated_at = timezone('utc', now());

        save_payload := jsonb_set(save_payload, '{randomCharacter}', random_character, true);
        save_payload := jsonb_set(save_payload, '{activeRaid}', raid_snapshot, true);
        update public.game_saves
        set payload = save_payload,
            save_version = 1,
            updated_at = timezone('utc', now())
        where user_id = target_user_id;

        return save_payload;
    end if;

    return save_payload;
end;
$$;
