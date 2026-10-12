using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Age or Distance.</summary>
    [Required, MaxLength(10)]
    public string CategoryType { get; set; } = "Distance";

    public int MaxParticipants { get; set; } = 100;
    public decimal Price { get; set; }

    public Event Event { get; set; } = null!;
    public List<Enrolment> Enrolments { get; set; } = new();
}
