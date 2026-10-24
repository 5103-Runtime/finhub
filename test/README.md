# Test Automation

Reqnroll and Selenium end-to-end tests for the FinHub web application.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- FinHub API running on `http://localhost:5013`
- FinHub web app running on `http://localhost:5173`

## Local test credentials

The automation tests use .NET user secrets. They do not read credentials from a file, so passwords are not stored in the repository.

From the repository root, set the local test credentials with the .NET CLI:

```powershell
dotnet user-secrets set "User:Email" "<USER_EMAIL>" --project test/FinHub.Automation
dotnet user-secrets set "User:Password" "<USER_PASSWORD>" --project test/FinHub.Automation
```

The test project uses the user-secrets ID `finhub-automation`. The values are
stored in the local .NET user-secrets store and are not written to the
repository.

For CI or another environment that cannot use the local user-secrets store,
provide environment variables instead. Environment variables are loaded after
user secrets and therefore take precedence:

| Environment variable               | Configuration value |
| ---------------------------------- | ------------------- |
| `FinHubAutomation__User__Email`    | `User:Email`        |
| `FinHubAutomation__User__Password` | `User:Password`     |

For example, set them for the current PowerShell session:

```powershell
$env:FinHubAutomation__User__Email = "<USER_EMAIL>"
$env:FinHubAutomation__User__Password = "<USER_PASSWORD>"
```

Never commit real credentials or include them in pull requests, logs, screenshots, or test reports. Use a dedicated test account with the minimum required access and complete its profile setup before running the tests; newly created users are redirected to `/setup-profile` instead of `/overview`.

## Running tests

From the repository root:

```powershell
dotnet test test/FinHub.Automation.slnx --settings test/sequential.runsettings
```

For faster execution, scenarios can run in parallel:

```powershell
dotnet test test/FinHub.Automation.slnx --settings test/parallel.runsettings
```

Run only the smoke scenarios:

```powershell
dotnet test test/FinHub.Automation.slnx --settings test/sequential.runsettings --filter TestCategory=smoke
```

The browser runs headlessly in CI. For local runs, set `HEADLESS=true` when a visible browser is not available or when you want to run without opening a browser window; headless mode is otherwise disabled by default.

See the [root README](../README.md) to start the API and web application.
