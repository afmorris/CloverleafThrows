using Microsoft.AspNetCore.Mvc;
using CloverleafThrows.Data.Repositories;

namespace CloverleafThrows.Controllers;

public class HomeController : Controller
{
    private readonly MesocycleRepository _mesocycles;
    private readonly WorkoutRepository _workouts;
    private readonly MeetRepository _meets;

    public HomeController(MesocycleRepository mesocycles, WorkoutRepository workouts, MeetRepository meets)
    {
        _mesocycles = mesocycles;
        _workouts = workouts;
        _meets = meets;
    }

    public async Task<IActionResult> Index()
    {
        var mesocycle = await _mesocycles.GetCurrentAsync();
        if (mesocycle == null)
        {
            ViewBag.Message = "No active training cycle.";
            return View(new List<CloverleafThrows.Models.CalendarWeek>());
        }

        ViewBag.Mesocycle = mesocycle;
        ViewBag.UpcomingMeets = await _meets.GetUpcomingAsync(3);
        var weeks = await _workouts.GetCalendarWeeksAsync(mesocycle.Id);
        return View(weeks);
    }
}
