using Dapper;
using CloverleafThrows.Models;

namespace CloverleafThrows.Data.Repositories;

public class WorkoutRepository
{
    private readonly IDbConnectionFactory _db;
    public WorkoutRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<WorkoutDay>> GetByMesocycleAsync(int mesocycleId)
    {
        using var conn = _db.CreateConnection();
        var days = (await conn.QueryAsync<WorkoutDay>(
            @"SELECT wd.*, m.Name AS MesocycleName
              FROM WorkoutDays wd
              JOIN Mesocycles m ON m.Id = wd.MesocycleId
              WHERE wd.MesocycleId = @mesocycleId
              ORDER BY wd.DayNumber",
            new { mesocycleId })).AsList();
        return days;
    }

    public async Task<WorkoutDay?> GetByDateAsync(DateTime date)
    {
        using var conn = _db.CreateConnection();
        var day = await conn.QueryFirstOrDefaultAsync<WorkoutDay>(
            @"SELECT wd.*, m.Name AS MesocycleName
              FROM WorkoutDays wd
              JOIN Mesocycles m ON m.Id = wd.MesocycleId
              WHERE wd.[Date] = @date",
            new { date });

        if (day != null)
            await LoadSectionsAsync(conn, day);

        return day;
    }

    public async Task<WorkoutDay?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var day = await conn.QueryFirstOrDefaultAsync<WorkoutDay>(
            @"SELECT wd.*, m.Name AS MesocycleName
              FROM WorkoutDays wd
              JOIN Mesocycles m ON m.Id = wd.MesocycleId
              WHERE wd.Id = @id",
            new { id });

        if (day != null)
            await LoadSectionsAsync(conn, day);

