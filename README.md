# Cloverleaf Throws

Workout tracker for the Cloverleaf throws team. Built with ASP.NET Core 10, Dapper, and SQL Server.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (**required** — .NET 10 or later)
- SQL Server 2019+ (or SQL Server Express / LocalDB)

## Local Development Setup

1. **Create the database:**
   ```sql
   CREATE DATABASE CloverleafThrows;
   ```

2. **Run the migration script:**
   ```bash
   sqlcmd -S localhost -d CloverleafThrows -i Migrations/001_InitialSchema.sql
   ```

3. **Update the connection string** in `appsettings.json` if needed.

4. **Run the app:**
   ```bash
   dotnet restore
   dotnet run
   ```

5. **Open the admin** at `http://localhost:5000/Admin`

## Deployment

This app uses a two-tier deployment model: a private dynamic instance for data entry, and a public static website on S3.

### 1. Build and run in Docker (homelab)

```bash
docker build -t cloverleaf-throws .
docker run -d \
  --name cloverleaf-throws \
  -p 5000:8080 \
  -e ConnectionStrings__DefaultConnection="Server=<your-sql-host>;Database=CloverleafThrows;User Id=<user>;Password=<pass>;TrustServerCertificate=True;Encrypt=false" \
  cloverleaf-throws
```

The admin area is at `http://<homelab-ip>:5000/Admin`. There is no authentication — keep this port off the public internet.

### 2. Enter data

Use the admin area to manage mesocycles, workouts, athletes, and meets. The public site at `http://<homelab-ip>:5000` previews what will be published.

### 3. Export to static HTML

With the Docker container running, crawl the public pages using `wget`:

```bash
wget \
  --mirror \
  --convert-links \
  --adjust-extension \
  --page-requisites \
  --no-parent \
  --reject "Admin*" \
  http://<homelab-ip>:5000
```

This produces a directory of static HTML, CSS, JS, and assets. The `--reject "Admin*"` flag ensures the admin area is not included in the export.

### 4. Publish to S3

```bash
aws s3 sync <homelab-ip>:5000/ s3://<your-bucket-name>/ \
  --delete \
  --acl public-read
```

The S3 bucket should be configured for static website hosting with `index.html` as the index document. Point your DNS CNAME to the bucket's website endpoint.

## Architecture

- **ASP.NET Core 10 MVC** with Razor views
- **Dapper** for all data access (no EF Core)
- **SQL Server** backend
- **No authentication** — admin area is private-network only

## Features

### Public Pages
- **`/`** — Calendar view showing the full mesocycle at a glance
- **`/Workout/Today`** — Today's workout detail
- **`/Workout/Detail?date=2026-03-12`** — Three-column workout layout
- **`/Workout/Print?date=2026-03-12`** — Print-friendly landscape layout
- **Weather swap** — Toggle SP/DT throws focus with one click

### Admin Area (`/Admin`)
- **Dashboard** — Today's workout, stats, upcoming meets, quick-swap
- **Workouts** — Full CRUD for workout days, duplicate/clone, drag-and-drop exercise reordering
- **Exercise Library** — Reusable exercise pool organized by category (20 warmups, shot put drills, discus drills, lifts, mobility, core)
- **Mesocycles** — Create training blocks, set current, generate days from week templates
- **Mesocycle Builder** — Define week templates (day types + SP/DT alternation), generate 1–12 weeks of workout days
- **Athletes** — Roster with event assignments (primary/secondary)
- **Meets** — Schedule with indoor/outdoor type
- **Training Load** — Season overview with day-type distribution, throws focus balance, week-by-week timeline

## Database Schema

Key tables: `Mesocycles` → `WorkoutDays` → `WorkoutSections` → `ExerciseGroups` → `WorkoutExercises`

Exercise library: `ExerciseCategories` → `Exercises`

Team: `Athletes` → `AthleteEvents`, `Meets`

Templates: `MesocycleTemplates` → `TemplateDays`

See `Migrations/001_InitialSchema.sql` for full schema with seed data.

## Design

UI matches [cloverleaftrack.com](https://cloverleaftrack.com):
- DM Sans typography
- Navy sidebar admin layout
- Color-coded day types and throwing focus badges
- Clean card-based design

## Project Structure

```
CloverleafThrows/
├── Controllers/
│   ├── HomeController.cs              # Public calendar
│   ├── WorkoutController.cs           # Public workout views + weather swap
│   ├── AdminController.cs             # Dashboard, season overview
│   ├── AdminWorkoutsController.cs     # Workout CRUD + AJAX exercise ops
│   └── AdminCrudControllers.cs        # Exercises, Mesocycles, Athletes, Meets
├── Data/
│   ├── DbConnectionFactory.cs         # Dapper connection wrapper
│   └── Repositories/
│       ├── WorkoutRepository.cs       # Workout day + section + exercise queries
│       └── OtherRepositories.cs       # Exercise, Mesocycle, Athlete, Meet repos
├── Migrations/
│   └── 001_InitialSchema.sql          # Full database schema + seed data
├── Models/
│   └── Entities.cs                    # All entity classes + view models
├── Views/
│   ├── Admin/                         # All admin views (dashboard, CRUD, etc.)
│   ├── Home/Index.cshtml              # Calendar grid
│   ├── Workout/                       # Detail, Print, NoWorkout, _SectionCard
│   └── Shared/_Layout.cshtml          # Public site layout
├── wwwroot/
│   ├── css/site.css                   # Public styles
│   ├── css/admin.css                  # Admin styles
│   ├── js/site.js                     # Public JS
│   ├── js/admin.js                    # Admin utilities
│   └── js/workout-editor.js           # Drag-and-drop exercise editor
├── Dockerfile
├── Program.cs
├── appsettings.json
└── CloverleafThrows.csproj
```
