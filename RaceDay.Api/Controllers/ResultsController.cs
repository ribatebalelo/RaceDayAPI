using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class ResultsController(AppDbContext db) : ApiControllerBase
{
    private IQueryable<RaceResult> Detailed => db.Results
        .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
        .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);

    /// <summary>Records the finish time and position for an enrolment in an event the Organiser owns.</summary>
    [HttpPost("api/enrolments/{enrolmentId:int}/results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> Create(int enrolmentId, ResultRequest request)
    {
        var enrolment = await db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound();
        if (enrolment.Category.Event.OrganiserId != CurrentUserId)
            return Forbidden("You can only record results for your own events.");

        if (enrolment.Status == "Cancelled")
            return ConflictProblem("Cannot record a result for a cancelled enrolment.");
        if (await db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return ConflictProblem("A result has already been recorded for this enrolment.");

        if (!IsValid(request)) return ValidationProblem(ModelState);

        var result = new RaceResult { EnrolmentId = enrolmentId };
        Apply(result, request);
        db.Results.Add(result);
        await db.SaveChangesAsync();

        var created = await Detailed.AsNoTracking().FirstAsync(r => r.ResultId == result.ResultId);
        return Created($"/api/categories/{enrolment.CategoryId}/results", ResultResponse.From(created));
    }

    /// <summary>Corrects a previously recorded result.</summary>
    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> Update(int id, ResultRequest request)
    {
        var result = await Detailed.FirstOrDefaultAsync(r => r.ResultId == id);
        if (result is null) return NotFound();
        if (result.Enrolment.Category.Event.OrganiserId != CurrentUserId)
            return Forbidden("You can only modify results for your own events.");

        if (!IsValid(request)) return ValidationProblem(ModelState);

        Apply(result, request);
        await db.SaveChangesAsync();
        return Ok(ResultResponse.From(result));
    }

    /// <summary>Returns the results of a category ordered by finishing position.</summary>
    [HttpGet("api/categories/{categoryId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> GetForCategory(int categoryId)
    {
        if (!await db.Categories.AnyAsync(c => c.CategoryId == categoryId)) return NotFound();

        var list = await Detailed.AsNoTracking()
            .Where(r => r.Enrolment.CategoryId == categoryId)
            .OrderBy(r => r.Position == null).ThenBy(r => r.Position)
            .ToListAsync();
        return Ok(list.Select(ResultResponse.From));
    }

    /// <summary>Lists the logged-in Participant's own results.</summary>
    [HttpGet("api/users/me/results")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> GetMine()
    {
        var uid = CurrentUserId;
        var list = await Detailed.AsNoTracking()
            .Where(r => r.Enrolment.ParticipantId == uid).ToListAsync();
        return Ok(list.Select(ResultResponse.From));
    }

    private bool IsValid(ResultRequest request)
    {
        if (request.Status == "Finished" && request.FinishTime is null)
            ModelState.AddModelError(nameof(request.FinishTime), "FinishTime is required when Status is Finished.");
        return ModelState.IsValid;
    }

    private static void Apply(RaceResult r, ResultRequest req)
    {
        r.FinishTime = req.FinishTime;
        r.Position = req.Position;
        r.Status = req.Status;
    }
}
