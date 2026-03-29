alter table game.item_defs
    add column if not exists shop_enabled boolean not null default false,
    add column if not exists shop_order int not null default 0;

update game.item_defs
set shop_enabled = true,
    shop_order = case name
        when 'Medkit' then 10
        when 'Light Pistol' then 20
        when 'Soft Armor Vest' then 30
        when 'Drum SMG' then 40
        when 'Small Backpack' then 50
        when 'Field Carbine' then 60
        when 'Light Plate Carrier' then 70
        when 'Tactical Backpack' then 80
        when 'Marksman Rifle' then 90
        when 'Medium Plate Carrier' then 100
        when 'Hiking Backpack' then 110
        when 'Support Machine Gun' then 120
        when 'Assault Plate Carrier' then 130
        when 'Raid Backpack' then 140
        else shop_order
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

update game.item_defs
set shop_enabled = false,
    shop_order = 0
where name not in (
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
