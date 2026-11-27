# API

ASP.NET Core Web API (.NET 10) using controllers.

## Conventions

- Every endpoint lives under `/api/`.
- Write routes out in full and in lowercase: `[Route("api/transactions")]`, not `[Route("api/[controller]")]`.
- Secrets (connection strings, JWT keys) go in user-secrets, never in committed files.

## Local Secrets

The project is configured with the user-secrets ID `supabase-api`. Set all local API values from the repository root:

```powershell
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=aws-0-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.bwvnqbuxainfmeumnjci;Password=<DB_PASSWORD>;SSL Mode=Require;No Reset On Close=true;Max Auto Prepare=0" --project api/FinHub.Api
dotnet user-secrets set "Supabase:Url" "https://bwvnqbuxainfmeumnjci.supabase.co" --project api/FinHub.Api
dotnet user-secrets set "Supabase:PublishableKey" "<SUPABASE_PUBLISHABLE_KEY>" --project api/FinHub.Api
```

The PostgreSQL connection string is used by EF Core, while the URL and publishable key are used to call Supabase Auth. Never use a service-role key in the frontend or commit any secret values.

## Password reset redirect

The password reset email links back to the web app. Set the page it should open with `Supabase:PasswordResetRedirectUrl`:

- Local development: already set to `http://localhost:5173/reset-password` in `appsettings.Development.json`.
- Deployment: set `Supabase__PasswordResetRedirectUrl` (or `Supabase:PasswordResetRedirectUrl`) to `https://<your-domain>/reset-password`.

The URL must also be listed under **Authentication > URL Configuration > Redirect URLs** in the Supabase dashboard. If it isn't, Supabase ignores the redirect and sends users to the project's Site URL instead.

If the setting is missing, the API logs a warning and the email link falls back to the Site URL.

## Tests

Unit tests live in `FinHub.Api.Tests` and run against the sample statements under `test/fixtures/statements`:

```powershell
dotnet test api/FinHub.Api.Tests
```

## Rate limits

Register, login, forgot-password and update-password calls are limited per client IP. The production values live in `appsettings.json` under `RateLimiting:Auth` (10 requests per 60 seconds).

Anonymous statement previews are limited per connection peer under `RateLimiting:GuestPreview` (one successful parse per 60 seconds). Once a preview succeeds, further supported uploads are rejected before parsing until the window expires. Unsupported file types and invalid statements do not use the success quota.

Adjust either limit per environment without a code change:

- Local development: add the corresponding `RateLimiting` section to `appsettings.Development.json`.
- CI and deployment: set `RateLimiting__Auth__PermitLimit`, `RateLimiting__Auth__WindowSeconds`, `RateLimiting__GuestPreview__PermitLimit`, and `RateLimiting__GuestPreview__WindowSeconds` as environment variables.

All four keys are required and must be positive; the API refuses to start otherwise. A rejected call returns `429` with a `Retry-After` header and `errorCode: "rate_limited"`.

## Administrator APIs

These endpoints are restricted to authenticated administrators with an application profile role of `administrator`. Normal users receive `403` with `errorCode: "administrator_required"`.

### User management

- `GET /api/admin/users` lists all application users. Optional `sortBy` values are `email`, `firstName`, `lastName`, `preferredName`, `role`, `createdAt`, and `updatedAt`; `sortOrder` accepts `asc` or `desc`.
- `GET /api/admin/users/{id}` retrieves one application user.

When both sorting parameters are omitted, the list is ordered by creation time, oldest first. A supplied `sortBy` defaults to descending order; `sortOrder` requires `sortBy`. Missing or unsupported sorting parameters return `400` with `errorCode: "missing_sort_field"`, `"invalid_sort_field"`, or `"invalid_sort_order"`.

Responses include the user's email from Supabase Auth alongside their application profile. An unknown user ID returns `404` with `errorCode: "user_not_found"`.

## Related Documentation

See the [root README](../README.md) to run it.

See [Statement import](docs/statement-import.md) for how uploaded statements are parsed.
