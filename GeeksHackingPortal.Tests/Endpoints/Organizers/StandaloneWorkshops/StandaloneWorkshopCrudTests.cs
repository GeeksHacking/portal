using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Helpers;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.StandaloneWorkshops;

public class StandaloneWorkshopCrudTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass client { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass anonymousClient { get; init; }

    [Test]
    public async Task CreateStandaloneWorkshop_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = TestDataHelper.CreateValidStandaloneWorkshopRequest(
            Guid.NewGuid().ToString()[..8]
        );

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
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
        var response = await client.HttpClient.PostAsJsonAsync(
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
        var response = await anonymousClient.HttpClient.PostAsJsonAsync(
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
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(client.HttpClient);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/standalone-workshops/{workshop.Id}/status"
        );
        var status = await response.Content.ReadFromJsonAsync<StandaloneWorkshopStatusResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.IsOrganizer).IsTrue();
        await Assert.That(status.IsRegistered).IsFalse();
    }
}
