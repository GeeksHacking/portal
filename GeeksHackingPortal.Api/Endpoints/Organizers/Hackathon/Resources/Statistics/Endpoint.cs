using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.Statistics;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/resources/statistics");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Resources"));
        Summary(s =>
        {
            s.Summary = "Get resource redemption statistics";
            s.Description =
                "Returns redemption statistics for all resources or a selected resource, including team-grouped participant breakdowns.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .WithCache()
            .FirstAsync(h => h.Id == req.HackathonId, ct);

        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resources = await sql.Queryable<Resource>()
            .Where(r => r.ActivityId == hackathon.ActivityId)
            .WithCache()
            .ToListAsync(ct);

        var selectedResource =
            req.ResourceId.HasValue
                ? resources.FirstOrDefault(resource => resource.Id == req.ResourceId.Value)
                : null;

        if (req.ResourceId.HasValue && selectedResource is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var participants = await sql.Queryable<Participant>()
            .LeftJoin<User>((p, u) => p.UserId == u.Id)
            .Where((p, u) => p.HackathonId == req.HackathonId)
            .Select(
                (p, u) =>
                    new ParticipantProjection
                    {
                        ParticipantId = p.Id,
                        UserId = p.UserId,
                        TeamId = p.TeamId,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                    }
            )
            .ToListAsync(ct);

        var teams = await sql.Queryable<Team>()
            .Where(team => team.HackathonId == req.HackathonId)
            .WithCache()
            .ToListAsync(ct);

        var scopedResources = selectedResource is null ? resources : [selectedResource];
        var scopedResourceIds = scopedResources.Select(resource => resource.Id).ToList();
        var participantUserIds = participants.Select(participant => participant.UserId).Distinct().ToList();

        var redemptions =
            scopedResourceIds.Count == 0 || participantUserIds.Count == 0
                ? []
                : await sql.Queryable<ResourceRedemption>()
                    .Where(redemption =>
                        redemption.ActivityId == hackathon.ActivityId
                        && scopedResourceIds.Contains(redemption.ResourceId)
                        && participantUserIds.Contains(redemption.UserId)
                    )
                    .OrderByDescending(redemption => redemption.CreatedAt)
                    .ToListAsync(ct);

        var resourceById = resources.ToDictionary(resource => resource.Id);
        var participantByUserId = participants.ToDictionary(participant => participant.UserId);
        var teamById = teams.ToDictionary(team => team.Id);
        var memberCountByTeamId = participants
            .Where(participant => participant.TeamId.HasValue)
            .GroupBy(participant => participant.TeamId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        var redemptionGroupsByResourceId = redemptions
            .GroupBy(redemption => redemption.ResourceId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.CreatedAt).ToList());

        var participantBreakdowns = redemptions
            .GroupBy(redemption => redemption.UserId)
            .Select(group =>
            {
                if (!participantByUserId.TryGetValue(group.Key, out var participant))
                    return null;

                var orderedRedemptions = group.OrderByDescending(item => item.CreatedAt).ToList();
                var firstRedemption = orderedRedemptions.MinBy(item => item.CreatedAt);
                var latestRedemption = orderedRedemptions.MaxBy(item => item.CreatedAt);

                return new ParticipantBreakdownProjection
                {
                    TeamId = participant.TeamId,
                    Item = new ParticipantBreakdownItem
                    {
                        ParticipantId = participant.ParticipantId,
                        UserId = participant.UserId,
                        UserName = BuildUserName(participant.FirstName, participant.LastName),
                        RedemptionCount = orderedRedemptions.Count,
                        DistinctResourcesRedeemed = orderedRedemptions
                            .Select(item => item.ResourceId)
                            .Distinct()
                            .Count(),
                        FirstRedeemedAt = firstRedemption?.CreatedAt.AssumeStoredAsUtc(),
                        LastRedeemedAt = latestRedemption?.CreatedAt.AssumeStoredAsUtc(),
                        Redemptions =
                        [
                            .. orderedRedemptions.Select(redemption => new ParticipantRedemptionEventItem
                            {
                                RedemptionId = redemption.Id,
                                ResourceId = redemption.ResourceId,
                                ResourceName =
                                    resourceById.GetValueOrDefault(redemption.ResourceId)?.Name ?? "Unknown resource",
                                Timestamp = redemption.CreatedAt.AssumeStoredAsUtc(),
                            }),
                        ],
                    },
                };
            })
            .OfType<ParticipantBreakdownProjection>()
            .ToList();

        var teamBreakdown = participantBreakdowns
            .GroupBy(participant => participant.TeamId)
            .Select(group =>
            {
                var participantsInGroup = group
                    .Select(item => item.Item)
                    .OrderByDescending(item => item.LastRedeemedAt)
                    .ThenByDescending(item => item.RedemptionCount)
                    .ThenBy(item => item.UserName)
                    .ToList();

                var teamId = group.Key;
                var teamName = "No team";

                if (teamId.HasValue && teamById.TryGetValue(teamId.Value, out var knownTeam))
                {
                    teamName = knownTeam.Name;
                }

                return new TeamBreakdownItem
                {
                    TeamId = teamId,
                    TeamName = teamName,
                    MemberCount = teamId.HasValue ? memberCountByTeamId.GetValueOrDefault(teamId.Value) : participants.Count(participant => !participant.TeamId.HasValue),
                    RedeemerCount = participantsInGroup.Count,
                    TotalRedemptions = participantsInGroup.Sum(item => item.RedemptionCount),
                    DistinctResourcesRedeemed = participantsInGroup
                        .SelectMany(item => item.Redemptions)
                        .Select(item => item.ResourceId)
                        .Distinct()
                        .Count(),
                    LastRedeemedAt = participantsInGroup.Max(item => item.LastRedeemedAt),
                    Participants = participantsInGroup,
                };
            })
            .OrderByDescending(group => group.TotalRedemptions)
            .ThenByDescending(group => group.LastRedeemedAt)
            .ThenBy(group => group.TeamName)
            .ToList();

        var resourceSummaries = scopedResources
            .Select(resource =>
            {
                var resourceRedemptions = redemptionGroupsByResourceId.GetValueOrDefault(resource.Id) ?? [];
                var latestRedemption = resourceRedemptions.FirstOrDefault();

                return new ResourceSummaryItem
                {
                    ResourceId = resource.Id,
                    ResourceName = resource.Name,
                    IsPublished = resource.IsPublished,
                    TotalRedemptions = resourceRedemptions.Count,
                    UniqueRedeemers = resourceRedemptions.Select(item => item.UserId).Distinct().Count(),
                    LastRedeemedAt = latestRedemption?.CreatedAt.AssumeStoredAsUtc(),
                };
            })
            .OrderByDescending(item => item.TotalRedemptions)
            .ThenBy(item => item.ResourceName)
            .ToList();

        var recentActivity = redemptions
            .Select(redemption =>
            {
                if (!participantByUserId.TryGetValue(redemption.UserId, out var participant))
                    return null;

                var teamId = participant.TeamId;
                var teamName = "No team";

                if (teamId.HasValue && teamById.TryGetValue(teamId.Value, out var knownTeam))
                    teamName = knownTeam.Name;

                return new RecentRedemptionItem
                {
                    RedemptionId = redemption.Id,
                    ResourceId = redemption.ResourceId,
                    ResourceName = resourceById.GetValueOrDefault(redemption.ResourceId)?.Name ?? "Unknown resource",
                    ParticipantId = participant.ParticipantId,
                    UserId = participant.UserId,
                    UserName = BuildUserName(participant.FirstName, participant.LastName),
                    TeamId = teamId,
                    TeamName = teamName,
                    Timestamp = redemption.CreatedAt.AssumeStoredAsUtc(),
                };
            })
            .OfType<RecentRedemptionItem>()
            .Take(25)
            .ToList();

        var participantsWithRedemptions = participantBreakdowns.Count;
        var redeemersWithoutTeam = participantBreakdowns.Count(item => !item.TeamId.HasValue);

        await Send.OkAsync(
            new Response
            {
                ResourceId = selectedResource?.Id,
                ResourceName = selectedResource?.Name,
                ResourceCount = scopedResources.Count,
                ResourcesWithRedemptions = resourceSummaries.Count(item => item.TotalRedemptions > 0),
                ResourcesWithoutRedemptions = resourceSummaries.Count(item => item.TotalRedemptions == 0),
                TotalParticipants = participants.Count,
                ParticipantsWithRedemptions = participantsWithRedemptions,
                ParticipantsWithoutRedemptions = Math.Max(participants.Count - participantsWithRedemptions, 0),
                TeamsWithRedemptions = teamBreakdown.Count(item => item.TeamId.HasValue),
                RedeemersWithoutTeam = redeemersWithoutTeam,
                TotalRedemptions = redemptions.Count,
                AverageRedemptionsPerRedeemer =
                    participantsWithRedemptions == 0
                        ? 0
                        : Math.Round((decimal)redemptions.Count / participantsWithRedemptions, 2),
                FirstRedeemedAt = redemptions.MinBy(item => item.CreatedAt)?.CreatedAt.AssumeStoredAsUtc(),
                LastRedeemedAt = redemptions.MaxBy(item => item.CreatedAt)?.CreatedAt.AssumeStoredAsUtc(),
                ResourceSummaries = resourceSummaries,
                TeamBreakdown = teamBreakdown,
                RecentActivity = recentActivity,
            },
            ct
        );
    }

    private static string BuildUserName(string? firstName, string? lastName)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
    }
}

file class ParticipantProjection
{
    public required Guid ParticipantId { get; set; }
    public required Guid UserId { get; set; }
    public Guid? TeamId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

file class ParticipantBreakdownProjection
{
    public Guid? TeamId { get; set; }
    public required ParticipantBreakdownItem Item { get; set; }
}
