using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

public class WorkshopsTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass client { get; init; }

    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Workshop Test {suffix}",
            Description = "A test hackathon for workshop tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"WST{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = true,
        };
    }

    [Test]
    public async Task CreateWorkshop_ShouldSucceed()
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        var workshopRequest = new
        {
            Title = "Test Workshop",
            Description = "A test workshop",
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            Location = "Room 101",
            MaxParticipants = 50,
            IsPublished = true,
        };

        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/workshops",
            workshopRequest
        );

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        var workshop = await response.Content.ReadFromJsonAsync<WorkshopResponse>();
        await Assert.That(workshop).IsNotNull();
        await Assert.That(workshop!.Title).IsEqualTo("Test Workshop");
    }

    [Test]
    public async Task ListWorkshops_ShouldReturnCreatedWorkshops()
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Create workshop
        var workshopRequest = new
        {
            Title = "List Test Workshop",
            Description = "A test workshop for list",
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            Location = "Room 101",
            MaxParticipants = 50,
            IsPublished = true,
        };

        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/workshops",
            workshopRequest
        );

        // List workshops
        var listResponse = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops"
        );

        await Assert.That(listResponse.IsSuccessStatusCode).IsTrue();
        var result = await listResponse.Content.ReadFromJsonAsync<WorkshopListResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Workshops.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task UpdateWorkshop_ShouldSucceed()
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Create workshop
        var workshopRequest = new
        {
            Title = "Original Title",
            Description = "Original description",
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            Location = "Room 101",
            MaxParticipants = 50,
            IsPublished = true,
        };

        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/workshops",
            workshopRequest
        );
        var workshop = await createResponse.Content.ReadFromJsonAsync<WorkshopResponse>();

        // Update workshop
        var updateRequest = new
        {
            Title = "Updated Title",
            Description = "Updated description",
            StartTime = DateTimeOffset.UtcNow.AddDays(2),
            EndTime = DateTimeOffset.UtcNow.AddDays(2).AddHours(2),
            Location = "Room 202",
            MaxParticipants = 100,
            IsPublished = false,
        };

        var updateResponse = await client.HttpClient.PutAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops/{workshop!.Id}",
            updateRequest
        );

        await Assert.That(updateResponse.IsSuccessStatusCode).IsTrue();
        var updated = await updateResponse.Content.ReadFromJsonAsync<WorkshopResponse>();
        await Assert.That(updated!.Title).IsEqualTo("Updated Title");
    }

    [Test]
    public async Task DeleteWorkshop_ShouldSucceed()
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Create workshop
        var workshopRequest = new
        {
            Title = "Delete Test Workshop",
            Description = "A workshop to be deleted",
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            Location = "Room 101",
            MaxParticipants = 50,
            IsPublished = true,
        };

        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/workshops",
            workshopRequest
        );
        var workshop = await createResponse.Content.ReadFromJsonAsync<WorkshopResponse>();

        // Delete workshop
        var deleteResponse = await client.HttpClient.DeleteAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops/{workshop!.Id}"
        );

        await Assert.That(deleteResponse.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task UpdateWorkshop_CacheInvalidation_ReturnsUpdatedData()
    {
        // Arrange - Create hackathon and workshop
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        var workshopRequest = new
        {
            Title = "Original Workshop",
            Description = "Original description",
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            Location = "Room 101",
            MaxParticipants = 50,
            IsPublished = true,
        };
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/workshops",
            workshopRequest
        );
        var workshop = await createResponse.Content.ReadFromJsonAsync<WorkshopResponse>();

        // Act 1 - List workshops to populate cache
        var response1 = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops"
        );
        var result1 = await response1.Content.ReadFromJsonAsync<WorkshopListResponse>();
        await Assert.That(result1).IsNotNull();
        var originalWorkshop = result1!.Workshops.FirstOrDefault(w => w.Id == workshop!.Id);
        await Assert.That(originalWorkshop).IsNotNull();
        await Assert.That(originalWorkshop!.Title).IsEqualTo("Original Workshop");

        // Act 2 - Update the workshop
        var updateRequest = new
        {
            Title = "Updated Workshop",
            Description = "Updated description",
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            Location = "Room 202",
            MaxParticipants = 100,
            IsPublished = true,
        };
        var updateResponse = await client.HttpClient.PutAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops/{workshop!.Id}",
            updateRequest
        );
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 3 - List workshops again to verify cache was invalidated
        var response2 = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops"
        );
        var result2 = await response2.Content.ReadFromJsonAsync<WorkshopListResponse>();

        // Assert - Should return updated data, not cached old data
        await Assert.That(result2).IsNotNull();
        var updatedWorkshop = result2!.Workshops.FirstOrDefault(w => w.Id == workshop.Id);
        await Assert.That(updatedWorkshop).IsNotNull();
        await Assert.That(updatedWorkshop!.Title).IsEqualTo("Updated Workshop");
        await Assert.That(updatedWorkshop.Location).IsEqualTo("Room 202");
        await Assert.That(updatedWorkshop.MaxParticipants).IsEqualTo(100);
    }
}

public class WorkshopResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Location { get; set; } = null!;
    public int MaxParticipants { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class WorkshopListResponse
{
    public List<WorkshopDto> Workshops { get; set; } = new();
}

public class WorkshopDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Location { get; set; } = null!;
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public bool IsPublished { get; set; }
}

