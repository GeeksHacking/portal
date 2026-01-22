using HackOMania.Api.Entities;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Workers;

public class DatabaseInitBackgroundService(
    ILogger<DatabaseInitBackgroundService> logger,
    IWebHostEnvironment env,
    ISqlSugarClient sql
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Initializing database...");

        sql.CodeFirst.InitTables<GitHubOnlineAccount>();
        sql.CodeFirst.InitTables<Hackathon>();
        sql.CodeFirst.InitTables<Judge>();
        sql.CodeFirst.InitTables<Organizer>();
        sql.CodeFirst.InitTables<Participant>();
        sql.CodeFirst.InitTables<ParticipantReview>();
        sql.CodeFirst.InitTables<Challenge>();
        sql.CodeFirst.InitTables<ChallengeSubmission>();
        sql.CodeFirst.InitTables<Resource>();
        sql.CodeFirst.InitTables<ResourceRedemption>();
        sql.CodeFirst.InitTables<Team>();
        sql.CodeFirst.InitTables<User>();
        sql.CodeFirst.InitTables<RegistrationQuestion>();
        sql.CodeFirst.InitTables<RegistrationQuestionOption>();
        sql.CodeFirst.InitTables<ParticipantRegistrationSubmission>();
        sql.CodeFirst.InitTables<VenueCheckIn>();
        sql.CodeFirst.InitTables<Workshop>();
        sql.CodeFirst.InitTables<WorkshopParticipant>();

        try
        {
            await sql.Ado.ExecuteCommandAsync("ALTER TABLE Team ADD COLUMN SelectedChallengeId CHAR(36) NULL;");
        }
        catch { /* ignore */ }

        try
        {
            await sql.Ado.ExecuteCommandAsync("ALTER TABLE Team ADD COLUMN ChallengeSelectedAt DATETIME NULL;");
        }
        catch { /* ignore */ }

        try
        {
            await sql.Ado.ExecuteCommandAsync("ALTER TABLE Hackathon ADD COLUMN ChallengeSelectionEndDate DATETIME NULL;");
        }
        catch { /* ignore */ }

        if (env.IsDevelopment() && !await sql.Queryable<Hackathon>().AnyAsync(stoppingToken))
        {
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
                SubmissionsEndDate = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero),
                ChallengeSelectionEndDate = new DateTimeOffset(2026, 3, 14, 23, 59, 59, TimeSpan.Zero),
                JudgingStartDate = new DateTimeOffset(2026, 3, 15, 13, 0, 0, TimeSpan.Zero),
                JudgingEndDate = new DateTimeOffset(2026, 3, 15, 18, 0, 0, TimeSpan.Zero),
            };

            await sql.Insertable(hackathon).ExecuteCommandAsync(stoppingToken);

            var standardQuestions = RegistrationQuestionTemplateService.CreateStandardQuestions(
                hackathonId
            );
            var questions = standardQuestions.Select(item => item.Question).ToList();
            var options = standardQuestions.SelectMany(item => item.Options).ToList();

            if (questions.Count > 0)
            {
                await sql.Insertable(questions).ExecuteCommandAsync(stoppingToken);
            }

            if (options.Count > 0)
            {
                await sql.Insertable(options).ExecuteCommandAsync(stoppingToken);
            }

            var challenges = new List<Challenge>
            {
                new()
                {
                    Id = new Guid("1e21229d-a3c7-4f28-812e-4b2eb9e735cd"),
                    HackathonId = hackathonId,
                    Title = "Sustainable City Toolkit",
                    Description =
                        "Build a prototype that helps citizens reduce waste, energy use, or emissions in urban environments.",
                    IsPublished = true,
                },
                new()
                {
                    Id = new Guid("462e5566-e0d7-45b8-91dc-e53611ff06c0"),
                    HackathonId = hackathonId,
                    Title = "Community Health Companion",
                    Description =
                        "Create a solution that improves access to preventive care, mental wellness, or healthy habits.",
                    IsPublished = true,
                },
                new()
                {
                    Id = new Guid("50cacb91-12e8-46dd-9e35-0b2d7d7a0409"),
                    HackathonId = hackathonId,
                    Title = "Inclusive Learning Launchpad",
                    Description =
                        "Design a product that makes learning more accessible for underserved communities.",
                    IsPublished = true,
                },
            };

            await sql.Insertable(challenges).ExecuteCommandAsync(stoppingToken);

            var resources = new List<Resource>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    Name = "Mentor Office Hours",
                    Description = "Redeem a 30-minute session with a domain mentor.",
                    RedemptionStmt = "true",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    Name = "Cloud Credits Pack",
                    Description = "Get starter credits for deployment and testing.",
                    RedemptionStmt = "true",
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    Name = "Prototype Kit",
                    Description = "Pick up a hardware kit for rapid prototyping.",
                    RedemptionStmt = "true",
                },
            };

            await sql.Insertable(resources).ExecuteCommandAsync(stoppingToken);
        }
    }
}
