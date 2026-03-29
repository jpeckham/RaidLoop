update game.item_defs
set weight = case name
    when 'Rusty Knife' then 1
    when 'Light Pistol' then 2
    when 'Drum SMG' then 12
    when 'Field Carbine' then 7
    when 'Marksman Rifle' then 10
    when 'Battle Rifle' then 10
    when 'Support Machine Gun' then 18
    when 'Soft Armor Vest' then 9
    when 'Reinforced Vest' then 7
    when 'Light Plate Carrier' then 7
    when 'Medium Plate Carrier' then 22
    when 'Heavy Plate Carrier' then 28
    when 'Assault Plate Carrier' then 32
    when 'Small Backpack' then 1
    when 'Large Backpack' then 1
    when 'Tactical Backpack' then 2
    when 'Hiking Backpack' then 2
    when 'Raid Backpack' then 8
    when 'Medkit' then 1
    when 'Bandage' then 1
    when 'Ammo Box' then 4
    when 'Scrap Metal' then 10
    when 'Rare Scope' then 1
    when 'Legendary Trigger Group' then 1
    else weight
end
where name in (
    'Rusty Knife',
    'Light Pistol',
    'Drum SMG',
    'Field Carbine',
    'Marksman Rifle',
    'Battle Rifle',
    'Support Machine Gun',
    'Soft Armor Vest',
    'Reinforced Vest',
    'Light Plate Carrier',
    'Medium Plate Carrier',
    'Heavy Plate Carrier',
    'Assault Plate Carrier',
    'Small Backpack',
    'Large Backpack',
    'Tactical Backpack',
    'Hiking Backpack',
    'Raid Backpack',
    'Medkit',
    'Bandage',
    'Ammo Box',
    'Scrap Metal',
    'Rare Scope',
    'Legendary Trigger Group'
);
