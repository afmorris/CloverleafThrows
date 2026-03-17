# CloverleafThrows — Agent Guide

Workout tracker for the Cloverleaf throws team. ASP.NET Core 10 MVC, Dapper, SQL Server.

## Stack

- **Runtime**: .NET 10 (required)
- **Web**: ASP.NET Core MVC with Razor views
- **Data access**: Dapper only — no Entity Framework, no auto-migrations
- **Database**: SQL Server 2019+
- **Auth**: None — the admin area is unauthenticated by design
- **Packages**: Dapper 2.1.35, Microsoft.Data.SqlClient 5.2.2

## Solution Structure

Three projects in `CloverleafThrows.sln`:

```
CloverleafThrows.Models/
  CloverleafThrows.Models.csproj
  Entities.cs                    # ALL entity classes live here (no view models)

CloverleafThrows.Data/
  CloverleafThrows.Data.csproj   # References Models
  DbConnectionFactory.cs         # IDbConnectionFactory + SqlConnectionFactory
  Interfaces.cs                  # All repository interfaces (IWorkoutRepository, etc.)
  Repositories/
    WorkoutRepository.cs         # Workout, section, exercise queries
    OtherRepositories.cs         # Exercise, Mesocycle, Athlete, Meet repos

CloverleafThrows.Web/
  CloverleafThrows.Web.csproj    # References Data + Models
  Program.cs
  appsettings.json               # Connection string key: "DefaultConnection"
  Dockerfile                     # 4-stage build
  Controllers/
    HomeController.cs            # Public calendar
    WorkoutController.cs         # Public workout views + weather swap
    AdminController.cs           # Dashboard, season overview
    AdminWorkoutsController.cs   # Workout CRUD + AJAX exercise ops
    AdminCrudControllers.cs      # Exercises, Mesocycles, Athletes, Meets
  Models/
    ViewModels.cs                # ALL view models live here
  Views/
    Admin/                       # Admin views (dashboard, CRUD)
    Home/Index.cshtml            # Calendar grid
    Workout/                     # Detail, Print, NoWorkout, _SectionCard
    Shared/_Layout.cshtml        # Public layout
    Shared/_AdminLayout.cshtml   # Admin layout
  wwwroot/
    css/site.css, css/admin.css
    js/site.js, js/admin.js, js/workout-editor.js

Migrations/
  001_InitialSchema.sql          # Full schema + seed data — applied manually via sqlcmd
compose.yaml                     # Docker Compose for local builds
```

## Deployment Model

This is important context for understanding which features matter and which don't.

1. **Docker (homelab)** — the dynamic app runs in a private Docker container on a homelab server. The admin area is used to enter and manage data. This instance is never public.
2. **wget static export** — when ready to publish, `wget --mirror` crawls the public pages and produces a directory of static HTML/CSS/JS. The admin area is excluded.
3. **S3** — the static export is synced to an S3 bucket configured for static website hosting. DNS points to the bucket. The public site is entirely static with no server-side logic.

Implications:
- The SwapFocus action (POST to `/Workout/SwapFocus`) works in the Docker instance but is a dead form submit in the static export — that's acceptable, since coaches use the live Docker instance to adjust throws focus before publishing.
- There is no API surface that needs to be secured on the public site because there is no public site server.
- Features that require server-side state (form submissions, etc.) only need to work in the Docker instance.

## Dev Commands

```bash
dotnet restore
dotnet run --project CloverleafThrows.Web

# Apply a new migration:
sqlcmd -S <server> -d CloverleafThrows -i Migrations/002_MyChange.sql

# Docker build + run:
docker compose up --build
```

No test project exists — don't look for one or create one unless asked.

## Data Access Conventions

All database access goes through Dapper. Repositories receive `IDbConnectionFactory` via constructor injection and implement their interface. Registered as `AddScoped<IRepo, RepoImpl>()` in Program.cs.

**Primary constructors on all repositories and controllers:**
```csharp
public class WorkoutRepository(IDbConnectionFactory db) : IWorkoutRepository
```

**Always use named parameters:**
```csharp
await conn.ExecuteAsync("UPDATE ... WHERE Id = @Id", new { Id = id, Name = name });
```

**INSERT returning the new ID — use `ExecuteScalarAsync<int>`:**
```csharp
var id = await conn.ExecuteScalarAsync<int>(
    "INSERT INTO Table (...) OUTPUT INSERTED.Id VALUES (...)", new { ... });
```

**Collections — materialize with `.ToList()`:**
```csharp
return (await conn.QueryAsync<Exercise>("SELECT ...", new { ... })).ToList();
```

