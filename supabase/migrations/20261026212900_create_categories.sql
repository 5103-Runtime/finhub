create table public.categories (
  id uuid primary key default gen_random_uuid(),
  user_id uuid references auth.users (id) on delete cascade,
  name text not null,
  slug text not null,
  is_system boolean not null default false,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint categories_name_length check (
    name = btrim(name, E' \t\r\n') and char_length(name) between 1 and 50
  ),
  constraint categories_slug_format check (slug ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'),
  constraint categories_system_ownership check (
    (is_system and user_id is null) or (not is_system and user_id is not null)
  )
);

create unique index categories_system_slug_idx
  on public.categories (slug)
  where is_system;

create unique index categories_user_name_idx
  on public.categories (user_id, lower(name))
  where not is_system;

create index categories_user_id_idx on public.categories (user_id);

comment on table public.categories is
  'System categories shared by all users and optional user-owned custom categories.';

comment on column public.categories.user_id is
  'Null for immutable system categories; set to the owning user for custom categories.';

comment on column public.categories.slug is
  'Stable lowercase identifier reserved for use by the API and web client as a machine-readable category key.';

comment on column public.categories.is_system is
  'System categories are seeded separately and cannot be changed or deleted by users.';

alter table public.categories enable row level security;

create policy "Users can view system and their own categories"
  on public.categories
  for select
  to authenticated
  using (is_system or (select auth.uid()) = user_id);

create policy "Users can create their own custom categories"
  on public.categories
  for insert
  to authenticated
  with check (
    not is_system and (select auth.uid()) = user_id
  );

create policy "Users can update their own custom categories"
  on public.categories
  for update
  to authenticated
  using (
    not is_system and (select auth.uid()) = user_id
  )
  with check (
    not is_system and (select auth.uid()) = user_id
  );

create policy "Users can delete their own custom categories"
  on public.categories
  for delete
  to authenticated
  using (
    not is_system and (select auth.uid()) = user_id
  );

create or replace function public.prevent_system_category_mutation()
returns trigger
language plpgsql
as $$
begin
  if tg_op = 'DELETE' then
    if old.is_system then
      raise exception 'System categories cannot be deleted';
    end if;
    return old;
  end if;

  if new.user_id is distinct from old.user_id
    or new.is_system is distinct from old.is_system then
    raise exception 'Category ownership and system status cannot be changed';
  end if;

  if old.is_system then
    raise exception 'System categories cannot be modified';
  end if;

  return new;
end;
$$;

create or replace function public.prevent_category_name_collision()
returns trigger
language plpgsql
as $$
begin
  if exists (
    select 1
    from public.categories category
    where category.id <> new.id
      and lower(category.name) = lower(new.name)
      and (category.is_system or new.is_system)
  ) then
    raise exception 'Category names cannot be shared by system and custom categories';
  end if;

  return new;
end;
$$;

create trigger categories_prevent_name_collision
  before insert or update of name, is_system on public.categories
  for each row execute procedure public.prevent_category_name_collision();

create trigger categories_prevent_system_mutation
  before update or delete on public.categories
  for each row execute procedure public.prevent_system_category_mutation();

create trigger categories_set_updated_at
  before update on public.categories
  for each row execute procedure public.set_updated_at();

alter table public.transactions
  add column category_id uuid references public.categories (id) on delete set null;

create index transactions_category_id_idx on public.transactions (category_id);

comment on column public.transactions.category_id is
  'Optional single category assigned to this transaction; null means uncategorized.';

create or replace function public.validate_transaction_category()
returns trigger
language plpgsql
as $$
begin
  if new.category_id is not null
    and not exists (
      select 1
      from public.categories category
      join public.financial_accounts account on account.id = new.account_id
      where category.id = new.category_id
        and (category.is_system or category.user_id = account.user_id)
    ) then
    raise exception 'Transaction category must be a system category or belong to the account owner';
  end if;

  return new;
end;
$$;

create trigger transactions_validate_category
  before insert or update of account_id, category_id on public.transactions
  for each row execute procedure public.validate_transaction_category();
