create table public.financial_accounts (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  name text not null,
  institution text,
  account_type text not null,
  currency text not null default 'CAD',
  created_at timestamptz not null default timezone('utc', now()),
  updated_at timestamptz not null default timezone('utc', now()),
  constraint financial_accounts_name_length check (name = btrim(name, E' \t\r\n') and char_length(name) between 1 and 100),
  constraint financial_accounts_institution_length check (
    institution is null or (institution = btrim(institution, E' \t\r\n') and char_length(institution) between 1 and 100)
  ),
  constraint financial_accounts_account_type check (
    account_type in ('chequing', 'savings', 'credit_card')
  ),
  constraint financial_accounts_currency_format check (currency ~ '^[A-Z]{3}$')
);

create index financial_accounts_user_id_idx on public.financial_accounts (user_id);

comment on table public.financial_accounts is
  'Chequing, savings, and credit card accounts a user imports statements for; each belongs to one Supabase Auth user.';

alter table public.financial_accounts enable row level security;

create policy "Users can view their own financial accounts"
  on public.financial_accounts
  for select
  to authenticated
  using ((select auth.uid()) = user_id);

create policy "Users can create their own financial accounts"
  on public.financial_accounts
  for insert
  to authenticated
  with check ((select auth.uid()) = user_id);

create policy "Users can update their own financial accounts"
  on public.financial_accounts
  for update
  to authenticated
  using ((select auth.uid()) = user_id)
  with check ((select auth.uid()) = user_id);

create policy "Users can delete their own financial accounts"
  on public.financial_accounts
  for delete
  to authenticated
  using ((select auth.uid()) = user_id);

create trigger financial_accounts_set_updated_at
  before update on public.financial_accounts
  for each row execute procedure public.set_updated_at();
