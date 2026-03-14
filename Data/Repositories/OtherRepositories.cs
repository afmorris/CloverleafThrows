using Dapper;
using CloverleafThrows.Models;

namespace CloverleafThrows.Data.Repositories;

// ============================================================
// Exercise Library
// ============================================================
public class ExerciseRepository
{
    private readonly IDbConnectionFactory _db;
    public ExerciseRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Exercise>> GetAllAsync(bool includeInactive = false)
    {
        using var conn = _db.CreateConnection();
        var where = includeInactive ? "" : "WHERE e.IsActive = 1";
        return (await conn.QueryAsync<Exercise>(
            $@"SELECT e.*, c.Name AS CategoryName
               FROM Exercises e JOIN ExerciseCategories c ON c.Id = e.CategoryId
               {where} ORDER BY c.SortOrder, e.Name")).AsList();
    }

    public async Task<List<Exercise>> GetByCategoryAsync(int categoryId)
    {
        using var conn = _db.CreateConnection();
        return (await conn.QueryAsync<Exercise>(
            "SELECT * FROM Exercises WHERE CategoryId = @categoryId AND IsActive = 1 ORDER BY Name",
            new { categoryId })).AsList();
    }

    public async Task<List<ExerciseCategory>> GetCategoriesAsync()
    {
        using var conn = _db.CreateConnection();
        return (await conn.QueryAsync<ExerciseCategory>(
            "SELECT * FROM ExerciseCategories ORDER BY SortOrder")).AsList();
    }

    public async Task<Exercise?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Exercise>(
            "SELECT e.*, c.Name AS CategoryName FROM Exercises e JOIN ExerciseCategories c ON c.Id = e.CategoryId WHERE e.Id = @id",
            new { id });
    }

    public async Task<int> CreateAsync(Exercise exercise)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO Exercises (Name, CategoryId, DefaultReps, Notes)
              OUTPUT INSERTED.Id
              VALUES (@Name, @CategoryId, @DefaultReps, @Notes)",
            exercise);
    }

    public async Task UpdateAsync(Exercise exercise)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Exercises SET Name = @Name, CategoryId = @CategoryId,
              DefaultReps = @DefaultReps, Notes = @Notes, IsActive = @IsActive,
              UpdatedAt = GETUTCDATE()
              WHERE Id = @Id",
            exercise);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Exercises SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @id",
            new { id });
    }
}

// ============================================================
// Mesocycles
// ============================================================
public class MesocycleRepository
{
    private readonly IDbConnectionFactory _db;
    public MesocycleRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Mesocycle>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return (await conn.QueryAsync<Mesocycle>(
            @"SELECT m.*,
                (SELECT COUNT(*) FROM WorkoutDays WHERE MesocycleId = m.Id) AS TotalDays
              FROM Mesocycles m ORDER BY m.StartDate DESC")).AsList();
    }

    public async Task<Mesocycle?> GetCurrentAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Mesocycle>(
            @"SELECT m.*,
                (SELECT COUNT(*) FROM WorkoutDays WHERE MesocycleId = m.Id) AS TotalDays
              FROM Mesocycles m WHERE m.IsCurrent = 1");
    }

    public async Task<Mesocycle?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Mesocycle>(
            @"SELECT m.*,
                (SELECT COUNT(*) FROM WorkoutDays WHERE MesocycleId = m.Id) AS TotalDays
              FROM Mesocycles m WHERE m.Id = @id",
            new { id });
    }

