using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Uploads;

public class UploadImageHandler
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB — theo BR Achievement ảnh, áp dụng chung cho mọi loại ảnh upload

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private static readonly HashSet<string> AllowedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "tutorials", "achievements", "learning-paths", "community-posts", "daily-challenge", "mode-tests", "shop"
    };

    private readonly IFileStorageService _fileStorage;

    public UploadImageHandler(IFileStorageService fileStorage) => _fileStorage = fileStorage;

    public async Task<string> HandleAsync(UploadImageCommand command, CancellationToken ct = default)
    {
        Validate(command);

        return await _fileStorage.UploadImageAsync(command.FileStream, command.FileName, command.Folder, ct);
    }

    private static void Validate(UploadImageCommand command)
    {
        if (command.FileSizeBytes <= 0)
            throw new DomainException("File is empty.");

        if (command.FileSizeBytes > MaxFileSizeBytes)
            throw new DomainException("Image must not exceed 10MB.");

        if (command.ContentType is null || !AllowedContentTypes.Contains(command.ContentType))
            throw new DomainException("Only JPEG, PNG, WEBP or GIF images are allowed.");

        if (!AllowedFolders.Contains(command.Folder))
            throw new DomainException("Invalid upload folder.");
    }
}
