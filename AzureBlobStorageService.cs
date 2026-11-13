using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services;

public sealed class AzureBlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    { "image/jpeg", "image/png", "image/webp" };

    public async Task<string> UploadAsync(IFormFile file, string containerName, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("AzureBlobStorage")
            ?? configuration["AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage is not configured. Set the AzureBlobStorage connection string.");
        if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Image must be between 1 byte and 5 MB.");
        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Only JPG, PNG and WEBP images are accepted.");

        var extension = file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg", "image/png" => ".png", "image/webp" => ".webp", _ => ".img"
        };
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        var blob = container.GetBlobClient(blobName);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType }, cancellationToken: cancellationToken);
        return blob.Uri.ToString();
    }
}
