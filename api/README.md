# API
ASP.NET Core Web API (.NET 10) using controllers.

## Conventions
- Every endpoint lives under `/api/`.
- Write routes out in full and in lowercase: `[Route("api/transactions")]`, not `[Route("api/[controller]")]`.
- Secrets (connection strings, JWT keys) go in user-secrets, never in committed files.

## Local secrets

The project is configured with the user-secrets ID `supabase-api`. Set backend-only values from the repository root:

```powershell
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=db.bwvnqbuxainfmeumnjci.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<DB_PASSWORD>;SSL Mode=Require" --project api/FinHub.Api
```

See the [root README](../README.md) to run it.
