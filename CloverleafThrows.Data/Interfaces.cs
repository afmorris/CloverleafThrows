using CloverleafThrows.Models;

namespace CloverleafThrows.Data;

public interface IWorkoutRepository
{
    Task<List<WorkoutDay>> GetByMesocycleAsync(int mesocycleId);
    Task<WorkoutDay?> GetByDateAsync(DateTime date);
    Task<WorkoutDay?> GetByIdAsync(int id);
    Task<WorkoutDay?> GetTodayAsync();
    Task<int> CreateDayAsync(WorkoutDay day);
    Task UpdateDayAsync(WorkoutDay day);
    Task DeleteDayAsync(int id);
    Task SwapThrowsFocusAsync(int id);
    Task<int> DuplicateDayAsync(int sourceId, DateTime newDate, int newDayNumber);
    Task<int> AddSectionAsync(WorkoutSection section);
    Task<int> AddGroupAsync(ExerciseGroup grp);
    Task DeleteGroupAsync(int id);
    Task UpdateGroupLabelAsync(int id, string? label);
    Task<int> AddExerciseAsync(WorkoutExercise ex);
    Task UpdateExerciseAsync(WorkoutExercise ex);
    Task DeleteExerciseAsync(int id);
    Task ReorderExercisesAsync(List<(int Id, int SortOrder)> items);
    Task UpdateCoachNotesAsync(int workoutDayId, string? notes);
    Task<List<CalendarWeek>> GetCalendarWeeksAsync(int mesocycleId);
    Task<List<DailyLoadSummary>> GetLoadSummaryAsync(int mesocycleId);
}

public interface IExerciseRepository
{
    Task<List<Exercise>> GetAllAsync(bool includeInactive = false);
    Task<List<Exercise>> GetByCategoryAsync(int categoryId);
    Task<List<ExerciseCategory>> GetCategoriesAsync();
    Task<Exercise?> GetByIdAsync(int id);
    Task<int> CreateAsync(Exercise exercise);
    Task UpdateAsync(Exercise exercise);
    Task DeleteAsync(int id);
}

public interface IMesocycleRepository
{
    Task<List<Mesocycle>> GetAllAsync();
    Task<Mesocycle?> GetCurrentAsync();
    Task<Mesocycle?> GetByIdAsync(int id);
    Task<int> CreateAsync(Mesocycle mesocycle);
    Task UpdateAsync(Mesocycle mesocycle);
    Task SetCurrentAsync(int id);
    Task<List<MesocycleTemplate>> GetTemplatesAsync();
    Task<int> CreateTemplateAsync(MesocycleTemplate template);
    Task AddTemplateDayAsync(TemplateDay day);
    Task<int> GenerateFromTemplateAsync(int mesocycleId, int templateId, DateTime startDate, int weeks);
}

public interface IAthleteRepository
{
    Task<List<Athlete>> GetAllAsync(bool includeInactive = false);
    Task<Athlete?> GetByIdAsync(int id);
    Task<int> CreateAsync(Athlete athlete);
    Task UpdateAsync(Athlete athlete);
    Task SetEventsAsync(int athleteId, List<AthleteEvent> events);
}

public interface IMeetRepository
{
    Task<List<Meet>> GetAllAsync();
    Task<List<Meet>> GetUpcomingAsync(int count = 5);
    Task<Meet?> GetByIdAsync(int id);
    Task<int> CreateAsync(Meet meet);
    Task UpdateAsync(Meet meet);
    Task DeleteAsync(int id);
}
