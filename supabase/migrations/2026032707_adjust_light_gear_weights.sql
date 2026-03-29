update game.item_defs
set weight = case name
    when 'Light Pistol' then 2
    when 'Soft Armor Vest' then 9
    when 'Small Backpack' then 1
    when 'Medkit' then 1
    else weight
end
where name in (
    'Light Pistol',
    'Soft Armor Vest',
    'Small Backpack',
    'Medkit'
);
