using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Helpers;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.StandaloneWorkshops;

public class StandaloneWorkshopManagementTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass Client { get; init; }

    [Test]
    public async Task RegistrationQuestionCrud_ForStandaloneWorkshop_RoundTrips()
    {
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        var suffix = Guid.NewGuid().ToString()[..8];

        var createResponse = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/registration/questions",
            new
            {
                QuestionText = "What should we know?",
                QuestionKey = $"standalone_question_{suffix}",
                Type = "Text",
                DisplayOrder = 1,
                IsRequired = true,
            }
        );
        var created = await createResponse.Content.ReadFromJsonAsync<RegistrationQuestionResponse>();

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(created).IsNotNull();

        var updateResponse = await Client.HttpClient.PatchAsJsonAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/registration/questions/{created!.Id}",
            new { QuestionText = "Updated standalone question", IsRequired = false }
        );
        var updated = await updateResponse.Content.ReadFromJsonAsync<RegistrationQuestionResponse>();

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(updated!.QuestionText).IsEqualTo("Updated standalone question");
        await Assert.That(updated.IsRequired).IsFalse();

        var listResponse = await Client.HttpClient.GetAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/registration/questions"
        );
        var list = await listResponse.Content.ReadFromJsonAsync<RegistrationQuestionListResponse>();

        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(list!.Questions.Any(q => q.Id == created.Id)).IsTrue();

        var deleteResponse = await Client.HttpClient.DeleteAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/registration/questions/{created.Id}"
        );

        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task TimelineCrud_ForStandaloneWorkshop_RoundTrips()
    {
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        var start = DateTimeOffset.UtcNow.AddDays(2);

        var createResponse = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/timeline",
            new
            {
                Title = "Doors Open",
                Description = "Registration starts",
                StartTime = start,
                EndTime = start.AddHours(1),
            }
        );
        var created = await createResponse.Content.ReadFromJsonAsync<TimelineItemResponse>();

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(created).IsNotNull();

        var updateResponse = await Client.HttpClient.PatchAsJsonAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/timeline/{created!.Id}",
            new { Title = "Updated Doors Open" }
        );
        var updated = await updateResponse.Content.ReadFromJsonAsync<TimelineItemResponse>();

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(updated!.Title).IsEqualTo("Updated Doors Open");

        var listResponse = await Client.HttpClient.GetAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/timeline"
        );
        var list = await listResponse.Content.ReadFromJsonAsync<TimelineListResponse>();

        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(list!.TimelineItems.Any(i => i.Id == created.Id)).IsTrue();

        var deleteResponse = await Client.HttpClient.DeleteAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/timeline/{created.Id}"
        );

        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Participants_ForStandaloneWorkshop_ReturnsJoinedRegistration()
    {
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        var joinResponse = await Client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );

        await Assert.That(joinResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var listResponse = await Client.HttpClient.GetAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/participants"
        );
        var list = await listResponse.Content.ReadFromJsonAsync<StandaloneParticipantListResponse>();

        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(list!.RegisteredCount).IsEqualTo(1);
        await Assert.That(list.Participants).Count().IsEqualTo(1);

        var participant = list.Participants.Single();
        var getResponse = await Client.HttpClient.GetAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/participants/{participant.UserId}"
        );
        var detail = await getResponse.Content.ReadFromJsonAsync<StandaloneParticipantResponse>();

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(detail!.RegistrationId).IsEqualTo(participant.RegistrationId);

        var withdrawResponse = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/participants/{participant.UserId}/withdraw",
            new { }
        );

        await Assert.That(withdrawResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Organizers_ForStandaloneWorkshop_ListsCreator()
    {
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);

        var response = await Client.HttpClient.GetAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/organizers"
        );
        var result = await response.Content.ReadFromJsonAsync<StandaloneOrganizerListResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result!.Organizers).Count().IsGreaterThanOrEqualTo(1);
    }

    private sealed class RegistrationQuestionListResponse
    {
        public List<RegistrationQuestionResponse> Questions { get; set; } = [];
    }

    private sealed class RegistrationQuestionResponse
    {
        public Guid Id { get; set; }
        public string QuestionText { get; set; } = "";
        public bool IsRequired { get; set; }
    }

    private sealed class TimelineListResponse
    {
        public List<TimelineItemResponse> TimelineItems { get; set; } = [];
    }

    private sealed class TimelineItemResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
    }

    private sealed class StandaloneParticipantListResponse
    {
        public int RegisteredCount { get; set; }
        public List<StandaloneParticipantResponse> Participants { get; set; } = [];
    }

    private sealed class StandaloneParticipantResponse
    {
        public Guid RegistrationId { get; set; }
        public Guid UserId { get; set; }
    }

    private sealed class StandaloneOrganizerListResponse
    {
        public List<StandaloneOrganizerResponse> Organizers { get; set; } = [];
    }

    private sealed class StandaloneOrganizerResponse
    {
        public Guid UserId { get; set; }
    }
}
