using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>
/// Hosts the real API in memory, swapping SQL Server for an in-memory database
/// so tests are fast and need no external setup (works locally and in GitHub Actions).
/// </summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    public const string OrganiserCode = "TEST-CODE";
    public const string Password = "Passw0rd!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Auth:OrganiserInviteCode", OrganiserCode);

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public static class Api
{
    /// <summary>Registers a fresh user and logs in; the returned client carries the session cookie.</summary>
    public static async Task<HttpClient> LoginAs(RaceDayFactory factory, string role, string? email = null)
    {
        var client = factory.CreateClient();
        email ??= $"{Guid.NewGuid():N}@test.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password = RaceDayFactory.Password,
            role,
            organiserCode = role == "Organiser" ? RaceDayFactory.OrganiserCode : null
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = RaceDayFactory.Password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    public static object NewEvent(string status = "Published", string name = "Test Race") => new
    {
        eventName = name,
        description = "A test event",
        location = "Johannesburg",
        eventDate = "2027-03-14",
        distanceKm = 10,
        eventType = "Run",
        status
    };

    public static async Task<int> CreateEvent(HttpClient organiser, string status = "Published")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEvent(status));
        response.EnsureSuccessStatusCode();
        return await ReadId(response, "eventId");
    }

    public static async Task<int> CreateCategory(HttpClient organiser, int eventId, int maxParticipants = 50)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "10km",
            categoryType = "Distance",
            maxParticipants,
            price = 150
        });
        response.EnsureSuccessStatusCode();
        return await ReadId(response, "categoryId");
    }

    public static async Task<int> Enrol(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        response.EnsureSuccessStatusCode();
        return await ReadId(response, "enrolmentId");
    }

    public static async Task<int> ReadId(HttpResponseMessage response, string property)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(property).GetInt32();
    }
}
