using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }
    public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    [Required, MaxLength(10)]
    public string EventType { get; set; } = "Run";

    /// <summary>Draft, Published, Cancelled or Completed.</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser Organiser { get; set; } = null!;
    public List<Category> Categories { get; set; } = new();
}
