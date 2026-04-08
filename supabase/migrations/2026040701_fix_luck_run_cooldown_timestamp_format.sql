create or replace function game.settle_random_character(random_character jsonb, random_available_at jsonb)
returns jsonb
language plpgsql
volatile
as $$
declare
    normalized_random_character jsonb := game.normalize_random_character(random_character);
    normalized_available_at jsonb := coalesce(random_available_at, to_jsonb('0001-01-01T00:00:00+00:00'::text));
    available_at_text text;
begin
    if jsonb_typeof(normalized_available_at) = 'string' then
        available_at_text := replace(trim(both '"' from normalized_available_at::text), ' ', 'T');
        if available_at_text <> '0001-01-01T00:00:00+00:00'
           and position('+' in available_at_text) = 0
           and position('Z' in available_at_text) = 0 then
            available_at_text := available_at_text || '+00:00';
        end if;

        normalized_available_at := to_jsonb(available_at_text);
    end if;

    if normalized_random_character is not null
       and jsonb_array_length(coalesce(normalized_random_character->'inventory', '[]'::jsonb)) = 0 then
        normalized_random_character := null;
        normalized_available_at := to_jsonb('0001-01-01T00:00:00+00:00'::text);
    end if;

    return jsonb_build_object(
        'randomCharacterAvailableAt', normalized_available_at,
        'randomCharacter', normalized_random_character
    );
end;
$$;

revoke all on function game.settle_random_character(jsonb, jsonb) from public;
