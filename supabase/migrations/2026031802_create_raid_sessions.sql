create table if not exists public.raid_sessions (
    user_id uuid primary key references auth.users (id) on delete cascade,
    profile text not null,
    payload jsonb not null,
    created_at timestamptz not null default timezone('utc', now()),
    updated_at timestamptz not null default timezone('utc', now())
);

create index if not exists idx_raid_sessions_updated_at on public.raid_sessions (updated_at desc);
