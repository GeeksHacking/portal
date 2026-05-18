using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Delete;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;

namespace GeeksHackingPortal.Tests.Endpoints.Admin.OAuthApplications;

public class OAuthApplicationDeletionServiceTests
{
    [Test]
    public async Task DeleteOwnedAsync_RetriesWithReloadedApplicationAfterConcurrencyConflict()
    {
        var ownerUserId = Guid.NewGuid();
        var staleApplication = new object();
        var refreshedApplication = new object();
        var operations = new FakeOAuthApplicationDeletionOperations
        {
            FindResults = [staleApplication, refreshedApplication],
            OwnershipResults = [true, true],
        };
        operations.DeleteImplementation = application =>
        {
            if (ReferenceEquals(application, staleApplication))
            {
                throw new OpenIddictExceptions.ConcurrencyException("stale application");
            }

            return Task.CompletedTask;
        };
        var service = new OAuthApplicationDeletionService(operations, NullLogger<OAuthApplicationDeletionService>.Instance);

        var result = await service.DeleteOwnedAsync("app-id", ownerUserId, CancellationToken.None);

        await Assert.That(result).IsEqualTo(OAuthApplicationDeletionResult.Deleted);
        await Assert.That(operations.FindByIdCalls).IsEqualTo(2);
        await Assert.That(operations.DeleteCalls).IsEqualTo(2);
        await Assert.That(ReferenceEquals(operations.LastDeletedApplication, refreshedApplication)).IsTrue();
    }

    [Test]
    public async Task DeleteOwnedAsync_ReturnsNotFoundWhenApplicationDisappearsAfterConcurrencyConflict()
    {
        var ownerUserId = Guid.NewGuid();
        var staleApplication = new object();
        var operations = new FakeOAuthApplicationDeletionOperations
        {
            FindResults = [staleApplication, null],
            OwnershipResults = [true],
        };
        operations.DeleteImplementation = _ =>
            throw new OpenIddictExceptions.ConcurrencyException("stale application");
        var service = new OAuthApplicationDeletionService(operations, NullLogger<OAuthApplicationDeletionService>.Instance);

        var result = await service.DeleteOwnedAsync("app-id", ownerUserId, CancellationToken.None);

        await Assert.That(result).IsEqualTo(OAuthApplicationDeletionResult.NotFound);
        await Assert.That(operations.FindByIdCalls).IsEqualTo(2);
        await Assert.That(operations.DeleteCalls).IsEqualTo(1);
    }

    [Test]
    public async Task DeleteOwnedAsync_ReturnsNotFoundWhenApplicationIsNotOwnedByCurrentUser()
    {
        var operations = new FakeOAuthApplicationDeletionOperations
        {
            FindResults = [new object()],
            OwnershipResults = [false],
        };
        var service = new OAuthApplicationDeletionService(operations, NullLogger<OAuthApplicationDeletionService>.Instance);

        var result = await service.DeleteOwnedAsync("app-id", Guid.NewGuid(), CancellationToken.None);

        await Assert.That(result).IsEqualTo(OAuthApplicationDeletionResult.NotFound);
        await Assert.That(operations.DeleteCalls).IsEqualTo(0);
    }

    private sealed class FakeOAuthApplicationDeletionOperations : IOAuthApplicationDeletionOperations
    {
        public required List<object?> FindResults { get; init; }
        public required List<bool> OwnershipResults { get; init; }
        public Func<object, Task> DeleteImplementation { get; set; } = _ => Task.CompletedTask;
        public int FindByIdCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public object? LastDeletedApplication { get; private set; }

        public Task<object?> FindByIdAsync(string applicationId, CancellationToken ct)
        {
            if (FindResults.Count is 0)
            {
                throw new InvalidOperationException("FindResults must contain at least one result.");
            }

            var index = Math.Min(FindByIdCalls, FindResults.Count - 1);
            FindByIdCalls++;
            return Task.FromResult(FindResults[index]);
        }

        public ValueTask<bool> IsOwnedByAsync(object application, Guid ownerUserId, CancellationToken ct)
        {
            if (OwnershipResults.Count is 0)
            {
                throw new InvalidOperationException(
                    "OwnershipResults must contain at least one result."
                );
            }

            var index = Math.Min(FindByIdCalls - 1, OwnershipResults.Count - 1);
            return ValueTask.FromResult(OwnershipResults[index]);
        }

        public async Task DeleteAsync(object application, CancellationToken ct)
        {
            DeleteCalls++;
            await DeleteImplementation(application);
            LastDeletedApplication = application;
        }
    }
}
