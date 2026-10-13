using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Helpers;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.ActivityFiles;

public class ActivityFilesTests
{
    // A valid 1x1 PNG.
    private static readonly byte[] PngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII="
    );

    // A JPEG only needs its start-of-image marker to be recognised.
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

    /// <summary>
    /// Downloads go straight to object storage through presigned URLs, not through the API.
    /// </summary>
    private static readonly HttpClient StorageClient = new();

    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass Client { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass AnonymousClient { get; init; }

    [Test]
    public async Task StandaloneWorkshopBanner_CanBeUploadedReplacedAndRemoved()
    {
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        var bannerRoute = $"/organizers/standalone-workshops/{workshop.Id}/banner";

        var uploadResponse = await Client.HttpClient.PutAsync(bannerRoute, FileContent(PngBytes, "banner.png", "image/png"));
        var banner = await uploadResponse.Content.ReadFromJsonAsync<ActivityFileResponse>();

        await Assert.That(uploadResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(banner!.Kind).IsEqualTo("Banner");
        await Assert.That(banner.FileName).IsEqualTo("banner.png");
        await Assert.That(banner.ContentType).IsEqualTo("image/png");
        await Assert.That(banner.SizeBytes).IsEqualTo(PngBytes.Length);

        var publicWorkshop = await AnonymousClient.HttpClient.GetFromJsonAsync<PublicStandaloneWorkshopResponse>(
            $"/participants/standalone-workshops/{workshop.Id}"
        );
        await Assert.That(publicWorkshop!.BannerUrl).IsNotNull();

        var download = await StorageClient.GetAsync(publicWorkshop.BannerUrl);
        await Assert.That(download.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(download.Content.Headers.ContentType?.MediaType).IsEqualTo("image/png");
        await Assert.That(await download.Content.ReadAsByteArrayAsync()).IsEquivalentTo(PngBytes);

        // Replacing the banner keeps a single banner and removes the old image from storage.
        var replaceResponse = await Client.HttpClient.PutAsync(bannerRoute, FileContent(JpegBytes, "banner.jpg", "image/jpeg"));
        var replacement = await replaceResponse.Content.ReadFromJsonAsync<ActivityFileResponse>();

        await Assert.That(replaceResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(replacement!.ContentType).IsEqualTo("image/jpeg");

        var files = await Client.HttpClient.GetFromJsonAsync<OrganizerActivityFilesResponse>(
            $"/organizers/standalone-workshops/{workshop.Id}/files"
        );
        await Assert.That(files!.Banner?.Id).IsEqualTo(replacement.Id);
        await Assert.That(files.Files).IsEmpty();
        await Assert.That((await StorageClient.GetAsync(banner.Url)).StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        var deleteResponse = await Client.HttpClient.DeleteAsync(bannerRoute);
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        publicWorkshop = await AnonymousClient.HttpClient.GetFromJsonAsync<PublicStandaloneWorkshopResponse>(
            $"/participants/standalone-workshops/{workshop.Id}"
        );
        await Assert.That(publicWorkshop!.BannerUrl).IsNull();
        await Assert.That((await StorageClient.GetAsync(replacement.Url)).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Banner_RejectsFilesThatAreNotImages()
    {
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);

        // The declared content type is ignored; the bytes are not an image.
        var response = await Client.HttpClient.PutAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/banner",
            FileContent(Encoding.UTF8.GetBytes("<html></html>"), "banner.png", "image/png")
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Attachments_CanBeUploadedListedDownloadedAndDeleted()
    {
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        var contents = Encoding.UTF8.GetBytes("Workshop slides");

        var uploadResponse = await Client.HttpClient.PostAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/files",
            FileContent(contents, "slides.txt", "text/plain")
        );
        var file = await uploadResponse.Content.ReadFromJsonAsync<ActivityFileResponse>();

        await Assert.That(uploadResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(file!.Kind).IsEqualTo("Attachment");
        await Assert.That(file.FileName).IsEqualTo("slides.txt");

        var organizerFiles = await Client.HttpClient.GetFromJsonAsync<OrganizerActivityFilesResponse>(
            $"/organizers/standalone-workshops/{workshop.Id}/files"
        );
        await Assert.That(organizerFiles!.Banner).IsNull();
        await Assert.That(organizerFiles.Files.Select(f => f.Id)).IsEquivalentTo(new[] { file.Id });

        var publicFiles = await AnonymousClient.HttpClient.GetFromJsonAsync<PublicActivityFilesResponse>(
            $"/participants/standalone-workshops/{workshop.Id}/files"
        );
        await Assert.That(publicFiles!.Files.Select(f => f.Id)).IsEquivalentTo(new[] { file.Id });

        var download = await StorageClient.GetAsync(publicFiles.Files[0].Url);
        await Assert.That(download.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(download.Content.Headers.ContentDisposition?.DispositionType).IsEqualTo("attachment");
        await Assert.That(download.Content.Headers.ContentDisposition?.FileName?.Trim('"')).IsEqualTo("slides.txt");
        await Assert.That(await download.Content.ReadAsByteArrayAsync()).IsEquivalentTo(contents);

        var fileRoute = $"/organizers/standalone-workshops/{workshop.Id}/files/{file.Id}";
        await Assert.That((await Client.HttpClient.DeleteAsync(fileRoute)).StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That((await Client.HttpClient.DeleteAsync(fileRoute)).StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        publicFiles = await AnonymousClient.HttpClient.GetFromJsonAsync<PublicActivityFilesResponse>(
            $"/participants/standalone-workshops/{workshop.Id}/files"
        );
        await Assert.That(publicFiles!.Files).IsEmpty();
        await Assert.That((await StorageClient.GetAsync(file.Url)).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task PublicFiles_AreHiddenUntilTheActivityIsPublished()
    {
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient, isPublished: false);
        var uploadResponse = await Client.HttpClient.PostAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/files",
            FileContent(Encoding.UTF8.GetBytes("draft"), "draft.txt", "text/plain")
        );
        await Assert.That(uploadResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var response = await AnonymousClient.HttpClient.GetAsync(
            $"/participants/standalone-workshops/{workshop.Id}/files"
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task HackathonBanner_IsReturnedWithPublicHackathonDetails()
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(Client.HttpClient);

        var uploadResponse = await Client.HttpClient.PutAsync(
            $"/organizers/hackathons/{hackathon.Id}/banner",
            FileContent(PngBytes, "hackathon.png", "image/png")
        );
        await Assert.That(uploadResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var publicHackathon = await AnonymousClient.HttpClient.GetFromJsonAsync<PublicHackathonResponse>(
            $"/participants/hackathons/{hackathon.Id}"
        );
        await Assert.That(publicHackathon!.BannerUrl).IsNotNull();
        await Assert.That((await StorageClient.GetAsync(publicHackathon.BannerUrl)).StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task HackathonWorkshopBanner_IsReturnedInParticipantWorkshopList()
    {
        var hackathon = await TestDataHelper.CreateHackathonAndJoinAsync(Client.HttpClient);
        var workshopId = await CreateHackathonWorkshopAsync(hackathon.Id);

        var uploadResponse = await Client.HttpClient.PutAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops/{workshopId}/banner",
            FileContent(PngBytes, "workshop.png", "image/png")
        );
        await Assert.That(uploadResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var workshops = await Client.HttpClient.GetFromJsonAsync<ParticipantWorkshopBannerListResponse>(
            $"/participants/hackathons/{hackathon.Id}/workshops"
        );
        var workshop = workshops!.Workshops.Single(w => w.Id == workshopId);
        await Assert.That(workshop.BannerUrl).IsNotNull();
        await Assert.That((await StorageClient.GetAsync(workshop.BannerUrl)).StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task HackathonWorkshopRoutes_RequireTheWorkshopToBelongToTheHackathon()
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(Client.HttpClient);
        var otherHackathon = await TestDataHelper.CreateHackathonAsync(Client.HttpClient);
        var workshopId = await CreateHackathonWorkshopAsync(hackathon.Id);

        var response = await Client.HttpClient.PutAsync(
            $"/organizers/hackathons/{otherHackathon.Id}/workshops/{workshopId}/banner",
            FileContent(PngBytes, "workshop.png", "image/png")
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task NonOrganizers_CannotManageActivityFiles()
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(Client.HttpClient);
        var workshopId = await CreateHackathonWorkshopAsync(hackathon.Id);
        var standaloneWorkshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        await using var outsider = await CreateUserClientAsync();

        foreach (var route in new[]
        {
            $"/organizers/hackathons/{hackathon.Id}/banner",
            $"/organizers/hackathons/{hackathon.Id}/workshops/{workshopId}/banner",
            $"/organizers/standalone-workshops/{standaloneWorkshop.Id}/banner",
        })
        {
            var response = await outsider.HttpClient.PutAsync(route, FileContent(PngBytes, "banner.png", "image/png"));
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        }

        var listResponse = await outsider.HttpClient.GetAsync(
            $"/organizers/standalone-workshops/{standaloneWorkshop.Id}/files"
        );
        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeletingHackathonWorkshop_RemovesItsFiles()
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(Client.HttpClient);
        var workshopId = await CreateHackathonWorkshopAsync(hackathon.Id);

        var uploadResponse = await Client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops/{workshopId}/files",
            FileContent(Encoding.UTF8.GetBytes("handout"), "handout.txt", "text/plain")
        );
        var file = await uploadResponse.Content.ReadFromJsonAsync<ActivityFileResponse>();
        await Assert.That(uploadResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var deleteResponse = await Client.HttpClient.DeleteAsync(
            $"/organizers/hackathons/{hackathon.Id}/workshops/{workshopId}"
        );
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        await Assert.That((await StorageClient.GetAsync(file!.Url)).StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    private static MultipartFormDataContent FileContent(byte[] bytes, string fileName, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "File", fileName } };
    }

    private async Task<Guid> CreateHackathonWorkshopAsync(Guid hackathonId)
    {
        var response = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/workshops",
            new
            {
                Title = "Files Test Workshop",
                Description = "A workshop for activity file tests",
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
                Location = "Room 101",
                MaxParticipants = 50,
                IsPublished = true,
            }
        );
        response.EnsureSuccessStatusCode();
        var workshop = await response.Content.ReadFromJsonAsync<ParticipantWorkshopBannerItem>();
        return workshop!.Id;
    }

    private static async Task<AuthenticatedHttpClientDataClass> CreateUserClientAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = new AuthenticatedHttpClientDataClass
        {
            GitHubId = Math.Abs(BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0)) | 1,
            GitHubLogin = $"files-outsider-{suffix}",
            FirstName = "Outsider",
            LastName = suffix,
            Email = $"files-outsider-{suffix}@example.com",
        };

        await client.InitializeAsync();
        return client;
    }
}
