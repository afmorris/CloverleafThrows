using Microsoft.AspNetCore.Mvc;
using CloverleafThrows.Data;
using CloverleafThrows.Models;

namespace CloverleafThrows.Web.Controllers;

public class AdminController(
    IMesocycleRepository mesocycles,
    IWorkoutRepository workouts,
    IMeetRepository meets,
    IAthleteRepository athletes) : Controller
{
    // ---- Dashboard ----

    public async Task<IActionResult> Dashboard()
    {
        var mesocycle = await mesocycles.GetCurrentAsync();

        ViewBag.Mesocycle = mesocycle;
        ViewBag.UpcomingMeets = await meets.GetUpcomingAsync(5);
        ViewBag.AthleteCount = (await athletes.GetAllAsync()).Count;
        ViewBag.Today = await workouts.GetTodayAsync();

        if (mesocycle != null)
        {
            ViewBag.Days = await workouts.GetByMesocycleAsync(mesocycle.Id);
            ViewBag.LoadData = await workouts.GetLoadSummaryAsync(mesocycle.Id);
        }

        return View("~/Views/Admin/Dashboard.cshtml");
    }

    // ---- Season Overview ----

    public async Task<IActionResult> SeasonOverview(int? mesocycleId)
    {
        var mesocycle = mesocycleId.HasValue
            ? await mesocycles.GetByIdAsync(mesocycleId.Value)
            : await mesocycles.GetCurrentAsync();

        if (mesocycle == null)
            return RedirectToAction("Dashboard");

        var vm = new SeasonOverviewViewModel
        {
            Mesocycle = mesocycle,
            Days = await workouts.GetByMesocycleAsync(mesocycle.Id),
            LoadData = await workouts.GetLoadSummaryAsync(mesocycle.Id),
            Meets = await meets.GetAllAsync()
        };

        return View("~/Views/Admin/SeasonOverview.cshtml", vm);
    }
}
