create or replace function game.rename_legacy_item_name(old_name text)
returns text
language sql
immutable
as $$
    select case old_name
        when 'Makarov' then 'Light Pistol'
        when 'PPSH' then 'Drum SMG'
        when 'AK74' then 'Field Carbine'
        when 'AK47' then 'Battle Rifle'
        when 'SVDS' then 'Marksman Rifle'
        when 'PKP' then 'Support Machine Gun'
        when '6B2 body armor' then 'Soft Armor Vest'
        when 'BNTI Kirasa-N' then 'Reinforced Vest'
        when '6B13 assault armor' then 'Light Plate Carrier'
        when 'FORT Defender-2' then 'Medium Plate Carrier'
        when '6B43 Zabralo-Sh body armor' then 'Heavy Plate Carrier'
        when 'NFM THOR' then 'Assault Plate Carrier'
        when 'Tasmanian Tiger Trooper 35' then 'Hiking Backpack'
        when '6Sh118' then 'Raid Backpack'
        else old_name
    end;
$$;

create or replace function game.rename_legacy_item(item jsonb)
returns jsonb
language plpgsql
stable
as $$
declare
    original_name text := coalesce(item->>'name', item->>'Name');
    renamed_name text := game.rename_legacy_item_name(original_name);
    authored jsonb;
begin
    if item is null then
        return null;
    end if;

    authored := game.authored_item(renamed_name);

    if authored is not null then
        return authored;
    end if;

    return jsonb_set(game.normalize_item(item), '{name}', to_jsonb(renamed_name), true);
end;
$$;

create or replace function game.rename_legacy_items(items jsonb)
returns jsonb
language sql
stable
as $$
    select coalesce(
        (
            select jsonb_agg(game.rename_legacy_item(value) order by ordinality)
            from jsonb_array_elements(coalesce(items, '[]'::jsonb)) with ordinality
        ),
        '[]'::jsonb
    );
$$;

create or replace function game.rename_legacy_on_person_items(items jsonb)
returns jsonb
language sql
stable
as $$
    select coalesce(
        (
            select jsonb_agg(
                jsonb_build_object(
                    'item', game.rename_legacy_item(coalesce(value->'item', value->'Item')),
                    'isEquipped', coalesce((value->>'isEquipped')::boolean, (value->>'IsEquipped')::boolean, false)
                )
                order by ordinality
            )
            from jsonb_array_elements(coalesce(items, '[]'::jsonb)) with ordinality
        ),
        '[]'::jsonb
    );
$$;

create or replace function game.rename_legacy_random_character(random_character jsonb)
returns jsonb
language sql
stable
as $$
    select case
        when random_character is null then null
        else jsonb_build_object(
            'name', coalesce(random_character->>'name', random_character->>'Name'),
            'inventory', game.rename_legacy_items(coalesce(random_character->'inventory', random_character->'Inventory'))
        )
    end;
$$;

create or replace function game.rename_legacy_active_raid(active_raid jsonb)
returns jsonb
language plpgsql
stable
as $$
declare
    updated jsonb := active_raid;
begin
    if active_raid is null or active_raid = 'null'::jsonb then
        return active_raid;
    end if;

    updated := jsonb_set(updated, '{equippedItems}', game.rename_legacy_items(coalesce(active_raid->'equippedItems', '[]'::jsonb)), true);
    updated := jsonb_set(updated, '{carriedLoot}', game.rename_legacy_items(coalesce(active_raid->'carriedLoot', '[]'::jsonb)), true);
    updated := jsonb_set(updated, '{discoveredLoot}', game.rename_legacy_items(coalesce(active_raid->'discoveredLoot', '[]'::jsonb)), true);

    return updated;
end;
$$;

