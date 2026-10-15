# Migrations

Use Supabase SQL migrations for database objects that Supabase owns, including:

- Tables in the `public` schema
- Foreign keys to Supabase Auth's `auth.users` table
- Row Level Security policies
- Database functions and triggers
- Indexes and constraints

Do not place passwords, access tokens, connection strings, or publishable keys in migration files.

`supabase/migrations/` contains the database migrations for the shared Supabase PostgreSQL project.

- `_create_profiles` creates `public.profiles`. It extends Supabase Auth with application profile data. It does not replace `auth.users`, and it does not store passwords.
- `_create_financial_accounts` creates `public.financial_accounts`, described under [Financial accounts](./financial-accounts.md).
- `_use_now_for_timestamps` switches the `created_at` and `updated_at` defaults on both tables, and `set_updated_at()`, from `timezone('utc', now())` to `now()`. The old form returns a timestamp without a zone that Postgres casts back using the session time zone; `now()` is already a `timestamptz`.
- `_restrict_table_access_to_api` revokes `anon` and `authenticated` privileges on both tables and stops Postgres granting them on future tables, so the API, connecting as `postgres`, is the only allowed client.

## Create a migration

Create a new migration from the repository root:

```powershell
npx supabase@latest migration new example_account_migration
```

This creates a timestamped file under `supabase/migrations/`. Put the SQL schema change in that file, then commit the migration so every team member receives the same database change.

## Run migrations locally

Install a runtime the Supabase CLI supports (Docker Desktop, Docker Engine, etc) and keep it running. The CLI uses it to start a local copy of the stack.

From the repository root:

```powershell
npx supabase@latest start
npx supabase@latest db reset
```

`start` downloads and runs the local Postgres, Auth, and Studio containers and prints their URLs. `db reset` drops the local database and applies every file in `supabase/migrations/` from scratch. Run it after writing or editing a migration to verify changes before opening pull requests.

Stop the containers when done:

```powershell
npx supabase@latest stop
```

`db reset` without flags only touches the local database. Never pass **``--linked``**; that resets the shared project and destroys database data.

## Apply a migration to Supabase

Push unapplied migrations to the linked project:

```powershell
npx supabase@latest db push
```

The CLI may request the database password. Enter it directly in the terminal; do not add it to a migration file, README, `.env` file, or GitHub repository.

Do not run `db reset --linked`. It resets the shared public database and destroys database data.

## API and EF Core Ownership

Supabase migrations own the creation of `public.profiles` and `public.financial_accounts`, their policies, and their triggers. The ASP.NET API maps these tables with EF Core entities and queries them through `FinHubDbContext`.

Do not create EF Core migrations for these tables. EF Core should map and query them; Supabase migrations remain the source of truth for the schema.
