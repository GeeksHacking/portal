using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GeeksHackingPortal.Api.Options;
using GeeksHackingPortal.Api.Services;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;
using Microsoft.Extensions.Options;

namespace GeeksHackingPortal.Tests.Endpoints.Admin.Activities;

public class HeaderImageTests
{
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
    ];

    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass AuthenticatedClient { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass Client { get; init; }

    private async Task<HackathonResponse> CreateHackathonAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var suffix = Guid.NewGuid().ToString()[..8];
        var response = await AuthenticatedClient.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            new CreateHackathonRequest
            {
                Name = $"Header Image Hackathon {suffix}",
                Description = "Header image tests",
                Venue = "Virtual",
                HomepageUri = new Uri("https://example.com/hackathon"),
                ShortCode = $"IMG{suffix}",
                EventStartDate = now.AddDays(7),
                EventEndDate = now.AddDays(9),
                SubmissionsStartDate = now.AddDays(7).AddHours(2),
                SubmissionsEndDate = now.AddDays(8).AddHours(20),
                JudgingStartDate = now.AddDays(8).AddHours(21),
                JudgingEndDate = now.AddDays(9).AddHours(-2),
                IsPublished = true,
            }
        );
        return (await response.Content.ReadFromJsonAsync<HackathonResponse>())!;
    }

    private sealed record UploadUrlResponse(string UploadUrl, string Key, DateTimeOffset ExpiresAt);

    private sealed record SetResponse(string HeaderImageUrl);

    private async Task<HttpResponseMessage> RequestUploadUrlAsync(
        Guid activityId,
        string contentType = "image/png",
        long contentLength = 12
    ) =>
        await AuthenticatedClient.HttpClient.PostAsJsonAsync(
            $"/admin/activities/{activityId}/header-image/upload-url",
            new { contentType, contentLength }
        );

    /// <summary>Gets an upload URL and PUTs the bytes straight to storage, like the browser does.</summary>
    private async Task<string> UploadToStorageAsync(Guid activityId, byte[] bytes, string contentType = "image/png")
    {
        var response = await RequestUploadUrlAsync(activityId, contentType, bytes.Length);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var upload = (await response.Content.ReadFromJsonAsync<UploadUrlResponse>())!;

        using var storage = new HttpClient();
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var put = await storage.PutAsync(upload.UploadUrl, content);
        await Assert.That(put.IsSuccessStatusCode).IsTrue();

        return upload.Key;
    }

    private Task<HttpResponseMessage> SetHeaderImageAsync(Guid activityId, string key) =>
        AuthenticatedClient.HttpClient.PutAsJsonAsync($"/admin/activities/{activityId}/header-image", new { key });

    [Test]
    public async Task UploadUrl_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.HttpClient.PostAsJsonAsync(
            $"/admin/activities/{Guid.NewGuid()}/header-image/upload-url",
            new { contentType = "image/png", contentLength = 12 }
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Set_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.HttpClient.PutAsJsonAsync(
            $"/admin/activities/{Guid.NewGuid()}/header-image",
            new { key = "hackathon/x/header-x.png" }
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Delete_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.HttpClient.DeleteAsync(
            $"/admin/activities/{Guid.NewGuid()}/header-image"
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UploadUrl_UnsupportedContentType_ReturnsBadRequest()
    {
        var hackathon = await CreateHackathonAsync();

        var response = await RequestUploadUrlAsync(hackathon.Id, "image/gif");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UploadUrl_OversizedFile_ReturnsBadRequest()
    {
        var hackathon = await CreateHackathonAsync();

        var response = await RequestUploadUrlAsync(hackathon.Id, contentLength: 5 * 1024 * 1024 + 1);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UploadUrl_UnknownActivity_ReturnsNotFound()
    {
        var response = await RequestUploadUrlAsync(Guid.NewGuid());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Set_AfterDirectUpload_StoresPublicImage()
    {
        var hackathon = await CreateHackathonAsync();
        var key = await UploadToStorageAsync(hackathon.Id, PngBytes);

        var response = await SetHeaderImageAsync(hackathon.Id, key);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<SetResponse>())!;
        await Assert.That(body.HeaderImageUrl).EndsWith(key);

        // The bucket is publicly readable without credentials.
        using var anonymous = new HttpClient();
        var image = await anonymous.GetByteArrayAsync(body.HeaderImageUrl);
        await Assert.That(image).IsEquivalentTo(PngBytes);

        var get = await Client.HttpClient.GetAsync($"/participants/hackathons/{hackathon.Id}");
        using var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        await Assert.That(json.RootElement.GetProperty("headerImageUrl").GetString()).IsEqualTo(body.HeaderImageUrl);
    }

    [Test]
    public async Task Set_NonImageUpload_ReturnsBadRequestAndDeletesObject()
    {
        var hackathon = await CreateHackathonAsync();
        // Declared as a PNG but the bytes are not an image.
        var key = await UploadToStorageAsync(hackathon.Id, "not an image"u8.ToArray());

        var response = await SetHeaderImageAsync(hackathon.Id, key);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        // Rejected uploads are removed, so confirming again finds nothing.
        var retry = await SetHeaderImageAsync(hackathon.Id, key);
        await Assert.That(retry.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Set_WithoutUploading_ReturnsBadRequest()
    {
        var hackathon = await CreateHackathonAsync();
        var upload = (await (await RequestUploadUrlAsync(hackathon.Id)).Content.ReadFromJsonAsync<UploadUrlResponse>())!;

        var response = await SetHeaderImageAsync(hackathon.Id, upload.Key);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Set_KeyOfAnotherActivity_ReturnsBadRequest()
    {
        var hackathon = await CreateHackathonAsync();
        var other = await CreateHackathonAsync();
        var key = await UploadToStorageAsync(other.Id, PngBytes);

        var response = await SetHeaderImageAsync(hackathon.Id, key);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Delete_WithoutImage_ReturnsNoContent()
    {
        var hackathon = await CreateHackathonAsync();

        var response = await AuthenticatedClient.HttpClient.DeleteAsync(
            $"/admin/activities/{hackathon.Id}/header-image"
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task PublicHackathonGet_IncludesNullHeaderImageUrl_WhenUnset()
    {
        var hackathon = await CreateHackathonAsync();

        var response = await Client.HttpClient.GetAsync($"/participants/hackathons/{hackathon.Id}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(json.RootElement.TryGetProperty("headerImageUrl", out var url)).IsTrue();
        await Assert.That(url.ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task HeaderImageStorage_WithWriteSecretsJson_ConfiguresClient()
    {
        using var storage = new HeaderImageStorage(
            Options.Create(
                new R2Options
                {
                    AccountId = "acc-123",
                    BucketName = "portal",
                    PublicBaseUrl = "https://portal-r2.geekshacking.com",
                    WriteSecrets = """{"accessKeyId":"key-id","secretAccessKey":"secret-key"}""",
                }
            )
        );

        await Assert.That(storage.IsConfigured).IsTrue();
    }
}
