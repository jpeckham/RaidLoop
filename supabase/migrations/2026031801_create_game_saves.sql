create table if not exists public.game_saves (
    user_id uuid primary key references auth.users (id) on delete cascade,
    save_version integer not null default 1,
    payload jsonb not null,
    created_at timestamptz not null default timezone('utc', now()),
    updated_at timestamptz not null default timezone('utc', now())
);

create index if not exists idx_game_saves_updated_at on public.game_saves (updated_at desc);
