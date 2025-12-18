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

        sql.DbMaintenance.CreateDatabase();
        sql.CodeFirst.InitTables<Entities.GitHubOnlineAccount>();
        sql.CodeFirst.InitTables<Entities.Hackathon>();
        sql.CodeFirst.InitTables<Entities.HackathonUser>();
        sql.CodeFirst.InitTables<Entities.Judge>();
        sql.CodeFirst.InitTables<Entities.OnlineAccount>();
        sql.CodeFirst.InitTables<Entities.Organizer>();
        sql.CodeFirst.InitTables<Entities.Participant>();
        sql.CodeFirst.InitTables<Entities.ParticipantReview>();
        sql.CodeFirst.InitTables<Entities.Challenge>();
        sql.CodeFirst.InitTables<Entities.ChallengeSubmission>();
        sql.CodeFirst.InitTables<Entities.Resource>();
        sql.CodeFirst.InitTables<Entities.ResourceRedemption>();
        sql.CodeFirst.InitTables<Entities.Team>();
        sql.CodeFirst.InitTables<Entities.User>();

        return Task.CompletedTask;
    }
}
