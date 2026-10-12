using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public record UserResponse(int UserId, string FullName, string Email, string Role, DateTime CreatedAt)
{
    public static UserResponse From(AppUser u) => new(u.UserId, u.FullName, u.Email, u.Role, u.CreatedAt);
}

public record CategoryResponse(int CategoryId, int EventId, string CategoryName, string CategoryType, int MaxParticipants, decimal Price)
{
    public static CategoryResponse From(Category c) =>
        new(c.CategoryId, c.EventId, c.CategoryName, c.CategoryType, c.MaxParticipants, c.Price);
}

public record EventResponse(
    int EventId, int OrganiserId, string EventName, string? Description, string Location,
    DateOnly EventDate, decimal DistanceKm, string EventType, string Status,
    IReadOnlyList<CategoryResponse>? Categories = null)
{
    public static EventResponse From(Event e, bool includeCategories = false) =>
        new(e.EventId, e.OrganiserId, e.EventName, e.Description, e.Location, e.EventDate,
            e.DistanceKm, e.EventType, e.Status,
            includeCategories ? e.Categories.Select(CategoryResponse.From).ToList() : null);
}

public record EnrolmentResponse(
    int EnrolmentId, int ParticipantId, string ParticipantName,
    int CategoryId, string CategoryName, int EventId, string EventName,
    DateTime EnrolmentDate, string Status, string? BibNumber)
{
    /// <summary>Requires Participant and Category.Event to be loaded.</summary>
    public static EnrolmentResponse From(Enrolment e) =>
        new(e.EnrolmentId, e.ParticipantId, e.Participant.FullName,
            e.CategoryId, e.Category.CategoryName, e.Category.EventId, e.Category.Event.EventName,
            e.EnrolmentDate, e.Status, e.BibNumber);
}

public record ResultResponse(
    int ResultId, int EnrolmentId, string ParticipantName, string CategoryName, string EventName,
    TimeSpan? FinishTime, int? Position, string Status)
{
    /// <summary>Requires Enrolment.Participant and Enrolment.Category.Event to be loaded.</summary>
    public static ResultResponse From(RaceResult r) =>
        new(r.ResultId, r.EnrolmentId, r.Enrolment.Participant.FullName,
            r.Enrolment.Category.CategoryName, r.Enrolment.Category.Event.EventName,
            r.FinishTime, r.Position, r.Status);
}
