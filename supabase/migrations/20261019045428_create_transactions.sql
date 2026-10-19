revoke all on all sequences in schema public from anon, authenticated;

create table public.transactions (
  id uuid primary key default gen_random_uuid(),
  account_id uuid not null references public.financial_accounts (id) on delete cascade,
  transaction_date date not null,
  description text not null,
  amount numeric(14, 2) not null,
  balance_after numeric(14, 2),
  statement_line integer,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint transactions_description_length check (
    description = btrim(description, E' \t\r\n') and char_length(description) between 1 and 500
  ),
  constraint transactions_statement_line_positive check (statement_line is null or statement_line > 0)
);

create index transactions_account_id_idx on public.transactions (account_id);

comment on table public.transactions is
  'Ledger lines imported from statements or entered by hand; each belongs to one financial account.';

alter table public.transactions enable row level security;

create policy "Users can view transactions in their own accounts"
  on public.transactions
  for select
  to authenticated
  using (account_id in (select id from public.financial_accounts where user_id = (select auth.uid())));

create policy "Users can create transactions in their own accounts"
  on public.transactions
  for insert
  to authenticated
  with check (account_id in (select id from public.financial_accounts where user_id = (select auth.uid())));

create policy "Users can update transactions in their own accounts"
  on public.transactions
  for update
  to authenticated
  using (account_id in (select id from public.financial_accounts where user_id = (select auth.uid())))
  with check (account_id in (select id from public.financial_accounts where user_id = (select auth.uid())));

create policy "Users can delete transactions in their own accounts"
  on public.transactions
  for delete
  to authenticated
  using (account_id in (select id from public.financial_accounts where user_id = (select auth.uid())));

create trigger transactions_set_updated_at
  before update on public.transactions
  for each row execute procedure public.set_updated_at();
