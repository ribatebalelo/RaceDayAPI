# RaceDay

RaceDay is a race-event management system. **Organisers** publish events (runs, walks, cycles) and define age/distance categories; **Participants** browse events and enrol in a category; Organisers then record finish times and positions, which Participants can view.

This repository contains:

| Part | Location |
|---|---|
| Part 1 - planning (ERD, endpoint plan, SQL script) | `/docs` |
| Part 2 - ASP.NET Core Web API + unit tests | `/RaceDay.Api`, `/RaceDay.Tests` |

## Roles

| Role | Can do |
|---|---|
| **Organiser** | Create, update and delete their own events; define categories; view enrolments for their events; record and correct results. Registering as an Organiser needs an **invite code** so the role cannot be self-assigned. |
| **Participant** | Register freely; view events, categories and results; enrol in / cancel an enrolment; view their own enrolments and results. |

Both roles can register, log in, and view/update their own profile.

## Tech
ASP.NET Core 8 Web API, Entity Framework Core 8 (code-first, SQL Server), cookie-based session authentication, PBKDF2 password hashing, Swagger (Swashbuckle), xUnit integration tests.

## Setup and run

Prerequisites: .NET 8 SDK, SQL Server (LocalDB or SQL Server Express), and the EF tool (`dotnet tool install --global dotnet-ef`).

```bash
# 1. Restore
dotnet restore RaceDay.Api/RaceDay.Api.csproj

# 2. Check the connection string in RaceDay.Api/appsettings.json
#    (default: (localdb)\MSSQLLocalDB, database RaceDayDB)

# 3. Create the first migration (once) - the app applies migrations on startup
cd RaceDay.Api
dotnet ef migrations add InitialCreate
cd ..

# 4. Run the API
dotnet run --project RaceDay.Api
```

Open the URL printed in the console (e.g. `https://localhost:7xxx`) - it redirects to **/swagger**.

### Using Swagger
1. `POST /api/auth/register` - to register as an Organiser use `organiserCode` = `RACEDAY-ORG-2026` (set in `appsettings.json` under `Auth:OrganiserInviteCode`).
2. `POST /api/auth/login` - this starts the session; Swagger UI then sends the session cookie automatically.
3. Try the protected endpoints. Use `POST /api/auth/logout` to switch accounts.

## Running the tests
```bash
dotnet test RaceDay.Tests/RaceDay.Tests.csproj
```
Tests run the real API in memory (EF Core InMemory provider), so no database is needed.

## CI/CD
GitHub Actions (`.github/workflows/ci.yml`) restores, builds and runs all tests on every push.

![Green build](docs/ci-green-build.png)

## Video walkthrough
[Part 2 demo - unlisted YouTube video](#)
