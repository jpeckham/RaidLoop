update game.item_defs
set weight = case name
    when 'Rusty Knife' then 1
    when 'Makarov' then 2
    when 'PPSH' then 12
    when 'AK74' then 7
    when 'SVDS' then 10
    when 'AK47' then 10
    when 'PKP' then 18
    when '6B2 body armor' then 9
    when 'BNTI Kirasa-N' then 7
    when '6B13 assault armor' then 7
    when 'FORT Defender-2' then 22
    when '6B43 Zabralo-Sh body armor' then 28
    when 'NFM THOR' then 19
    when 'Small Backpack' then 1
    when 'Large Backpack' then 1
    when 'Tactical Backpack' then 2
    when 'Tasmanian Tiger Trooper 35' then 2
    when '6Sh118' then 8
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
    'Makarov',
    'PPSH',
    'AK74',
    'SVDS',
    'AK47',
    'PKP',
    '6B2 body armor',
    'BNTI Kirasa-N',
    '6B13 assault armor',
    'FORT Defender-2',
    '6B43 Zabralo-Sh body armor',
    'NFM THOR',
    'Small Backpack',
    'Large Backpack',
    'Tactical Backpack',
    'Tasmanian Tiger Trooper 35',
    '6Sh118',
    'Medkit',
    'Bandage',
    'Ammo Box',
    'Scrap Metal',
    'Rare Scope',
    'Legendary Trigger Group'
);
