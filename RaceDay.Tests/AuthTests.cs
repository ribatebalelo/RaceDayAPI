using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class AuthTests(RaceDayFactory factory) : IClassFixture<RaceDayFactory>
{
    private static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

    [Fact]
    public async Task Register_Participant_ReturnsCreated_AndDoesNotExposePassword()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Sarah van Wyk", email = NewEmail(), password = RaceDayFactory.Password, role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Participant", body);
    }

    [Fact]
    public async Task Register_Organiser_WithValidCode_ReturnsCreated()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Thabo Mokoena", email = NewEmail(), password = RaceDayFactory.Password,
            role = "Organiser", organiserCode = RaceDayFactory.OrganiserCode
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithoutCode_ReturnsForbidden()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Sneaky", email = NewEmail(), password = RaceDayFactory.Password, role = "Organiser"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        var payload = new { fullName = "Dup", email, password = RaceDayFactory.Password, role = "Participant" };

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", payload)).StatusCode);
    }

    [Fact]
    public async Task Register_InvalidInput_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Bad", email = "not-an-email", password = "short", role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk_AndStartsSession()
    {
        var email = NewEmail();
        var client = await Api.LoginAs(factory, "Participant", email);

        // The session cookie set by login lets us reach a protected endpoint.
        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var json = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "User", email, password = RaceDayFactory.Password, role = "Participant" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = NewEmail(), password = RaceDayFactory.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutSession_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var client = await Api.LoginAs(factory, "Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ChangesName()
    {
        var client = await Api.LoginAs(factory, "Participant");
        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "New Name", email = NewEmail() });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New Name", json.GetProperty("fullName").GetString());
    }
}
