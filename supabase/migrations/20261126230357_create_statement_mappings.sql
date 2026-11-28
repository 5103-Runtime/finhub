create or replace function public.is_statement_mapping_list(list text[])
returns boolean
language sql
immutable
set search_path = ''
as $$
  select coalesce(array_ndims(list) = 1, false)
    and not exists (
      select 1
      from unnest(list) as entry
      where entry is null
        or entry = ''
        or entry <> btrim(entry, E' \t\r\n')
    )
    and not exists (
      select 1
      from unnest(list) as entry
      group by entry
      having count(*) > 1
    );
$$;

comment on function public.is_statement_mapping_list(text[]) is
  'True for a one-dimensional list with at least one entry and no null, empty, untrimmed, or repeated entries.';

create table public.statement_mappings (
  id uuid primary key default gen_random_uuid(),
  bank text not null,
  account_type text not null,
  match text[] not null,
  account_type_column text,
  account_type_values text[],
  account_column text,
  date_column text not null,
  date_format text not null,
  description_columns text[] not null,
  amount_column text not null,
  invert boolean not null,
  balance_column text,
  status_column text,
  import_statuses text[],
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint statement_mappings_bank_format check (bank ~ '^[a-z0-9_]+$'),
  constraint statement_mappings_account_type check (
    account_type in ('chequing', 'savings', 'credit_card')
  ),
  constraint statement_mappings_date_format_trimmed check (
    date_format <> '' and date_format = btrim(date_format, E' \t\r\n')
  ),
  constraint statement_mappings_list_entries check (
    public.is_statement_mapping_list(match)
    and public.is_statement_mapping_list(description_columns)
    and (account_type_values is null or public.is_statement_mapping_list(account_type_values))
    and (import_statuses is null or public.is_statement_mapping_list(import_statuses))
  ),
  constraint statement_mappings_columns_in_match check (
    date_column = any (match)
    and amount_column = any (match)
    and description_columns <@ match
    and (account_type_column is null or account_type_column = any (match))
    and (account_column is null or account_column = any (match))
    and (balance_column is null or balance_column = any (match))
    and (status_column is null or status_column = any (match))
  ),
  constraint statement_mappings_account_type_values_with_column check (
    (account_type_column is null) = (account_type_values is null)
  ),
  constraint statement_mappings_import_statuses_with_column check (
    (status_column is null) = (import_statuses is null)
  ),
  constraint statement_mappings_bank_account_type_key unique (bank, account_type)
);

comment on table public.statement_mappings is
  'How each bank''s CSV export maps onto ledger transactions; one row per bank and account type, shared by all users.';

comment on column public.statement_mappings.match is
  'Header cells that must all be present for the mapping to apply; every column the mapping reads is in this list.';

comment on column public.statement_mappings.account_type_values is
  'Values of account_type_column this mapping applies to, for banks that share one header across account types.';

comment on column public.statement_mappings.date_format is
  '.NET date format string; the API checks that it can read a full date.';

comment on column public.statement_mappings.invert is
  'True for exports that list charges as positive, so every sign is flipped.';

comment on column public.statement_mappings.import_statuses is
  'Values of status_column whose rows are imported; rows with any other status are skipped.';

alter table public.statement_mappings enable row level security;

create trigger statement_mappings_set_updated_at
  before update on public.statement_mappings
  for each row execute procedure public.set_updated_at();

insert into public.statement_mappings (
  bank,
  account_type,
  match,
  account_type_column,
  account_type_values,
  account_column,
  date_column,
  date_format,
  description_columns,
  amount_column,
  invert,
  balance_column,
  status_column,
  import_statuses
)
values
  (
    'rbc',
    'chequing',
    array['Account Type', 'Account Number', 'Transaction Date', 'Cheque Number', 'Description 1', 'Description 2', 'CAD$', 'USD$'],
    'Account Type',
    array['Chequing'],
    'Account Number',
    'Transaction Date',
    'M/d/yyyy',
    array['Description 1', 'Description 2'],
    'CAD$',
    false,
    null,
    null,
    null
  ),
  (
    'rbc',
    'credit_card',
    array['Account Type', 'Account Number', 'Transaction Date', 'Cheque Number', 'Description 1', 'Description 2', 'CAD$', 'USD$'],
    'Account Type',
    array['Visa', 'MasterCard'],
    'Account Number',
    'Transaction Date',
    'M/d/yyyy',
    array['Description 1', 'Description 2'],
    'CAD$',
    false,
    null,
    null,
    null
  ),
  (
    'rbc',
    'savings',
    array['Account Type', 'Account Number', 'Transaction Date', 'Cheque Number', 'Description 1', 'Description 2', 'CAD$', 'USD$'],
    'Account Type',
    array['Savings'],
    'Account Number',
    'Transaction Date',
    'M/d/yyyy',
    array['Description 1', 'Description 2'],
    'CAD$',
    false,
    null,
    null,
    null
  ),
  (
    'scotiabank',
    'chequing',
    array['Filter', 'Date', 'Description', 'Sub-description', 'Type of Transaction', 'Amount', 'Balance'],
    null,
    null,
    null,
    'Date',
    'M/d/yyyy',
    array['Description', 'Sub-description'],
    'Amount',
    false,
    'Balance',
    null,
    null
  ),
  (
    'scotiabank',
    'credit_card',
    array['Filter', 'Date', 'Description', 'Sub-description', 'Status', 'Type of Transaction', 'Amount', 'Associated card number'],
    null,
    null,
    null,
    'Date',
    'M/d/yyyy',
    array['Description', 'Sub-description'],
    'Amount',
    true,
    null,
    'Status',
    array['posted']
  );