**Multi-line SQL — use raw string literals:**
```csharp
var sql = """
    SELECT wd.*, m.Name AS MesocycleName
    FROM WorkoutDays wd
    JOIN Mesocycles m ON m.Id = wd.MesocycleId
    WHERE wd.MesocycleId = @mesocycleId
    """;
```

**Bracket reserved words** in SQL: `[Date]`, `[Order]`, etc.

**Nested/related data** is loaded in separate queries (no Dapper multi-map split-on). Load the parent, then loop children:
```csharp
var sections = await LoadSectionsAsync(conn, day);
```

**Transactions:**
```csharp
conn.Open();
using var tx = conn.BeginTransaction();
try { /* pass tx to each ExecuteAsync call */ tx.Commit(); }
catch { tx.Rollback(); throw; }
```

**Soft-delete** some entities (Exercises: `SET IsActive = 0`).
**Hard-delete** others (WorkoutExercises: `DELETE FROM ...`).
Check existing repo patterns before choosing — match what's already there.

**Always include `UpdatedAt = GETUTCDATE()`** in UPDATE statements.

## Model & View Model Conventions

- Entity classes live in `CloverleafThrows.Models/Entities.cs`. Do not create separate files per class.
- View models live in `CloverleafThrows.Web/Models/ViewModels.cs`. Do not create separate files per view model.
- Both files stay consolidated — no splitting.

View models are named `{Feature}ViewModel`:
```csharp
public class WorkoutEditViewModel
{
    public WorkoutDay Day { get; set; } = new();
    public List<ExerciseCategory> Categories { get; set; } = new();
    public int MesocycleId { get; set; }
}
```

Computed/display properties belong on the entity class in C#, not in SQL:
```csharp
public string FullName => $"{FirstName} {LastName}";
public string DayTypeColor => DayType switch { "Oly / Lower" => "#D6E4F0", ... };
```

## Controller Conventions

- Destructive operations are POST, not DELETE/PUT.
- AJAX endpoints return `Json(new { ... })` with a flat anonymous object.
- Redirects use `RedirectToAction("Action", new { id })`.
- No data annotation validation attributes on models — validate in the controller before calling the repo.
- Controllers use primary constructors and inject repository interfaces (not concrete classes).

**AJAX response shape:**
```csharp
return Json(new { id, success = true });
```

## Database Schema Conventions

When writing a new migration:

- Name it `{NNN}_{Description}.sql` (e.g., `002_AddAthleteNotes.sql`)
- All tables get: `Id INT IDENTITY(1,1) PRIMARY KEY`, `CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()`, `UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()`
- PascalCase column names; foreign keys as `{Table}Id`
- Index every foreign key column: `CREATE INDEX IX_Table_Column ON Table(Column)`
- Use `ON DELETE CASCADE` for child records
- Unicode text: `NVARCHAR(n)`; date-only: `DATE`; timestamps: `DATETIME2`; booleans: `BIT`

## Domain Values (hardcoded — do not change without updating all references)

**Day types:** `"Lower Body"`, `"Upper Body"`, `"Throwing Only"`, `"Jump / Sprint"`, `"Full Body"`

**Throws focus:** `"Shot Put"`, `"Discus"` (templates may use `"Alternate"` which resolves at generation time)

**Section names:** `"Throws Warmup"`, `"Throwing"`, `"Lifting"`, `"Mobility"`, `"Core"`

## Auth Model

There is no authentication. The admin area is intentionally open — the app runs privately in Docker and is never exposed to the public internet. The public website is a static export (generated by wget and served from S3).

Do not add authentication back. Do not add `[Authorize]` attributes.

## What NOT to Do

- **No authentication.** Do not add `[Authorize]`, cookie auth, login pages, or BCrypt. The app is private-network only.
- **No EF Core.** Don't add it, don't scaffold migrations with `dotnet ef`.
- **No new NuGet packages** without discussing first.
- **Don't split `Entities.cs`** into per-class files.
- **Don't split `OtherRepositories.cs`** into per-repo files unless explicitly asked.
- **Don't split `ViewModels.cs`** into per-class files.
- **Don't use positional Dapper parameters** (`?`) — always named (`@Param`).
- **Don't register repositories as singletons** — they must be `AddScoped`.
- **Don't inject concrete repository classes** into controllers — always use the interface.
- **Don't compute display values in SQL** — use C# properties on the model.
- **Don't create new migration files** unless asked — schema changes are intentional and manual.
