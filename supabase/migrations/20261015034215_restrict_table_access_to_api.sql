revoke all on table public.profiles, public.financial_accounts from anon, authenticated;

alter default privileges for role postgres in schema public
  revoke all on tables from anon, authenticated;
