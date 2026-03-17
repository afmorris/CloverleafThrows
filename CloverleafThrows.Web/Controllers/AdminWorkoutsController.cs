using Microsoft.AspNetCore.Mvc;
using CloverleafThrows.Data;
using CloverleafThrows.Models;

namespace CloverleafThrows.Web.Controllers;

public class AdminWorkoutsController(
    IWorkoutRepository workouts,
    IExerciseRepository exercises,
    IMesocycleRepository mesocycles) : Controller
{
    public async Task<IActionResult> Index(int? mesocycleId)
    {
        var mesocycle = mesocycleId.HasValue
            ? await mesocycles.GetByIdAsync(mesocycleId.Value)
            : await mesocycles.GetCurrentAsync();

        if (mesocycle == null)
        {
            ViewBag.Message = "No mesocycle found. Create one first.";
            return View("~/Views/Admin/Workouts/Index.cshtml", new List<WorkoutDay>());
        }

        ViewBag.Mesocycle = mesocycle;
        var days = await workouts.GetByMesocycleAsync(mesocycle.Id);
        return View("~/Views/Admin/Workouts/Index.cshtml", days);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int mesocycleId)
    {
        var mesocycle = await mesocycles.GetByIdAsync(mesocycleId);
        if (mesocycle == null) return NotFound();

        var vm = new WorkoutEditViewModel
        {
            Day = new WorkoutDay { MesocycleId = mesocycleId, Date = DateTime.Today },
            Categories = await exercises.GetCategoriesAsync(),
            ExerciseLibrary = await exercises.GetAllAsync(),
            MesocycleId = mesocycleId
        };
        return View("~/Views/Admin/Workouts/Edit.cshtml", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(WorkoutDay day)
    {
        var id = await workouts.CreateDayAsync(day);
        return RedirectToAction("Edit", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var day = await workouts.GetByIdAsync(id);
        if (day == null) return NotFound();

        var vm = new WorkoutEditViewModel
        {
            Day = day,
            Categories = await exercises.GetCategoriesAsync(),
            ExerciseLibrary = await exercises.GetAllAsync(),
            MesocycleId = day.MesocycleId
        };
        return View("~/Views/Admin/Workouts/Edit.cshtml", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(WorkoutDay day)
    {
        await workouts.UpdateDayAsync(day);
        return RedirectToAction("Edit", new { id = day.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int mesocycleId)
    {
        await workouts.DeleteDayAsync(id);
        return RedirectToAction("Index", new { mesocycleId });
    }

    [HttpPost]
    public async Task<IActionResult> Duplicate(int id, DateTime newDate, int newDayNumber)
    {
        var newId = await workouts.DuplicateDayAsync(id, newDate, newDayNumber);
        return RedirectToAction("Edit", new { id = newId });
    }

    [HttpPost]
    public async Task<IActionResult> SwapFocus(int id)
    {
        await workouts.SwapThrowsFocusAsync(id);
        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateNotes(int workoutDayId, string? coachNotes)
    {
        await workouts.UpdateCoachNotesAsync(workoutDayId, coachNotes);
        return RedirectToAction("Edit", new { id = workoutDayId });
    }

    // ---- AJAX endpoints for exercise management ----

    [HttpPost]
    public async Task<IActionResult> AddSection(int workoutDayId, string name, string? headerColor)
    {
        var id = await workouts.AddSectionAsync(new WorkoutSection
        {
            WorkoutDayId = workoutDayId,
            Name = name,
            HeaderColor = headerColor,
            SortOrder = 99
        });
        return Json(new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AddGroup(int workoutSectionId, string? label)
    {
        var id = await workouts.AddGroupAsync(new ExerciseGroup
        {
            WorkoutSectionId = workoutSectionId,
            Label = label,
            SortOrder = 99
        });
        return Json(new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AddExercise(int exerciseGroupId, int? exerciseId,
        string exerciseName, string? number, string reps)
    {
        var id = await workouts.AddExerciseAsync(new WorkoutExercise
        {
            ExerciseGroupId = exerciseGroupId,
            ExerciseId = exerciseId,
            ExerciseName = exerciseName,
            Number = number,
            Reps = reps,
            SortOrder = 99
        });
        return Json(new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateExercise(WorkoutExercise exercise)
    {
        await workouts.UpdateExerciseAsync(exercise);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        await workouts.DeleteGroupAsync(id);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateGroupLabel(int id, string? label)
    {
        await workouts.UpdateGroupLabelAsync(id, label);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        await workouts.DeleteExerciseAsync(id);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> ReorderExercises([FromBody] List<ReorderItem> items)
    {
        var mapped = items.Select(i => (i.Id, i.SortOrder)).ToList();
        await workouts.ReorderExercisesAsync(mapped);
        return Json(new { success = true });
    }

    public class ReorderItem { public int Id { get; set; } public int SortOrder { get; set; } }
}
