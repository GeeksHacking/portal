using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Workers;

public class DatabaseInitBackgroundService(
    ILogger<DatabaseInitBackgroundService> logger,
    ISqlSugarClient sql
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
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

        return Task.CompletedTask;
    }
}
