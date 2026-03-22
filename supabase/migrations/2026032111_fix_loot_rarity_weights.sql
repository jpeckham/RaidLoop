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
                return jsonb_build_array(game.authored_item('Makarov'));
            when 1 then
                return jsonb_build_array(
                    game.authored_item('Bandage'),
                    game.authored_item('6B2 body armor')
                );
            else
                return jsonb_build_array(game.authored_item('6B2 body armor'));
        end case;
    end if;

    if roll < 65 then
        return jsonb_build_array(
            game.authored_item('PPSH'),
            game.authored_item('Bandage')
        );
    end if;

    if roll < 68 then
        case floor(random() * 3)::int
            when 0 then
                return jsonb_build_array(
                    game.authored_item('AK74'),
                    game.authored_item('6B2 body armor')
                );
            when 1 then
                return jsonb_build_array(
                    game.authored_item('AK47'),
                    game.authored_item('Bandage')
                );
            else
                return jsonb_build_array(game.authored_item('6B13 assault armor'));
        end case;
    end if;

    if roll < 69 then
        case floor(random() * 2)::int
            when 0 then
                return jsonb_build_array(game.authored_item('SVDS'));
            else
                return jsonb_build_array(game.authored_item('FORT Defender-2'));
        end case;
    end if;

    case floor(random() * 2)::int
        when 0 then
            return jsonb_build_array(game.authored_item('PKP'));
        else
            return jsonb_build_array(game.authored_item('NFM THOR'));
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
                    game.authored_item('Makarov'),
                    game.authored_item('Ammo Box')
                );
            end if;

            if roll < 52 then
                return jsonb_build_array(game.authored_item('PPSH'));
            end if;

            if roll < 58 then
                case floor(random() * 2)::int
                    when 0 then
                        return jsonb_build_array(game.authored_item('AK74'));
                    else
                        return jsonb_build_array(game.authored_item('AK47'));
                end case;
            end if;

            if roll < 61 then
                return jsonb_build_array(game.authored_item('SVDS'));
            end if;

            return jsonb_build_array(game.authored_item('PKP'));

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
                return jsonb_build_array(game.authored_item('PPSH'));
            end if;

            if roll < 58 then
                case floor(random() * 2)::int
                    when 0 then
                        return jsonb_build_array(game.authored_item('Rare Scope'));
                    else
                        return jsonb_build_array(game.authored_item('AK74'));
                end case;
            end if;

            if roll < 61 then
                return jsonb_build_array(game.authored_item('SVDS'));
            end if;

            return jsonb_build_array(game.authored_item('Legendary Trigger Group'));
    end case;
end;
$$;
