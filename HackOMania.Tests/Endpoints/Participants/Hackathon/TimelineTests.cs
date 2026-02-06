using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Helpers;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class TimelineTests
{
    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetTimeline_ForPublishedHackathon_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);

        // Create a timeline item
        var now = DateTimeOffset.UtcNow;
        var createRequest = new
        {
            Title = "Opening Ceremony",
            Description = "Welcome and introduction to the hackathon",
            StartTime = now.AddDays(7),
            EndTime = now.AddDays(7).AddHours(1),
        };

        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline",
            createRequest
        );
        createResponse.EnsureSuccessStatusCode();

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon.Id}/timeline"
        );
        var result = await response.Content.ReadFromJsonAsync<TimelineListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TimelineItems).IsNotNull();
        await Assert.That(result.TimelineItems).IsNotEmpty();
        await Assert.That(result.TimelineItems[0].Title).IsEqualTo("Opening Ceremony");
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetTimeline_ForHackathonByShortCode_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);

        // Create a timeline item
        var now = DateTimeOffset.UtcNow;
        var createRequest = new
        {
            Title = "Hacking Begins",
            Description = "Start building your projects",
            StartTime = now.AddDays(7).AddHours(2),
            EndTime = now.AddDays(8).AddHours(20),
        };

        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline",
            createRequest
        );

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon.ShortCode}/timeline"
        );
        var result = await response.Content.ReadFromJsonAsync<TimelineListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TimelineItems).IsNotNull();
        await Assert.That(result.TimelineItems).IsNotEmpty();
        await Assert.That(result.TimelineItems[0].Title).IsEqualTo("Hacking Begins");
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CreateTimelineItem_AsOrganizer_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);
        var now = DateTimeOffset.UtcNow;
        var request = new
        {
            Title = "Team Formation",
            Description = "Find your teammates",
            StartTime = now.AddDays(7),
            EndTime = now.AddDays(7).AddHours(1),
        };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<TimelineItemResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Title).IsEqualTo("Team Formation");
        await Assert.That(result.Description).IsEqualTo("Find your teammates");
        await Assert.That(result.HackathonId).IsEqualTo(hackathon.Id);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task UpdateTimelineItem_AsOrganizer_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);
        var now = DateTimeOffset.UtcNow;

        // Create a timeline item
        var createRequest = new
        {
            Title = "Presentations",
            Description = "Show your projects",
            StartTime = now.AddDays(9),
            EndTime = now.AddDays(9).AddHours(2),
        };

        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline",
            createRequest
        );
        var createdItem = await createResponse.Content.ReadFromJsonAsync<TimelineItemResponse>();

        // Act - Update the item
        var updateRequest = new
        {
            Title = "Final Presentations",
            Description = "Present your amazing projects to the judges",
        };

        var response = await client.HttpClient.PatchAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline/{createdItem!.Id}",
            updateRequest
        );
        var result = await response.Content.ReadFromJsonAsync<TimelineItemResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Title).IsEqualTo("Final Presentations");
        await Assert
            .That(result.Description)
            .IsEqualTo("Present your amazing projects to the judges");
        await Assert.That(result.Id).IsEqualTo(createdItem.Id);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task DeleteTimelineItem_AsOrganizer_ReturnsNoContent(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);
        var now = DateTimeOffset.UtcNow;

        // Create a timeline item
        var createRequest = new
        {
            Title = "Lunch Break",
            Description = "Grab some food",
            StartTime = now.AddDays(8),
            EndTime = now.AddDays(8).AddHours(1),
        };

        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline",
            createRequest
        );
        var createdItem = await createResponse.Content.ReadFromJsonAsync<TimelineItemResponse>();

        // Act - Delete the item
        var response = await client.HttpClient.DeleteAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline/{createdItem!.Id}"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Verify it's deleted by trying to fetch timeline
        var getResponse = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon.Id}/timeline"
        );
        var timeline = await getResponse.Content.ReadFromJsonAsync<TimelineListResponse>();
        await Assert.That(timeline!.TimelineItems).IsEmpty();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetTimeline_OrderedByStartTime_ReturnsCorrectOrder(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);
        var now = DateTimeOffset.UtcNow;

        // Create timeline items in non-chronological order
        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline",
            new
            {
                Title = "Third Event",
                StartTime = now.AddDays(9),
                EndTime = now.AddDays(9).AddHours(1),
            }
        );

        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline",
            new
            {
                Title = "First Event",
                StartTime = now.AddDays(7),
                EndTime = now.AddDays(7).AddHours(1),
            }
        );

        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline",
            new
            {
                Title = "Second Event",
                StartTime = now.AddDays(8),
                EndTime = now.AddDays(8).AddHours(1),
            }
        );

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon.Id}/timeline"
        );
        var result = await response.Content.ReadFromJsonAsync<TimelineListResponse>();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TimelineItems).Count().IsEqualTo(3);
        await Assert.That(result.TimelineItems[0].Title).IsEqualTo("First Event");
        await Assert.That(result.TimelineItems[1].Title).IsEqualTo("Second Event");
        await Assert.That(result.TimelineItems[2].Title).IsEqualTo("Third Event");
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CreateTimelineItem_WithInvalidTimes_ReturnsBadRequest(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);
        var now = DateTimeOffset.UtcNow;
        var request = new
        {
            Title = "Invalid Event",
            Description = "This should fail",
            StartTime = now.AddDays(8),
            EndTime = now.AddDays(7), // EndTime before StartTime
        };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task UpdateTimelineItem_WithInvalidTimes_ReturnsBadRequest(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);
        var now = DateTimeOffset.UtcNow;

        // Create a valid timeline item
        var createRequest = new
        {
            Title = "Valid Event",
            Description = "Initially valid",
            StartTime = now.AddDays(7),
            EndTime = now.AddDays(8),
        };

        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline",
            createRequest
        );
        var createdItem = await createResponse.Content.ReadFromJsonAsync<TimelineItemResponse>();

        // Act - Update with invalid time
        var updateRequest = new
        {
            EndTime = now.AddDays(6), // EndTime before StartTime
        };

        var response = await client.HttpClient.PatchAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/timeline/{createdItem!.Id}",
            updateRequest
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }
}
