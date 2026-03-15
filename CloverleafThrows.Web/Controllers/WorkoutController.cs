using Microsoft.AspNetCore.Mvc;
using CloverleafThrows.Data;

namespace CloverleafThrows.Web.Controllers;

public class WorkoutController(IWorkoutRepository workouts) : Controller
{
    public async Task<IActionResult> Today()
    {
        var workout = await workouts.GetTodayAsync();
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

        var workout = await workouts.GetByDateAsync(parsedDate);
        if (workout == null) return NotFound();

        return View(workout);
    }

    public async Task<IActionResult> Print(string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return NotFound();

        var workout = await workouts.GetByDateAsync(parsedDate);
        if (workout == null) return NotFound();

        return View(workout);
    }

    [HttpPost]
    public async Task<IActionResult> SwapFocus(int id, string returnUrl)
    {
        await workouts.SwapThrowsFocusAsync(id);
        return LocalRedirect(returnUrl ?? "/");
    }
}
