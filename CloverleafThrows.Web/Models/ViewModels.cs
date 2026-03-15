using CloverleafThrows.Models;

namespace CloverleafThrows.Models;

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
