-- WarHook online leaderboards.
-- Run once in your Supabase project (Dashboard -> SQL Editor -> New query).

create table if not exists public.scores (
    id bigint generated always as identity primary key,
    mode text not null check (mode in ('game_a', 'game_b')),
    initials text not null,
    score int not null,
    player_id text,
    created_at timestamptz not null default now(),
    client_ip inet not null default inet_client_addr()
);

alter table public.scores add column if not exists player_id text;

create index if not exists scores_mode_score_idx on public.scores (mode, score desc);

alter table public.scores enable row level security;

-- Anyone may read the table; writes only happen through the RPC below.
drop policy if exists "scores are readable" on public.scores;
create policy "scores are readable" on public.scores
    for select to anon using (true);

-- Best score per player and initials (newest timestamp wins ties). Legacy rows
-- without a player_id remain visible but cannot be marked as the current player.
drop function if exists public.top_scores(text, integer);
drop function if exists public.top_scores(text, text, integer);
create function public.top_scores(p_mode text, p_player_id text default null, p_limit int default 10)
returns table (initials text, score int, created_at timestamptz, is_mine boolean)
language sql stable security definer set search_path = public as $$
    select initials, score, created_at,
        coalesce(player_id = p_player_id, false) as is_mine
    from (
        select distinct on (s.player_id, s.initials) s.initials, s.score, s.created_at, s.player_id
        from public.scores s
        where s.mode = p_mode
        order by s.player_id, s.initials, s.score desc, s.created_at asc
    ) best
    order by score desc, created_at asc
    limit greatest(least(coalesce(p_limit, 10), 25), 1);
$$;

-- Validated submission. The anon key is public, so the RPC enforces the rules:
-- known mode, three initials, sane score range, a valid player id, and a
-- per-IP rate limit.
drop function if exists public.submit_score(text, text, integer);
drop function if exists public.submit_score(text, text, integer, text);
create function public.submit_score(p_mode text, p_initials text, p_score int, p_player_id text)
returns boolean
language plpgsql security definer set search_path = public as $$
declare
    v_initials text := upper(coalesce(p_initials, ''));
begin
    if p_mode not in ('game_a', 'game_b') then
        raise exception 'invalid mode';
    end if;
    if v_initials !~ '^[A-Z0-9]{3}$' then
        raise exception 'invalid initials';
    end if;
    if p_player_id is null or p_player_id !~ '^[a-f0-9]{32}$' then
        raise exception 'invalid player id';
    end if;
    if p_score is null or p_score < 0 or p_score > 9999999 then
        raise exception 'invalid score';
    end if;
    if exists (
        select 1 from public.scores s
        where s.client_ip = inet_client_addr()
          and s.created_at > now() - interval '5 seconds'
    ) then
        raise exception 'rate limited';
    end if;
    insert into public.scores (mode, initials, score, player_id)
        values (p_mode, v_initials, p_score, p_player_id);
    return true;
end;
$$;

grant execute on function public.submit_score(text, text, int, text) to anon;
grant execute on function public.top_scores(text, text, int) to anon;

-- Supabase's PostgREST layer caches function signatures. Refresh it so a
-- newly-created RPC is available to the verification request immediately.
notify pgrst, 'reload schema';
