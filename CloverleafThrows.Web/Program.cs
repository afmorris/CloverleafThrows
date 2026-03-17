using CloverleafThrows.Data;
using CloverleafThrows.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' not found.");

builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<IWorkoutRepository, WorkoutRepository>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IMesocycleRepository, MesocycleRepository>();
builder.Services.AddScoped<IAthleteRepository, AthleteRepository>();
builder.Services.AddScoped<IMeetRepository, MeetRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(name: "workoutDetail", pattern: "Workout/Detail/{date}", defaults: new { controller = "Workout", action = "Detail" });
app.MapControllerRoute(name: "workoutPrint", pattern: "Workout/Print/{date}", defaults: new { controller = "Workout", action = "Print" });
app.MapControllerRoute(name: "admin", pattern: "Admin/{action=Dashboard}/{id?}", defaults: new { controller = "Admin" });
app.MapControllerRoute(name: "adminWorkouts", pattern: "Admin/Workouts/{action=Index}/{id?}", defaults: new { controller = "AdminWorkouts" });
app.MapControllerRoute(name: "adminExercises", pattern: "Admin/Exercises/{action=Index}/{id?}", defaults: new { controller = "AdminExercises" });
app.MapControllerRoute(name: "adminMesocycles", pattern: "Admin/Mesocycles/{action=Index}/{id?}", defaults: new { controller = "AdminMesocycles" });
app.MapControllerRoute(name: "adminAthletes", pattern: "Admin/Athletes/{action=Index}/{id?}", defaults: new { controller = "AdminAthletes" });
app.MapControllerRoute(name: "adminMeets", pattern: "Admin/Meets/{action=Index}/{id?}", defaults: new { controller = "AdminMeets" });
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
