
# RaceDay

RaceDay is a race-event management system. **Organisers** publish events (runs, walks, and cycles) and define age and distance categories. **Participants** browse events and enrol in a category. Organisers then record finishing times and positions, which participants can view.

The system is designed to centralise event management, participant enrolments, and race results. A structured database design helps maintain data integrity, organise relationships between entities, and support efficient data retrieval (Coronel and Morris, 2019).

This repository contains:

| Part | Location |
|---|---|
| Part 1 - Planning (ERD, endpoint plan, SQL script) | [`/docs`](./docs) |
| Part 2 - ASP.NET Core Web API + unit tests | [`/RaceDay.Api`](./RaceDay.Api), [`/RaceDay.Tests`](./RaceDay.Tests) |

## Roles

| Role | Permissions |
|---|---|
| **Organiser** | Create, update, and delete their own events; define categories; view enrolments for their events; record and correct results. Registering as an Organiser requires an **invite code** to prevent the role from being self-assigned. |
| **Participant** | Register freely; view events, categories, and results; enrol in or cancel an enrolment; view their own enrolments and results. |

Both roles can register, log in, and view or update their own profiles.

## Tech Stack

- ASP.NET Core 8 Web API
- Entity Framework Core 8 (Code First)
- Microsoft SQL Server
- Cookie-based session authentication
- PBKDF2 password hashing
- Swagger (Swashbuckle)
- xUnit integration tests
- EF Core InMemory provider
- GitHub Actions for CI/CD

## Entity Relationship Diagram (ERD)

The Entity Relationship Diagram (ERD) represents the proposed database structure of RaceDay, including its entities, attributes, primary keys, foreign keys, and relationships. ERDs are useful during database design because they help identify relationships between data entities and provide a blueprint for database implementation (Coronel and Morris, 2019).

The RaceDay ERD illustrates the relationships between organisers, events, participants, categories, enrolments, and race results. These relationships support the consistent organisation of event information, participant registrations, and recorded results.

<!-- Replace the placeholder below with your ERD image. -->

![RaceDay Entity Relationship Diagram](./docs/erd.png)

*Figure 1: Entity Relationship Diagram for the RaceDay race-event management system.*

> **Note:** Save your ERD image as `erd.png` inside the `docs` folder, or update the image path above to match your actual image filename.

## Setup and Run

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- SQL Server (LocalDB or SQL Server Express)
- Entity Framework Core CLI tools

Install the EF Core CLI tools:

```bash
dotnet tool install --global dotnet-ef
```

### 1. Restore Dependencies

```bash
dotnet restore RaceDay.Api/RaceDay.Api.csproj
```

### 2. Configure the Database

Check the connection string in `RaceDay.Api/appsettings.json`.

Default connection:

```text
(localdb)\MSSQLLocalDB
```

Database name: `RaceDayDB`

### 3. Create the Initial Migration

Run this step once if the initial migration does not already exist.

```bash
cd RaceDay.Api
dotnet ef migrations add InitialCreate
cd ..
```

The application applies migrations on startup.

### 4. Run the API

```bash
dotnet run --project RaceDay.Api
```

Open the URL displayed in the console, for example `https://localhost:7xxx`. The application redirects to `/swagger`.

## Using Swagger

1. **Register:** Send a `POST` request to `/api/auth/register`.

   To register as an Organiser, provide the invite code using the `organiserCode` field. Include all other required registration fields.

   The example invite code is configured in `appsettings.json` under `Auth:OrganiserInviteCode`:

   ```json
   {
     "organiserCode": "RACEDAY-ORG-2026"
   }
   ```

2. **Log in:** Send a `POST` request to `/api/auth/login`. Swagger UI should send the session cookie automatically when the API and browser cookie configuration support it.

3. **Test endpoints:** Use the authenticated session to access endpoints permitted for your role.

4. **Log out:** Send a `POST` request to `/api/auth/logout` before switching accounts.

> **Security:** Replace the example invite code before deployment. Do not commit production secrets to source control.

## Running the Tests

Run the integration tests with:

```bash
dotnet test RaceDay.Tests/RaceDay.Tests.csproj
```

The tests use the EF Core InMemory provider, so a separate SQL Server database is not required for the test suite.

## CI/CD

GitHub Actions is configured in `.github/workflows/ci.yml`.

On every push, the workflow restores dependencies, builds the application, and runs the tests.

## Project Screenshot

![RaceDay project screenshot](https://github.com/user-attachments/assets/be2e4569-bd64-450b-9217-40463719385a)

## Video Walkthrough

[Part 2 Demo - Unlisted YouTube Video](#)

<!-- Replace # with the actual unlisted YouTube video URL. -->

## References

Coronel, C. and Morris, S. (2019) *Database systems: design, implementation, & management*. 13th edn. Boston, MA: Cengage Learning.

