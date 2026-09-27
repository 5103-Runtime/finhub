# API
ASP.NET Core Web API (.NET 10) using controllers.

## Conventions
- Every endpoint lives under `/api/`.
- Write routes out in full and in lowercase: `[Route("api/transactions")]`, not `[Route("api/[controller]")]`.
- Secrets (connection strings, JWT keys) go in user-secrets, never in committed files.

See the [root README](../README.md) to run it.
