using System;

namespace HackOMania.Api.Options;

public class AppOptions
{
    public required string FrontendUrl { get; set; }
    public HackathonCreationMode CreationMode { get; set; } = HackathonCreationMode.RootOnly;
}

public enum HackathonCreationMode
{
    RootOnly,
    Anyone,
}
