using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The id of the logged-in user, read from the session.</summary>
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected bool IsOrganiser => User.IsInRole(Roles.Organiser);

    protected ObjectResult Forbidden(string detail) =>
        StatusCode(StatusCodes.Status403Forbidden,
            new ProblemDetails { Title = "Forbidden", Detail = detail, Status = StatusCodes.Status403Forbidden });

    protected ObjectResult ConflictProblem(string detail) =>
        Conflict(new ProblemDetails { Title = "Conflict", Detail = detail, Status = StatusCodes.Status409Conflict });
}
