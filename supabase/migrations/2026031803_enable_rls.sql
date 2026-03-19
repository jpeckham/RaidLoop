alter table public.game_saves enable row level security;
alter table public.raid_sessions enable row level security;

drop policy if exists "Users can view their own saves" on public.game_saves;
create policy "Users can view their own saves"
on public.game_saves
for select
to authenticated
using (auth.uid() = user_id);

drop policy if exists "Users can update their own saves" on public.game_saves;
create policy "Users can update their own saves"
on public.game_saves
for update
to authenticated
using (auth.uid() = user_id)
with check (auth.uid() = user_id);

drop policy if exists "Users can view their own raid sessions" on public.raid_sessions;
create policy "Users can view their own raid sessions"
on public.raid_sessions
for select
to authenticated
using (auth.uid() = user_id);

drop policy if exists "Users can update their own raid sessions" on public.raid_sessions;
create policy "Users can update their own raid sessions"
on public.raid_sessions
for update
to authenticated
using (auth.uid() = user_id)
with check (auth.uid() = user_id);
