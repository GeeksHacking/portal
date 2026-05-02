using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Teams.JoinByCode;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/teams/join");
        Description(b => b.WithTags("Teams"));
        Summary(s =>
        {
            s.Summary = "Join a team by join code";
            s.Description =
                "Joins the current user to a team using only the team's join code. If the user is not already a participant in the hackathon, they will be automatically registered.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        // Find the team by join code
        var team = await sql.Queryable<Team>()
            .Where(t => t.JoinCode == req.JoinCode)
            
            .FirstAsync(ct);

        if (team is null)
        {
            AddError(r => r.JoinCode, "Invalid join code.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Get the hackathon for this team
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Includes(h => h.Activity)
            
            .InSingleAsync(team.HackathonId);

        if (hackathon is null || !hackathon.Activity.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Check if user is already a participant in this hackathon
        var participant = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == userId.Value)
            
            .FirstAsync(ct);

        var autoJoinedHackathon = false;

        if (participant is null)
        {
            var now = DateTimeOffset.UtcNow;
            var participantId = Guid.NewGuid();
            // Auto-register the user as a participant in the hackathon
            participant = new Participant
            {
                Id = participantId,
                HackathonId = hackathon.Id,
                UserId = userId.Value,
                TeamId = team.Id,
                JoinedAt = now,
            };

            var registration = new ActivityRegistration
            {
                Id = participantId,
                ActivityId = hackathon.Id,
                UserId = userId.Value,
                Status = ActivityRegistrationStatus.Registered,
                RegisteredAt = now,
            };

            var transactionResult = await sql.Ado.UseTranAsync(async () =>
            {
                await sql.Insertable(participant).ExecuteCommandAsync(ct);
                await sql.Insertable(registration).ExecuteCommandAsync(ct);
            });

            if (!transactionResult.IsSuccess)
            {
                throw transactionResult.ErrorException!;
            }

            autoJoinedHackathon = true;
        }
        else
        {
            // User is already a participant, check if they're already in a team
            if (participant.TeamId.HasValue)
            {
                AddError(r => r.JoinCode, "You are already in a team for this hackathon.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            // Update the participant's team
            participant.TeamId = team.Id;
            participant.WithdrawnAt = null;

            var registration = await sql.Queryable<ActivityRegistration>()
                .InSingleAsync(participant.Id);

            if (registration is null)
            {
                registration = new ActivityRegistration
                {
                    Id = participant.Id,
                    ActivityId = hackathon.Id,
                    UserId = userId.Value,
                    RegisteredAt = participant.JoinedAt,
                };
            }

            registration.Status = ActivityRegistrationStatus.Registered;
            registration.WithdrawnAt = null;

            var transactionResult = await sql.Ado.UseTranAsync(async () =>
            {
                await sql.Updateable(participant).ExecuteCommandAsync(ct);
                await sql.Storageable(registration).ExecuteCommandAsync(ct);
            });

            if (!transactionResult.IsSuccess)
            {
                throw transactionResult.ErrorException!;
            }
        }

        await Send.OkAsync(
            new Response
            {
                TeamId = team.Id,
                HackathonId = hackathon.Id,
                AutoJoinedHackathon = autoJoinedHackathon,
            },
            ct
        );
    }
}
