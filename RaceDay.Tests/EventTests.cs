using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class EventTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", Api.NewEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Run", json.GetProperty("eventType").GetString());
        Assert.Equal(10, json.GetProperty("distanceKm").GetDecimal());
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_ReturnsForbidden()
    {
        var participant = await Api.LoginAs(factory, "Participant");
        var response = await participant.PostAsJsonAsync("/api/events", Api.NewEvent());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateEvent_ReturnsUnauthorized()
    {
        var anonymous = factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync("/api/events", Api.NewEvent());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_ReturnsBadRequest()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "Bad", location = "X", eventDate = "2027-01-01", distanceKm = 5, eventType = "Swim", status = "Draft"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anyone_CanViewPublishedEvents_ButNotDrafts()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var publishedId = await Api.CreateEvent(organiser, "Published");
        var draftId = await Api.CreateEvent(organiser, "Draft");

        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/events/{publishedId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/events/{draftId}")).StatusCode);

        // The owner can still see their own draft.
        Assert.Equal(HttpStatusCode.OK, (await organiser.GetAsync($"/api/events/{draftId}")).StatusCode);

        var list = await anonymous.GetFromJsonAsync<JsonElement>("/api/events");
        var ids = list.EnumerateArray().Select(e => e.GetProperty("eventId").GetInt32()).ToList();
        Assert.Contains(publishedId, ids);
        Assert.DoesNotContain(draftId, ids);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var id = await Api.CreateEvent(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{id}", Api.NewEvent(name: "Renamed Race"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Renamed Race", json.GetProperty("eventName").GetString());
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await Api.LoginAs(factory, "Organiser");
        var other = await Api.LoginAs(factory, "Organiser");
        var id = await Api.CreateEvent(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{id}", Api.NewEvent(name: "Hijacked"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentEvent_ReturnsNotFound()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var response = await organiser.PutAsJsonAsync("/api/events/999999", Api.NewEvent());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var id = await Api.CreateEvent(organiser);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteEvent_ReturnsForbidden()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var id = await Api.CreateEvent(organiser);

        var response = await participant.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_EventWithEnrolments_ReturnsConflict()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId);
        await Api.Enrol(participant, categoryId);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanAddCategory_AndAnyoneCanViewIt()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var eventId = await Api.CreateEvent(organiser);
        await Api.CreateCategory(organiser, eventId);

        var anonymous = factory.CreateClient();
        var categories = await anonymous.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/categories");
        Assert.Equal(1, categories.GetArrayLength());
        Assert.Equal("10km", categories[0].GetProperty("categoryName").GetString());
    }

    [Fact]
    public async Task Participant_CannotAddCategory_ReturnsForbidden()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var eventId = await Api.CreateEvent(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "Senior", categoryType = "Age", maxParticipants = 10, price = 50 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotAddCategoryToAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await Api.LoginAs(factory, "Organiser");
        var other = await Api.LoginAs(factory, "Organiser");
        var eventId = await Api.CreateEvent(owner);

        var response = await other.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "Under 20", categoryType = "Age", maxParticipants = 10, price = 50 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
