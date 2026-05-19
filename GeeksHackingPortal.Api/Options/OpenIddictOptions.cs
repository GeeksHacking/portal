namespace GeeksHackingPortal.Api.Options;

public class OpenIddictOptions
{
    public required OpenIddictCertificateOptions SigningCertificate { get; set; }
    public required OpenIddictCertificateOptions EncryptionCertificate { get; set; }
}

public class OpenIddictCertificateOptions
{
    public required string Base64Pfx { get; set; }
    public string? Password { get; set; }
}
