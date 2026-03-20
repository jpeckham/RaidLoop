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
    loop_entry jsonb;
begin
    if target_user_id is null then
        raise exception 'Authenticated user required';
    end if;

    save_payload := game.normalize_save_payload(game.bootstrap_player(target_user_id));
    on_person_items := coalesce(save_payload->'onPersonItems', '[]'::jsonb);
    random_character := save_payload->'randomCharacter';

    if action = 'start-main-raid' then
        if random_character is not null and jsonb_array_length(coalesce(random_character->'inventory', '[]'::jsonb)) > 0 then
            return save_payload;
        end if;

        if exists (
            select 1
            from jsonb_array_elements(on_person_items) on_person_entry
            where coalesce((coalesce(on_person_entry->'item', on_person_entry->'Item')->>'type')::int, -1) in (0, 1, 2)
              and not coalesce((on_person_entry->>'isEquipped')::boolean, (on_person_entry->>'IsEquipped')::boolean, false)
        ) then
            return save_payload;
        end if;

        if not exists (
            select 1
            from jsonb_array_elements(on_person_items) on_person_entry
            where coalesce((coalesce(on_person_entry->'item', on_person_entry->'Item')->>'type')::int, -1) = 0
              and coalesce((on_person_entry->>'isEquipped')::boolean, (on_person_entry->>'IsEquipped')::boolean, false)
        ) then
            return save_payload;
        end if;

        for loop_entry in
            select coalesce(value->'item', value->'Item')
            from jsonb_array_elements(on_person_items) value
        loop
            loadout := loadout || jsonb_build_array(game.normalize_item(loop_entry));
        end loop;

        raid_snapshot := game.build_raid_snapshot(loadout, raider_name);

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
        raider_name := random_character->>'name';
        loadout := coalesce(random_character->'inventory', '[]'::jsonb);
        raid_snapshot := game.build_raid_snapshot(loadout, raider_name);

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
