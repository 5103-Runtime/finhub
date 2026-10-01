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

The current migration creates `public.profiles`. It extends Supabase Auth with application profile data. It does not replace `auth.users`, and it does not store passwords.

## Apply a migration to Supabase

Push unapplied migrations to the linked project:

```powershell
npx supabase@latest db push
```

The CLI may request the database password. Enter it directly in the terminal; do not add it to a migration file, README, `.env` file, or GitHub repository.

Do not run `db reset` against the shared project. It is intended for local development and can destroy database data.

## Verify The Migration

In the Supabase Dashboard:

1. Open **Table Editor** and confirm `public.profiles` exists.
2. Open the table policies and confirm Row Level Security is enabled.
3. Confirm the policies restrict users to their own profile.
4. In **Authentication**, confirm users remain managed by Supabase Auth.
5. Register a test user later and confirm the profile trigger creates a matching profile row.

## Authentication and profiles

Supabase Auth owns credentials and password hashing. The `public.profiles` table stores application profile data such as:

- `first_name`
- `last_name`
- `preferred_name`
- `date_of_birth`
- `avatar_url`
- timestamps

These fields are nullable so registration can begin with only an email and password. Date of birth is private personal information, so the table is protected by RLS and should not be publicly readable.

The profile table is not the place for financial data. Future tables such as `financial_accounts` and `transactions` should have their own migrations and reference the authenticated user's ID.

## API and EF Core Ownership

Supabase migrations own the creation of `public.profiles`, its policies, and its triggers. Later, the ASP.NET API can map that table with an EF Core entity and query it through `FinHubDbContext`.

Do not create a second EF Core migration for the same `profiles` table. EF Core should map and query the table; Supabase migrations should remain the source of truth for this Supabase-specific schema.
