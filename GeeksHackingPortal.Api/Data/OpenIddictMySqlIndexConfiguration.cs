namespace GeeksHackingPortal.Api.Data;

public static class OpenIddictMySqlIndexConfiguration
{
    // TiDB/MySQL InnoDB composite index limit is 3072 bytes (utf8mb4 => up to 4 bytes/char).
    public static readonly int[] TokenAppStatusSubjectTypePrefixLengths = [255, 50, 191, 150];

    public const string TokenAppStatusSubjectTypeIndexName =
        "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type";
}
