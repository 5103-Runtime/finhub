alter table public.profiles
  add column role text not null default 'user',
  add constraint profiles_role_check
    check (role in ('user', 'administrator'));

comment on column public.profiles.role is
  'Application role. New users are assigned the user role; administrators are assigned it explicitly.';

create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer set search_path = public
as $$
begin
  insert into public.profiles (
    id,
    first_name,
    last_name,
    preferred_name,
    role
  )
  values (
    new.id,
    nullif(new.raw_user_meta_data ->> 'first_name', ''),
    nullif(new.raw_user_meta_data ->> 'last_name', ''),
    nullif(new.raw_user_meta_data ->> 'preferred_name', ''),
    'user'
  )
  on conflict (id) do nothing;

  return new;
end;
$$;

create or replace function public.is_administrator()
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
  select exists (
    select 1
    from public.profiles
    where id = (select auth.uid())
      and role = 'administrator'
  );
$$;

revoke execute on function public.is_administrator() from public, anon;
grant execute on function public.is_administrator() to authenticated;

create policy "Administrators can view profiles"
  on public.profiles
  for select
  to authenticated
  using (public.is_administrator());

create or replace function public.prevent_profile_role_change()
returns trigger
language plpgsql
security invoker
set search_path = ''
as $$
begin
  if new.role is distinct from old.role
    and coalesce(current_setting('request.jwt.claim.role', true), '') = 'authenticated'
  then
    raise exception 'Profile roles cannot be changed through authenticated API sessions';
  end if;

  return new;
end;
$$;

create trigger profiles_prevent_role_change
  before update on public.profiles
  for each row execute function public.prevent_profile_role_change();
