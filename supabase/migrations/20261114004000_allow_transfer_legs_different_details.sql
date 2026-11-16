create or replace function public.validate_transfer_transaction_update()
returns trigger
language plpgsql
as $$
declare
  transfer_record public.transfers;
  expected_amount numeric(14, 2);
begin
  if new.transfer_id is null or old.transfer_id is null then
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

    if new.amount is distinct from expected_amount then
      raise exception 'Transfer transaction amount must match the transfer';
    end if;
  end if;

  return new;
end;
$$;

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
    transfer_record_id := case
      when tg_op = 'DELETE' then old.transfer_id
      when new.transfer_id is null then old.transfer_id
      else new.transfer_id
    end;
    if transfer_record_id is null then
      if tg_op = 'DELETE' then
        return old;
      end if;
      return new;
    end if;
    if not exists (select 1 from public.transfers t where t.id = transfer_record_id) then
      if tg_op = 'DELETE' or new.transfer_id is null then
        return case when tg_op = 'DELETE' then old else new end;
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
     )
     or not exists (
       select 1
       from public.transactions t
       where t.transfer_id = transfer_record.id
         and t.account_id = transfer_record.to_account_id
         and t.amount = transfer_record.amount
     ) then
    raise exception 'Transfer must have one matching outgoing and incoming transaction';
  end if;

  if tg_op = 'DELETE' then
    return old;
  end if;
  return new;
end;
$$;
