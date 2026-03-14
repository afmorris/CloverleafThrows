using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloverleafThrows.Data.Repositories;
using CloverleafThrows.Models;

namespace CloverleafThrows.Controllers;

// ============================================================
// Exercises
// ============================================================
[Authorize]
public class AdminExercisesController : Controller
{
    private readonly ExerciseRepository _exercises;
    public AdminExercisesController(ExerciseRepository exercises) => _exercises = exercises;

    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await _exercises.GetCategoriesAsync();
        return View("~/Views/Admin/Exercises/Index.cshtml", await _exercises.GetAllAsync(true));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _exercises.GetCategoriesAsync();
        return View("~/Views/Admin/Exercises/Edit.cshtml", new Exercise());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Exercise exercise)
    {
        await _exercises.CreateAsync(exercise);
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var exercise = await _exercises.GetByIdAsync(id);
        if (exercise == null) return NotFound();
        ViewBag.Categories = await _exercises.GetCategoriesAsync();
        return View("~/Views/Admin/Exercises/Edit.cshtml", exercise);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Exercise exercise)
    {
        await _exercises.UpdateAsync(exercise);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _exercises.DeleteAsync(id);
        return RedirectToAction("Index");
    }

    // AJAX: search exercises for workout editor
    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        var all = await _exercises.GetAllAsync();
        var matches = all
            .Where(e => e.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(15)
            .Select(e => new { e.Id, e.Name, e.DefaultReps, e.CategoryName })
            .ToList();
        return Json(matches);
    }
}

// ============================================================
// Mesocycles
// ============================================================
[Authorize]
public class AdminMesocyclesController : Controller
{
    private readonly MesocycleRepository _mesocycles;
    public AdminMesocyclesController(MesocycleRepository mesocycles) => _mesocycles = mesocycles;

    public async Task<IActionResult> Index()
        => View("~/Views/Admin/Mesocycles/Index.cshtml", await _mesocycles.GetAllAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new MesocycleBuilderViewModel
        {
            StartDate = GetNextMonday(),
            Templates = await _mesocycles.GetTemplatesAsync()
        };
        return View("~/Views/Admin/Mesocycles/Create.cshtml", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(MesocycleBuilderViewModel vm)
    {
        var endDate = vm.StartDate.AddDays(vm.Weeks * 7 - 3); // Fri of last week

        var mesocycle = new Mesocycle
        {
            Name = vm.Name,
            Description = vm.Description,
            StartDate = vm.StartDate,
            EndDate = endDate,
            IsCurrent = true
        };

        var id = await _mesocycles.CreateAsync(mesocycle);
        await _mesocycles.SetCurrentAsync(id);

        // Generate days from template if selected
        if (vm.TemplateId.HasValue)
        {
            await _mesocycles.GenerateFromTemplateAsync(id, vm.TemplateId.Value, vm.StartDate, vm.Weeks);
        }

        return RedirectToAction("Index", "AdminWorkouts", new { mesocycleId = id });
    }

    [HttpPost]
    public async Task<IActionResult> SetCurrent(int id)
    {
        await _mesocycles.SetCurrentAsync(id);
        return RedirectToAction("Index");
    }

    // ---- Templates ----

    [HttpGet]
    public async Task<IActionResult> Templates()
        => View("~/Views/Admin/Mesocycles/Templates.cshtml", await _mesocycles.GetTemplatesAsync());

    [HttpPost]
    public async Task<IActionResult> CreateTemplate(string name, string? description,
        int[] dayOfWeek, string[] dayType, string[] throwsFocus)
    {
        var templateId = await _mesocycles.CreateTemplateAsync(new MesocycleTemplate
        {
            Name = name,
            Description = description
        });

        for (int i = 0; i < dayOfWeek.Length; i++)
        {
            await _mesocycles.AddTemplateDayAsync(new TemplateDay
            {
                TemplateId = templateId,
                DayOfWeek = dayOfWeek[i],
                DayType = dayType[i],
                ThrowsFocus = throwsFocus[i]
            });
        }

        return RedirectToAction("Templates");
    }

    private static DateTime GetNextMonday()
    {
        var today = DateTime.Today;
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
    }
}

// ============================================================
// Athletes
// ============================================================
[Authorize]
public class AdminAthletesController : Controller
{
    private readonly AthleteRepository _athletes;
    public AdminAthletesController(AthleteRepository athletes) => _athletes = athletes;

    public async Task<IActionResult> Index()
        => View("~/Views/Admin/Athletes/Index.cshtml", await _athletes.GetAllAsync());

    [HttpGet]
    public IActionResult Create()
        => View("~/Views/Admin/Athletes/Edit.cshtml", new Athlete());

    [HttpPost]
    public async Task<IActionResult> Create(Athlete athlete, string[] eventNames, int[] primaryEvents)
    {
        var id = await _athletes.CreateAsync(athlete);
        await SaveEvents(id, eventNames, primaryEvents);
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var athlete = await _athletes.GetByIdAsync(id);
        if (athlete == null) return NotFound();
        return View("~/Views/Admin/Athletes/Edit.cshtml", athlete);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Athlete athlete, string[] eventNames, int[] primaryEvents)
    {
        await _athletes.UpdateAsync(athlete);
        await SaveEvents(athlete.Id, eventNames, primaryEvents);
        return RedirectToAction("Index");
    }

    private async Task SaveEvents(int athleteId, string[] eventNames, int[] primaryEvents)
    {
        var events = eventNames
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select((name, idx) => new AthleteEvent
            {
                EventName = name.Trim(),
                IsPrimary = primaryEvents.Contains(idx)
            })
            .ToList();

        await _athletes.SetEventsAsync(athleteId, events);
    }
}

// ============================================================
// Meets
// ============================================================
[Authorize]
public class AdminMeetsController : Controller
{
    private readonly MeetRepository _meets;
    public AdminMeetsController(MeetRepository meets) => _meets = meets;

    public async Task<IActionResult> Index()
        => View("~/Views/Admin/Meets/Index.cshtml", await _meets.GetAllAsync());

    [HttpGet]
    public IActionResult Create()
        => View("~/Views/Admin/Meets/Edit.cshtml", new Meet { Date = DateTime.Today });

    [HttpPost]
    public async Task<IActionResult> Create(Meet meet)
    {
        await _meets.CreateAsync(meet);
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var meet = await _meets.GetByIdAsync(id);
        if (meet == null) return NotFound();
        return View("~/Views/Admin/Meets/Edit.cshtml", meet);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Meet meet)
    {
        await _meets.UpdateAsync(meet);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _meets.DeleteAsync(id);
        return RedirectToAction("Index");
    }
}
