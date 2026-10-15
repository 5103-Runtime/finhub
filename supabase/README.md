# Supabase Database Setup

This folder contains the database migrations for the shared Supabase PostgreSQL project.

## What belongs here

Use Supabase SQL migrations for database objects that Supabase owns, including:

- Tables in the `public` schema
- Foreign keys to Supabase Auth's `auth.users` table
- Row Level Security policies
- Database functions and triggers
- Indexes and constraints

Do not place passwords, access tokens, connection strings, or publishable keys in migration files.

## Install and run the CLI

The CLI can run through `npx`; it does not need to be installed globally or added to `web/package.json`.

Run these commands from the repository root:

```powershell
npx supabase@latest --version
npx supabase@latest login
npx supabase@latest init
npx supabase@latest link --project-ref bwvnqbuxainfmeumnjci
```

`login` authenticates the CLI and opens a browser. `init` creates the local Supabase configuration. Run it once per repository. `link` associates this repository with the Supabase project and is normally run once per development machine.

If `supabase/config.toml` already exists, do not run `init` again.

## Create a migration

Create a new migration from the repository root:

```powershell
npx supabase@latest migration new create_profiles
```

This creates a timestamped file under `supabase/migrations/`. Put the SQL schema change in that file, then commit the migration so every team member receives the same database change.

## Migrations

- `_create_profiles` creates `public.profiles`. It extends Supabase Auth with application profile data. It does not replace `auth.users`, and it does not store passwords.
- `_create_financial_accounts` creates `public.financial_accounts`, described under [Financial accounts](#financial-accounts).

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

## Verify The Migration

In the Supabase Dashboard:

1. Open **Table Editor** and confirm `public.profiles` exists.
2. Open the table policies and confirm Row Level Security is enabled.
3. Confirm the policies restrict users to their own profile.
4. In **Authentication**, confirm users remain managed by Supabase Auth.
5. Register a test user later and confirm the profile trigger creates a matching profile row.

## Authentication and profiles

Supabase Auth owns credentials and password hashing. The `public.profiles` table is not the place for financial data; it stores application profile data such as:

- `first_name`
- `last_name`
- `preferred_name`
- `date_of_birth`
  - Date of birth is private personal information, so the table is protected by RLS and should not be publicly readable.
- `avatar_url`
- timestamps

These fields are nullable so registration can begin with only an email and password.

## Financial accounts

`public.financial_accounts` holds the chequing, savings, and credit card accounts a user imports statements for. Transactions will reference an account.

### Design

- **`user_id` references `auth.users`, not `public.profiles`.**
- `on delete cascade` means deleting a user removes their accounts, and later their transactions through the same chain. Front-end should HEAVILY warn upon deleting data / accounts.
- **`account_type` is one of `chequing`, `savings`, or `credit_card`.** The type matters beyond display: credit card statements may invert the sign of every amount depending on the input format.
- `name` is 1 to 100 characters and must arrive trimmed. The DB rejects a name with stray spaces; the API should trim before inserting.
- `institution` is nullable and follows the same trim rule. Null means no bank recorded; the API maps a blank value to null before insert. The value is a label only; file mappings match on a statement’s signature, not on this column.
- `currency` is a three-letter ISO 4217 code, default `CAD`. A check constraint keeps lowercase codes and symbols out; the API validates against the supported list.

Deliberately left out:

- A stored balance. The account carries no balance of its own; the API derives balances from the account's imported statements.

### Security

Row Level Security is enabled with select, insert, update, and delete policies, each scoped to `(select auth.uid()) = user_id`. The API bypasses RLS, so it must filter by the authenticated user itself.

## API and EF Core Ownership

Supabase migrations own the creation of `public.profiles` and `public.financial_accounts`, their policies, and their triggers. The ASP.NET API maps these tables with EF Core entities and queries them through `FinHubDbContext`.

Do not create EF Core migrations for these tables. EF Core should map and query them; Supabase migrations remain the source of truth for the schema.
