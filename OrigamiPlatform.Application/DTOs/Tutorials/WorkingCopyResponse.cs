namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record WorkingCopyResponse(Guid WorkingCopyId, Guid OriginalId, string Status);
