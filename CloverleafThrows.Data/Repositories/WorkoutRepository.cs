using System.Data;
using Dapper;
using CloverleafThrows.Models;

namespace CloverleafThrows.Data.Repositories;

public class WorkoutRepository(IDbConnectionFactory db) : IWorkoutRepository
{
    public async Task<List<WorkoutDay>> GetByMesocycleAsync(int mesocycleId)
    {
        using var conn = db.CreateConnection();
        var sql = """
            SELECT wd.*, m.Name AS MesocycleName
            FROM WorkoutDays wd
            JOIN Mesocycles m ON m.Id = wd.MesocycleId
            WHERE wd.MesocycleId = @mesocycleId
            ORDER BY wd.DayNumber
            """;
        return (await conn.QueryAsync<WorkoutDay>(sql, new { mesocycleId })).ToList();
    }

    public async Task<WorkoutDay?> GetByDateAsync(DateTime date)
    {
        using var conn = db.CreateConnection();
        var sql = """
            SELECT wd.*, m.Name AS MesocycleName
            FROM WorkoutDays wd
            JOIN Mesocycles m ON m.Id = wd.MesocycleId
            WHERE wd.[Date] = @date
            """;
        var day = await conn.QueryFirstOrDefaultAsync<WorkoutDay>(sql, new { date });
        if (day != null) await LoadSectionsAsync(conn, day);
        return day;
    }

    public async Task<WorkoutDay?> GetByIdAsync(int id)
    {
        using var conn = db.CreateConnection();
        var sql = """
            SELECT wd.*, m.Name AS MesocycleName
            FROM WorkoutDays wd
            JOIN Mesocycles m ON m.Id = wd.MesocycleId
            WHERE wd.Id = @id
            """;
        var day = await conn.QueryFirstOrDefaultAsync<WorkoutDay>(sql, new { id });
        if (day != null) await LoadSectionsAsync(conn, day);
        return day;
    }

    public async Task<WorkoutDay?> GetTodayAsync() => await GetByDateAsync(DateTime.Today);

