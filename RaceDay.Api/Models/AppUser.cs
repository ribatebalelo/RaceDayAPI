using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class AppUser
{
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = Roles.Participant;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Event> OrganisedEvents { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}
