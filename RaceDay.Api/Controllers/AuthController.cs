using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController(AppDbContext db, IConfiguration config) : ApiControllerBase
{
    /// <summary>Registers a new Organiser or Participant account.</summary>
    /// <remarks>Organisers must supply the organiser invite code so the privileged role cannot be self-assigned.</remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        if (request.Role == Roles.Organiser)
        {
            var expected = config["Auth:OrganiserInviteCode"];
            if (string.IsNullOrEmpty(expected) || request.OrganiserCode != expected)
                return Forbidden("A valid organiser invite code is required to register as an Organiser.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return ConflictProblem("An account with this email already exists.");

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.Role
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created("/api/users/me", UserResponse.From(user));
    }

    /// <summary>Logs in with email and password and starts a session (cookie).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Invalid email or password.", Status = 401 });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(UserResponse.From(user));
    }

    /// <summary>Ends the current session.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
