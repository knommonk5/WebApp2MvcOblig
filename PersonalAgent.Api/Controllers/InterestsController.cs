using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAgent.Api.DTOs.Interests;
using PersonalAgent.Api.Services;

namespace PersonalAgent.Api.Controllers;

[ApiController]
[Route("api/interests")]
[Authorize(AuthenticationSchemes = "Identity.Bearer")]
public class InterestsController : ControllerBase
{
    private readonly InterestService _service;

    public InterestsController(InterestService service)
    {
        _service = service;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Bruker-ID mangler.");

    [HttpGet]
    public async Task<ActionResult<List<InterestDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync(CurrentUserId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InterestDto>> GetById(int id)
    {
        var interest = await _service.GetByIdAsync(id, CurrentUserId);

        if (interest is null)
        {
            return NotFound();
        }

        return Ok(interest);
    }

    [HttpPost]
    public async Task<ActionResult<InterestDto>> Create(
        CreateInterestDto dto)
    {
        ValidateText(dto.Name, dto.Category);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var interest = await _service.CreateAsync(dto, CurrentUserId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = interest.Id },
            interest);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InterestDto>> Update(
        int id,
        UpdateInterestDto dto)
    {
        ValidateText(dto.Name, dto.Category);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var interest = await _service.UpdateAsync(id, dto, CurrentUserId);

        if (interest is null)
        {
            return NotFound();
        }

        return Ok(interest);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id, CurrentUserId);

        return result switch
        {
            DeleteInterestResult.Deleted => NoContent(),
            DeleteInterestResult.NotFound => NotFound(),
            DeleteInterestResult.HasDependencies => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Interessen kan ikke slettes.",
                detail: "Flytt eller slett tilknyttede preferanser og regler først."),
            _ => throw new InvalidOperationException("Ukjent sletteresultat.")
        };
    }

    private void ValidateText(string name, string category)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
        {
            ModelState.AddModelError(
                nameof(CreateInterestDto.Name),
                "Navnet må inneholde minst to tegn utenom mellomrom.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            ModelState.AddModelError(
                nameof(CreateInterestDto.Category),
                "Kategori er påkrevd.");
        }
    }
}