using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class RaceResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    /// <summary>Finished, DNF or DSQ.</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Finished";

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Enrolment Enrolment { get; set; } = null!;
}