        return day;
    }

    public async Task<WorkoutDay?> GetTodayAsync()
        => await GetByDateAsync(DateTime.Today);

    public async Task<int> CreateDayAsync(WorkoutDay day)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO WorkoutDays (MesocycleId, DayNumber, [Date], DayType, ThrowsFocus, CoachNotes)
              OUTPUT INSERTED.Id
              VALUES (@MesocycleId, @DayNumber, @Date, @DayType, @ThrowsFocus, @CoachNotes)",
            day);
    }

    public async Task UpdateDayAsync(WorkoutDay day)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE WorkoutDays SET
                DayNumber = @DayNumber, [Date] = @Date, DayType = @DayType,
                ThrowsFocus = @ThrowsFocus, CoachNotes = @CoachNotes,
                UpdatedAt = GETUTCDATE()
              WHERE Id = @Id",
            day);
    }

    public async Task DeleteDayAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM WorkoutDays WHERE Id = @id", new { id });
    }

    public async Task SwapThrowsFocusAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE WorkoutDays SET
                ThrowsFocus = CASE ThrowsFocus WHEN 'Shot Put' THEN 'Discus' ELSE 'Shot Put' END,
                UpdatedAt = GETUTCDATE()
              WHERE Id = @id",
            new { id });
    }

    public async Task<int> DuplicateDayAsync(int sourceId, DateTime newDate, int newDayNumber)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var source = await conn.QueryFirstAsync<WorkoutDay>(
                "SELECT * FROM WorkoutDays WHERE Id = @sourceId",
                new { sourceId }, tx);

            var newDayId = await conn.QuerySingleAsync<int>(
                @"INSERT INTO WorkoutDays (MesocycleId, DayNumber, [Date], DayType, ThrowsFocus, CoachNotes)
                  OUTPUT INSERTED.Id
                  VALUES (@MesocycleId, @DayNumber, @Date, @DayType, @ThrowsFocus, @CoachNotes)",
                new
                {
                    source.MesocycleId,
                    DayNumber = newDayNumber,
                    Date = newDate,
                    source.DayType,
                    source.ThrowsFocus,
                    source.CoachNotes
                }, tx);

            var sections = await conn.QueryAsync<WorkoutSection>(
                "SELECT * FROM WorkoutSections WHERE WorkoutDayId = @sourceId ORDER BY SortOrder",
                new { sourceId }, tx);

            foreach (var section in sections)
            {
                var newSectionId = await conn.QuerySingleAsync<int>(
                    @"INSERT INTO WorkoutSections (WorkoutDayId, Name, SortOrder, HeaderColor)
                      OUTPUT INSERTED.Id
                      VALUES (@WorkoutDayId, @Name, @SortOrder, @HeaderColor)",
                    new { WorkoutDayId = newDayId, section.Name, section.SortOrder, section.HeaderColor }, tx);

                var groups = await conn.QueryAsync<ExerciseGroup>(
                    "SELECT * FROM ExerciseGroups WHERE WorkoutSectionId = @Id ORDER BY SortOrder",
                    new { Id = section.Id }, tx);

                foreach (var grp in groups)
                {
                    var newGroupId = await conn.QuerySingleAsync<int>(
                        @"INSERT INTO ExerciseGroups (WorkoutSectionId, Label, SortOrder)
                          OUTPUT INSERTED.Id
                          VALUES (@WorkoutSectionId, @Label, @SortOrder)",
                        new { WorkoutSectionId = newSectionId, grp.Label, grp.SortOrder }, tx);

                    var exercises = await conn.QueryAsync<WorkoutExercise>(
                        "SELECT * FROM WorkoutExercises WHERE ExerciseGroupId = @Id ORDER BY SortOrder",
                        new { Id = grp.Id }, tx);

                    foreach (var ex in exercises)
                    {
                        await conn.ExecuteAsync(
                            @"INSERT INTO WorkoutExercises (ExerciseGroupId, ExerciseId, ExerciseName, Number, Reps, SortOrder)
                              VALUES (@ExerciseGroupId, @ExerciseId, @ExerciseName, @Number, @Reps, @SortOrder)",
                            new { ExerciseGroupId = newGroupId, ex.ExerciseId, ex.ExerciseName, ex.Number, ex.Reps, ex.SortOrder }, tx);
                    }
                }
            }

            tx.Commit();
            return newDayId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ---- Section / Group / Exercise CRUD ----

    public async Task<int> AddSectionAsync(WorkoutSection section)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO WorkoutSections (WorkoutDayId, Name, SortOrder, HeaderColor)
              OUTPUT INSERTED.Id
              VALUES (@WorkoutDayId, @Name, @SortOrder, @HeaderColor)",
            section);
    }

    public async Task<int> AddGroupAsync(ExerciseGroup grp)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO ExerciseGroups (WorkoutSectionId, Label, SortOrder)
              OUTPUT INSERTED.Id
              VALUES (@WorkoutSectionId, @Label, @SortOrder)",
            grp);
    }

    public async Task<int> AddExerciseAsync(WorkoutExercise ex)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO WorkoutExercises (ExerciseGroupId, ExerciseId, ExerciseName, Number, Reps, SortOrder)
              OUTPUT INSERTED.Id
              VALUES (@ExerciseGroupId, @ExerciseId, @ExerciseName, @Number, @Reps, @SortOrder)",
            ex);
    }

    public async Task UpdateExerciseAsync(WorkoutExercise ex)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE WorkoutExercises SET
                ExerciseId = @ExerciseId, ExerciseName = @ExerciseName,
                Number = @Number, Reps = @Reps, SortOrder = @SortOrder
              WHERE Id = @Id",
            ex);
    }

    public async Task DeleteExerciseAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM WorkoutExercises WHERE Id = @id", new { id });
    }

    public async Task ReorderExercisesAsync(List<(int Id, int SortOrder)> items)
    {
        using var conn = _db.CreateConnection();
        foreach (var (id, sort) in items)
            await conn.ExecuteAsync(
                "UPDATE WorkoutExercises SET SortOrder = @sort WHERE Id = @id",
                new { id, sort });
    }

    public async Task UpdateCoachNotesAsync(int workoutDayId, string? notes)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE WorkoutDays SET CoachNotes = @notes, UpdatedAt = GETUTCDATE() WHERE Id = @workoutDayId",
            new { workoutDayId, notes });
    }

    // ---- Calendar helpers ----

    public async Task<List<CalendarWeek>> GetCalendarWeeksAsync(int mesocycleId)
    {
        var days = await GetByMesocycleAsync(mesocycleId);
        var weeks = new List<CalendarWeek>();
        int weekNum = 0;
        CalendarWeek? current = null;

        foreach (var day in days.OrderBy(d => d.Date))
        {
            if (current == null || day.Date.DayOfWeek == DayOfWeek.Monday)
            {
                weekNum++;
                current = new CalendarWeek { WeekNumber = weekNum, Label = $"Week {weekNum}" };
                weeks.Add(current);
            }
            current.Days.Add(day);
        }
        return weeks;
    }

    // ---- Load summary for training overview ----

    public async Task<List<DailyLoadSummary>> GetLoadSummaryAsync(int mesocycleId)
    {
        using var conn = _db.CreateConnection();
        return (await conn.QueryAsync<DailyLoadSummary>(
            @"SELECT dls.*, wd.[Date], wd.DayType
              FROM DailyLoadSummary dls
              JOIN WorkoutDays wd ON wd.Id = dls.WorkoutDayId
              WHERE wd.MesocycleId = @mesocycleId
              ORDER BY wd.[Date]",
            new { mesocycleId })).AsList();
    }

    // ---- Private helpers ----

    private async Task LoadSectionsAsync(System.Data.IDbConnection conn, WorkoutDay day)
    {
        var sections = (await conn.QueryAsync<WorkoutSection>(
            "SELECT * FROM WorkoutSections WHERE WorkoutDayId = @Id ORDER BY SortOrder",
            new { day.Id })).AsList();

        foreach (var section in sections)
        {
            var groups = (await conn.QueryAsync<ExerciseGroup>(
                "SELECT * FROM ExerciseGroups WHERE WorkoutSectionId = @Id ORDER BY SortOrder",
                new { section.Id })).AsList();

            foreach (var grp in groups)
            {
                grp.Exercises = (await conn.QueryAsync<WorkoutExercise>(
                    "SELECT * FROM WorkoutExercises WHERE ExerciseGroupId = @Id ORDER BY SortOrder",
                    new { grp.Id })).AsList();
            }
            section.Groups = groups;
        }
        day.Sections = sections;
    }
}
