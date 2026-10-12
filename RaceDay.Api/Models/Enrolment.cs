using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    /// <summary>Pending, Confirmed or Cancelled.</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Confirmed";

    [MaxLength(10)]
    public string? BibNumber { get; set; }

    public AppUser Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public RaceResult? Result { get; set; }
}
