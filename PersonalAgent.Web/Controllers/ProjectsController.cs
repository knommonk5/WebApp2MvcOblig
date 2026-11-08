
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAgent.Infrastructure.Services;
using PersonalAgent.Web.ViewModels;

namespace PersonalAgent.Web.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly IProjectService _projects;

    public ProjectsController(IProjectService projects)
    {
        _projects = projects;
    }

    // Alle innloggede kan se prosjektoversikten.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var projects = await _projects.GetAllAsync();

        var model = projects.Select(p => new ProjectDetailsViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            CreatedAt = p.CreatedAt
        }).ToList();

        return View(model);
    }

    // Alle innloggede kan se prosjektdetaljer.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _projects.GetByIdAsync(id);

        if (project is null)
            return NotFound();

        return View(new ProjectDetailsViewModel
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProjectFormViewModel());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var project = await _projects.CreateAsync(
                model.Name,
                model.Description);

            TempData["Success"] = "Prosjektet ble opprettet.";

            return RedirectToAction(nameof(Details), new { id = project.Id });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projects.GetByIdAsync(id);

        if (project is null)
            return NotFound();

        return View(new ProjectFormViewModel
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        ProjectFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var updated = await _projects.UpdateAsync(
                id,
                model.Name,
                model.Description);

            if (!updated)
                return NotFound();

            TempData["Success"] = "Prosjektet ble oppdatert.";

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _projects.GetByIdAsync(id);

        if (project is null)
            return NotFound();

        return View(new ProjectDetailsViewModel
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var deleted = await _projects.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        TempData["Success"] = "Prosjektet ble slettet.";

        return RedirectToAction(nameof(Index));
    }
}
