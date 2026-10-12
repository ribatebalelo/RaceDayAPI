using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[Route("api/users")]
[Authorize]
public class UsersController(AppDbContext db) : ApiControllerBase
{
    /// <summary>Returns the logged-in user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        return user is null ? Unauthorized() : Ok(UserResponse.From(user));
    }

    /// <summary>Updates the logged-in user's name and email.</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> UpdateMe(UpdateProfileRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null) return Unauthorized();

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email && u.UserId != user.UserId))
            return ConflictProblem("That email is already in use.");

        user.FullName = request.FullName.Trim();
        user.Email = email;
        await db.SaveChangesAsync();
        return Ok(UserResponse.From(user));
    }

    /// <summary>Changes the logged-in user's password.</summary>
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null) return Unauthorized();

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(request.CurrentPassword), "Current password is incorrect.");
            return ValidationProblem(ModelState);
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
