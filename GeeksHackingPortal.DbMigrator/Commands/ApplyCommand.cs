using System.Text.Json;
using ConsoleAppFramework;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace GeeksHackingPortal.DbMigrator.Commands;

public class ApplyCommand(
     ILogger<ApplyCommand> logger,
     ISqlSugarClient sql)
{
    /// <summary>
    /// Apply pending SqlSugar schema changes after diff review.
    /// </summary>
    /// <param name="allowDestructive">Allow column deletions when applying schema changes.</param>
    /// <param name="seedDevelopmentTemplate">Seed the development template data after applying schema changes.</param>
    /// <param name="cancellationToken"></param>
    [Command("apply")]
    public void Apply(
        bool allowDestructive = false,
        bool seedDevelopmentTemplate = false,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "Applying pending database schema changes. AllowDestructive: {AllowDestructive}; SeedDevelopmentTemplate: {SeedDevelopmentTemplate}",
            allowDestructive,
            seedDevelopmentTemplate
        );

        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Creating database connection.");
        var entityTypes = SchemaDiffLogger.GetEntityTypes();
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Collecting SqlSugar schema differences for {EntityCount} entities.", entityTypes.Length);
        var differenceProvider = sql.CodeFirst.GetDifferenceTables(entityTypes);
        var schemaDifferences = differenceProvider.ToDiffList()
            .Where(table => table.IsDiff)
            .ToArray();

        SchemaDiffLogger.Write(
            logger,
            differenceProvider.ToDiffString()?.Trim() ?? string.Empty,
            schemaDifferences
        );

        if (schemaDifferences.Length > 0)
        {
            if (schemaDifferences.Any(table => table.DeleteColums.Count > 0) && !allowDestructive)
            {
                logger.LogWarning(
                    "Destructive database changes were detected and --allow-destructive was not supplied."
                );

                throw new InvalidOperationException(
                    "Destructive database changes detected. Review the diff and rerun with --allow-destructive after approval."
                );
            }

            cancellationToken.ThrowIfCancellationRequested();
            sql.CodeFirst.InitTables(entityTypes);
            logger.LogInformation("Database schema changes were applied.");
        }

        if (seedDevelopmentTemplate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Applying development template seed.");
            SeedDevelopmentTemplate(sql, logger);
        }
    }

    private static void SeedDevelopmentTemplate(ISqlSugarClient sql, ILogger logger)
    {
        if (!sql.DbMaintenance.IsAnyTable(nameof(Hackathon), false))
        {
            logger.LogInformation(
                "Skipping development seed because the database schema has not been migrated yet."
            );
            return;
        }

        if (sql.Queryable<Hackathon>().Any())
        {
            logger.LogInformation("Skipping development seed because hackathon data already exists.");
            return;
        }

        logger.LogInformation("Seeding development template data.");

        var hackathonId = new Guid("1e2beba8-0dd2-484f-b5b2-4b1b71a084e4");
        var eventStart = new DateTimeOffset(2026, 3, 13, 9, 0, 0, TimeSpan.Zero);
        var eventEnd = new DateTimeOffset(2026, 3, 15, 18, 0, 0, TimeSpan.Zero);

        var hackathon = new Hackathon
        {
            Id = hackathonId,
            Name = "HackOMania 2026",
            Description =
                "HackOMania 2026 brings builders together for a weekend of rapid prototyping, learning, and community.",
            Venue = "GeeksHacking Innovation Hub",
            HomepageUri = new Uri("https://hackomania.geekshacking.com/2026"),
            ShortCode = "HACKO26",
            IsPublished = true,
            EventStartDate = eventStart,
            EventEndDate = eventEnd,
            SubmissionsStartDate = new DateTimeOffset(2026, 3, 13, 10, 0, 0, TimeSpan.Zero),
            ChallengeSelectionEndDate = new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero),
            SubmissionsEndDate = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero),
            JudgingStartDate = new DateTimeOffset(2026, 3, 15, 13, 0, 0, TimeSpan.Zero),
            JudgingEndDate = new DateTimeOffset(2026, 3, 15, 18, 0, 0, TimeSpan.Zero),
        };
        sql.Insertable(hackathon).ExecuteCommand();

        var standardQuestions = RegistrationQuestionTemplateService.CreateStandardQuestions(hackathonId);
        var questions = standardQuestions.Select(item => item.Question).ToList();
        var options = standardQuestions.SelectMany(item => item.Options).ToList();

        if (questions.Count > 0)
        {
            sql.Insertable(questions).ExecuteCommand();
        }

        if (options.Count > 0)
        {
            sql.Insertable(options).ExecuteCommand();
        }

        var resources = new List<Resource>
        {
            new()
            {
                Id = Guid.NewGuid(),
                HackathonId = hackathonId,
                Name = "Mentor Office Hours",
                Description = "Redeem a 30-minute session with a domain mentor.",
                RedemptionStmt = "true",
                IsPublished = true,
            },
            new()
            {
                Id = Guid.NewGuid(),
                HackathonId = hackathonId,
                Name = "Cloud Credits Pack",
                Description = "Get starter credits for deployment and testing.",
                RedemptionStmt = "true",
                IsPublished = true,
            },
            new()
            {
                Id = Guid.NewGuid(),
                HackathonId = hackathonId,
                Name = "Prototype Kit",
                Description = "Pick up a hardware kit for rapid prototyping.",
                RedemptionStmt = "true",
                IsPublished = true,
            },
        };
        sql.Insertable(resources).ExecuteCommand();

        var testUserId = Guid.NewGuid();
        var testUser = new User
        {
            Id = testUserId,
            FirstName = "gunnicorn",
            LastName = "",
            Email = "anggunq@hotmail.com",
        };
        sql.Insertable(testUser).ExecuteCommand();

        var testGitHub = new GitHubOnlineAccount
        {
            Id = Guid.NewGuid(),
            UserId = testUserId,
            GitHubId = 47025159,
            GitHubLogin = "gunnicorn",
        };
        sql.Insertable(testGitHub).ExecuteCommand();

        var testOrganizer = new Organizer
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            UserId = testUserId,
            Type = OrganizerType.Admin,
        };
        sql.Insertable(testOrganizer).ExecuteCommand();

        logger.LogInformation("Development template data was seeded.");
    }
}
