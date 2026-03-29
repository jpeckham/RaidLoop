alter table game.item_defs
    add column if not exists shop_price int not null default 0;

update game.item_defs
set shop_price = case name
    when 'Medkit' then 120
    when 'Makarov' then 240
    when '6B2 body armor' then 380
    when 'PPSH' then 650
    when 'Small Backpack' then 100
    when 'AK74' then 1250
    when '6B13 assault armor' then 900
    when 'Tactical Backpack' then 300
    when 'SVDS' then 2200
    when 'FORT Defender-2' then 1500
    when 'Tasmanian Tiger Trooper 35' then 1600
    when 'PKP' then 3200
    when 'NFM THOR' then 2600
    when '6Sh118' then 2400
    else shop_price
end
where name in (
    'Medkit',
    'Makarov',
    '6B2 body armor',
    'PPSH',
    'Small Backpack',
    'AK74',
    '6B13 assault armor',
    'Tactical Backpack',
    'SVDS',
    'FORT Defender-2',
    'Tasmanian Tiger Trooper 35',
    'PKP',
    'NFM THOR',
    '6Sh118'
);

create or replace function game.shop_max_rarity(charisma int)
returns int
language sql
stable
as $$
    select case greatest(game.ability_modifier(charisma), 0)
        when 0 then 0
        when 1 then 1
        when 2 then 2
        when 3 then 3
        else 4
    end;
$$;

create or replace function game.current_shop_charisma(target_user_id uuid default auth.uid())
returns int
language sql
stable
security definer
set search_path = public, auth, game
as $$
    select coalesce(
        (
            select (game.normalize_save_payload(game.bootstrap_player(target_user_id))->'acceptedStats'->>'charisma')::int
        ),
        8
    );
$$;

create or replace function game.shop_item(item_name text)
returns jsonb
language sql
stable
as $$
    select game.authored_item(item_defs.name)
    from game.item_defs
    where item_defs.enabled
      and item_defs.shop_enabled
      and item_defs.name = item_name
      and item_defs.rarity <= game.shop_max_rarity(game.current_shop_charisma())
    limit 1;
$$;

create or replace function game.shop_item_price(item_name text)
returns int
language sql
stable
as $$
    select coalesce(
        (
            select greatest(
                1,
                round((item_defs.shop_price * (1 - (0.05 * greatest(game.ability_modifier(game.current_shop_charisma()), 0))))::numeric)::int
            )
            from game.item_defs
            where item_defs.enabled
              and item_defs.shop_enabled
              and item_defs.name = item_name
              and item_defs.rarity <= game.shop_max_rarity(game.current_shop_charisma())
            limit 1
        ),
        0
    );
$$;

revoke all on function game.shop_max_rarity(int) from public;
revoke all on function game.current_shop_charisma(uuid) from public;
revoke all on function game.shop_item(text) from public;
revoke all on function game.shop_item_price(text) from public;
