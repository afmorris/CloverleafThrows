using Microsoft.AspNetCore.Mvc;
using CloverleafThrows.Data;
using CloverleafThrows.Models;

namespace CloverleafThrows.Web.Controllers;

// ============================================================
// Exercises
// ============================================================
public class AdminExercisesController(IExerciseRepository exercises) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await exercises.GetCategoriesAsync();
        return View("~/Views/Admin/Exercises/Index.cshtml", await exercises.GetAllAsync(true));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await exercises.GetCategoriesAsync();
        return View("~/Views/Admin/Exercises/Edit.cshtml", new Exercise());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Exercise exercise)
    {
        await exercises.CreateAsync(exercise);
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var exercise = await exercises.GetByIdAsync(id);
        if (exercise == null) return NotFound();
        ViewBag.Categories = await exercises.GetCategoriesAsync();
        return View("~/Views/Admin/Exercises/Edit.cshtml", exercise);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Exercise exercise)
    {
        await exercises.UpdateAsync(exercise);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await exercises.DeleteAsync(id);
        return RedirectToAction("Index");
    }

    // AJAX: search exercises for workout editor
    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        var all = await exercises.GetAllAsync();
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
public class AdminMesocyclesController(IMesocycleRepository mesocycles) : Controller
{
    public async Task<IActionResult> Index()
        => View("~/Views/Admin/Mesocycles/Index.cshtml", await mesocycles.GetAllAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new MesocycleBuilderViewModel
        {
            StartDate = GetNextMonday(),
            Templates = await mesocycles.GetTemplatesAsync()
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

        var id = await mesocycles.CreateAsync(mesocycle);
        await mesocycles.SetCurrentAsync(id);

        if (vm.TemplateId.HasValue)
            await mesocycles.GenerateFromTemplateAsync(id, vm.TemplateId.Value, vm.StartDate, vm.Weeks);

        return RedirectToAction("Index", "AdminWorkouts", new { mesocycleId = id });
    }

    [HttpPost]
    public async Task<IActionResult> SetCurrent(int id)
    {
        await mesocycles.SetCurrentAsync(id);
        return RedirectToAction("Index");
    }

    // ---- Templates ----

    [HttpGet]
    public async Task<IActionResult> Templates()
        => View("~/Views/Admin/Mesocycles/Templates.cshtml", await mesocycles.GetTemplatesAsync());

    [HttpPost]
    public async Task<IActionResult> CreateTemplate(string name, string? description,
        int[] dayOfWeek, string[] dayType, string[] throwsFocus)
    {
        var templateId = await mesocycles.CreateTemplateAsync(new MesocycleTemplate
        {
            Name = name,
            Description = description
        });

        for (int i = 0; i < dayOfWeek.Length; i++)
        {
            await mesocycles.AddTemplateDayAsync(new TemplateDay
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
public class AdminAthletesController(IAthleteRepository athletes) : Controller
{
    public async Task<IActionResult> Index()
        => View("~/Views/Admin/Athletes/Index.cshtml", await athletes.GetAllAsync());

    [HttpGet]
    public IActionResult Create()
        => View("~/Views/Admin/Athletes/Edit.cshtml", new Athlete());

    [HttpPost]
    public async Task<IActionResult> Create(Athlete athlete, string[] eventNames, int[] primaryEvents)
    {
        var id = await athletes.CreateAsync(athlete);
        await SaveEvents(id, eventNames, primaryEvents);
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var athlete = await athletes.GetByIdAsync(id);
        if (athlete == null) return NotFound();
        return View("~/Views/Admin/Athletes/Edit.cshtml", athlete);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Athlete athlete, string[] eventNames, int[] primaryEvents)
    {
        await athletes.UpdateAsync(athlete);
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

        await athletes.SetEventsAsync(athleteId, events);
    }
}

// ============================================================
// Meets
// ============================================================
public class AdminMeetsController(IMeetRepository meets) : Controller
{
    public async Task<IActionResult> Index()
        => View("~/Views/Admin/Meets/Index.cshtml", await meets.GetAllAsync());

    [HttpGet]
    public IActionResult Create()
        => View("~/Views/Admin/Meets/Edit.cshtml", new Meet { Date = DateTime.Today });

    [HttpPost]
    public async Task<IActionResult> Create(Meet meet)
    {
        await meets.CreateAsync(meet);
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var meet = await meets.GetByIdAsync(id);
        if (meet == null) return NotFound();
        return View("~/Views/Admin/Meets/Edit.cshtml", meet);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Meet meet)
    {
        await meets.UpdateAsync(meet);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await meets.DeleteAsync(id);
        return RedirectToAction("Index");
    }
}
