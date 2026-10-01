create table public.profiles (
	id uuid primary key references auth.users (id) on delete cascade,
	first_name text,
	last_name text,
	preferred_name text,
	date_of_birth date,
	avatar_url text,
	created_at timestamptz not null default timezone('utc', now()),
	updated_at timestamptz not null default timezone('utc', now()),
	constraint profiles_first_name_length check (char_length(first_name) <= 100),
	constraint profiles_last_name_length check (char_length(last_name) <= 100),
	constraint profiles_preferred_name_length check (char_length(preferred_name) <= 100),
	constraint profiles_avatar_url_length check (char_length(avatar_url) <= 2048)
);

comment on table public.profiles is
	'Application profile data linked one-to-one with Supabase Auth users.';

comment on column public.profiles.date_of_birth is
	'Private personal data; access is restricted by Row Level Security.';

alter table public.profiles enable row level security;

create policy "Users can view their own profile"
	on public.profiles
	for select
	to authenticated
	using ((select auth.uid()) = id);

create policy "Users can update their own profile"
	on public.profiles
	for update
	to authenticated
	using ((select auth.uid()) = id)
	with check ((select auth.uid()) = id);

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
		preferred_name
	)
	values (
		new.id,
		nullif(new.raw_user_meta_data ->> 'first_name', ''),
		nullif(new.raw_user_meta_data ->> 'last_name', ''),
		nullif(new.raw_user_meta_data ->> 'preferred_name', '')
	)
	on conflict (id) do nothing;

	return new;
end;
$$;

drop trigger if exists on_auth_user_created on auth.users;

create trigger on_auth_user_created
	after insert on auth.users
	for each row execute procedure public.handle_new_user();

create or replace function public.set_updated_at()
returns trigger
language plpgsql
as $$
begin
	new.updated_at = timezone('utc', now());
	return new;
end;
$$;

create trigger profiles_set_updated_at
	before update on public.profiles
	for each row execute procedure public.set_updated_at();