do $$
begin
    if exists (
        select 1
        from pg_constraint
        where conname = 'enemy_loadout_variant_items_item_key_fkey'
          and conrelid = 'game.enemy_loadout_variant_items'::regclass
    ) then
        alter table game.enemy_loadout_variant_items
            drop constraint enemy_loadout_variant_items_item_key_fkey;
    end if;

    if exists (
        select 1
        from pg_constraint
        where conname = 'loot_table_variant_items_item_key_fkey'
          and conrelid = 'game.loot_table_variant_items'::regclass
    ) then
        alter table game.loot_table_variant_items
            drop constraint loot_table_variant_items_item_key_fkey;
    end if;
end
$$;

with item_name_map(old_key, new_key, old_name, new_name) as (
    values
        ('makarov', 'light_pistol', 'Makarov', 'Light Pistol'),
        ('ppsh', 'drum_smg', 'PPSH', 'Drum SMG'),
        ('ak74', 'field_carbine', 'AK74', 'Field Carbine'),
        ('ak47', 'battle_rifle', 'AK47', 'Battle Rifle'),
        ('svds', 'marksman_rifle', 'SVDS', 'Marksman Rifle'),
        ('pkp', 'support_machine_gun', 'PKP', 'Support Machine Gun'),
        ('6b2_body_armor', 'soft_armor_vest', '6B2 body armor', 'Soft Armor Vest'),
        ('bnti_kirasa_n', 'reinforced_vest', 'BNTI Kirasa-N', 'Reinforced Vest'),
        ('6b13_assault_armor', 'light_plate_carrier', '6B13 assault armor', 'Light Plate Carrier'),
        ('fort_defender_2', 'medium_plate_carrier', 'FORT Defender-2', 'Medium Plate Carrier'),
        ('6b43_zabralo_sh_body_armor', 'heavy_plate_carrier', '6B43 Zabralo-Sh body armor', 'Heavy Plate Carrier'),
        ('nfm_thor', 'assault_plate_carrier', 'NFM THOR', 'Assault Plate Carrier'),
        ('tasmanian_tiger_trooper_35', 'hiking_backpack', 'Tasmanian Tiger Trooper 35', 'Hiking Backpack'),
        ('6sh118', 'raid_backpack', '6Sh118', 'Raid Backpack')
)
update game.enemy_loadout_variant_items target
set item_key = item_name_map.new_key
from item_name_map
where target.item_key = item_name_map.old_key;

with item_name_map(old_key, new_key, old_name, new_name) as (
    values
        ('makarov', 'light_pistol', 'Makarov', 'Light Pistol'),
        ('ppsh', 'drum_smg', 'PPSH', 'Drum SMG'),
        ('ak74', 'field_carbine', 'AK74', 'Field Carbine'),
        ('ak47', 'battle_rifle', 'AK47', 'Battle Rifle'),
        ('svds', 'marksman_rifle', 'SVDS', 'Marksman Rifle'),
        ('pkp', 'support_machine_gun', 'PKP', 'Support Machine Gun'),
        ('6b2_body_armor', 'soft_armor_vest', '6B2 body armor', 'Soft Armor Vest'),
        ('bnti_kirasa_n', 'reinforced_vest', 'BNTI Kirasa-N', 'Reinforced Vest'),
        ('6b13_assault_armor', 'light_plate_carrier', '6B13 assault armor', 'Light Plate Carrier'),
        ('fort_defender_2', 'medium_plate_carrier', 'FORT Defender-2', 'Medium Plate Carrier'),
        ('6b43_zabralo_sh_body_armor', 'heavy_plate_carrier', '6B43 Zabralo-Sh body armor', 'Heavy Plate Carrier'),
        ('nfm_thor', 'assault_plate_carrier', 'NFM THOR', 'Assault Plate Carrier'),
        ('tasmanian_tiger_trooper_35', 'hiking_backpack', 'Tasmanian Tiger Trooper 35', 'Hiking Backpack'),
        ('6sh118', 'raid_backpack', '6Sh118', 'Raid Backpack')
)
update game.loot_table_variant_items target
set item_key = item_name_map.new_key
from item_name_map
where target.item_key = item_name_map.old_key;

