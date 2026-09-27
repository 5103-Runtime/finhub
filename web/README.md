# Web
React + TypeScript app built with Vite.

## Conventions
- Call the API with relative paths (`/api/...`), never a hardcoded host. In development, Vite proxies them to the development API.
- Install with `npm ci`, not `npm install`. [Why?](https://docs.npmjs.com/cli/commands/npm-ci)
- CI fails on lint errors; run `npm run lint` before pushing.

## Local environment

Copy the environment template before starting the web app:

```powershell
Copy-Item .env.template .env
```

Update `.env` with the local Supabase values. Variables beginning with `VITE_` are included in the browser bundle, so only the Supabase URL and publishable key belong in this file. Never add database passwords, direct PostgreSQL connection strings, service-role keys, or other backend secrets.

The `.env` file is ignored by Git. Commit changes to `.env.template` when the team needs a new browser-safe variable.

See the [root README](../README.md) to run it.
