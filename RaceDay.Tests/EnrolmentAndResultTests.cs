using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class EnrolmentAndResultTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId);

        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(categoryId, created.GetProperty("categoryId").GetInt32());
        Assert.Equal(eventId, created.GetProperty("eventId").GetInt32());
        Assert.False(string.IsNullOrEmpty(created.GetProperty("bibNumber").GetString()));

        // Recorded: visible to the participant...
        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/users/me/enrolments");
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(categoryId, mine[0].GetProperty("categoryId").GetInt32());

        // ...and to the organiser who owns the event.
        var roster = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/enrolments");
        Assert.Equal(1, roster.GetArrayLength());
    }

    [Fact]
    public async Task Organiser_CannotEnrol_ReturnsForbidden()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId);

        var response = await organiser.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotEnrol_ReturnsUnauthorized()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var eventId = await Api.CreateEvent(organiser);
        var categoryId = await Api.CreateCategory(organiser, eventId);

        var response = await factory.CreateClient().PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_SameCategoryTwice_ReturnsConflict()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));

        await Api.Enrol(participant, categoryId);
        var second = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_FullCategory_ReturnsConflict()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var first = await Api.LoginAs(factory, "Participant");
        var second = await Api.LoginAs(factory, "Participant");
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser), maxParticipants: 1);

        await Api.Enrol(first, categoryId);
        var response = await second.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_DraftEvent_ReturnsConflict()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser, "Draft"));

        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_NonExistentCategory_ReturnsNotFound()
    {
        var participant = await Api.LoginAs(factory, "Participant");
        var response = await participant.PostAsync("/api/categories/999999/enrolments", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewAnEventRoster_ReturnsForbidden()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var eventId = await Api.CreateEvent(organiser);

        var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanRecordResult_AndParticipantSeesOwnResult()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:52:14", position = 3, status = "Finished" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/users/me/results");
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(3, mine[0].GetProperty("position").GetInt32());
        Assert.Equal("00:52:14", mine[0].GetProperty("finishTime").GetString());
    }

    [Fact]
    public async Task Participant_CannotRecordResult_ReturnsForbidden()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:40:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotRecordResultForAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await Api.LoginAs(factory, "Organiser");
        var other = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var categoryId = await Api.CreateCategory(owner, await Api.CreateEvent(owner));
        var enrolmentId = await Api.Enrol(participant, categoryId);

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { finishTime = "00:40:00", position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_FinishedWithoutTime_ReturnsBadRequest()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            new { position = 1, status = "Finished" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_Twice_ReturnsConflict()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var participant = await Api.LoginAs(factory, "Participant");
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(participant, categoryId);
        var body = new { finishTime = "00:45:00", position = 1, status = "Finished" };

        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", body);
        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_ButNotSomeoneElses()
    {
        var organiser = await Api.LoginAs(factory, "Organiser");
        var owner = await Api.LoginAs(factory, "Participant");
        var stranger = await Api.LoginAs(factory, "Participant");
        var categoryId = await Api.CreateCategory(organiser, await Api.CreateEvent(organiser));
        var enrolmentId = await Api.Enrol(owner, categoryId);

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/enrolments/{enrolmentId}")).StatusCode);
    }
}
