update game.item_defs
set weight = case name
    when 'Makarov' then 2
    when '6B2 body armor' then 9
    when 'Small Backpack' then 1
    when 'Medkit' then 1
    else weight
end
where name in (
    'Makarov',
    '6B2 body armor',
    'Small Backpack',
    'Medkit'
);
