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

See the [root README](../README.md) to run it.

