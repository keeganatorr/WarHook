-- WarHook online leaderboards.
-- Run once in your Supabase project (Dashboard -> SQL Editor -> New query).

create table if not exists public.scores (
    id bigint generated always as identity primary key,
    mode text not null check (mode in ('game_a', 'game_b')),
    initials text not null,
    score int not null,
    created_at timestamptz not null default now(),
    client_ip inet not null default inet_client_addr()
);

create index if not exists scores_mode_score_idx on public.scores (mode, score desc);

alter table public.scores enable row level security;

-- Anyone may read the table; writes only happen through the RPC below.
drop policy if exists "scores are readable" on public.scores;
create policy "scores are readable" on public.scores
    for select to anon using (true);

-- Best score per initials (newest timestamp wins ties).
create or replace function public.top_scores(p_mode text, p_limit int default 10)
returns table (initials text, score int, created_at timestamptz)
language sql stable security definer set search_path = public as $$
    select initials, score, created_at
    from (
        select distinct on (s.initials) s.initials, s.score, s.created_at
        from public.scores s
        where s.mode = p_mode
        order by s.initials, s.score desc, s.created_at asc
    ) best
    order by score desc, created_at asc
    limit greatest(least(coalesce(p_limit, 10), 25), 1);
$$;

-- Validated submission. The anon key is public, so the RPC enforces the rules:
-- known mode, three initials, sane score range, and a per-IP rate limit.
create or replace function public.submit_score(p_mode text, p_initials text, p_score int)
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
    insert into public.scores (mode, initials, score) values (p_mode, v_initials, p_score);
    return true;
end;
$$;

grant execute on function public.submit_score(text, text, int) to anon;
grant execute on function public.top_scores(text, int) to anon;
