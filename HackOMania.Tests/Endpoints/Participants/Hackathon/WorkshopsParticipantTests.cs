using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class WorkshopsParticipantTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Participant Workshop Test {suffix}",
            Description = "A test hackathon for participant workshop tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"PWS{suffix}",
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
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinWorkshop_ShouldSucceed(AuthenticatedHttpClientDataClass client)
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Join hackathon as participant
        var joinResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon!.Id}/join",
            new { }
        );

        // Create workshop
        var workshopRequest = new
        {
            Title = "Join Test Workshop",
            Description = "A test workshop for join",
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            Location = "Room 101",
            MaxParticipants = 50,
            IsPublished = true,
        };

        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops",
            workshopRequest
        );
        var workshop = await createResponse.Content.ReadFromJsonAsync<WorkshopResponse>();

        // Join workshop as participant
        var joinWorkshopResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/workshops/{workshop!.Id}/join",
            new { }
        );

        await Assert.That(joinWorkshopResponse.IsSuccessStatusCode).IsTrue();
        var result = await joinWorkshopResponse.Content.ReadFromJsonAsync<JoinWorkshopResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.WorkshopId).IsEqualTo(workshop.Id);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListWorkshops_AsParticipant_ShouldShowPublishedOnly(
        AuthenticatedHttpClientDataClass client
    )
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Join hackathon as participant
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);

        // Create published workshop
        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops",
            new
            {
                Title = "Published Workshop",
                Description = "Published",
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
                Location = "Room 101",
                MaxParticipants = 50,
                IsPublished = true,
            }
        );

        // Create unpublished workshop
        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops",
            new
            {
                Title = "Unpublished Workshop",
                Description = "Unpublished",
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
                Location = "Room 102",
                MaxParticipants = 50,
                IsPublished = false,
            }
        );

        // List workshops as participant
        var listResponse = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon.Id}/workshops"
        );

        await Assert.That(listResponse.IsSuccessStatusCode).IsTrue();
        var result =
            await listResponse.Content.ReadFromJsonAsync<ParticipantWorkshopListResponse>();
        await Assert.That(result).IsNotNull();

        // Should only see published workshop
        await Assert.That(result!.Workshops.All(w => w.Title != "Unpublished Workshop")).IsTrue();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task MarkAttendance_ShouldSucceed(AuthenticatedHttpClientDataClass client)
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Join hackathon
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);

        // Create workshop
        var workshopRequest = new
        {
            Title = "Attendance Workshop",
            Description = "Workshop for attendance",
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            Location = "Room 101",
            MaxParticipants = 50,
            IsPublished = true,
        };

        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops",
            workshopRequest
        );
        var workshop = await createResponse.Content.ReadFromJsonAsync<WorkshopResponse>();

        // Join workshop
        var r = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/workshops/{workshop!.Id}/join",
            new { }
        );

        await Assert.That(r.IsSuccessStatusCode).IsTrue();

        // Mark attendance
        var attendanceResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/workshops/{workshop.Id}/attendance",
            new { }
        );

        var e = await attendanceResponse.Content.ReadAsStringAsync();
        await Assert.That(attendanceResponse.IsSuccessStatusCode).IsTrue();
        var result = await attendanceResponse.Content.ReadFromJsonAsync<AttendanceResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.HasAttended).IsTrue();
    }
}

public class JoinWorkshopResponse
{
    public Guid Id { get; set; }
    public Guid WorkshopId { get; set; }
    public string WorkshopTitle { get; set; } = null!;
    public DateTimeOffset JoinedAt { get; set; }
}

public class ParticipantWorkshopListResponse
{
    public List<ParticipantWorkshopDto> Workshops { get; set; } = new();
}

public class ParticipantWorkshopDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Location { get; set; } = null!;
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public bool IsJoined { get; set; }
    public bool HasAttended { get; set; }
}

public class AttendanceResponse
{
    public bool HasAttended { get; set; }
    public DateTimeOffset AttendedAt { get; set; }
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
