using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.API.Data;
using Xunit;

namespace RaceDay.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        var databaseName = $"RaceDayTests_{Guid.NewGuid():N}";
        _factory = factory.WithWebHostBuilder(builder => { builder.UseEnvironment("Testing"); builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RaceDayContext>>();
            services.AddDbContext<RaceDayContext>(options => options.UseInMemoryDatabase(databaseName));
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
            db.Database.EnsureCreated();
        }); });
    }

    private HttpClient Client() => _factory.CreateClient();

    private async Task<string> RegisterAndLogin(string role, string? email = null)
    {
        email ??= $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        var client = Client();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "RaceDayPass123!", firstName = "Test", lastName = role,
            phone = "0821234567", role
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "RaceDayPass123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(payload?.Token));
        return payload!.Token;
    }

    private static void Authenticate(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task Swagger_IsAvailable()
    {
        var response = await Client().GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Profile_RejectsUnauthenticatedRequest()
    {
        var response = await Client().GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Events_ArePubliclyReadable()
    {
        var response = await Client().GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationAndLogin_ReturnBearerToken()
    {
        var token = await RegisterAndLogin("Participant");
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Registration_RejectsDuplicateEmail()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        var client = Client();
        var body = new { email, password = "RaceDayPass123!", firstName = "Test", lastName = "Runner", phone = "0821234567", role = "Participant" };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", body)).StatusCode);
    }

    [Fact]
    public async Task Registration_RejectsWeakPassword()
    {
        var response = await Client().PostAsJsonAsync("/api/auth/register", new
        {
            email = $"weak-{Guid.NewGuid():N}@example.com", password = "password", firstName = "Test",
            lastName = "Runner", phone = "0821234567", role = "Participant"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_RejectsIncorrectPassword()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        var client = Client();
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = "RaceDayPass123!", firstName = "Test", lastName = "Runner", phone = "0821234567", role = "Participant" });
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPass123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        var token = await RegisterAndLogin("Participant");
        var client = Client(); Authenticate(client, token);
        var response = await client.PostAsJsonAsync("/api/events", new { title = "Test run", description = "Test", location = "Polokwane", eventDate = DateTime.UtcNow.AddDays(10), eventType = "Run", status = "Published" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var token = await RegisterAndLogin("Organiser");
        var client = Client(); Authenticate(client, token);
        var response = await client.PostAsJsonAsync("/api/events", new { title = "Test run", description = "Test event", location = "Polokwane", eventDate = DateTime.UtcNow.AddDays(10), eventType = "Run", status = "Published" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task OrganiserCannotManageAnotherOrganisersEvent()
    {
        var ownerToken = await RegisterAndLogin("Organiser");
        var owner = Client(); Authenticate(owner, ownerToken);
        var created = await owner.PostAsJsonAsync("/api/events", new { title = "Owner event", description = "Test", location = "Polokwane", eventDate = DateTime.UtcNow.AddDays(10), eventType = "Run", status = "Published" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var eventBody = await created.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(eventBody);

        var otherToken = await RegisterAndLogin("Organiser");
        var other = Client(); Authenticate(other, otherToken);
        var update = await other.PutAsJsonAsync($"/api/events/{eventBody!.EventId}", new { title = "Hijacked", description = "Test", location = "Polokwane", eventDate = DateTime.UtcNow.AddDays(10), eventType = "Run", status = "Published" });
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task ParticipantCanEnrolAndSeeRecordedEnrolment()
    {
        var organiser = Client(); Authenticate(organiser, await RegisterAndLogin("Organiser"));
        var eventResponse = await organiser.PostAsJsonAsync("/api/events", new { title = "Community 5K", description = "Test", location = "Polokwane", eventDate = DateTime.UtcNow.AddDays(10), eventType = "Run", status = "Published" });
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        var createdEvent = await eventResponse.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(createdEvent);
        var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{createdEvent!.EventId}/categories", new { name = "5 km", distanceKm = 5.0m, maxParticipants = 100, entryFee = 50.0m });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(category);

        var participant = Client(); Authenticate(participant, await RegisterAndLogin("Participant"));
        var enrolmentResponse = await participant.PostAsJsonAsync("/api/enrolments", new { eventId = createdEvent.EventId, categoryId = category!.CategoryId });
        Assert.Equal(HttpStatusCode.Created, enrolmentResponse.StatusCode);
        var mine = await participant.GetAsync("/api/enrolments/me");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var content = await mine.Content.ReadAsStringAsync();
        Assert.Contains("Community 5K", content);
        Assert.Contains("5 km", content);
    }

    [Fact]
    public async Task ParticipantCannotAccessOrganiserEnrolmentList()
    {
        var token = await RegisterAndLogin("Participant");
        var client = Client(); Authenticate(client, token);
        var response = await client.GetAsync("/api/events/1/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record LoginResponse(string Token);
    private sealed record EventResponse(int EventId);
    private sealed record CategoryResponse(int CategoryId);
}
