using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Helpers;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.StandaloneWorkshops;

public class StandaloneWorkshopCrudTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass Client { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass AnonymousClient { get; init; }

    [Test]
    public async Task CreateStandaloneWorkshop_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = TestDataHelper.CreateValidStandaloneWorkshopRequest(
            Guid.NewGuid().ToString()[..8]
        );

        // Act
        var response = await Client.HttpClient.PostAsJsonAsync(
            "/organizers/standalone-workshops",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<StandaloneWorkshopResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Title).IsEqualTo(request.Title);
        await Assert.That(result.Description).IsEqualTo(request.Description);
        await Assert.That(result.Location).IsEqualTo(request.Location);
        await Assert.That(result.ShortCode).IsEqualTo(request.ShortCode);
        await Assert.That(result.MaxParticipants).IsEqualTo(request.MaxParticipants);
    }

    [Test]
    public async Task CreateStandaloneWorkshop_WithEmailTemplates_RoundTrips()
    {
        // Arrange
        var request = TestDataHelper.CreateValidStandaloneWorkshopRequest(
            Guid.NewGuid().ToString()[..8]
        );
        request.EmailTemplates = new Dictionary<string, string>
        {
            ["registration-confirmed"] = "template-registration-confirmed",
        };

        // Act
        var response = await Client.HttpClient.PostAsJsonAsync(
            "/organizers/standalone-workshops",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<StandaloneWorkshopResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.EmailTemplates["registration-confirmed"])
            .IsEqualTo("template-registration-confirmed");
    }

    [Test]
    public async Task CreateStandaloneWorkshop_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = TestDataHelper.CreateValidStandaloneWorkshopRequest(
            Guid.NewGuid().ToString()[..8]
        );

        // Act
        var response = await AnonymousClient.HttpClient.PostAsJsonAsync(
            "/organizers/standalone-workshops",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateStandaloneWorkshop_CreatorIsActivityOrganizer()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);

        // Act
        var response = await Client.HttpClient.GetAsync(
            $"/participants/standalone-workshops/{workshop.Id}/status"
        );
        var status = await response.Content.ReadFromJsonAsync<StandaloneWorkshopStatusResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.IsOrganizer).IsTrue();
        await Assert.That(status.IsRegistered).IsFalse();
    }

    [Test]
    public async Task CreateStandaloneWorkshop_WithDuplicateShortCode_ReturnsBadRequest()
    {
        // Arrange
        var request = TestDataHelper.CreateValidStandaloneWorkshopRequest(
            Guid.NewGuid().ToString()[..8]
        );

        var firstResponse = await Client.HttpClient.PostAsJsonAsync(
            "/organizers/standalone-workshops",
            request
        );
        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var duplicateRequest = TestDataHelper.CreateValidStandaloneWorkshopRequest(
            Guid.NewGuid().ToString()[..8]
        );
        duplicateRequest.ShortCode = request.ShortCode;

        // Act
        var duplicateResponse = await Client.HttpClient.PostAsJsonAsync(
            "/organizers/standalone-workshops",
            duplicateRequest
        );

        // Assert
        await Assert.That(duplicateResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ListStandaloneWorkshops_ReturnsCreatedWorkshopWithCustomFields()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);

        // Act
        var response = await Client.HttpClient.GetAsync("/organizers/standalone-workshops");
        var result = await response.Content.ReadFromJsonAsync<StandaloneWorkshopListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        var listed = result!.StandaloneWorkshops.SingleOrDefault(w => w.Id == workshop.Id);
        await Assert.That(listed).IsNotNull();
        await Assert.That(listed!.Title).IsEqualTo(workshop.Title);
        await Assert.That(listed.ShortCode).IsEqualTo(workshop.ShortCode);
        await Assert.That(listed.MaxParticipants).IsEqualTo(workshop.MaxParticipants);
    }

    [Test]
    public async Task UpdateStandaloneWorkshop_WithCustomFields_ReturnsUpdatedWorkshop()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        var request = new
        {
            Title = "Updated Standalone Event",
            Description = "Updated description",
            Location = "Updated venue",
            HomepageUri = new Uri("https://example.com/updated-standalone-event"),
            ShortCode = $"UPD{Guid.NewGuid().ToString()[..8]}",
            StartTime = DateTimeOffset.UtcNow.AddDays(10),
            EndTime = DateTimeOffset.UtcNow.AddDays(10).AddHours(2),
            MaxParticipants = 42,
            IsPublished = false,
            EmailTemplates = new Dictionary<string, string>
            {
                ["registration-confirmed"] = "updated-template",
            },
        };

        // Act
        var response = await Client.HttpClient.PatchAsJsonAsync(
            $"/organizers/standalone-workshops/{workshop.Id}",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<StandaloneWorkshopResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Title).IsEqualTo(request.Title);
        await Assert.That(result.Description).IsEqualTo(request.Description);
        await Assert.That(result.Location).IsEqualTo(request.Location);
        await Assert.That(result.ShortCode).IsEqualTo(request.ShortCode);
        await Assert.That(result.MaxParticipants).IsEqualTo(request.MaxParticipants);
        await Assert.That(result.IsPublished).IsFalse();
        await Assert.That(result.EmailTemplates["registration-confirmed"]).IsEqualTo("updated-template");
    }

    [Test]
    public async Task GetStandaloneWorkshopAnalytics_AfterJoin_ReturnsSummaryCounts()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        await Client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        // Act
        var response = await Client.HttpClient.GetAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/analytics"
        );
        var result = await response.Content.ReadFromJsonAsync<StandaloneWorkshopAnalyticsResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RegisteredCount).IsEqualTo(1);
        await Assert.That(result.CapacityRemaining).IsEqualTo(workshop.MaxParticipants - 1);
        await Assert.That(result.CapacityUsedPercent).IsGreaterThan(0);
    }
}
