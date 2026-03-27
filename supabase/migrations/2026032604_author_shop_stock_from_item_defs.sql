alter table game.item_defs
    add column if not exists shop_enabled boolean not null default false,
    add column if not exists shop_order int not null default 0;

update game.item_defs
set shop_enabled = true,
    shop_order = case name
        when 'Medkit' then 10
        when 'Makarov' then 20
        when '6B2 body armor' then 30
        when 'PPSH' then 40
        when 'Small Backpack' then 50
        when 'AK74' then 60
        when '6B13 assault armor' then 70
        when 'Tactical Backpack' then 80
        when 'SVDS' then 90
        when 'FORT Defender-2' then 100
        when 'Tasmanian Tiger Trooper 35' then 110
        when 'PKP' then 120
        when 'NFM THOR' then 130
        when '6Sh118' then 140
        else shop_order
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

update game.item_defs
set shop_enabled = false,
    shop_order = 0
where name not in (
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

create or replace function game.shop_stock()
returns jsonb
language sql
stable
as $$
    select coalesce(
        jsonb_agg(game.authored_item(item_defs.name) order by item_defs.shop_order, item_defs.sort_order),
        '[]'::jsonb)
    from game.item_defs
    where item_defs.enabled
      and item_defs.shop_enabled;
$$;

create or replace function public.profile_bootstrap()
returns jsonb
language sql
security definer
set search_path = public, auth, game
as $$
    select game.normalize_save_payload(game.bootstrap_player(auth.uid()))
        || jsonb_build_object('ShopStock', game.shop_stock());
$$;

revoke all on function game.shop_stock() from public;
revoke all on function public.profile_bootstrap() from public;
grant execute on function public.profile_bootstrap() to authenticated;
