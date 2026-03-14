# Cloverleaf Throws

Workout tracker for the Cloverleaf throws team. Built with ASP.NET Core 8, Dapper, and SQL Server.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2019+ (or SQL Server Express / LocalDB)

## Setup

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

5. **Login to admin** at `/Admin` — default credentials: `coach` / `changeme`

## Architecture

- **ASP.NET Core 8 MVC** with Razor views
- **Dapper** for all data access (no EF Core)
- **SQL Server** backend
- **Cookie-based auth** for the admin area
- **BCrypt** password hashing

## Features

### Public Pages
- **`/`** — Calendar view showing the full mesocycle at a glance
- **`/Workout/Today`** — Today's workout detail
- **`/Workout/Detail?date=2026-03-12`** — Three-column workout layout
- **`/Workout/Print?date=2026-03-12`** — Print-friendly landscape layout
- **Weather swap** — Authenticated coaches can toggle SP/DT with one click

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
│   ├── AdminController.cs             # Auth, dashboard, season overview
│   ├── AdminWorkoutsController.cs     # Workout CRUD + AJAX exercise ops
│   └── AdminCrudControllers.cs        # Exercises, Mesocycles, Athletes, Meets
├── Data/
│   ├── DbConnectionFactory.cs         # Dapper connection wrapper
│   └── Repositories/
│       ├── WorkoutRepository.cs       # Workout day + section + exercise queries
│       └── OtherRepositories.cs       # Exercise, Mesocycle, Athlete, Meet, Auth repos
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
├── Program.cs
├── appsettings.json
└── CloverleafThrows.csproj
```
