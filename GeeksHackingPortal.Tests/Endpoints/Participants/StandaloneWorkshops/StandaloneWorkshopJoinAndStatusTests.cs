using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Helpers;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Participants.StandaloneWorkshops;

public class StandaloneWorkshopJoinAndStatusTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass Client { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass AnonymousClient { get; init; }

    [Test]
    public async Task JoinStandaloneWorkshop_WithValidWorkshop_ReturnsOk()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);

        // Act
        var response = await Client.HttpClient.PostAsync(
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
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        await Client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        // Act
        var response = await Client.HttpClient.PostAsync(
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
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);

        // Act
        var response = await Client.HttpClient.PostAsJsonAsync(
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
            Client.HttpClient,
            isPublished: false
        );

        // Act
        var response = await Client.HttpClient.PostAsync(
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
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        await Client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        // Act
        var response = await Client.HttpClient.GetAsync(
            $"/participants/standalone-workshops/{workshop.Id}/status"
        );
        var status = await response.Content.ReadFromJsonAsync<StandaloneWorkshopStatusResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(status).IsNotNull();
        await Assert.That(status!.IsRegistered).IsTrue();
        await Assert.That(status.RegistrationId).IsNotNull();
        await Assert.That(status.RegisteredAt).IsNotNull();
        await Assert.That(status.WithdrawnAt).IsNull();
    }

    [Test]
    public async Task WithdrawStandaloneWorkshop_AfterJoin_ReturnsWithdrawnStatus()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        await Client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        // Act
        var withdrawResponse = await Client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/withdraw",
            null
        );
        var statusResponse = await Client.HttpClient.GetAsync(
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
    public async Task RegistrationSubmissions_ForStandaloneWorkshop_RoundTripAndLockAfterCompletion()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        var suffix = Guid.NewGuid().ToString()[..8];

        var createQuestionResponse = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/registration/questions",
            new
            {
                QuestionText = "What should we prepare for you?",
                QuestionKey = $"standalone_submission_{suffix}",
                Type = "Text",
                DisplayOrder = 0,
                IsRequired = true,
                Category = "Preferences",
            }
        );
        var question =
            await createQuestionResponse.Content.ReadFromJsonAsync<RegistrationQuestionResponse>();

        var joinResponse = await Client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        // Act
        var submitResponse = await Client.HttpClient.PostAsJsonAsync(
            $"/participants/standalone-workshops/{workshop.Id}/registration/submissions",
            new
            {
                Submissions = new[]
                {
                    new
                    {
                        QuestionId = question!.Id,
                        Value = "Vegetarian lunch",
                    },
                },
            }
        );
        var submitResult =
            await submitResponse.Content.ReadFromJsonAsync<RegistrationSubmissionResponse>();

        var submissionsResponse = await Client.HttpClient.GetAsync(
            $"/participants/standalone-workshops/{workshop.Id}/registration/submissions"
        );
        var submissions =
            await submissionsResponse.Content.ReadFromJsonAsync<RegistrationSubmissionsListResponse>();

        var questionsResponse = await Client.HttpClient.GetAsync(
            $"/participants/standalone-workshops/{workshop.Id}/registration/questions"
        );
        var questions =
            await questionsResponse.Content.ReadFromJsonAsync<ParticipantRegistrationQuestionsResponse>();

        var secondSubmitResponse = await Client.HttpClient.PostAsJsonAsync(
            $"/participants/standalone-workshops/{workshop.Id}/registration/submissions",
            new
            {
                Submissions = new[]
                {
                    new
                    {
                        QuestionId = question.Id,
                        Value = "Updated answer",
                    },
                },
            }
        );

        // Assert
        await Assert.That(createQuestionResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(joinResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(submitResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(submitResult!.SubmissionsCount).IsEqualTo(1);
        await Assert.That(submitResult.Message).IsEqualTo("All submissions saved successfully.");
        await Assert.That(submissionsResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(submissions!.AnsweredQuestions).IsEqualTo(1);
        await Assert.That(submissions.RequiredQuestionsRemaining).IsEqualTo(0);
        await Assert.That(submissions.Submissions.Single().Value).IsEqualTo("Vegetarian lunch");
        await Assert.That(questionsResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(questions!.Categories.Single().Name).IsEqualTo("Preferences");
        await Assert.That(questions.Categories.Single().Questions.Single().CurrentSubmission!.Value)
            .IsEqualTo("Vegetarian lunch");
        await Assert.That(secondSubmitResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task RegistrationQuestions_ForStandaloneWorkshop_WithoutJoining_ReturnsBadRequest()
    {
        // Arrange
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);

        // Act
        var response = await Client.HttpClient.GetAsync(
            $"/participants/standalone-workshops/{workshop.Id}/registration/questions"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task JoinStandaloneWorkshop_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await AnonymousClient.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{Guid.NewGuid()}/join",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private sealed class RegistrationQuestionResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class RegistrationSubmissionResponse
    {
        public int SubmissionsCount { get; set; }
        public string Message { get; set; } = "";
    }

    private sealed class RegistrationSubmissionsListResponse
    {
        public List<RegistrationSubmissionItem> Submissions { get; set; } = [];
        public int AnsweredQuestions { get; set; }
        public int RequiredQuestionsRemaining { get; set; }
    }

    private sealed class RegistrationSubmissionItem
    {
        public string Value { get; set; } = "";
    }

    private sealed class ParticipantRegistrationQuestionsResponse
    {
        public List<RegistrationQuestionCategory> Categories { get; set; } = [];
    }

    private sealed class RegistrationQuestionCategory
    {
        public string Name { get; set; } = "";
        public List<ParticipantRegistrationQuestion> Questions { get; set; } = [];
    }

    private sealed class ParticipantRegistrationQuestion
    {
        public CurrentRegistrationSubmission? CurrentSubmission { get; set; }
    }

    private sealed class CurrentRegistrationSubmission
    {
        public string Value { get; set; } = "";
    }
}
