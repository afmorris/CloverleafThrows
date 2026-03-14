using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloverleafThrows.Data.Repositories;
using CloverleafThrows.Models;

namespace CloverleafThrows.Controllers;

public class AdminController : Controller
{
    private readonly AuthRepository _auth;
    private readonly MesocycleRepository _mesocycles;
    private readonly WorkoutRepository _workouts;
    private readonly MeetRepository _meets;
    private readonly AthleteRepository _athletes;

    public AdminController(
        AuthRepository auth, MesocycleRepository mesocycles,
        WorkoutRepository workouts, MeetRepository meets, AthleteRepository athletes)
    {
        _auth = auth;
        _mesocycles = mesocycles;
        _workouts = workouts;
        _meets = meets;
        _athletes = athletes;
    }

    // ---- Authentication ----

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard");
        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Admin/Login.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        var user = await _auth.GetByUsernameAsync(username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            ViewBag.Error = "Invalid username or password.";
            return View("~/Views/Admin/Login.cshtml");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("DisplayName", user.DisplayName ?? user.Username)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        await _auth.UpdateLastLoginAsync(user.Id);

        return LocalRedirect(returnUrl ?? "/Admin/Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // ---- Dashboard ----

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var mesocycle = await _mesocycles.GetCurrentAsync();
        var meets = await _meets.GetUpcomingAsync(5);
        var athletes = await _athletes.GetAllAsync();

        ViewBag.Mesocycle = mesocycle;
        ViewBag.UpcomingMeets = meets;
        ViewBag.AthleteCount = athletes.Count;
        ViewBag.Today = await _workouts.GetTodayAsync();

        if (mesocycle != null)
        {
            ViewBag.Days = await _workouts.GetByMesocycleAsync(mesocycle.Id);
            ViewBag.LoadData = await _workouts.GetLoadSummaryAsync(mesocycle.Id);
        }

        return View("~/Views/Admin/Dashboard.cshtml");
    }

    // ---- Season Overview ----

    [Authorize]
    public async Task<IActionResult> SeasonOverview(int? mesocycleId)
    {
        var mesocycle = mesocycleId.HasValue
            ? await _mesocycles.GetByIdAsync(mesocycleId.Value)
            : await _mesocycles.GetCurrentAsync();

        if (mesocycle == null)
            return RedirectToAction("Dashboard");

        var vm = new SeasonOverviewViewModel
        {
            Mesocycle = mesocycle,
            Days = await _workouts.GetByMesocycleAsync(mesocycle.Id),
            LoadData = await _workouts.GetLoadSummaryAsync(mesocycle.Id),
            Meets = await _meets.GetAllAsync()
        };

        return View("~/Views/Admin/SeasonOverview.cshtml", vm);
    }
}
