using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController(AppDbContext db) : ApiControllerBase
{
    /// <summary>Lists published events. Optional filters: eventType, location, from (date).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetAll(
        [FromQuery] string? eventType, [FromQuery] string? location, [FromQuery(Name = "from")] DateOnly? fromDate)
    {
        var query = db.Events.AsNoTracking().Where(e => e.Status == "Published");
        if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(e => e.EventType == eventType);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(e => e.Location.Contains(location));
        if (fromDate.HasValue) query = query.Where(e => e.EventDate >= fromDate.Value);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => EventResponse.From(e)));
    }

    /// <summary>Lists every event (including drafts) owned by the logged-in Organiser.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetMine()
    {
        var uid = CurrentUserId;
        var events = await db.Events.AsNoTracking()
            .Where(e => e.OrganiserId == uid)
            .OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => EventResponse.From(e)));
    }

    /// <summary>Returns one event with its categories. Drafts are visible only to their owner.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(int id)
    {
        var ev = await db.Events.AsNoTracking().Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();

        var isOwner = User.Identity?.IsAuthenticated == true && IsOrganiser && ev.OrganiserId == CurrentUserId;
        if (ev.Status != "Published" && !isOwner) return NotFound();

        return Ok(EventResponse.From(ev, includeCategories: true));
    }

    /// <summary>Creates an event owned by the logged-in Organiser.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> Create(EventRequest request)
    {
        var ev = new Event { OrganiserId = CurrentUserId };
        Apply(ev, request);
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, EventResponse.From(ev));
    }

    /// <summary>Updates an event the logged-in Organiser owns.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> Update(int id, EventRequest request)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only modify your own events.");

        Apply(ev, request);
        await db.SaveChangesAsync();
        return Ok(EventResponse.From(ev));
    }

    /// <summary>Deletes an event the Organiser owns, provided none of its categories have enrolments.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only delete your own events.");

        if (await db.Enrolments.AnyAsync(en => en.Category.EventId == id))
            return ConflictProblem("This event has enrolments and cannot be deleted.");

        db.Events.Remove(ev);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(Event ev, EventRequest r)
    {
        ev.EventName = r.EventName.Trim();
        ev.Description = r.Description?.Trim();
        ev.Location = r.Location.Trim();
        ev.EventDate = r.EventDate!.Value;
        ev.DistanceKm = r.DistanceKm;
        ev.EventType = r.EventType;
        ev.Status = r.Status;
    }
}
