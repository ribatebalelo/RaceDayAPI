using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class CategoriesController(AppDbContext db) : ApiControllerBase
{
    /// <summary>Lists the age/distance categories of an event.</summary>
    [HttpGet("api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId).OrderBy(c => c.CategoryName).ToListAsync();
        return Ok(categories.Select(CategoryResponse.From));
    }

    /// <summary>Adds an age or distance category to an event the Organiser owns.</summary>
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> Create(int eventId, CategoryRequest request)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only add categories to your own events.");

        var category = new Category { EventId = eventId };
        Apply(category, request);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", CategoryResponse.From(category));
    }

    /// <summary>Updates a category on an event the Organiser owns.</summary>
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> Update(int id, CategoryRequest request)
    {
        var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != CurrentUserId) return Forbidden("You can only modify categories of your own events.");

        Apply(category, request);
        await db.SaveChangesAsync();
        return Ok(CategoryResponse.From(category));
    }

    /// <summary>Deletes a category, provided it has no enrolments.</summary>
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != CurrentUserId) return Forbidden("You can only delete categories of your own events.");

        if (await db.Enrolments.AnyAsync(en => en.CategoryId == id))
            return ConflictProblem("This category has enrolments and cannot be deleted.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(Category c, CategoryRequest r)
    {
        c.CategoryName = r.CategoryName.Trim();
        c.CategoryType = r.CategoryType;
        c.MaxParticipants = r.MaxParticipants;
        c.Price = r.Price;
    }
}
