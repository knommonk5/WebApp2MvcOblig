using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAgent.Api.DTOs.Preferences;
using PersonalAgent.Api.Services;

namespace PersonalAgent.Api.Controllers;

[ApiController]
[Route("api/preferences")]
[Authorize(AuthenticationSchemes = "Identity.Bearer")]
public class PreferencesController : ControllerBase
{
    private readonly PreferenceService _service;

    public PreferencesController(PreferenceService service)
    {
        _service = service;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Bruker-ID mangler.");

    [HttpGet]
    public async Task<ActionResult<List<PreferenceDto>>> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int? interestId)
    {
        return Ok(await _service.GetAllAsync(CurrentUserId, interestId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PreferenceDto>> GetById(int id)
    {
        var preference = await _service.GetByIdAsync(id, CurrentUserId);

        if (preference is null)
        {
            return NotFound();
        }

        return Ok(preference);
    }

    [HttpPost]
    public async Task<ActionResult<PreferenceDto>> Create(
        SavePreferenceDto dto)
    {
        try
        {
            var preference = await _service.CreateAsync(dto, CurrentUserId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = preference.Id },
                preference);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ugyldig preferanse.",
                detail: exception.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PreferenceDto>> Update(
        int id, SavePreferenceDto dto)
    {
        try
        {
            var preference = await _service.UpdateAsync(
                id, dto, CurrentUserId);

            if (preference is null)
            {
                return NotFound();
            }

            return Ok(preference);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ugyldig preferanse.",
                detail: exception.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id, CurrentUserId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}