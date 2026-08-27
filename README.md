# Faculty Management Platform

Full-stack faculty management system for the Faculty of Informatics Engineering at Aleppo University. The monorepo contains a .NET 10 three-tier backend and a responsive React 19 frontend with Arabic/English and RTL/LTR support.

## What is included

- Student and staff signup with administrator approval
- Role-specific workspaces for Student, Teacher, Professor, Exams Officer, and Admin
- Five-year academic progression and mandatory course enrollment
- Automatic division assignment and administrator transfers
- Conflict-safe room booking in fixed 90-minute periods
- Theoretical, practical, meeting, and seminar schedules
- OR-Tools timetable generation with draft/publish workflow
- Individual, CSV, and XLSX marks entry with publication and correction history
- Student appeals, professor review, and final exams-office decisions
- Bilingual targeted announcements and real-time SignalR notifications
- Academic settings, audit history, Docker deployment, Swagger, and health checks

## Repository structure

- `FacultyManagement.Web`: React, TypeScript, Vite, SignalR client, responsive bilingual UI
- `FacultyManagement.Api`: REST controllers, JWT authentication, Swagger, SignalR, middleware
- `FacultyManagement.Business`: workflows, validation, academic rules, scheduling, OR-Tools, imports
- `FacultyManagement.Data`: Identity/EF Core entities, SQL Server mappings, migrations, seed data
- `FacultyManagement.UnitTests`: academic-rule tests
- `FacultyManagement.IntegrationTests`: API smoke tests

Backend controllers do not access EF Core directly. Its dependency direction is API -> Business -> Data. The frontend is independently buildable and communicates only through the public API and SignalR hub.

## Prerequisites

- .NET SDK 10.0.201 or a compatible .NET 10 patch
- Node.js 24 and npm 11
- SQL Server/LocalDB, or Docker Desktop

## Local development

The development configuration uses LocalDB and seeds `admin@faculty.local` with password `Admin123!`. Change it outside a local demonstration.

Start the backend:

```powershell
dotnet restore FacultyManagement.sln
dotnet ef database update --project FacultyManagement.Data --startup-project FacultyManagement.Api
dotnet run --project FacultyManagement.Api --launch-profile http
```

The API runs at `http://localhost:5176`, Swagger at `http://localhost:5176/swagger`, health at `/health`, and SignalR at `/hubs/faculty`.

In a second terminal, start the frontend:

```powershell
cd FacultyManagement.Web
npm install
npm run dev
```

Open `http://localhost:5173`. Vite proxies API and SignalR requests to `http://localhost:5176`. Override that target with `VITE_API_PROXY_TARGET` when necessary. For a separately hosted frontend, set `VITE_API_URL` at build time.

## Verification

```powershell
dotnet build FacultyManagement.sln
dotnet test FacultyManagement.sln --no-build
cd FacultyManagement.Web
npm test
npm run build
```

## Docker deployment

Copy `.env.example` to `.env`, replace every value, then run:

```powershell
docker compose up --build -d
```

The web application listens on `http://localhost:3000` and proxies API/SignalR traffic internally. The API is also exposed at `http://localhost:8080`. Production TLS should terminate at a reverse proxy or cloud platform.

## Initial administrator workflow

1. Sign in with the seeded Admin.
2. Create and activate an academic year and semester dates.
3. Add non-teaching days, rooms, courses, and course offerings.
4. Approve accounts and assign Professors and Teachers to offerings.
5. Synchronize mandatory enrollments; active students request division assignment.
6. Staff reserve sessions manually, or Admin generates and publishes a timetable.
7. Create exam periods, import or enter marks, close periods, preview promotion, then commit it.

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
- Teaching Sunday-Thursday in five fixed 90-minute periods

Semester dates, holidays, weekly session counts, labs, projector requirements, and other configurable values are maintained by Admin.

## Security notes

JWT access tokens expire after 15 minutes. The frontend keeps access tokens in memory; refresh tokens are hashed, rotated, revocable HttpOnly cookies. Accounts require approval, password resets revoke refresh tokens, authentication is rate-limited, and sensitive changes are audited. Configure real secrets, HTTPS, restricted CORS, SQL backups, and a production reverse proxy before deployment.

## Database migrations

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update --project FacultyManagement.Data --startup-project FacultyManagement.Api
```

The initial migration is included. Application startup also applies pending migrations when database seeding is enabled.
