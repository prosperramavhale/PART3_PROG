namespace RaceDay.API.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(IFormFile file, string containerName, CancellationToken cancellationToken = default);
}
