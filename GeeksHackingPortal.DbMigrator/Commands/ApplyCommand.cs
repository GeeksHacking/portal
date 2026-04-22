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

        cancellationToken.ThrowIfCancellationRequested();
        BackfillActivities(sql, logger);

        if (seedDevelopmentTemplate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Applying development template seed.");
            SeedDevelopmentTemplate(sql, logger);
        }
    }

    private static void BackfillActivities(ISqlSugarClient sql, ILogger logger)
    {
        if (!sql.DbMaintenance.IsAnyTable(nameof(Activity), false))
        {
            logger.LogInformation("Skipping activity backfill because the Activity table does not exist.");
            return;
        }

        var existingActivityIds = sql.Queryable<Activity>()
            .Select(activity => activity.Id)
            .ToList()
            .ToHashSet();

        BackfillHackathonActivities(sql, logger, existingActivityIds);
        BackfillWorkshopActivities(sql, logger, existingActivityIds);
        BackfillActivityRegistrations(sql, logger);
        BackfillActivityOrganizers(sql, logger);
    }

    private static void BackfillHackathonActivities(
        ISqlSugarClient sql,
        ILogger logger,
        HashSet<Guid> existingActivityIds
    )
    {
        if (!sql.DbMaintenance.IsAnyTable(nameof(Hackathon), false))
        {
            logger.LogInformation("Skipping hackathon activity backfill because the Hackathon table does not exist.");
            return;
        }

        var activities = sql.Queryable<Hackathon>()
            .ToList()
            .Where(hackathon => !existingActivityIds.Contains(hackathon.Id))
            .Select(hackathon => hackathon.EnsureActivity())
            .ToList();

        if (activities.Count == 0)
        {
            logger.LogInformation("No hackathon activities needed backfilling.");
            return;
        }

        sql.Insertable(activities).ExecuteCommand();
        foreach (var activity in activities)
        {
            existingActivityIds.Add(activity.Id);
        }

        logger.LogInformation("Backfilled {ActivityCount} hackathon activities.", activities.Count);
    }

    private static void BackfillWorkshopActivities(
        ISqlSugarClient sql,
        ILogger logger,
        HashSet<Guid> existingActivityIds
    )
    {
        if (!sql.DbMaintenance.IsAnyTable(nameof(Workshop), false))
        {
            logger.LogInformation("Skipping workshop activity backfill because the Workshop table does not exist.");
            return;
        }

        var activities = sql.Queryable<Workshop>()
            .ToList()
            .Where(workshop => !existingActivityIds.Contains(workshop.Id))
            .Select(workshop => workshop.EnsureActivity())
            .ToList();

        if (activities.Count == 0)
        {
            logger.LogInformation("No workshop activities needed backfilling.");
            return;
        }

        sql.Insertable(activities).ExecuteCommand();
        foreach (var activity in activities)
        {
            existingActivityIds.Add(activity.Id);
        }

        logger.LogInformation("Backfilled {ActivityCount} workshop activities.", activities.Count);
    }

    private static void BackfillActivityRegistrations(ISqlSugarClient sql, ILogger logger)
    {
        if (
            !sql.DbMaintenance.IsAnyTable(nameof(ActivityRegistration), false)
            || !sql.DbMaintenance.IsAnyTable(nameof(Participant), false)
        )
        {
            logger.LogInformation(
                "Skipping activity registration backfill because the required tables do not exist."
            );
            return;
        }

        var existingRegistrationIds = sql.Queryable<ActivityRegistration>()
            .Select(registration => registration.Id)
            .ToList()
            .ToHashSet();

        var registrations = sql.Queryable<Participant>()
            .ToList()
            .Where(participant => !existingRegistrationIds.Contains(participant.Id))
            .Select(participant => new ActivityRegistration
            {
                Id = participant.Id,
                ActivityId = participant.HackathonId,
                UserId = participant.UserId,
                Status = participant.WithdrawnAt.HasValue
                    ? ActivityRegistrationStatus.Withdrawn
                    : ActivityRegistrationStatus.Registered,
                RegisteredAt = participant.JoinedAt,
                WithdrawnAt = participant.WithdrawnAt,
            })
            .ToList();

        if (registrations.Count == 0)
        {
            logger.LogInformation("No activity registrations needed backfilling.");
            return;
        }

        sql.Insertable(registrations).ExecuteCommand();
        logger.LogInformation("Backfilled {RegistrationCount} activity registrations.", registrations.Count);
    }

    private static void BackfillActivityOrganizers(ISqlSugarClient sql, ILogger logger)
    {
        if (
            !sql.DbMaintenance.IsAnyTable(nameof(ActivityOrganizer), false)
            || !sql.DbMaintenance.IsAnyTable(nameof(Organizer), false)
        )
        {
            logger.LogInformation(
                "Skipping activity organizer backfill because the required tables do not exist."
            );
            return;
        }

        var existingOrganizerIds = sql.Queryable<ActivityOrganizer>()
            .Select(organizer => organizer.Id)
            .ToList()
            .ToHashSet();

        var organizers = sql.Queryable<Organizer>()
            .ToList()
            .Where(organizer => !existingOrganizerIds.Contains(organizer.Id))
            .Select(organizer => new ActivityOrganizer
            {
                Id = organizer.Id,
                ActivityId = organizer.HackathonId,
                UserId = organizer.UserId,
                Type = organizer.Type,
            })
            .ToList();

        if (organizers.Count == 0)
        {
            logger.LogInformation("No activity organizers needed backfilling.");
            return;
        }

        sql.Insertable(organizers).ExecuteCommand();
        logger.LogInformation("Backfilled {OrganizerCount} activity organizers.", organizers.Count);
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

        var activity = new Activity
        {
            Id = hackathonId,
            Kind = ActivityKind.Hackathon,
            Title = "HackOMania 2026",
            Description =
                "HackOMania 2026 brings builders together for a weekend of rapid prototyping, learning, and community.",
            Location = "GeeksHacking Innovation Hub",
            StartTime = eventStart,
            EndTime = eventEnd,
            IsPublished = true,
        };
        sql.Insertable(activity).ExecuteCommand();

        var hackathon = new Hackathon
        {
            Id = hackathonId,
            Activity = activity,
            Name = activity.Title,
            Description = activity.Description,
            Venue = activity.Location,
            IsPublished = activity.IsPublished,
            EventStartDate = activity.StartTime,
            EventEndDate = activity.EndTime,
            HomepageUri = new Uri("https://hackomania.geekshacking.com/2026"),
            ShortCode = "HACKO26",
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
                ActivityId = activity.Id,
                Name = "Mentor Office Hours",
                Description = "Redeem a 30-minute session with a domain mentor.",
                RedemptionStmt = "true",
                IsPublished = true,
            },
            new()
            {
                Id = Guid.NewGuid(),
                ActivityId = activity.Id,
                Name = "Cloud Credits Pack",
                Description = "Get starter credits for deployment and testing.",
                RedemptionStmt = "true",
                IsPublished = true,
            },
            new()
            {
                Id = Guid.NewGuid(),
                ActivityId = activity.Id,
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

        sql.Insertable(
                new ActivityOrganizer
                {
                    Id = testOrganizer.Id,
                    ActivityId = activity.Id,
                    UserId = testUserId,
                    Type = testOrganizer.Type,
                }
            )
            .ExecuteCommand();

        logger.LogInformation("Development template data was seeded.");
    }
}
