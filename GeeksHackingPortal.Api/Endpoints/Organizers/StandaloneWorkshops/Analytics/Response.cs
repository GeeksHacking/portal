namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Analytics;

public class Response
{
    public required int RegisteredCount { get; set; }

    public required int WithdrawnCount { get; set; }

    public required int CapacityRemaining { get; set; }

    public required double CapacityUsedPercent { get; set; }

    public required int CheckInCount { get; set; }

    public required int CurrentlyCheckedInCount { get; set; }

    public required int ResourceCount { get; set; }

    public required int ResourceRedemptionCount { get; set; }

    public required int EmailTemplateCount { get; set; }

}
