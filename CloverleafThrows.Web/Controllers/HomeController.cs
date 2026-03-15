using Microsoft.AspNetCore.Mvc;
using CloverleafThrows.Data;
using CloverleafThrows.Models;

namespace CloverleafThrows.Web.Controllers;

public class HomeController(IMesocycleRepository mesocycles, IWorkoutRepository workouts, IMeetRepository meets) : Controller
{
    public async Task<IActionResult> Index()
    {
        var mesocycle = await mesocycles.GetCurrentAsync();
        if (mesocycle == null)
        {
            ViewBag.Message = "No active training cycle.";
            return View(new List<CalendarWeek>());
        }

        ViewBag.Mesocycle = mesocycle;
        ViewBag.UpcomingMeets = await meets.GetUpcomingAsync(3);
        var weeks = await workouts.GetCalendarWeeksAsync(mesocycle.Id);
        return View(weeks);
    }
}
