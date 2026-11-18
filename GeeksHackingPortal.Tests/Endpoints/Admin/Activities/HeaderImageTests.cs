using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

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

    private static MultipartFormDataContent ImageForm(byte[] bytes, string contentType, string fileName)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "File", fileName } };
    }

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

    [Test]
    public async Task Upload_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.HttpClient.PutAsync(
            $"/admin/activities/{Guid.NewGuid()}/header-image",
            ImageForm(PngBytes, "image/png", "header.png")
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
    public async Task Upload_NonImageFile_ReturnsBadRequest()
    {
        var hackathon = await CreateHackathonAsync();

        // Claims to be a PNG but the bytes are not an image.
        var response = await AuthenticatedClient.HttpClient.PutAsync(
            $"/admin/activities/{hackathon.Id}/header-image",
            ImageForm("not an image"u8.ToArray(), "image/png", "header.png")
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Upload_OversizedFile_ReturnsBadRequest()
    {
        var hackathon = await CreateHackathonAsync();
        var bytes = new byte[5 * 1024 * 1024 + 1];
        PngBytes.CopyTo(bytes, 0);

        var response = await AuthenticatedClient.HttpClient.PutAsync(
            $"/admin/activities/{hackathon.Id}/header-image",
            ImageForm(bytes, "image/png", "header.png")
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Upload_UnknownActivity_ReturnsNotFound()
    {
        var response = await AuthenticatedClient.HttpClient.PutAsync(
            $"/admin/activities/{Guid.NewGuid()}/header-image",
            ImageForm(PngBytes, "image/png", "header.png")
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Upload_WhenStorageNotConfigured_ReturnsServiceUnavailable()
    {
        var hackathon = await CreateHackathonAsync();

        var response = await AuthenticatedClient.HttpClient.PutAsync(
            $"/admin/activities/{hackathon.Id}/header-image",
            ImageForm(PngBytes, "image/png", "header.png")
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
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
}
