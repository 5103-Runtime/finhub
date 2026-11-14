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

    if new.transaction_date is distinct from transfer_record.transfer_date
      or new.description is distinct from transfer_record.description
      or new.amount is distinct from expected_amount then
      raise exception 'Transfer transaction fields must match the transfer';
    end if;
  end if;

  return new;
end;
$$;