    public async Task<int> CreateAsync(Mesocycle mesocycle)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO Mesocycles (Name, Description, StartDate, EndDate, IsCurrent)
              OUTPUT INSERTED.Id
              VALUES (@Name, @Description, @StartDate, @EndDate, @IsCurrent)",
            mesocycle);
    }

    public async Task UpdateAsync(Mesocycle mesocycle)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Mesocycles SET Name = @Name, Description = @Description,
              StartDate = @StartDate, EndDate = @EndDate, IsCurrent = @IsCurrent,
              UpdatedAt = GETUTCDATE()
              WHERE Id = @Id",
            mesocycle);
    }

    public async Task SetCurrentAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Mesocycles SET IsCurrent = 0");
        await conn.ExecuteAsync("UPDATE Mesocycles SET IsCurrent = 1 WHERE Id = @id", new { id });
    }

    // ---- Templates ----

    public async Task<List<MesocycleTemplate>> GetTemplatesAsync()
    {
        using var conn = _db.CreateConnection();
        var templates = (await conn.QueryAsync<MesocycleTemplate>(
            "SELECT * FROM MesocycleTemplates ORDER BY Name")).AsList();

        foreach (var t in templates)
        {
            t.Days = (await conn.QueryAsync<TemplateDay>(
                "SELECT * FROM TemplateDays WHERE TemplateId = @Id ORDER BY DayOfWeek",
                new { t.Id })).AsList();
        }
        return templates;
    }

    public async Task<int> CreateTemplateAsync(MesocycleTemplate template)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO MesocycleTemplates (Name, Description)
              OUTPUT INSERTED.Id VALUES (@Name, @Description)",
            template);
    }

    public async Task AddTemplateDayAsync(TemplateDay day)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO TemplateDays (TemplateId, DayOfWeek, DayType, ThrowsFocus)
              VALUES (@TemplateId, @DayOfWeek, @DayType, @ThrowsFocus)",
            day);
    }

    /// <summary>
    /// Generate workout days from a template for a given number of weeks.
    /// </summary>
    public async Task<int> GenerateFromTemplateAsync(int mesocycleId, int templateId, DateTime startDate, int weeks)
    {
        using var conn = _db.CreateConnection();
        var templateDays = (await conn.QueryAsync<TemplateDay>(
            "SELECT * FROM TemplateDays WHERE TemplateId = @templateId ORDER BY DayOfWeek",
            new { templateId })).AsList();

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
                    @"INSERT INTO WorkoutDays (MesocycleId, DayNumber, [Date], DayType, ThrowsFocus)
                      VALUES (@MesocycleId, @DayNumber, @Date, @DayType, @ThrowsFocus)",
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
public class AthleteRepository
{
    private readonly IDbConnectionFactory _db;
    public AthleteRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Athlete>> GetAllAsync(bool includeInactive = false)
    {
        using var conn = _db.CreateConnection();
        var where = includeInactive ? "" : "WHERE a.IsActive = 1";
        var athletes = (await conn.QueryAsync<Athlete>(
            $"SELECT * FROM Athletes a {where} ORDER BY a.LastName, a.FirstName")).AsList();

        var events = (await conn.QueryAsync<AthleteEvent>(
            "SELECT * FROM AthleteEvents ORDER BY IsPrimary DESC, EventName")).AsList();

        foreach (var a in athletes)
            a.Events = events.Where(e => e.AthleteId == a.Id).ToList();

        return athletes;
    }

    public async Task<Athlete?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var athlete = await conn.QueryFirstOrDefaultAsync<Athlete>(
            "SELECT * FROM Athletes WHERE Id = @id", new { id });
        if (athlete != null)
            athlete.Events = (await conn.QueryAsync<AthleteEvent>(
                "SELECT * FROM AthleteEvents WHERE AthleteId = @id ORDER BY IsPrimary DESC", new { id })).AsList();
        return athlete;
    }

    public async Task<int> CreateAsync(Athlete athlete)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO Athletes (FirstName, LastName, GradYear, Gender, Notes)
              OUTPUT INSERTED.Id
              VALUES (@FirstName, @LastName, @GradYear, @Gender, @Notes)",
            athlete);
    }

    public async Task UpdateAsync(Athlete athlete)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Athletes SET FirstName = @FirstName, LastName = @LastName,
              GradYear = @GradYear, Gender = @Gender, IsActive = @IsActive,
              Notes = @Notes, UpdatedAt = GETUTCDATE()
              WHERE Id = @Id",
            athlete);
    }

    public async Task SetEventsAsync(int athleteId, List<AthleteEvent> events)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM AthleteEvents WHERE AthleteId = @athleteId", new { athleteId });
        foreach (var ev in events)
        {
            ev.AthleteId = athleteId;
            await conn.ExecuteAsync(
                @"INSERT INTO AthleteEvents (AthleteId, EventName, IsPrimary)
                  VALUES (@AthleteId, @EventName, @IsPrimary)",
                ev);
        }
    }
}

// ============================================================
// Meets
// ============================================================
public class MeetRepository
{
    private readonly IDbConnectionFactory _db;
    public MeetRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Meet>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return (await conn.QueryAsync<Meet>(
            "SELECT * FROM Meets WHERE IsActive = 1 ORDER BY [Date]")).AsList();
    }

    public async Task<List<Meet>> GetUpcomingAsync(int count = 5)
    {
        using var conn = _db.CreateConnection();
        return (await conn.QueryAsync<Meet>(
            "SELECT TOP(@count) * FROM Meets WHERE IsActive = 1 AND [Date] >= @today ORDER BY [Date]",
            new { count, today = DateTime.Today })).AsList();
    }

    public async Task<Meet?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Meet>(
            "SELECT * FROM Meets WHERE Id = @id", new { id });
    }

    public async Task<int> CreateAsync(Meet meet)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO Meets (Name, [Date], Location, MeetType, Notes)
              OUTPUT INSERTED.Id
              VALUES (@Name, @Date, @Location, @MeetType, @Notes)",
            meet);
    }

    public async Task UpdateAsync(Meet meet)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Meets SET Name = @Name, [Date] = @Date, Location = @Location,
              MeetType = @MeetType, Notes = @Notes, IsActive = @IsActive, UpdatedAt = GETUTCDATE()
              WHERE Id = @Id",
            meet);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Meets SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @id", new { id });
    }
}

// ============================================================
// Auth
// ============================================================
public class AuthRepository
{
    private readonly IDbConnectionFactory _db;
    public AuthRepository(IDbConnectionFactory db) => _db = db;

    public async Task<AdminUser?> GetByUsernameAsync(string username)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AdminUser>(
            "SELECT * FROM AdminUsers WHERE Username = @username",
            new { username });
    }

    public async Task UpdateLastLoginAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE AdminUsers SET LastLoginAt = GETUTCDATE() WHERE Id = @id",
            new { id });
    }

    public async Task UpdatePasswordAsync(int id, string passwordHash)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE AdminUsers SET PasswordHash = @passwordHash WHERE Id = @id",
            new { id, passwordHash });
    }
}
