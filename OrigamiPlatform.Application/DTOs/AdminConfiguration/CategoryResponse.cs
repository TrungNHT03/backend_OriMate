namespace OrigamiPlatform.Application.DTOs.AdminConfiguration;

public record CategoryResponse(int Id, string Name, bool IsActive, DateTime CreatedAt);
