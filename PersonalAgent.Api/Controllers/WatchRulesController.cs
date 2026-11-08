using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAgent.Api.DTOs.WatchRules;
using PersonalAgent.Api.Services;

namespace PersonalAgent.Api.Controllers;

[ApiController]
[Route("api/watch-rules")]
[Authorize(AuthenticationSchemes = "Identity.Bearer")]
public class WatchRulesController : ControllerBase
{
    private readonly WatchRuleService _service;

    public WatchRulesController(WatchRuleService service)
    {
        _service = service;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Bruker-ID mangler.");

    [HttpGet]
    public async Task<ActionResult<List<WatchRuleDto>>> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int? interestId,
        [FromQuery] bool? enabled)
    {
        return Ok(await _service.GetAllAsync(
            CurrentUserId, interestId, enabled));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WatchRuleDto>> GetById(int id)
    {
        var rule = await _service.GetByIdAsync(id, CurrentUserId);

        if (rule is null)
        {
            return NotFound();
        }

        return Ok(rule);
    }

    [HttpPost]
    public async Task<ActionResult<WatchRuleDto>> Create(
        SaveWatchRuleDto dto)
    {
        try
        {
            var rule = await _service.CreateAsync(dto, CurrentUserId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = rule.Id },
                rule);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ugyldig overvåkingsregel.",
                detail: exception.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WatchRuleDto>> Update(
        int id, SaveWatchRuleDto dto)
    {
        try
        {
            var rule = await _service.UpdateAsync(id, dto, CurrentUserId);

            if (rule is null)
            {
                return NotFound();
            }

            return Ok(rule);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ugyldig overvåkingsregel.",
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