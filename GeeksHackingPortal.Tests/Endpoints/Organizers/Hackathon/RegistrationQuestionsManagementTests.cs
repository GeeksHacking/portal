using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

public class RegistrationQuestionsManagementTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass client { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass anonymousClient { get; init; }

    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Registration Questions Test Hackathon {suffix}",
            Description = "A test hackathon for registration questions tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"REGQ{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = true,
        };
    }

    private static async Task<Guid> CreateHackathonAsync(AuthenticatedHttpClientDataClass client)
    {
        var request = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var response = await client.HttpClient.PostAsJsonAsync("/organizers/hackathons", request);
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();
        return result!.Id;
    }

    [Test]
    public async Task ListRegistrationQuestions_WithValidHackathon_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/registration/questions"
        );
        var result =
            await response.Content.ReadFromJsonAsync<OrganizerRegistrationQuestionsResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Questions).IsNotNull();
    }

    [Test]
    public async Task CreateRegistrationQuestion_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);
        var suffix = Guid.NewGuid().ToString()[..8];
        var request = new
        {
            QuestionText = "What is your experience level?",
            QuestionKey = $"experience_level_{suffix}",
            Type = "Text",
            DisplayOrder = 1,
            IsRequired = true,
            HelpText = "Select your experience level",
            Category = "Background",
        };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/registration/questions",
            request
        );

        var result = await response.Content.ReadFromJsonAsync<CreateRegistrationQuestionResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.QuestionText).IsEqualTo(request.QuestionText);
        await Assert.That(result.QuestionKey).IsEqualTo(request.QuestionKey);
    }

    [Test]
    public async Task CreateRegistrationQuestion_WithDuplicateKey_ReturnsError()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);
        var questionKey = $"duplicate_key_{Guid.NewGuid().ToString()[..8]}";

        var request1 = new
        {
            QuestionText = "First question",
            QuestionKey = questionKey,
            Type = "Text",
            DisplayOrder = 1,
            IsRequired = false,
        };

        // Create first question
        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/registration/questions",
            request1
        );

        var request2 = new
        {
            QuestionText = "Second question with same key",
            QuestionKey = questionKey,
            Type = "Text",
            DisplayOrder = 2,
            IsRequired = false,
        };

        // Act - Try to create second question with same key
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/registration/questions",
            request2
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ListRegistrationQuestions_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/registration/questions"
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ListRegistrationQuestions_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await anonymousClient.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/registration/questions"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateRegistrationQuestion_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            QuestionText = "Unauthorized question",
            QuestionKey = "unauthorized_key",
            Type = "Text",
            DisplayOrder = 1,
            IsRequired = false,
        };

        // Act
        var response = await anonymousClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/registration/questions",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

