
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAgent.Core.Entities;
using PersonalAgent.Infrastructure.Services;
using PersonalAgent.Web.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PersonalAgent.Core.Enums;
using PersonalAgent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace PersonalAgent.Web.Controllers;

[Authorize]
public class IssuesController : Controller
{
    private readonly IIssueService _issues;
    private readonly IProjectService _projects;
    private readonly UserManager<MvcApplicationUser> _userManager;
    private readonly ICommentService _comments;

    public IssuesController(
        IIssueService issues,
        IProjectService projects,
        UserManager<MvcApplicationUser> userManager,
        ICommentService comments)
    {
        _issues = issues;
        _projects = projects;
        _userManager = userManager;
        _comments = comments;
    }

    private static IssueDetailsViewModel ToViewModel(Issue issue)
    {
        return new IssueDetailsViewModel
        {
            Id = issue.Id,
            ProjectId = issue.ProjectId,
            ProjectName = issue.Project?.Name ?? "",
            Title = issue.Title,
            Description = issue.Description,
            Status = issue.Status,
            AssignedUserId = issue.AssignedUserId,
            CreatedAt = issue.CreatedAt
        };
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? projectId)
    {
        var issues = projectId.HasValue
            ? await _issues.GetByProjectIdAsync(projectId.Value)
            : await _issues.GetAllAsync();

        var model = issues.Select(ToViewModel).ToList();

        ViewBag.ProjectId = projectId;

        return View(model);
    }

   
    
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var issue = await _issues.GetByIdAsync(id);

        if (issue is null)
            return NotFound();

        var model = ToViewModel(issue);

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var isAdmin = User.IsInRole("Admin");

        ViewBag.IsAdmin = isAdmin;

        ViewBag.CanChangeStatus =
            isAdmin ||
            (!string.IsNullOrWhiteSpace(userId) &&
             issue.AssignedUserId == userId &&
             (issue.Status == IssueStatus.Assigned ||
              issue.Status == IssueStatus.InProgress));

        if (isAdmin)
        {
            ViewBag.AssignableUsers = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();
        }

        if (!string.IsNullOrWhiteSpace(issue.AssignedUserId))
        {
            var assignedUser = await _userManager
                .FindByIdAsync(issue.AssignedUserId);

            model.AssignedUserName =
                assignedUser?.Email ??
                assignedUser?.UserName;
        }

        var comments = await _comments.GetByIssueIdAsync(id);

        var authorIds = comments
            .Select(c => c.AuthorId)
            .Distinct()
            .ToList();

        var authors = await _userManager.Users
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => u.Email ?? u.UserName ?? "Ukjent bruker");

        model.Comments = comments.Select(c =>
            new IssueCommentViewModel
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                AuthorName = authors.GetValueOrDefault(
                    c.AuthorId, "Ukjent bruker")
            }).ToList();

        return View(model);
    }


    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Create(int? projectId)
    {
        if (projectId.HasValue)
        {
            var project = await _projects.GetByIdAsync(projectId.Value);

            if (project is null)
                return NotFound();
        }

        ViewBag.Projects = await _projects.GetAllAsync();

        return View(new IssueFormViewModel
        {
            ProjectId = projectId ?? 0
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IssueFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Projects = await _projects.GetAllAsync();
            return View(model);
        }

        try
        {
            var issue = await _issues.CreateAsync(
                model.ProjectId,
                model.Title,
                model.Description);

            TempData["Success"] = "Saken ble opprettet.";

            return RedirectToAction(nameof(Details), new { id = issue.Id });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.Projects = await _projects.GetAllAsync();
            return View(model);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var issue = await _issues.GetByIdAsync(id);

        if (issue is null)
            return NotFound();

        return View(new IssueFormViewModel
        {
            Id = issue.Id,
            ProjectId = issue.ProjectId,
            Title = issue.Title,
            Description = issue.Description
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        IssueFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var existing = await _issues.GetByIdAsync(id);

        if (existing is null)
            return NotFound();

        // Prosjekttilhørighet skal ikke kunne endres
        // ved å manipulere POST-data.
        if (model.ProjectId != existing.ProjectId)
            return BadRequest();

        try
        {
            var updated = await _issues.UpdateAsync(
                id,
                model.Title,
                model.Description);

            if (!updated)
                return NotFound();

            TempData["Success"] = "Saken ble oppdatert.";

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
        var issue = await _issues.GetByIdAsync(id);

        if (issue is null)
            return NotFound();

        return View(ToViewModel(issue));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var issue = await _issues.GetByIdAsync(id);

        if (issue is null)
            return NotFound();

        var projectId = issue.ProjectId;

        var deleted = await _issues.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        TempData["Success"] = "Saken ble slettet.";

        return RedirectToAction(nameof(Index), new { projectId });
    }
    
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(
        int id,
        string? assignedUserId)
    {
        try
        {
            var updated = await _issues.AssignAsync(
                id, assignedUserId);

            if (!updated)
                return NotFound();

            TempData["Success"] = "Tildelingen ble oppdatert.";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        int id,
        IssueStatus newStatus)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var isAdmin = User.IsInRole("Admin");

        try
        {
            var updated = await _issues.ChangeStatusAsync(
                id,
                newStatus,
                userId,
                isAdmin);

            if (!updated)
                return NotFound();

            TempData["Success"] = "Status ble oppdatert.";
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (ArgumentOutOfRangeException)
        {
            return BadRequest();
        }

        return RedirectToAction(nameof(Details), new { id });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(
        CreateIssueCommentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Kommentaren må inneholde mellom 1 og 2000 tegn.";

            return RedirectToAction(
                nameof(Details),
                new { id = model.IssueId });
        }

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            await _comments.CreateAsync(
                model.IssueId,
                userId,
                model.Content);

            TempData["Success"] = "Kommentaren ble lagt til.";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(
            nameof(Details),
            new { id = model.IssueId });
    }
}
