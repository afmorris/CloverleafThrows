using Microsoft.AspNetCore.Mvc;
using CloverleafThrows.Data.Repositories;

namespace CloverleafThrows.Controllers;

public class WorkoutController : Controller
{
    private readonly WorkoutRepository _workouts;

    public WorkoutController(WorkoutRepository workouts) => _workouts = workouts;

    public async Task<IActionResult> Today()
    {
        var workout = await _workouts.GetTodayAsync();
        if (workout == null)
        {
            ViewBag.Message = "No workout scheduled for today.";
            return View("NoWorkout");
        }
        return View("Detail", workout);
    }

    public async Task<IActionResult> Detail(string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return NotFound();

        var workout = await _workouts.GetByDateAsync(parsedDate);
        if (workout == null) return NotFound();

        return View(workout);
    }

    public async Task<IActionResult> Print(string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return NotFound();

        var workout = await _workouts.GetByDateAsync(parsedDate);
        if (workout == null) return NotFound();

        return View(workout);
    }

    // Weather-based quick swap (public-facing, requires auth)
    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> SwapFocus(int id, string returnUrl)
    {
        await _workouts.SwapThrowsFocusAsync(id);
        return LocalRedirect(returnUrl ?? "/");
    }
}
