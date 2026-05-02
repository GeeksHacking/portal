using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Participants;

public class ListRequest
{
    public Guid StandaloneWorkshopId { get; set; }
}

public class ParticipantRequest
{
    public Guid StandaloneWorkshopId { get; set; }
    public Guid UserId { get; set; }
}

public class ListResponse
{
    public required int TotalCount { get; init; }
    public required int RegisteredCount { get; init; }
    public required int WithdrawnCount { get; init; }
    public required List<ParticipantItem> Participants { get; init; }
}

public class ParticipantResponse : ParticipantItem
{
    public required List<VenueCheckInItem> VenueCheckIns { get; init; }
}

public class ParticipantItem
{
    public required Guid RegistrationId { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset RegisteredAt { get; init; }
    public DateTimeOffset? WithdrawnAt { get; init; }
    public required List<RegistrationSubmissionItem> RegistrationSubmissions { get; init; }
}

public class RegistrationSubmissionItem
{
    public required Guid QuestionId { get; init; }
    public required string QuestionText { get; init; }
    public required string Value { get; init; }
    public string? FollowUpValue { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public class VenueCheckInItem
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CheckInTime { get; init; }
    public DateTimeOffset? CheckOutTime { get; init; }
    public required bool IsCheckedIn { get; init; }
}

public class WithdrawResponse
{
    public required string Message { get; init; }
}

public class ListEndpoint(ISqlSugarClient sql) : Endpoint<ListRequest, ListResponse>
{
    public override void Configure()
    {
        Get("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/participants");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Activity Participants"));
    }

    public override async Task HandleAsync(ListRequest req, CancellationToken ct)
    {
        var exists = await sql.Queryable<StandaloneWorkshop>()
            .AnyAsync(w => w.Id == req.StandaloneWorkshopId, ct);
        if (!exists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var registrations = await sql.Queryable<ActivityRegistration>()
            .Where(r => r.ActivityId == req.StandaloneWorkshopId)
            .OrderByDescending(r => r.RegisteredAt)
            .ToListAsync(ct);

        var participants = await BuildParticipantsAsync(registrations, ct);
        await Send.OkAsync(
            new ListResponse
            {
                TotalCount = registrations.Count,
                RegisteredCount = registrations.Count(r => r.Status == ActivityRegistrationStatus.Registered),
                WithdrawnCount = registrations.Count(r => r.Status == ActivityRegistrationStatus.Withdrawn),
                Participants = participants,
            },
            ct
        );
    }

    private async Task<List<ParticipantItem>> BuildParticipantsAsync(
        List<ActivityRegistration> registrations,
        CancellationToken ct
    )
    {
        if (registrations.Count == 0)
        {
            return [];
        }

        var registrationIds = registrations.Select(r => r.Id).ToList();
        var userIds = registrations.Select(r => r.UserId).Distinct().ToList();
        var users = (await sql.Queryable<User>().Where(u => userIds.Contains(u.Id)).ToListAsync(ct))
            .ToDictionary(u => u.Id, u => u);
        var submissions = await sql.Queryable<ParticipantRegistrationSubmission>()
            .LeftJoin<RegistrationQuestion>((s, q) => s.QuestionId == q.Id)
            .Where((s, q) => registrationIds.Contains(s.ActivityRegistrationId))
            .Select(
                (s, q) =>
                    new
                    {
                        s.ActivityRegistrationId,
                        Item = new RegistrationSubmissionItem
                        {
                            QuestionId = s.QuestionId,
                            QuestionText = q.QuestionText,
                            Value = s.Value,
                            FollowUpValue = s.FollowUpValue,
                            UpdatedAt = s.UpdatedAt,
                        },
                    }
            )
            .ToListAsync(ct);
        var submissionsByRegistration = submissions
            .GroupBy(s => s.ActivityRegistrationId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Item).ToList());

        return
        [
            .. registrations.Select(r =>
            {
                var user = users.GetValueOrDefault(r.UserId);
                return new ParticipantItem
                {
                    RegistrationId = r.Id,
                    UserId = r.UserId,
                    Name = user?.Name ?? "Unknown",
                    Email = user?.Email ?? string.Empty,
                    Status = r.Status.ToString(),
                    RegisteredAt = r.RegisteredAt,
                    WithdrawnAt = r.WithdrawnAt,
                    RegistrationSubmissions = submissionsByRegistration.GetValueOrDefault(r.Id) ?? [],
                };
            }),
        ];
    }
}

public class GetEndpoint(ISqlSugarClient sql) : Endpoint<ParticipantRequest, ParticipantResponse>
{
    public override void Configure()
    {
        Get("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/participants/{UserId:guid}");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Activity Participants"));
    }

