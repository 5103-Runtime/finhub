# FinHub

A centralized financial ledger for students and young adults. Import statements from multiple banks and FinHub normalizes them into one categorized ledger, with dashboards and spending insights.

Statements are parsed in the browser; only the extracted transactions are sent to the server.

![License](https://img.shields.io/badge/License-MIT-green)

## Tech Stack
<p align="left">
  <img height="35" src="https://img.shields.io/badge/.NET-%23512BD4?logo=dotnet&logoColor=white&style=for-the-badge"/>
  <img height="35" src="https://img.shields.io/badge/React-%2320232A?logo=react&logoColor=%2361DAFB&style=for-the-badge"/>
  <img height="35" src="https://img.shields.io/badge/TypeScript-%23007ACC?logo=typescript&logoColor=white&style=for-the-badge"/>
  <img height="35" src="https://img.shields.io/badge/Vite-%2300C0FF?logo=vite&logoColor=white&style=for-the-badge"/>
  <img height="35" src="https://img.shields.io/badge/PostgreSQL-%234169E1?logo=postgresql&logoColor=white&style=for-the-badge"/>
</p>

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
