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
