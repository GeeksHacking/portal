namespace HackOMania.Api.Endpoints.Participants.Hackathon.List;

public class Response
{
    public class Response_Hackathon
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string Venue { get; set; } = null!;

        public string HomepageUri { get; set; } = null!;

        public string ShortCode { get; set; } = null!;

        public bool IsPublished { get; set; }

        public DateTimeOffset EventStartDate { get; set; }

        public DateTimeOffset EventEndDate { get; set; }

        public DateTimeOffset SubmissionsStartDate { get; set; }

        public DateTimeOffset SubmissionsEndDate { get; set; }

        public DateTimeOffset JudgingStartDate { get; set; }

        public DateTimeOffset JudgingEndDate { get; set; }
    }

    public IEnumerable<Response_Hackathon> Hackathons { get; set; } = [];
}
