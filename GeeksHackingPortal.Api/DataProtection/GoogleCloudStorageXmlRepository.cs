using System.Text;
using System.Xml.Linq;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace GeeksHackingPortal.Api.DataProtection;

public sealed class GoogleCloudStorageXmlRepository(
    StorageClient storageClient,
    string bucketName,
    string? keyPrefix
) : IXmlRepository
{
    private readonly StorageClient _storageClient =
        storageClient ?? throw new ArgumentNullException(nameof(storageClient));
    private readonly string _bucketName = string.IsNullOrWhiteSpace(bucketName)
        ? throw new ArgumentException("Bucket name is required.", nameof(bucketName))
        : bucketName;
    private readonly string _prefix = NormalizePrefix(keyPrefix);

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var elements = new List<XElement>();

        foreach (var obj in _storageClient.ListObjects(_bucketName, _prefix))
        {
            if (!obj.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var memory = new MemoryStream();
            _storageClient.DownloadObject(obj, memory);
            memory.Position = 0;

            using var reader = new StreamReader(
                memory,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true
            );
            elements.Add(XElement.Parse(reader.ReadToEnd()));
        }

        return elements;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            throw new ArgumentException("Friendly name is required.", nameof(friendlyName));
        }

        var objectName = $"{_prefix}{friendlyName}.xml";
        var payload = Encoding.UTF8.GetBytes(element.ToString(SaveOptions.DisableFormatting));

        using var memory = new MemoryStream(payload);
        _storageClient.UploadObject(_bucketName, objectName, "application/xml", memory);
    }

    private static string NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return string.Empty;
        }

        return prefix.EndsWith('/') ? prefix : $"{prefix}/";
    }
}