    public async Task<int> CreateDayAsync(WorkoutDay day)
    {
        using var conn = db.CreateConnection();
        var sql = """
            INSERT INTO WorkoutDays (MesocycleId, DayNumber, [Date], DayType, ThrowsFocus, CoachNotes)
            OUTPUT INSERTED.Id
            VALUES (@MesocycleId, @DayNumber, @Date, @DayType, @ThrowsFocus, @CoachNotes)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, day);
    }

    public async Task UpdateDayAsync(WorkoutDay day)
    {
        using var conn = db.CreateConnection();
        var sql = """
            UPDATE WorkoutDays SET
                DayNumber = @DayNumber, [Date] = @Date, DayType = @DayType,
                ThrowsFocus = @ThrowsFocus, CoachNotes = @CoachNotes,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, day);
    }

    public async Task DeleteDayAsync(int id)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM WorkoutDays WHERE Id = @id", new { id });
    }

    public async Task SwapThrowsFocusAsync(int id)
    {
        using var conn = db.CreateConnection();
        var sql = """
            UPDATE WorkoutDays SET
                ThrowsFocus = CASE ThrowsFocus WHEN 'Shot Put' THEN 'Discus' ELSE 'Shot Put' END,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @id
            """;
        await conn.ExecuteAsync(sql, new { id });
    }

    public async Task<int> DuplicateDayAsync(int sourceId, DateTime newDate, int newDayNumber)
    {
        using var conn = db.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var source = await conn.QueryFirstAsync<WorkoutDay>(
                "SELECT * FROM WorkoutDays WHERE Id = @sourceId",
                new { sourceId }, tx);

            var newDayId = await conn.ExecuteScalarAsync<int>(
                """
                INSERT INTO WorkoutDays (MesocycleId, DayNumber, [Date], DayType, ThrowsFocus, CoachNotes)
                OUTPUT INSERTED.Id
                VALUES (@MesocycleId, @DayNumber, @Date, @DayType, @ThrowsFocus, @CoachNotes)
                """,
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
                var newSectionId = await conn.ExecuteScalarAsync<int>(
                    """
                    INSERT INTO WorkoutSections (WorkoutDayId, Name, SortOrder, HeaderColor)
                    OUTPUT INSERTED.Id
                    VALUES (@WorkoutDayId, @Name, @SortOrder, @HeaderColor)
                    """,
                    new { WorkoutDayId = newDayId, section.Name, section.SortOrder, section.HeaderColor }, tx);

                var groups = await conn.QueryAsync<ExerciseGroup>(
                    "SELECT * FROM ExerciseGroups WHERE WorkoutSectionId = @Id ORDER BY SortOrder",
                    new { Id = section.Id }, tx);

                foreach (var grp in groups)
                {
                    var newGroupId = await conn.ExecuteScalarAsync<int>(
                        """
                        INSERT INTO ExerciseGroups (WorkoutSectionId, Label, SortOrder)
                        OUTPUT INSERTED.Id
                        VALUES (@WorkoutSectionId, @Label, @SortOrder)
                        """,
                        new { WorkoutSectionId = newSectionId, grp.Label, grp.SortOrder }, tx);

                    var exercises = await conn.QueryAsync<WorkoutExercise>(
                        "SELECT * FROM WorkoutExercises WHERE ExerciseGroupId = @Id ORDER BY SortOrder",
                        new { Id = grp.Id }, tx);

                    foreach (var ex in exercises)
                    {
                        await conn.ExecuteAsync(
                            """
                            INSERT INTO WorkoutExercises (ExerciseGroupId, ExerciseId, ExerciseName, Number, Reps, SortOrder)
                            VALUES (@ExerciseGroupId, @ExerciseId, @ExerciseName, @Number, @Reps, @SortOrder)
                            """,
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
        using var conn = db.CreateConnection();
        var sql = """
            INSERT INTO WorkoutSections (WorkoutDayId, Name, SortOrder, HeaderColor)
            OUTPUT INSERTED.Id
            VALUES (@WorkoutDayId, @Name, @SortOrder, @HeaderColor)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, section);
    }

    public async Task<int> AddGroupAsync(ExerciseGroup grp)
    {
        using var conn = db.CreateConnection();
        var sql = """
            INSERT INTO ExerciseGroups (WorkoutSectionId, Label, SortOrder)
            OUTPUT INSERTED.Id
            VALUES (@WorkoutSectionId, @Label, @SortOrder)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, grp);
    }

    public async Task DeleteGroupAsync(int id)
    {
        using var conn = db.CreateConnection();
        // WorkoutExercises has no ON DELETE CASCADE, so clear children first
        await conn.ExecuteAsync("DELETE FROM WorkoutExercises WHERE ExerciseGroupId = @id", new { id });
        await conn.ExecuteAsync("DELETE FROM ExerciseGroups WHERE Id = @id", new { id });
    }

    public async Task UpdateGroupLabelAsync(int id, string? label)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE ExerciseGroups SET Label = @label WHERE Id = @id",
            new { id, label });
    }

    public async Task<int> AddExerciseAsync(WorkoutExercise ex)
    {
        using var conn = db.CreateConnection();
        var sql = """
            INSERT INTO WorkoutExercises (ExerciseGroupId, ExerciseId, ExerciseName, Number, Reps, SortOrder)
            OUTPUT INSERTED.Id
            VALUES (@ExerciseGroupId, @ExerciseId, @ExerciseName, @Number, @Reps, @SortOrder)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, ex);
    }

    public async Task UpdateExerciseAsync(WorkoutExercise ex)
    {
        using var conn = db.CreateConnection();
        var sql = """
            UPDATE WorkoutExercises SET
                ExerciseId = @ExerciseId, ExerciseName = @ExerciseName,
                Number = @Number, Reps = @Reps, SortOrder = @SortOrder
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, ex);
    }

    public async Task DeleteExerciseAsync(int id)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM WorkoutExercises WHERE Id = @id", new { id });
    }

    public async Task ReorderExercisesAsync(List<(int Id, int SortOrder)> items)
    {
        using var conn = db.CreateConnection();
        foreach (var (id, sort) in items)
            await conn.ExecuteAsync(
                "UPDATE WorkoutExercises SET SortOrder = @sort WHERE Id = @id",
                new { id, sort });
    }

    public async Task UpdateCoachNotesAsync(int workoutDayId, string? notes)
    {
        using var conn = db.CreateConnection();
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
        using var conn = db.CreateConnection();
        var sql = """
            SELECT dls.*, wd.[Date], wd.DayType
            FROM DailyLoadSummary dls
            JOIN WorkoutDays wd ON wd.Id = dls.WorkoutDayId
            WHERE wd.MesocycleId = @mesocycleId
            ORDER BY wd.[Date]
            """;
        return (await conn.QueryAsync<DailyLoadSummary>(sql, new { mesocycleId })).ToList();
    }

    // ---- Private helpers ----

    private static async Task LoadSectionsAsync(IDbConnection conn, WorkoutDay day)
    {
        var sections = (await conn.QueryAsync<WorkoutSection>(
            "SELECT * FROM WorkoutSections WHERE WorkoutDayId = @Id ORDER BY SortOrder",
            new { day.Id })).ToList();

        foreach (var section in sections)
        {
            var groups = (await conn.QueryAsync<ExerciseGroup>(
                "SELECT * FROM ExerciseGroups WHERE WorkoutSectionId = @Id ORDER BY SortOrder",
                new { section.Id })).ToList();

            foreach (var grp in groups)
            {
                grp.Exercises = (await conn.QueryAsync<WorkoutExercise>(
                    "SELECT * FROM WorkoutExercises WHERE ExerciseGroupId = @Id ORDER BY SortOrder",
                    new { grp.Id })).ToList();
            }
            section.Groups = groups;
        }
        day.Sections = sections;
    }
}
