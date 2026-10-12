using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class EnrolmentsController(AppDbContext db) : ApiControllerBase
{
    private IQueryable<Enrolment> Detailed => db.Enrolments
        .Include(e => e.Participant)
        .Include(e => e.Category).ThenInclude(c => c.Event);

    /// <summary>Enrols the logged-in Participant in a category, linking participant, event and category.</summary>
    /// <remarks>Fails with 409 if already enrolled, the category is full, or the event is not published.</remarks>
    [HttpPost("api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentResponse>> Enrol(int categoryId)
    {
        var uid = CurrentUserId;
        var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return NotFound();

        if (category.Event.Status != "Published")
            return ConflictProblem("This event is not open for enrolment.");

        var existing = await db.Enrolments.FirstOrDefaultAsync(e => e.ParticipantId == uid && e.CategoryId == categoryId);
        if (existing is not null && existing.Status != "Cancelled")
            return ConflictProblem("You are already enrolled in this category.");

        var active = await db.Enrolments.CountAsync(e => e.CategoryId == categoryId && e.Status != "Cancelled");
        if (active >= category.MaxParticipants)
            return ConflictProblem("This category is full.");

        Enrolment enrolment;
        if (existing is not null)
        {
            // Re-enrolling after a cancellation re-uses the original row (participant + category is unique).
            existing.Status = "Confirmed";
            existing.EnrolmentDate = DateTime.UtcNow;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment { ParticipantId = uid, CategoryId = categoryId, Status = "Confirmed" };
            db.Enrolments.Add(enrolment);
        }
        await db.SaveChangesAsync();

        enrolment.BibNumber ??= $"E{category.EventId}-{enrolment.EnrolmentId:D4}";
        await db.SaveChangesAsync();

        var response = await Detailed.AsNoTracking().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return Created("/api/users/me/enrolments", EnrolmentResponse.From(response));
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    [HttpGet("api/users/me/enrolments")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> GetMine()
    {
        var uid = CurrentUserId;
        var list = await Detailed.AsNoTracking()
            .Where(e => e.ParticipantId == uid).OrderByDescending(e => e.EnrolmentDate).ToListAsync();
        return Ok(list.Select(EnrolmentResponse.From));
    }

    /// <summary>Lists all enrolments across the categories of an event the Organiser owns.</summary>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> GetForEvent(int eventId)
    {
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only view enrolments for your own events.");

        var list = await Detailed.AsNoTracking()
            .Where(e => e.Category.EventId == eventId).OrderBy(e => e.EnrolmentDate).ToListAsync();
        return Ok(list.Select(EnrolmentResponse.From));
    }

    /// <summary>Lists the enrolments of one category of an event the Organiser owns.</summary>
    [HttpGet("api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> GetForCategory(int categoryId)
    {
        var category = await db.Categories.AsNoTracking().Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != CurrentUserId) return Forbidden("You can only view enrolments for your own events.");

        var list = await Detailed.AsNoTracking()
            .Where(e => e.CategoryId == categoryId).OrderBy(e => e.EnrolmentDate).ToListAsync();
        return Ok(list.Select(EnrolmentResponse.From));
    }

    /// <summary>Cancels an enrolment. Allowed for the enrolled Participant or the Organiser who owns the event.</summary>
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound();

        var uid = CurrentUserId;
        var isParticipantOwner = !IsOrganiser && enrolment.ParticipantId == uid;
        var isEventOrganiser = IsOrganiser && enrolment.Category.Event.OrganiserId == uid;
        if (!isParticipantOwner && !isEventOrganiser)
            return Forbidden("You cannot cancel this enrolment.");

        enrolment.Status = "Cancelled";
        await db.SaveChangesAsync();
        return NoContent();
    }
}
