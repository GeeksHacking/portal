namespace HackOMania.Api.Endpoints.Participants.Hackathon.Workshops.MarkAttendance;

public class Response
{
    public required bool HasAttended { get; set; }
    public required DateTimeOffset AttendedAt { get; set; }
}
