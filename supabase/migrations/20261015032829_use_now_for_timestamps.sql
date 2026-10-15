alter table public.profiles
  alter column created_at set default now(),
  alter column updated_at set default now();

alter table public.financial_accounts
  alter column created_at set default now(),
  alter column updated_at set default now();

create or replace function public.set_updated_at()
returns trigger
language plpgsql
as $$
begin
  new.updated_at = now();
  return new;
end;
$$;
