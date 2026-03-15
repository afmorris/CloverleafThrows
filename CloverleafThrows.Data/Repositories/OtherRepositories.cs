using Dapper;
using CloverleafThrows.Models;

namespace CloverleafThrows.Data.Repositories;

// ============================================================
// Exercise Library
// ============================================================
public class ExerciseRepository(IDbConnectionFactory db) : IExerciseRepository
{
    public async Task<List<Exercise>> GetAllAsync(bool includeInactive = false)
    {
        using var conn = db.CreateConnection();
        var where = includeInactive ? "" : "WHERE e.IsActive = 1";
        var sql = $"""
            SELECT e.*, c.Name AS CategoryName
            FROM Exercises e JOIN ExerciseCategories c ON c.Id = e.CategoryId
            {where} ORDER BY c.SortOrder, e.Name
            """;
        return (await conn.QueryAsync<Exercise>(sql)).ToList();
    }

    public async Task<List<Exercise>> GetByCategoryAsync(int categoryId)
    {
        using var conn = db.CreateConnection();
        return (await conn.QueryAsync<Exercise>(
            "SELECT * FROM Exercises WHERE CategoryId = @categoryId AND IsActive = 1 ORDER BY Name",
            new { categoryId })).ToList();
    }

    public async Task<List<ExerciseCategory>> GetCategoriesAsync()
    {
        using var conn = db.CreateConnection();
        return (await conn.QueryAsync<ExerciseCategory>(
            "SELECT * FROM ExerciseCategories ORDER BY SortOrder")).ToList();
    }

    public async Task<Exercise?> GetByIdAsync(int id)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Exercise>(
            "SELECT e.*, c.Name AS CategoryName FROM Exercises e JOIN ExerciseCategories c ON c.Id = e.CategoryId WHERE e.Id = @id",
            new { id });
    }

    public async Task<int> CreateAsync(Exercise exercise)
    {
        using var conn = db.CreateConnection();
        var sql = """
            INSERT INTO Exercises (Name, CategoryId, DefaultReps, Notes)
            OUTPUT INSERTED.Id
            VALUES (@Name, @CategoryId, @DefaultReps, @Notes)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, exercise);
    }

    public async Task UpdateAsync(Exercise exercise)
    {
        using var conn = db.CreateConnection();
        var sql = """
            UPDATE Exercises SET Name = @Name, CategoryId = @CategoryId,
                DefaultReps = @DefaultReps, Notes = @Notes, IsActive = @IsActive,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, exercise);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Exercises SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @id",
            new { id });
    }
}

// ============================================================
// Mesocycles
// ============================================================
public class MesocycleRepository(IDbConnectionFactory db) : IMesocycleRepository
{
    public async Task<List<Mesocycle>> GetAllAsync()
    {
        using var conn = db.CreateConnection();
        var sql = """
            SELECT m.*,
                (SELECT COUNT(*) FROM WorkoutDays WHERE MesocycleId = m.Id) AS TotalDays
            FROM Mesocycles m ORDER BY m.StartDate DESC
            """;
        return (await conn.QueryAsync<Mesocycle>(sql)).ToList();
    }

    public async Task<Mesocycle?> GetCurrentAsync()
    {
        using var conn = db.CreateConnection();
        var sql = """
            SELECT m.*,
                (SELECT COUNT(*) FROM WorkoutDays WHERE MesocycleId = m.Id) AS TotalDays
            FROM Mesocycles m WHERE m.IsCurrent = 1
            """;
        return await conn.QueryFirstOrDefaultAsync<Mesocycle>(sql);
    }

    public async Task<Mesocycle?> GetByIdAsync(int id)
    {
        using var conn = db.CreateConnection();
        var sql = """
            SELECT m.*,
                (SELECT COUNT(*) FROM WorkoutDays WHERE MesocycleId = m.Id) AS TotalDays
            FROM Mesocycles m WHERE m.Id = @id
            """;
        return await conn.QueryFirstOrDefaultAsync<Mesocycle>(sql, new { id });
    }

    public async Task<int> CreateAsync(Mesocycle mesocycle)
    {
        using var conn = db.CreateConnection();
        var sql = """
            INSERT INTO Mesocycles (Name, Description, StartDate, EndDate, IsCurrent)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Description, @StartDate, @EndDate, @IsCurrent)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, mesocycle);
    }

    public async Task UpdateAsync(Mesocycle mesocycle)
    {
        using var conn = db.CreateConnection();
        var sql = """
            UPDATE Mesocycles SET Name = @Name, Description = @Description,
                StartDate = @StartDate, EndDate = @EndDate, IsCurrent = @IsCurrent,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, mesocycle);
    }

    public async Task SetCurrentAsync(int id)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Mesocycles SET IsCurrent = 0");
        await conn.ExecuteAsync("UPDATE Mesocycles SET IsCurrent = 1 WHERE Id = @id", new { id });
    }

    // ---- Templates ----

    public async Task<List<MesocycleTemplate>> GetTemplatesAsync()
    {
        using var conn = db.CreateConnection();
        var templates = (await conn.QueryAsync<MesocycleTemplate>(
            "SELECT * FROM MesocycleTemplates ORDER BY Name")).ToList();

        foreach (var t in templates)
        {
            t.Days = (await conn.QueryAsync<TemplateDay>(
                "SELECT * FROM TemplateDays WHERE TemplateId = @Id ORDER BY DayOfWeek",
                new { t.Id })).ToList();
        }
        return templates;
    }

    public async Task<int> CreateTemplateAsync(MesocycleTemplate template)
    {
        using var conn = db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO MesocycleTemplates (Name, Description) OUTPUT INSERTED.Id VALUES (@Name, @Description)",
            template);
    }

    public async Task AddTemplateDayAsync(TemplateDay day)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO TemplateDays (TemplateId, DayOfWeek, DayType, ThrowsFocus) VALUES (@TemplateId, @DayOfWeek, @DayType, @ThrowsFocus)",
            day);
    }

    public async Task<int> GenerateFromTemplateAsync(int mesocycleId, int templateId, DateTime startDate, int weeks)
    {
        using var conn = db.CreateConnection();
        var templateDays = (await conn.QueryAsync<TemplateDay>(
            "SELECT * FROM TemplateDays WHERE TemplateId = @templateId ORDER BY DayOfWeek",
            new { templateId })).ToList();

        int dayNumber = 1;
        bool alternateFlip = false;

        for (int week = 0; week < weeks; week++)
        {
            foreach (var td in templateDays)
            {
                var date = startDate.AddDays(week * 7 + (td.DayOfWeek - 1));

                var throwsFocus = td.ThrowsFocus;
                if (throwsFocus == "Alternate")
                {
                    throwsFocus = alternateFlip ? "Discus" : "Shot Put";
                    alternateFlip = !alternateFlip;
                }

                await conn.ExecuteAsync(
                    """
                    INSERT INTO WorkoutDays (MesocycleId, DayNumber, [Date], DayType, ThrowsFocus)
                    VALUES (@MesocycleId, @DayNumber, @Date, @DayType, @ThrowsFocus)
                    """,
                    new { MesocycleId = mesocycleId, DayNumber = dayNumber, Date = date, td.DayType, ThrowsFocus = throwsFocus });

                dayNumber++;
            }
        }

        return dayNumber - 1;
    }
}

