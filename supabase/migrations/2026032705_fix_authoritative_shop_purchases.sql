alter table game.item_defs
    add column if not exists shop_price int not null default 0;

update game.item_defs
set shop_price = case name
    when 'Medkit' then 120
    when 'Light Pistol' then 240
    when 'Soft Armor Vest' then 380
    when 'Drum SMG' then 650
    when 'Small Backpack' then 100
    when 'Field Carbine' then 1250
    when 'Light Plate Carrier' then 900
    when 'Tactical Backpack' then 300
    when 'Marksman Rifle' then 2200
    when 'Medium Plate Carrier' then 1500
    when 'Hiking Backpack' then 1600
    when 'Support Machine Gun' then 3200
    when 'Assault Plate Carrier' then 2600
    when 'Raid Backpack' then 2400
    else shop_price
end
where name in (
    'Medkit',
    'Light Pistol',
    'Soft Armor Vest',
    'Drum SMG',
    'Small Backpack',
    'Field Carbine',
    'Light Plate Carrier',
    'Tactical Backpack',
    'Marksman Rifle',
    'Medium Plate Carrier',
    'Hiking Backpack',
    'Support Machine Gun',
    'Assault Plate Carrier',
    'Raid Backpack'
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
