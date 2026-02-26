using HackOMania.Api.Services;

namespace HackOMania.Tests.Services;

public class SqlSugarRedisCacheKeyTests
{
    [Test]
    public async Task ToSemanticCacheKey_HackathonDetailsKey_AddsSemanticPrefix()
    {
        // Arrange
        var rawKey =
            "SqlSugarDataCache.SELECT * FROM `Hackathon` h WHERE h.`Id` = @HackathonId LIMIT 1";

        // Act
        var semanticKey = SqlSugarRedisCache.ToSemanticCacheKey(rawKey);

        // Assert
        await Assert
            .That(semanticKey)
            .StartsWith("SqlSugarDataCache.hackathon-details.", StringComparison.Ordinal);
    }

    [Test]
    public async Task ToSemanticCacheKey_NonSqlSugarKey_ReturnsOriginalKey()
    {
        // Arrange
        var rawKey = "custom-cache-key";

        // Act
        var semanticKey = SqlSugarRedisCache.ToSemanticCacheKey(rawKey);

        // Assert
        await Assert.That(semanticKey).IsEqualTo(rawKey);
    }
}
