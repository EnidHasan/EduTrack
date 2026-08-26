# EduTrack — Member 1 checkpoint

ASP.NET Core MVC academic information system foundation with SQL Server, EF Core, and ASP.NET Core Identity.

## Included

- Responsive admin shell, sidebar/navbar, dashboard, polished login, forms, tables, empty states, alerts, and mobile navigation
- Identity authentication with `Admin`, `Teacher`, and `Student` roles, secure password policy, lockout, and authorization
- Admin account management (create/edit, role assignment, password reset, enable/disable)
- Full Student, Teacher, and Course CRUD with validation, search, uniqueness constraints, and teacher-course assignment
- SQL Server `ApplicationDbContext`, initial migration, relationships, indexes, and automatic development seeding

## Run in Visual Studio

1. Open `EduTrack.slnx` in Visual Studio 2026.
2. Confirm SQL Server LocalDB is installed, or replace `DefaultConnection` in `EduTrack.Web/appsettings.json` with your SSMS SQL Server instance, for example:
   `Server=.;Database=EduTrackDb;Trusted_Connection=True;TrustServerCertificate=True`
3. Set `EduTrack.Web` as the startup project and run it. In Development, pending migrations are applied automatically.
4. Sign in with the seeded checkpoint account:
   - Email: `admin@edutrack.edu`
   - Password: `Admin@12345`

Change the seed password before demonstration or deployment. For production, store it with User Secrets or environment variables (`SeedAdmin__Password`) instead of source-controlled configuration.

## EF Core commands

```powershell
dotnet ef database update --project EduTrack.Web
dotnet ef migrations add MigrationName --project EduTrack.Web --output-dir Data/Migrations
```

The current local development database is `EduTrackDb` and can be inspected in SSMS under `(localdb)\MSSQLLocalDB`.
