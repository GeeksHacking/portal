using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Helpers;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Participants.StandaloneWorkshops;

public class StandaloneWorkshopJoinAndStatusTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass client { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass anonymousClient { get; init; }

    [Test]
    public async Task JoinStandaloneWorkshop_WithValidWorkshop_ReturnsOk()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(client.HttpClient);

        // Act
        var response = await client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );
        var result = await response.Content.ReadFromJsonAsync<StandaloneWorkshopJoinResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.StandaloneWorkshopId).IsEqualTo(workshop.Id);
    }

    [Test]
    public async Task JoinStandaloneWorkshop_AlreadyJoined_ReturnsOk()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(client.HttpClient);
        await client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        // Act
        var response = await client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task JoinStandaloneWorkshopByShortCode_WithValidShortCode_ReturnsOk()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(client.HttpClient);

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            "/participants/standalone-workshops/join",
            new { workshop.ShortCode }
        );
        var result = await response.Content.ReadFromJsonAsync<StandaloneWorkshopJoinResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.StandaloneWorkshopId).IsEqualTo(workshop.Id);
    }

    [Test]
    public async Task JoinStandaloneWorkshop_WithUnpublishedWorkshop_ReturnsNotFound()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(
            client.HttpClient,
            isPublished: false
        );

        // Act
        var response = await client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetStandaloneWorkshopStatus_AfterJoin_ReturnsRegistered()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(client.HttpClient);
        await client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/standalone-workshops/{workshop.Id}/status"
        );
        var status = await response.Content.ReadFromJsonAsync<StandaloneWorkshopStatusResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.IsRegistered).IsTrue();
        await Assert.That(status.RegisteredAt).IsNotNull();
        await Assert.That(status.WithdrawnAt).IsNull();
    }

    [Test]
    public async Task WithdrawStandaloneWorkshop_AfterJoin_ReturnsWithdrawnStatus()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(client.HttpClient);
        await client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        // Act
        var withdrawResponse = await client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/withdraw",
            null
        );
        var statusResponse = await client.HttpClient.GetAsync(
            $"/participants/standalone-workshops/{workshop.Id}/status"
        );
        var status = await statusResponse.Content.ReadFromJsonAsync<StandaloneWorkshopStatusResponse>();

        // Assert
        await Assert.That(withdrawResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(statusResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.IsRegistered).IsFalse();
        await Assert.That(status.WithdrawnAt).IsNotNull();
    }

    [Test]
    public async Task JoinStandaloneWorkshop_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await anonymousClient.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{Guid.NewGuid()}/join",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
