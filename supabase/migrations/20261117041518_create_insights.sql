create table public.insight_preferences (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  insight_type text not null,
  is_enabled boolean not null default true,
  threshold numeric(6, 2),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint insight_preferences_insight_type check (
    insight_type in ('category_spike', 'unusual_purchase')
  ),
  constraint insight_preferences_threshold_positive check (threshold is null or threshold > 0),
  constraint insight_preferences_user_id_insight_type_key unique (user_id, insight_type)
);

comment on table public.insight_preferences is
  'Per-user settings for one insight type; a missing row or null threshold means the API default applies.';

comment on column public.insight_preferences.threshold is
  'Percent above the trailing average for category_spike; multiple of the category median for unusual_purchase.';

alter table public.insight_preferences enable row level security;

create policy "Users can view their own insight preferences"
  on public.insight_preferences
  for select
  to authenticated
  using ((select auth.uid()) = user_id);

create policy "Users can create their own insight preferences"
  on public.insight_preferences
  for insert
  to authenticated
  with check ((select auth.uid()) = user_id);

create policy "Users can update their own insight preferences"
  on public.insight_preferences
  for update
  to authenticated
  using ((select auth.uid()) = user_id)
  with check ((select auth.uid()) = user_id);

create policy "Users can delete their own insight preferences"
  on public.insight_preferences
  for delete
  to authenticated
  using ((select auth.uid()) = user_id);

create trigger insight_preferences_set_updated_at
  before update on public.insight_preferences
  for each row execute procedure public.set_updated_at();

create table public.insight_states (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  insight_key text not null,
  read_at timestamptz,
  dismissed_at timestamptz,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint insight_states_insight_key_length check (
    insight_key = btrim(insight_key, E' \t\r\n') and char_length(insight_key) between 1 and 200
  ),
  constraint insight_states_user_id_insight_key_key unique (user_id, insight_key)
);

comment on table public.insight_states is
  'Read and dismissed markers for insights the API computes on request; insights themselves are not stored.';

comment on column public.insight_states.insight_key is
  'Identifier the API derives from the insight type, its subject, and its period.';

alter table public.insight_states enable row level security;

create policy "Users can view their own insight states"
  on public.insight_states
  for select
  to authenticated
  using ((select auth.uid()) = user_id);

create policy "Users can create their own insight states"
  on public.insight_states
  for insert
  to authenticated
  with check ((select auth.uid()) = user_id);

create policy "Users can update their own insight states"
  on public.insight_states
  for update
  to authenticated
  using ((select auth.uid()) = user_id)
  with check ((select auth.uid()) = user_id);

create policy "Users can delete their own insight states"
  on public.insight_states
  for delete
  to authenticated
  using ((select auth.uid()) = user_id);

create trigger insight_states_set_updated_at
  before update on public.insight_states
  for each row execute procedure public.set_updated_at();

create table public.insight_mutes (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  category_id uuid references public.categories (id) on delete cascade,
  account_id uuid references public.financial_accounts (id) on delete cascade,
  created_at timestamptz not null default now(),
  constraint insight_mutes_one_target check (num_nonnulls(category_id, account_id) = 1),
  constraint insight_mutes_user_id_category_id_key unique (user_id, category_id),
  constraint insight_mutes_user_id_account_id_key unique (user_id, account_id)
);

create index insight_mutes_category_id_idx on public.insight_mutes (category_id);
create index insight_mutes_account_id_idx on public.insight_mutes (account_id);

comment on table public.insight_mutes is
  'Categories and accounts a user has excluded from insights; everything else is included.';

alter table public.insight_mutes enable row level security;

create policy "Users can view their own insight mutes"
  on public.insight_mutes
  for select
  to authenticated
  using ((select auth.uid()) = user_id);

create policy "Users can create their own insight mutes"
  on public.insight_mutes
  for insert
  to authenticated
  with check ((select auth.uid()) = user_id);

create policy "Users can delete their own insight mutes"
  on public.insight_mutes
  for delete
  to authenticated
  using ((select auth.uid()) = user_id);

create or replace function public.validate_insight_mute()
returns trigger
language plpgsql
as $$
begin
  if new.category_id is not null
    and not exists (
      select 1
      from public.categories category
      where category.id = new.category_id
        and (category.is_system or category.user_id = new.user_id)
    ) then
    raise exception 'Muted category must be a system category or belong to the mute owner';
  end if;

  if new.account_id is not null
    and not exists (
      select 1
      from public.financial_accounts account
      where account.id = new.account_id
        and account.user_id = new.user_id
    ) then
    raise exception 'Muted account must belong to the mute owner';
  end if;

  return new;
end;
$$;

create trigger insight_mutes_validate
  before insert or update of user_id, category_id, account_id on public.insight_mutes
  for each row execute procedure public.validate_insight_mute();
