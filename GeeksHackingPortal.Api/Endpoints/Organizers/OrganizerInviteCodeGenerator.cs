using System.Security.Cryptography;

namespace GeeksHackingPortal.Api.Endpoints.Organizers;

internal static class OrganizerInviteCodeGenerator
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate() =>
        new(RandomNumberGenerator.GetItems<char>(Chars, 8));
}
