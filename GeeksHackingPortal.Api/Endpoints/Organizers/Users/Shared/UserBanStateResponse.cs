namespace GeeksHackingPortal.Api.Endpoints.Organizers.Users.Shared;

public class UserBanStateResponse
{
    public required Guid UserId { get; init; }
    public required bool IsBanned { get; init; }
    public DateTimeOffset? BannedAt { get; init; }
    public string? BanReason { get; init; }
}
