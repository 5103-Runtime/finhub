drop trigger categories_prevent_value_collision on public.categories;
drop function public.prevent_category_value_collision();

drop index public.categories_system_slug_idx;
drop index public.categories_user_slug_idx;

alter table public.categories
  drop column slug;

create or replace function public.prevent_category_name_collision()
returns trigger
language plpgsql
as $$
begin
  if exists (
    select 1
    from public.categories category
    where category.id <> new.id
      and category.is_system
      and not new.is_system
      and lower(category.name) = lower(new.name)
  ) then
    raise exception 'Custom category names cannot be shared with system categories';
  end if;

  return new;
end;
$$;

create trigger categories_prevent_name_collision
  before insert or update of name, is_system on public.categories
  for each row execute procedure public.prevent_category_name_collision();
