create or replace function game.random_enemy_loadout()
returns jsonb
language plpgsql
volatile
as $$
declare
    roll int := floor(random() * 70)::int;
begin
    if roll < 55 then
        case floor(random() * 3)::int
            when 0 then
                return jsonb_build_array(game.authored_item('Light Pistol'));
            when 1 then
                return jsonb_build_array(
                    game.authored_item('Bandage'),
                    game.authored_item('Soft Armor Vest')
                );
            else
                return jsonb_build_array(game.authored_item('Soft Armor Vest'));
        end case;
    end if;

    if roll < 65 then
        return jsonb_build_array(
            game.authored_item('Drum SMG'),
            game.authored_item('Bandage')
        );
    end if;

    if roll < 68 then
        case floor(random() * 3)::int
            when 0 then
                return jsonb_build_array(
                    game.authored_item('Field Carbine'),
                    game.authored_item('Soft Armor Vest')
                );
            when 1 then
                return jsonb_build_array(
                    game.authored_item('Battle Rifle'),
                    game.authored_item('Bandage')
                );
            else
                return jsonb_build_array(game.authored_item('Light Plate Carrier'));
        end case;
    end if;

    if roll < 69 then
        case floor(random() * 2)::int
            when 0 then
                return jsonb_build_array(game.authored_item('Marksman Rifle'));
            else
                return jsonb_build_array(game.authored_item('Medium Plate Carrier'));
        end case;
    end if;

    case floor(random() * 2)::int
        when 0 then
            return jsonb_build_array(game.authored_item('Support Machine Gun'));
        else
            return jsonb_build_array(game.authored_item('Assault Plate Carrier'));
    end case;
end;
$$;

create or replace function game.random_loot_items_for_container(container_name text)
returns jsonb
language plpgsql
volatile
as $$
declare
    roll int;
begin
    case container_name
        when 'Weapons Crate' then
            roll := floor(random() * 63)::int;

            if roll < 40 then
                return jsonb_build_array(
                    game.authored_item('Light Pistol'),
                    game.authored_item('Ammo Box')
                );
            end if;

            if roll < 52 then
                return jsonb_build_array(game.authored_item('Drum SMG'));
            end if;

            if roll < 58 then
                case floor(random() * 2)::int
                    when 0 then
                        return jsonb_build_array(game.authored_item('Field Carbine'));
                    else
                        return jsonb_build_array(game.authored_item('Battle Rifle'));
                end case;
            end if;

            if roll < 61 then
                return jsonb_build_array(game.authored_item('Marksman Rifle'));
            end if;

            return jsonb_build_array(game.authored_item('Support Machine Gun'));

        when 'Medical Container' then
            case floor(random() * 10)::int
                when 0, 1, 2, 3 then
                    return jsonb_build_array(
                        game.authored_item('Medkit'),
                        game.authored_item('Bandage')
                    );
                when 4, 5, 6 then
                    return jsonb_build_array(
                        game.authored_item('Bandage'),
                        game.authored_item('Ammo Box')
                    );
                when 7, 8 then
                    return jsonb_build_array(game.authored_item('Medkit'));
                else
                    return jsonb_build_array(
                        game.authored_item('Medkit'),
                        game.authored_item('Ammo Box')
                    );
            end case;

        when 'Dead Body' then
            return game.random_enemy_loadout();

        else
            roll := floor(random() * 63)::int;

            if roll < 40 then
                case floor(random() * 3)::int
                    when 0 then
                        return jsonb_build_array(
                            game.authored_item('Bandage'),
                            game.authored_item('Ammo Box')
                        );
                    when 1 then
                        return jsonb_build_array(game.authored_item('Scrap Metal'));
                    else
                        return jsonb_build_array(game.authored_item('Medkit'));
                end case;
            end if;

            if roll < 52 then
                return jsonb_build_array(game.authored_item('Drum SMG'));
            end if;

            if roll < 58 then
                case floor(random() * 2)::int
                    when 0 then
                        return jsonb_build_array(game.authored_item('Rare Scope'));
                    else
                        return jsonb_build_array(game.authored_item('Field Carbine'));
                end case;
            end if;

            if roll < 61 then
                return jsonb_build_array(game.authored_item('Marksman Rifle'));
            end if;

            return jsonb_build_array(game.authored_item('Legendary Trigger Group'));
    end case;
end;
$$;
