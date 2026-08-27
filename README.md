# Faculty Management API

ASP.NET Core backend for the Faculty of Informatics Engineering at Aleppo University. It implements account approval, five-year academic progression, mandatory course enrollment, automatic divisions, room booking, timetable generation, marks and appeals, bilingual announcements, and SignalR notifications.

## Architecture

- `FacultyManagement.Api`: REST controllers, JWT authentication, Swagger, SignalR, middleware
- `FacultyManagement.Business`: workflows, validation, academic rules, scheduling, OR-Tools generator, imports
- `FacultyManagement.Data`: Identity/EF Core entities, SQL Server mappings, seed data
- `FacultyManagement.UnitTests`: academic-rule tests
- `FacultyManagement.IntegrationTests`: API smoke tests

Controllers never access EF Core directly. The dependency direction is API → Business → Data.

## Prerequisites

- .NET SDK 10.0.201 or a compatible .NET 10 patch
- SQL Server/LocalDB, or Docker Desktop

## Local development

The development configuration uses LocalDB and seeds `admin@faculty.local` with password `Admin123!`. Change it outside a local demonstration.

```powershell
dotnet restore FacultyManagement.sln
dotnet run --project FacultyManagement.Api
```

Swagger is at `/swagger`, health at `/health`, and the SignalR hub at `/hubs/faculty`.

## Docker deployment

Copy `.env.example` to `.env`, replace every value, then run `docker compose up --build -d`. The API listens on `http://localhost:8080`. Production TLS should terminate at a reverse proxy or cloud platform.

## Initial workflow

1. Sign in with the seeded Admin.
2. Create and activate an academic year and its semester dates.
3. Add non-teaching days, rooms, courses, and course offerings.
4. Assign Professors and Teachers, then approve student/staff signups.
5. Synchronize mandatory enrollments; students request division assignment.
6. Staff reserve sessions manually, or Admin generates and publishes a timetable.
7. Create exam periods, import/enter marks, close periods, preview promotion, then commit it.

## Mark import format

CSV and XLSX imports use the first worksheet and exact headers:

```text
UniversityNumber,CourseCode,Result,Mark
20260001,CS101,Numeric,78
20260002,CS101,Absent,
```

`Result` is `Numeric`, `Absent`, `NotEntered`, or `Withheld`. The complete import is rejected when any row is invalid.

## Fixed rules and settings

- 5 study years
- Pass mark 60/100
- At most 4 failures for promotion; the fifth causes repetition
- Default division capacity 30
- Appeal deadline 5 days
- Teaching Sunday–Thursday in five fixed 90-minute periods

Semester dates, holidays, weekly session counts, labs, and projector requirements are maintained by Admin.

## Security

JWT access tokens expire after 15 minutes. Refresh tokens are hashed, rotated, revocable HttpOnly cookies. Accounts require approval; password resets revoke active refresh tokens. Authentication is rate-limited, and sensitive changes are audited. Configure real secrets, HTTPS, CORS, and backups before deployment.

## Database migrations

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update --project FacultyManagement.Data --startup-project FacultyManagement.Api
```

The initial migration is included in the repository. Application startup also applies pending migrations automatically.