    public override async Task HandleAsync(ParticipantRequest req, CancellationToken ct)
    {
        var registration = await sql.Queryable<ActivityRegistration>()
            .Where(r => r.ActivityId == req.StandaloneWorkshopId && r.UserId == req.UserId)
            .FirstAsync(ct);
        if (registration is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var user = await sql.Queryable<User>().InSingleAsync(registration.UserId);
        var submissions = await sql.Queryable<ParticipantRegistrationSubmission>()
            .LeftJoin<RegistrationQuestion>((s, q) => s.QuestionId == q.Id)
            .Where((s, q) => s.ActivityRegistrationId == registration.Id)
            .Select(
                (s, q) =>
                    new RegistrationSubmissionItem
                    {
                        QuestionId = s.QuestionId,
                        QuestionText = q.QuestionText,
                        Value = s.Value,
                        FollowUpValue = s.FollowUpValue,
                        UpdatedAt = s.UpdatedAt,
                    }
            )
            .ToListAsync(ct);
        var checkIns = await sql.Queryable<VenueCheckIn>()
            .Where(c => c.ActivityRegistrationId == registration.Id)
            .OrderByDescending(c => c.CheckInTime)
            .ToListAsync(ct);

        await Send.OkAsync(
            new ParticipantResponse
            {
                RegistrationId = registration.Id,
                UserId = registration.UserId,
                Name = user?.Name ?? "Unknown",
                Email = user?.Email ?? string.Empty,
                Status = registration.Status.ToString(),
                RegisteredAt = registration.RegisteredAt,
                WithdrawnAt = registration.WithdrawnAt,
                RegistrationSubmissions = submissions,
                VenueCheckIns =
                [
                    .. checkIns.Select(c => new VenueCheckInItem
                    {
                        Id = c.Id,
                        CheckInTime = c.CheckInTime,
                        CheckOutTime = c.CheckOutTime,
                        IsCheckedIn = c.IsCheckedIn,
                    }),
                ],
            },
            ct
        );
    }
}

public class WithdrawEndpoint(ISqlSugarClient sql) : Endpoint<ParticipantRequest, WithdrawResponse>
{
    public override void Configure()
    {
        Post("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/participants/{UserId:guid}/withdraw");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Activity Participants"));
    }

    public override async Task HandleAsync(ParticipantRequest req, CancellationToken ct)
    {
        var registration = await sql.Queryable<ActivityRegistration>()
            .InnerJoin<Activity>((r, a) => r.ActivityId == a.Id)
            .Where((r, a) =>
                r.ActivityId == req.StandaloneWorkshopId
                && r.UserId == req.UserId
                && r.Status == ActivityRegistrationStatus.Registered
            )
            .Select((r, a) => r)
            .FirstAsync(ct);
        if (registration is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        registration.Status = ActivityRegistrationStatus.Withdrawn;
        registration.WithdrawnAt = DateTimeOffset.UtcNow;
        await sql.Updateable(registration).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new WithdrawResponse
            {
                Message = "Participant has been withdrawn from the standalone workshop",
            },
            ct
        );
    }
}
