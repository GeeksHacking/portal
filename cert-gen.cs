using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

const string signingSecretId = "openiddict-signing-certificate-pfx";
const string encryptionSecretId = "openiddict-encryption-certificate-pfx";
const string passwordSecretId = "openiddict-certificate-password";

var password = GetOption(args, "--password");
if (string.IsNullOrWhiteSpace(password))
{
    password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}

var validYears = int.TryParse(GetOption(args, "--years"), out var parsedYears)
    ? parsedYears
    : 5;

var signingCertificate = CreateCertificate(
    "OpenIddict Server Signing Certificate",
    X509KeyUsageFlags.DigitalSignature,
    validYears
);

var encryptionCertificate = CreateCertificate(
    "OpenIddict Server Encryption Certificate",
    X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
    validYears
);

var signingPfx = Convert.ToBase64String(
    signingCertificate.Export(X509ContentType.Pkcs12, password)
);
var encryptionPfx = Convert.ToBase64String(
    encryptionCertificate.Export(X509ContentType.Pkcs12, password)
);

Console.WriteLine("Generated OpenIddict production certificates.");
Console.WriteLine();
Console.WriteLine($"Password: {password}");
Console.WriteLine();
Console.WriteLine("Secret values:");
Console.WriteLine($"{signingSecretId}={signingPfx}");
Console.WriteLine($"{encryptionSecretId}={encryptionPfx}");
Console.WriteLine($"{passwordSecretId}={password}");
Console.WriteLine();
Console.WriteLine("GCP Secret Manager commands:");
Console.WriteLine(ToGCloudCommand(signingSecretId, signingPfx));
Console.WriteLine(ToGCloudCommand(encryptionSecretId, encryptionPfx));
Console.WriteLine(ToGCloudCommand(passwordSecretId, password));
return;

static X509Certificate2 CreateCertificate(
    string subject,
    X509KeyUsageFlags keyUsage,
    int validYears
)
{
    using var rsa = RSA.Create(4096);
    var request = new CertificateRequest(
        $"CN={subject}",
        rsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1
    );

    request.CertificateExtensions.Add(
        new X509BasicConstraintsExtension(
            certificateAuthority: false,
            hasPathLengthConstraint: false,
            pathLengthConstraint: 0,
            critical: true
        )
    );
    request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, critical: true));
    request.CertificateExtensions.Add(
        new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false)
    );

    var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
    var notAfter = notBefore.AddYears(validYears);

    return request.CreateSelfSigned(notBefore, notAfter);
}

static string? GetOption(string[] args, string name)
{
    for (var index = 0; index < args.Length; index++)
    {
        if (!string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        return index + 1 < args.Length ? args[index + 1] : null;
    }

    return null;
}

static string ToGCloudCommand(string secretId, string value) =>
    $"printf '{value}' | gcloud secrets versions add {secretId} --data-file=-";
