namespace CloverleafThrows.Models;

// ---- Exercises ----

public class ExerciseCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public class Exercise
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
    public string? DefaultReps { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CategoryName { get; set; } // joined
}

// ---- Mesocycles ----

public class Mesocycle
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public int TotalDays { get; set; } // computed
}

// ---- Workout Days ----

public class WorkoutDay
{
    public int Id { get; set; }
    public int MesocycleId { get; set; }
    public int DayNumber { get; set; }
    public DateTime Date { get; set; }
    public string DayType { get; set; } = "";
    public string ThrowsFocus { get; set; } = "";
    public string? CoachNotes { get; set; }
    public string? MesocycleName { get; set; } // joined

    // Loaded separately
    public List<WorkoutSection> Sections { get; set; } = new();

    public string DayTypeColor => DayType switch
    {
        "Oly / Lower" => "#D6E4F0",
        "Upper Body" => "#E2EFDA",
        "Throwing Only" => "#FCE4D6",
        "Jump / Sprint" => "#FFF2CC",
        "Full Body" => "#D9D2E9",
        _ => "#F5F5F5"
    };

    public string DayTypeTextColor => DayType switch
    {
        "Oly / Lower" => "#1F4E79",
        "Upper Body" => "#375623",
        "Throwing Only" => "#C55A11",
        "Jump / Sprint" => "#7F6000",
        "Full Body" => "#4A3670",
        _ => "#333333"
    };

    public string ThrowsFocusColor => ThrowsFocus switch
    {
        "Shot Put" => "#FCE4D6",
        "Discus" => "#D6E4F0",
        _ => "#F5F5F5"
    };

    public string ThrowsFocusTextColor => ThrowsFocus switch
    {
        "Shot Put" => "#C55A11",
        "Discus" => "#1F4E79",
        _ => "#333333"
    };

    // Convenience accessors for sections
    public WorkoutSection? ThrowsWarmup => Sections.FirstOrDefault(s => s.Name == "Throws Warmup");
    public WorkoutSection? Throwing => Sections.FirstOrDefault(s => s.Name == "Throwing");
    public WorkoutSection? Lifting => Sections.FirstOrDefault(s => s.Name == "Lifting");
    public WorkoutSection? Mobility => Sections.FirstOrDefault(s => s.Name == "Mobility");
    public WorkoutSection? Core => Sections.FirstOrDefault(s => s.Name == "Core");
}

public class WorkoutSection
{
    public int Id { get; set; }
    public int WorkoutDayId { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public string? HeaderColor { get; set; }
    public List<ExerciseGroup> Groups { get; set; } = new();
}

public class ExerciseGroup
{
    public int Id { get; set; }
    public int WorkoutSectionId { get; set; }
    public string? Label { get; set; }
    public int SortOrder { get; set; }
    public List<WorkoutExercise> Exercises { get; set; } = new();
}

public class WorkoutExercise
{
    public int Id { get; set; }
    public int ExerciseGroupId { get; set; }
    public int? ExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public string? Number { get; set; }
    public string Reps { get; set; } = "";
    public int SortOrder { get; set; }
}

// ---- Athletes ----

public class Athlete
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int? GradYear { get; set; }
    public string Gender { get; set; } = "M";
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public List<AthleteEvent> Events { get; set; } = new();

    public string FullName => $"{FirstName} {LastName}";
    public string GradYearDisplay => GradYear.HasValue ? $"'{GradYear % 100:D2}" : "";
}

public class AthleteEvent
{
    public int Id { get; set; }
    public int AthleteId { get; set; }
    public string EventName { get; set; } = "";
    public bool IsPrimary { get; set; }
}

// ---- Meets ----

public class Meet
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
    public string? Location { get; set; }
    public string MeetType { get; set; } = "Outdoor";
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

// ---- Auth ----

public class AdminUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? DisplayName { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

// ---- Training Load ----

public class DailyLoadSummary
{
    public int Id { get; set; }
    public int WorkoutDayId { get; set; }
    public int TotalExercises { get; set; }
    public int ThrowingVolume { get; set; }
    public int LiftingSets { get; set; }
    public bool HasPlyos { get; set; }
    public bool HasSprints { get; set; }
    public DateTime? Date { get; set; } // joined
    public string? DayType { get; set; } // joined
}

// ---- Calendar ----

public class CalendarWeek
{
    public int WeekNumber { get; set; }
    public string Label { get; set; } = "";
    public List<WorkoutDay> Days { get; set; } = new();
}

// ---- Templates ----

public class MesocycleTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public List<TemplateDay> Days { get; set; } = new();
}

public class TemplateDay
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public int DayOfWeek { get; set; } // 1=Mon ... 5=Fri
    public string DayType { get; set; } = "";
    public string ThrowsFocus { get; set; } = "";

    public string DayOfWeekName => DayOfWeek switch
    {
        1 => "Monday", 2 => "Tuesday", 3 => "Wednesday",
        4 => "Thursday", 5 => "Friday", _ => "?"
    };
}

// ---- View Models ----

public class WorkoutEditViewModel
{
    public WorkoutDay Day { get; set; } = new();
    public List<ExerciseCategory> Categories { get; set; } = new();
    public List<Exercise> ExerciseLibrary { get; set; } = new();
    public int MesocycleId { get; set; }
}

public class MesocycleBuilderViewModel
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public int Weeks { get; set; } = 4;
    public int? TemplateId { get; set; }
    public List<MesocycleTemplate> Templates { get; set; } = new();
}

public class SeasonOverviewViewModel
{
    public Mesocycle Mesocycle { get; set; } = new();
    public List<WorkoutDay> Days { get; set; } = new();
    public List<DailyLoadSummary> LoadData { get; set; } = new();
    public List<Meet> Meets { get; set; } = new();
}