with item_name_map(old_key, new_key, old_name, new_name) as (
    values
        ('makarov', 'light_pistol', 'Makarov', 'Light Pistol'),
        ('ppsh', 'drum_smg', 'PPSH', 'Drum SMG'),
        ('ak74', 'field_carbine', 'AK74', 'Field Carbine'),
        ('ak47', 'battle_rifle', 'AK47', 'Battle Rifle'),
        ('svds', 'marksman_rifle', 'SVDS', 'Marksman Rifle'),
        ('pkp', 'support_machine_gun', 'PKP', 'Support Machine Gun'),
        ('6b2_body_armor', 'soft_armor_vest', '6B2 body armor', 'Soft Armor Vest'),
        ('bnti_kirasa_n', 'reinforced_vest', 'BNTI Kirasa-N', 'Reinforced Vest'),
        ('6b13_assault_armor', 'light_plate_carrier', '6B13 assault armor', 'Light Plate Carrier'),
        ('fort_defender_2', 'medium_plate_carrier', 'FORT Defender-2', 'Medium Plate Carrier'),
        ('6b43_zabralo_sh_body_armor', 'heavy_plate_carrier', '6B43 Zabralo-Sh body armor', 'Heavy Plate Carrier'),
        ('nfm_thor', 'assault_plate_carrier', 'NFM THOR', 'Assault Plate Carrier'),
        ('tasmanian_tiger_trooper_35', 'hiking_backpack', 'Tasmanian Tiger Trooper 35', 'Hiking Backpack'),
        ('6sh118', 'raid_backpack', '6Sh118', 'Raid Backpack')
)
update game.item_defs target
set item_key = item_name_map.new_key,
    name = item_name_map.new_name
from item_name_map
where target.item_key = item_name_map.old_key;

update game.item_defs
set weight = 32
where item_key = 'assault_plate_carrier'
  and weight < 32;

do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where conname = 'enemy_loadout_variant_items_item_key_fkey'
          and conrelid = 'game.enemy_loadout_variant_items'::regclass
    ) then
        alter table game.enemy_loadout_variant_items
            add constraint enemy_loadout_variant_items_item_key_fkey
            foreign key (item_key) references game.item_defs(item_key);
    end if;

    if not exists (
        select 1
        from pg_constraint
        where conname = 'loot_table_variant_items_item_key_fkey'
          and conrelid = 'game.loot_table_variant_items'::regclass
    ) then
        alter table game.loot_table_variant_items
            add constraint loot_table_variant_items_item_key_fkey
            foreign key (item_key) references game.item_defs(item_key);
    end if;
end
$$;

update public.game_saves
set payload = jsonb_set(
        jsonb_set(
            jsonb_set(
                jsonb_set(
                    coalesce(payload, '{}'::jsonb),
                    '{mainStash}',
                    game.rename_legacy_items(coalesce(payload->'mainStash', payload->'MainStash', '[]'::jsonb)),
                    true
                ),
                '{onPersonItems}',
                game.rename_legacy_on_person_items(coalesce(payload->'onPersonItems', payload->'OnPersonItems', '[]'::jsonb)),
                true
            ),
            '{randomCharacter}',
            coalesce(game.rename_legacy_random_character(coalesce(payload->'randomCharacter', payload->'RandomCharacter')), 'null'::jsonb),
            true
        ),
        '{activeRaid}',
        coalesce(game.rename_legacy_active_raid(coalesce(payload->'activeRaid', payload->'ActiveRaid', 'null'::jsonb)), 'null'::jsonb),
        true
    )
where coalesce(payload::text, '') ~ 'Makarov|PPSH|AK74|AK47|SVDS|PKP|6B2 body armor|BNTI Kirasa-N|6B13 assault armor|FORT Defender-2|6B43 Zabralo-Sh body armor|NFM THOR|Tasmanian Tiger Trooper 35|6Sh118';
