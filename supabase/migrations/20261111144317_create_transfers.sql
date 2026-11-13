create unique index financial_accounts_id_user_id_idx
  on public.financial_accounts (id, user_id);

create table public.transfers (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users (id) on delete cascade,
  from_account_id uuid not null,
  to_account_id uuid not null,
  amount numeric(14, 2) not null,
  transfer_date date not null,
  description text not null,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint transfers_distinct_accounts check (from_account_id <> to_account_id),
  constraint transfers_amount_positive check (amount > 0),
  constraint transfers_description_length check (
    description = btrim(description, E' \t\r\n') and char_length(description) between 1 and 500
  ),
  constraint transfers_from_account_owner_fkey
    foreign key (from_account_id, user_id)
    references public.financial_accounts (id, user_id)
    on delete restrict,
  constraint transfers_to_account_owner_fkey
    foreign key (to_account_id, user_id)
    references public.financial_accounts (id, user_id)
    on delete restrict
);

comment on table public.transfers is
  'Internal money movements represented by two linked transaction ledger lines.';

comment on column public.transfers.user_id is
  'The Supabase Auth user who owns both accounts in the transfer.';

create index transfers_user_id_idx on public.transfers (user_id);
create index transfers_from_account_id_idx on public.transfers (from_account_id);
create index transfers_to_account_id_idx on public.transfers (to_account_id);

alter table public.transactions
  add column transfer_id uuid references public.transfers (id) on delete cascade;

create index transactions_transfer_id_idx on public.transactions (transfer_id);

comment on column public.transactions.transfer_id is
  'Optional transfer identifier for linked internal transfer ledger lines.';

create or replace function public.validate_transfer_currency()
returns trigger
language plpgsql
as $$
begin
  if not exists (
    select 1
    from public.financial_accounts from_account
    join public.financial_accounts to_account on to_account.id = new.to_account_id
    where from_account.id = new.from_account_id
      and from_account.currency = to_account.currency
  ) then
    raise exception 'Transfer accounts must use the same currency';
  end if;

  return new;
end;
$$;

create trigger transfers_validate_currency
  before insert or update of user_id, from_account_id, to_account_id on public.transfers
  for each row execute procedure public.validate_transfer_currency();

create or replace function public.validate_transfer_transaction_update()
returns trigger
language plpgsql
as $$
declare
  transfer_record public.transfers;
  expected_amount numeric(14, 2);
begin
  if old.transfer_id is null then
    return new;
  end if;

  if old.transfer_id is distinct from new.transfer_id
    or old.account_id is distinct from new.account_id then
    raise exception 'Transfer transaction links and accounts cannot be changed';
  end if;

  if old.transaction_date is distinct from new.transaction_date
    or old.description is distinct from new.description
    or old.amount is distinct from new.amount then
    select *
      into transfer_record
      from public.transfers
      where id = old.transfer_id;

    if not found then
      raise exception 'Transfer transaction references a missing transfer';
    end if;

    expected_amount := case
      when new.account_id = transfer_record.from_account_id then -transfer_record.amount
      when new.account_id = transfer_record.to_account_id then transfer_record.amount
      else null
    end;

    if new.transaction_date is distinct from transfer_record.transfer_date
      or new.description is distinct from transfer_record.description
      or new.amount is distinct from expected_amount then
      raise exception 'Transfer transaction fields must match the transfer';
    end if;
  end if;

  return new;
end;
$$;

create trigger transactions_validate_transfer_update
  before update of transfer_id, account_id, transaction_date, description, amount
  on public.transactions
  for each row execute procedure public.validate_transfer_transaction_update();

alter policy "Users can create transactions in their own accounts"
  on public.transactions
  with check (
    account_id in (select id from public.financial_accounts where user_id = (select auth.uid()))
    and (
      transfer_id is null
      or transfer_id in (
        select id
        from public.transfers
          where user_id = (select auth.uid())
      )
    )
  );

alter policy "Users can update transactions in their own accounts"
  on public.transactions
  using (account_id in (select id from public.financial_accounts where user_id = (select auth.uid())))
  with check (
    account_id in (select id from public.financial_accounts where user_id = (select auth.uid()))
    and (
      transfer_id is null
      or transfer_id in (
        select id
        from public.transfers
          where user_id = (select auth.uid())
      )
    )
  );

alter table public.transfers enable row level security;

create policy "Users can view transfers in their own accounts"
  on public.transfers
  for select
  to authenticated
  using (user_id = (select auth.uid()));

create policy "Users can create transfers in their own accounts"
  on public.transfers
  for insert
  to authenticated
  with check (user_id = (select auth.uid()));

create policy "Users can update transfers in their own accounts"
  on public.transfers
  for update
  to authenticated
  using (user_id = (select auth.uid()))
  with check (user_id = (select auth.uid()));

create policy "Users can delete transfers in their own accounts"
  on public.transfers
  for delete
  to authenticated
  using (user_id = (select auth.uid()));

create trigger transfers_set_updated_at
  before update on public.transfers
  for each row execute procedure public.set_updated_at();

create or replace function public.validate_transfer_ledger()
returns trigger
language plpgsql
as $$
declare
  transfer_record public.transfers;
  transaction_count integer;
  transfer_record_id uuid;
begin
  if tg_table_name = 'transactions' then
    transfer_record_id := case when tg_op = 'DELETE' then old.transfer_id else new.transfer_id end;
    if transfer_record_id is null then
      if tg_op = 'DELETE' then
        return old;
      end if;
      return new;
    end if;
    if not exists (select 1 from public.transfers t where t.id = transfer_record_id) then
      if tg_op = 'DELETE' then
        return old;
      end if;
      raise exception 'Transaction references a missing transfer';
    end if;
  else
    transfer_record_id := case when tg_op = 'DELETE' then old.id else new.id end;
  end if;

  select *
    into transfer_record
    from public.transfers
    where id = transfer_record_id;

  select count(*)
    into transaction_count
    from public.transactions t
    where t.transfer_id = transfer_record.id;

  if transaction_count <> 2
     or not exists (
       select 1
       from public.transactions t
       where t.transfer_id = transfer_record.id
         and t.account_id = transfer_record.from_account_id
         and t.amount = -transfer_record.amount
         and t.transaction_date = transfer_record.transfer_date
         and t.description = transfer_record.description
     )
     or not exists (
       select 1
       from public.transactions t
       where t.transfer_id = transfer_record.id
         and t.account_id = transfer_record.to_account_id
         and t.amount = transfer_record.amount
         and t.transaction_date = transfer_record.transfer_date
         and t.description = transfer_record.description
     ) then
    raise exception 'Transfer must have one matching outgoing and incoming transaction';
  end if;

  if tg_op = 'DELETE' then
    return old;
  end if;
  return new;
end;
$$;

create constraint trigger transactions_validate_transfer_ledger
  after insert or update or delete on public.transactions
  deferrable initially deferred
  for each row execute procedure public.validate_transfer_ledger();

create constraint trigger transfers_validate_transfer_ledger
  after insert or update on public.transfers
  deferrable initially deferred
  for each row execute procedure public.validate_transfer_ledger();
