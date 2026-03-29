insert into game.item_defs (
    item_key,
    name,
    item_type,
    value,
    slots,
    rarity,
    display_rarity,
    magazine_capacity,
    backpack_capacity,
    armor_damage_reduction,
    armor_penetration,
    supports_single_shot,
    supports_burst_fire,
    supports_full_auto,
    burst_attack_penalty,
    damage_die_size,
    enabled,
    sort_order,
    notes,
    shop_enabled,
    shop_order
)
values
    ('reinforced_vest', 'Reinforced Vest', 1, 160, 1, 1, 2, 0, 0, 2, 0, true, false, false, 3, 6, true, 85, 'Uncommon armor tier between 6B2 and 6B13', true, 35),
    ('large_backpack', 'Large Backpack', 2, 50, 1, 1, 2, 0, 4, 0, 0, true, false, false, 3, 6, true, 135, 'Uncommon backpack tier between Small and Tactical', true, 55)
on conflict (item_key) do update
set name = excluded.name,
    item_type = excluded.item_type,
    value = excluded.value,
    slots = excluded.slots,
    rarity = excluded.rarity,
    display_rarity = excluded.display_rarity,
    magazine_capacity = excluded.magazine_capacity,
    backpack_capacity = excluded.backpack_capacity,
    armor_damage_reduction = excluded.armor_damage_reduction,
    armor_penetration = excluded.armor_penetration,
    supports_single_shot = excluded.supports_single_shot,
    supports_burst_fire = excluded.supports_burst_fire,
    supports_full_auto = excluded.supports_full_auto,
    burst_attack_penalty = excluded.burst_attack_penalty,
    damage_die_size = excluded.damage_die_size,
    enabled = excluded.enabled,
    sort_order = excluded.sort_order,
    notes = excluded.notes,
    shop_enabled = excluded.shop_enabled,
    shop_order = excluded.shop_order;

update game.item_defs
set rarity = 0,
    display_rarity = 1,
    backpack_capacity = 3,
    shop_enabled = true,
    shop_order = 50
where item_key = 'small_backpack';

update game.item_defs
set shop_order = case item_key
    when 'reinforced_vest' then 35
    when 'drum_smg' then 40
    when 'small_backpack' then 50
    when 'large_backpack' then 55
    when 'light_plate_carrier' then 70
    when 'tactical_backpack' then 80
    else shop_order
end
where shop_enabled;
