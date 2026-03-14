using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloverleafThrows.Data.Repositories;
using CloverleafThrows.Models;

namespace CloverleafThrows.Controllers;

[Authorize]
public class AdminWorkoutsController : Controller
{
    private readonly WorkoutRepository _workouts;
    private readonly ExerciseRepository _exercises;
    private readonly MesocycleRepository _mesocycles;

    public AdminWorkoutsController(
        WorkoutRepository workouts, ExerciseRepository exercises, MesocycleRepository mesocycles)
    {
        _workouts = workouts;
        _exercises = exercises;
        _mesocycles = mesocycles;
    }

    public async Task<IActionResult> Index(int? mesocycleId)
    {
        var mesocycle = mesocycleId.HasValue
            ? await _mesocycles.GetByIdAsync(mesocycleId.Value)
            : await _mesocycles.GetCurrentAsync();

        if (mesocycle == null)
        {
            ViewBag.Message = "No mesocycle found. Create one first.";
            return View("~/Views/Admin/Workouts/Index.cshtml", new List<WorkoutDay>());
        }

        ViewBag.Mesocycle = mesocycle;
        var days = await _workouts.GetByMesocycleAsync(mesocycle.Id);
        return View("~/Views/Admin/Workouts/Index.cshtml", days);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int mesocycleId)
    {
        var mesocycle = await _mesocycles.GetByIdAsync(mesocycleId);
        if (mesocycle == null) return NotFound();

        var vm = new WorkoutEditViewModel
        {
            Day = new WorkoutDay { MesocycleId = mesocycleId, Date = DateTime.Today },
            Categories = await _exercises.GetCategoriesAsync(),
            ExerciseLibrary = await _exercises.GetAllAsync(),
            MesocycleId = mesocycleId
        };
        return View("~/Views/Admin/Workouts/Edit.cshtml", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(WorkoutDay day)
    {
        var id = await _workouts.CreateDayAsync(day);
        return RedirectToAction("Edit", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var day = await _workouts.GetByIdAsync(id);
        if (day == null) return NotFound();

        var vm = new WorkoutEditViewModel
        {
            Day = day,
            Categories = await _exercises.GetCategoriesAsync(),
            ExerciseLibrary = await _exercises.GetAllAsync(),
            MesocycleId = day.MesocycleId
        };
        return View("~/Views/Admin/Workouts/Edit.cshtml", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(WorkoutDay day)
    {
        await _workouts.UpdateDayAsync(day);
        return RedirectToAction("Edit", new { id = day.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int mesocycleId)
    {
        await _workouts.DeleteDayAsync(id);
        return RedirectToAction("Index", new { mesocycleId });
    }

    [HttpPost]
    public async Task<IActionResult> Duplicate(int id, DateTime newDate, int newDayNumber)
    {
        var newId = await _workouts.DuplicateDayAsync(id, newDate, newDayNumber);
        return RedirectToAction("Edit", new { id = newId });
    }

    [HttpPost]
    public async Task<IActionResult> SwapFocus(int id)
    {
        await _workouts.SwapThrowsFocusAsync(id);
        var day = await _workouts.GetByIdAsync(id);
        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateNotes(int workoutDayId, string? coachNotes)
    {
        await _workouts.UpdateCoachNotesAsync(workoutDayId, coachNotes);
        return RedirectToAction("Edit", new { id = workoutDayId });
    }

    // ---- AJAX endpoints for exercise management ----

    [HttpPost]
    public async Task<IActionResult> AddSection(int workoutDayId, string name, string? headerColor)
    {
        var id = await _workouts.AddSectionAsync(new WorkoutSection
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
        var id = await _workouts.AddGroupAsync(new ExerciseGroup
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
        var id = await _workouts.AddExerciseAsync(new WorkoutExercise
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
        await _workouts.UpdateExerciseAsync(exercise);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        await _workouts.DeleteExerciseAsync(id);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> ReorderExercises([FromBody] List<ReorderItem> items)
    {
        var mapped = items.Select(i => (i.Id, i.SortOrder)).ToList();
        await _workouts.ReorderExercisesAsync(mapped);
        return Json(new { success = true });
    }

    public class ReorderItem { public int Id { get; set; } public int SortOrder { get; set; } }
}
