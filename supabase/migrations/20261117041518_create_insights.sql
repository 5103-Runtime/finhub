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
  insight_type text not null,
  category_id uuid references public.categories (id) on delete cascade,
  transaction_id uuid references public.transactions (id) on delete cascade,
  currency text,
  period_start date,
  read_at timestamptz,
  dismissed_at timestamptz,
  dismissed_value numeric(14, 2),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint insight_states_insight_type check (
    insight_type in ('category_spike', 'unusual_purchase')
  ),
  constraint insight_states_subject check (
    (
      insight_type = 'category_spike'
      and category_id is not null
      and currency is not null
      and period_start is not null
      and transaction_id is null
    )
    or (
      insight_type = 'unusual_purchase'
      and transaction_id is not null
      and category_id is null
      and currency is null
      and period_start is null
    )
  ),
  constraint insight_states_currency_format check (currency is null or currency ~ '^[A-Z]{3}$'),
  constraint insight_states_period_start_first_of_month check (
    period_start is null or extract(day from period_start) = 1
  ),
  constraint insight_states_has_marker check (read_at is not null or dismissed_at is not null),
  constraint insight_states_dismissed_value_with_dismissal check (
    (dismissed_at is null) = (dismissed_value is null)
  ),
  constraint insight_states_dismissed_value_positive check (
    dismissed_value is null or dismissed_value > 0
  ),
  constraint insight_states_category_spike_key
    unique (user_id, insight_type, category_id, currency, period_start),
  constraint insight_states_unusual_purchase_key
    unique (user_id, insight_type, transaction_id)
);

create index insight_states_category_id_idx on public.insight_states (category_id);
create index insight_states_transaction_id_idx on public.insight_states (transaction_id);

comment on table public.insight_states is
  'Read and dismissed markers for insights the API computes on request; insights themselves are not stored.';

comment on column public.insight_states.period_start is
  'First day of the calendar month a category_spike covers.';

comment on column public.insight_states.dismissed_value is
  'Positive amount the insight showed when it was dismissed: the period spend for category_spike, the purchase amount for unusual_purchase.';

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

create or replace function public.validate_insight_state()
returns trigger
language plpgsql
set search_path = ''
as $$
begin
  if new.category_id is not null
    and not exists (
      select 1
      from public.categories category
      where category.id = new.category_id
        and (category.is_system or category.user_id = new.user_id)
    ) then
    raise exception 'Insight category must be a system category or belong to the state owner';
  end if;

  if new.transaction_id is not null
    and not exists (
      select 1
      from public.transactions transaction
      join public.financial_accounts account on account.id = transaction.account_id
      where transaction.id = new.transaction_id
        and account.user_id = new.user_id
    ) then
    raise exception 'Insight transaction must belong to the state owner';
  end if;

  return new;
end;
$$;

create trigger insight_states_validate
  before insert or update of user_id, category_id, transaction_id on public.insight_states
  for each row execute procedure public.validate_insight_state();

create trigger insight_states_set_updated_at
  before update on public.insight_states
  for each row execute procedure public.set_updated_at();

create table public.insight_mutes (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  category_id uuid references public.categories (id) on delete cascade,
  account_id uuid,
  created_at timestamptz not null default now(),
  constraint insight_mutes_one_target check (num_nonnulls(category_id, account_id) = 1),
  constraint insight_mutes_user_id_category_id_key unique (user_id, category_id),
  constraint insight_mutes_user_id_account_id_key unique (user_id, account_id),
  constraint insight_mutes_account_id_user_id_fkey
    foreign key (account_id, user_id)
    references public.financial_accounts (id, user_id)
    on delete cascade
);

create index insight_mutes_category_id_idx on public.insight_mutes (category_id);

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
set search_path = ''
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

  return new;
end;
$$;

create trigger insight_mutes_validate
  before insert or update of user_id, category_id on public.insight_mutes
  for each row execute procedure public.validate_insight_mute();
