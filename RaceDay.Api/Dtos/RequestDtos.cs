using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Organiser or Participant.</summary>
    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be Organiser or Participant.")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Required only when registering as an Organiser.</summary>
    public string? OrganiserCode { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}

public class EventRequest
{
    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateOnly? EventDate { get; set; }

    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Draft, Published, Cancelled or Completed.</summary>
    [RegularExpression("^(Draft|Published|Cancelled|Completed)$", ErrorMessage = "Invalid status.")]
    public string Status { get; set; } = "Draft";
}

public class CategoryRequest
{
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Age or Distance.</summary>
    [Required, RegularExpression("^(Age|Distance)$", ErrorMessage = "CategoryType must be Age or Distance.")]
    public string CategoryType { get; set; } = string.Empty;

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 100000)]
    public decimal Price { get; set; }
}

public class ResultRequest
{
    /// <summary>Finish time as hh:mm:ss. Required when Status is Finished.</summary>
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 1000000)]
    public int? Position { get; set; }

    /// <summary>Finished, DNF or DSQ.</summary>
    [RegularExpression("^(Finished|DNF|DSQ)$", ErrorMessage = "Status must be Finished, DNF or DSQ.")]
    public string Status { get; set; } = "Finished";
}
