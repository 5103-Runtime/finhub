create table public.categorization_rules (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  category_id uuid not null references public.categories (id) on delete cascade,
  account_id uuid references public.financial_accounts (id) on delete cascade,
  description_contains text,
  amount_direction text,
  priority integer not null,
  is_enabled boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint categorization_rules_description_contains_length check (
    description_contains is null
    or (description_contains = btrim(description_contains, E' \t\r\n') and char_length(description_contains) between 1 and 500)
  ),
  constraint categorization_rules_amount_direction check (
    amount_direction is null or amount_direction in ('in', 'out')
  ),
  constraint categorization_rules_has_criterion check (
    account_id is not null or description_contains is not null or amount_direction is not null
  ),
  constraint categorization_rules_user_id_priority_key unique (user_id, priority) deferrable initially deferred
);

create index categorization_rules_category_id_idx on public.categorization_rules (category_id);
create index categorization_rules_account_id_idx on public.categorization_rules (account_id);

comment on table public.categorization_rules is
  'Rules a user defines to assign a category to matching transactions; each belongs to one Supabase Auth user.';

alter table public.categorization_rules enable row level security;

create policy "Users can view their own categorization rules"
  on public.categorization_rules
  for select
  to authenticated
  using ((select auth.uid()) = user_id);

create policy "Users can create their own categorization rules"
  on public.categorization_rules
  for insert
  to authenticated
  with check ((select auth.uid()) = user_id);

create policy "Users can update their own categorization rules"
  on public.categorization_rules
  for update
  to authenticated
  using ((select auth.uid()) = user_id)
  with check ((select auth.uid()) = user_id);

create policy "Users can delete their own categorization rules"
  on public.categorization_rules
  for delete
  to authenticated
  using ((select auth.uid()) = user_id);

create or replace function public.validate_categorization_rule()
returns trigger
language plpgsql
as $$
begin
  if not exists (
    select 1
    from public.categories category
    where category.id = new.category_id
      and (category.is_system or category.user_id = new.user_id)
  ) then
    raise exception 'Rule category must be a system category or belong to the rule owner';
  end if;

  if new.account_id is not null
    and not exists (
      select 1
      from public.financial_accounts account
      where account.id = new.account_id
        and account.user_id = new.user_id
    ) then
    raise exception 'Rule account must belong to the rule owner';
  end if;

  return new;
end;
$$;

create trigger categorization_rules_validate
  before insert or update of user_id, category_id, account_id on public.categorization_rules
  for each row execute procedure public.validate_categorization_rule();

create trigger categorization_rules_set_updated_at
  before update on public.categorization_rules
  for each row execute procedure public.set_updated_at();
