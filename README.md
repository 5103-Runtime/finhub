# FinHub

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Node.js 24](https://nodejs.org/)

## Running locally

Start the API on http://localhost:5013:

```bash
dotnet run --project api/FinHub.Api
```

In a second terminal, start the web app on http://localhost:5173:

```bash
cd web
npm ci
npm run dev
```

The web app proxies `/api/` requests to the API, so both need to be running.
