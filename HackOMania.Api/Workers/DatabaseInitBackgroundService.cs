using HackOMania.Api.Entities;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Workers;

public class DatabaseInitBackgroundService(
    ILogger<DatabaseInitBackgroundService> logger,
    IWebHostEnvironment env,
    ISqlSugarClient sql
)
{
    public async Task InitializeAsync(CancellationToken stoppingToken = default)
    {
        logger.LogInformation("Initializing database...");

        sql.CodeFirst.InitTables<GitHubOnlineAccount>();
        sql.CodeFirst.InitTables<Hackathon>();
        sql.CodeFirst.InitTables<Judge>();
        sql.CodeFirst.InitTables<Organizer>();
        sql.CodeFirst.InitTables<Participant>();
        sql.CodeFirst.InitTables<ParticipantReview>();
        sql.CodeFirst.InitTables<ParticipantEmailDelivery>();
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
        sql.CodeFirst.InitTables<HackathonNotificationTemplate>();
        sql.CodeFirst.InitTables<EventTimelineItem>();

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

            // testing with myself (anggun) as organizer
            var testUserId = Guid.NewGuid();
            var testUser = new User
            {
                Id = testUserId,
                FirstName = "gunnicorn",
                LastName = "",
                Email = "anggunq@hotmail.com",
            };
            await sql.Insertable(testUser).ExecuteCommandAsync(stoppingToken);

            var testGitHub = new GitHubOnlineAccount
            {
                Id = Guid.NewGuid(),
                UserId = testUserId,
                GitHubId = 47025159,
                GitHubLogin = "gunnicorn",
            };
            await sql.Insertable(testGitHub).ExecuteCommandAsync(stoppingToken);

            var testOrganizer = new Organizer
            {
                Id = Guid.NewGuid(),
                HackathonId = hackathonId,
                UserId = testUserId,
                Type = OrganizerType.Admin,
            };

            // IF THIS DOES NOT WORK - unfortunately you have to call this api manually in scalar for your local testing lol
            await sql.Insertable(testOrganizer).ExecuteCommandAsync(stoppingToken);
        }
    }
}
