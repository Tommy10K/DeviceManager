# DeviceManager

Thanks for reviewing this project.

This README is intentionally split in two parts:
1. Exact steps/commands to run the app.
2. Concise project notes (architecture and feature behavior).

Expected environment: Windows (SQL Server LocalDB).

## Stack

- .NET SDK: `10.x` (target framework: `net10.0`)
- ASP.NET Core Web API
- Entity Framework Core + SQL Server (`10.0.5`)
- Swagger/OpenAPI used (`Swashbuckle.AspNetCore 10.1.7`)
- Angular `21.2.x`
- TypeScript `5.9.x`
- Node.js `22 LTS` + npm `11.11.0`
- SQL Server Express LocalDB `16.x` (SQL Server 2022 LocalDB)

## Required Installs

Use these versions (or newer compatible versions in the same major line):

- .NET SDK `10.x`
- Node.js `22 LTS` or `24.x`
- npm `11.x`
- SQL Server Express LocalDB `16.x` (instance name: `MSSQLLocalDB`)
- SQLCMD CLI tools + Microsoft ODBC Driver for SQL Server (`17` or `18`)

## Run (Copy/Paste)

From any parent folder:

```powershell
git clone <REPO_URL>
cd DeviceManager
```

From repo root (`DeviceManager`):

```powershell
dotnet restore
dotnet tool restore
```

From frontend folder:

```powershell
cd client/device-manager-ui
npm install
cd ../..
```

From repo root, ensure LocalDB is running:

```powershell
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

If `create` says the instance already exists, continue with `start`.

From repo root, create schema and seed:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d master -i "scripts\01_CreateDatabase.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d DeviceManagerDb -i "scripts\02_CreateTables.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d DeviceManagerDb -i "scripts\03_SeedData.sql"
```

Terminal A (API):

```powershell
cd src/DeviceManager.API
dotnet run
```

Terminal B (UI):

```powershell
cd client/device-manager-ui
npm run start -- --host localhost --port 4200
```

URLs:

- UI: `http://localhost:4200`
- API Swagger: `http://localhost:5109/swagger`

Seeded accounts:

- Admin: `admin@test.com` / `admin1`
- User: `user@test.com` / `user11`

## Optional: Enable Gemini AI

To enable AI description generation quickly, hardcode your key in:

- `src/DeviceManager.API/appsettings.json`

Update the `Gemini` section:

```json
"Gemini": {
  "ApiKey": "YOUR_GEMINI_API_KEY"
}
```

## Ports In Use

If `4200`, `5109`, or `7172` are busy:

```powershell
$ports = @(4200, 5109, 7172)
Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
  Where-Object { $ports -contains $_.LocalPort } |
  Select-Object -ExpandProperty OwningProcess -Unique |
  ForEach-Object { Stop-Process -Id $_ -Force }
```

## Project Notes

Architecture (high level):

- Clean Architecture style with four backend layers: Domain, Application, Infrastructure, API.
- Domain contains entities/enums, Application contains use cases/business rules, Infrastructure contains EF Core/JWT/Gemini integrations.
- API controllers stay thin and call Application services; Angular frontend consumes API endpoints.

GitHub workflow and delivery discipline:

- I committed frequently in small, steady steps instead of making large one-time changes.
- I worked in separate branches for features and fixes, then merged them after validation.
- I linked work to GitHub Issues and closed them with auto-close words like `Closes #<issue-number>`.
- I used milestones to organize work and keep progress visible across the project.

Gemini description generation:

- AI descriptions are generated through Gemini with a fixed instruction prompt and explicit device specs.
- The request sends only structured fields: Name, Manufacturer, OS, Type, RAM, and Processor.
- The prompt asks for a single professional sentence, no markdown/filler, max length constrained.

Search and quirks:

- Device list search is debounced (`300ms`) with `distinctUntilChanged` to reduce API chatter.
- Tags are unique by database constraint (`UQ_Device_Tag`) and conflict handling is surfaced by the API.
- Role-based behavior is enforced (admin manage actions, user assignment flow).

If you have any questions, feel free to contact me.