// ============================================================
// Athletes
// ============================================================
public class AthleteRepository(IDbConnectionFactory db) : IAthleteRepository
{
    public async Task<List<Athlete>> GetAllAsync(bool includeInactive = false)
    {
        using var conn = db.CreateConnection();
        var where = includeInactive ? "" : "WHERE a.IsActive = 1";
        var athletes = (await conn.QueryAsync<Athlete>(
            $"SELECT * FROM Athletes a {where} ORDER BY a.LastName, a.FirstName")).ToList();

        var events = (await conn.QueryAsync<AthleteEvent>(
            "SELECT * FROM AthleteEvents ORDER BY IsPrimary DESC, EventName")).ToList();

        foreach (var a in athletes)
            a.Events = events.Where(e => e.AthleteId == a.Id).ToList();

        return athletes;
    }

    public async Task<Athlete?> GetByIdAsync(int id)
    {
        using var conn = db.CreateConnection();
        var athlete = await conn.QueryFirstOrDefaultAsync<Athlete>(
            "SELECT * FROM Athletes WHERE Id = @id", new { id });
        if (athlete != null)
            athlete.Events = (await conn.QueryAsync<AthleteEvent>(
                "SELECT * FROM AthleteEvents WHERE AthleteId = @id ORDER BY IsPrimary DESC", new { id })).ToList();
        return athlete;
    }

    public async Task<int> CreateAsync(Athlete athlete)
    {
        using var conn = db.CreateConnection();
        var sql = """
            INSERT INTO Athletes (FirstName, LastName, GradYear, Gender, Notes)
            OUTPUT INSERTED.Id
            VALUES (@FirstName, @LastName, @GradYear, @Gender, @Notes)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, athlete);
    }

    public async Task UpdateAsync(Athlete athlete)
    {
        using var conn = db.CreateConnection();
        var sql = """
            UPDATE Athletes SET FirstName = @FirstName, LastName = @LastName,
                GradYear = @GradYear, Gender = @Gender, IsActive = @IsActive,
                Notes = @Notes, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, athlete);
    }

    public async Task SetEventsAsync(int athleteId, List<AthleteEvent> events)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM AthleteEvents WHERE AthleteId = @athleteId", new { athleteId });
        foreach (var ev in events)
        {
            ev.AthleteId = athleteId;
            await conn.ExecuteAsync(
                "INSERT INTO AthleteEvents (AthleteId, EventName, IsPrimary) VALUES (@AthleteId, @EventName, @IsPrimary)",
                ev);
        }
    }
}

// ============================================================
// Meets
// ============================================================
public class MeetRepository(IDbConnectionFactory db) : IMeetRepository
{
    public async Task<List<Meet>> GetAllAsync()
    {
        using var conn = db.CreateConnection();
        return (await conn.QueryAsync<Meet>(
            "SELECT * FROM Meets WHERE IsActive = 1 ORDER BY [Date]")).ToList();
    }

    public async Task<List<Meet>> GetUpcomingAsync(int count = 5)
    {
        using var conn = db.CreateConnection();
        return (await conn.QueryAsync<Meet>(
            "SELECT TOP(@count) * FROM Meets WHERE IsActive = 1 AND [Date] >= @today ORDER BY [Date]",
            new { count, today = DateTime.Today })).ToList();
    }

    public async Task<Meet?> GetByIdAsync(int id)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Meet>(
            "SELECT * FROM Meets WHERE Id = @id", new { id });
    }

    public async Task<int> CreateAsync(Meet meet)
    {
        using var conn = db.CreateConnection();
        var sql = """
            INSERT INTO Meets (Name, [Date], Location, MeetType, Notes)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Date, @Location, @MeetType, @Notes)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, meet);
    }

    public async Task UpdateAsync(Meet meet)
    {
        using var conn = db.CreateConnection();
        var sql = """
            UPDATE Meets SET Name = @Name, [Date] = @Date, Location = @Location,
                MeetType = @MeetType, Notes = @Notes, IsActive = @IsActive, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, meet);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Meets SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @id",
            new { id });
    }
}
