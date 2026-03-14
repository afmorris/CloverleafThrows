using Microsoft.AspNetCore.Authentication.Cookies;
using CloverleafThrows.Data;
using CloverleafThrows.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' not found.");

builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<WorkoutRepository>();
builder.Services.AddScoped<ExerciseRepository>();
builder.Services.AddScoped<MesocycleRepository>();
builder.Services.AddScoped<AthleteRepository>();
builder.Services.AddScoped<MeetRepository>();
builder.Services.AddScoped<AuthRepository>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "CloverleafThrows.Auth";
        options.Cookie.HttpOnly = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "admin", pattern: "Admin/{action=Dashboard}/{id?}", defaults: new { controller = "Admin" });
app.MapControllerRoute(name: "adminWorkouts", pattern: "Admin/Workouts/{action=Index}/{id?}", defaults: new { controller = "AdminWorkouts" });
app.MapControllerRoute(name: "adminExercises", pattern: "Admin/Exercises/{action=Index}/{id?}", defaults: new { controller = "AdminExercises" });
app.MapControllerRoute(name: "adminMesocycles", pattern: "Admin/Mesocycles/{action=Index}/{id?}", defaults: new { controller = "AdminMesocycles" });
app.MapControllerRoute(name: "adminAthletes", pattern: "Admin/Athletes/{action=Index}/{id?}", defaults: new { controller = "AdminAthletes" });
app.MapControllerRoute(name: "adminMeets", pattern: "Admin/Meets/{action=Index}/{id?}", defaults: new { controller = "AdminMeets" });
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
