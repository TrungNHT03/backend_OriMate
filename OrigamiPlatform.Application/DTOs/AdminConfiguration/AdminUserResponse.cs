namespace OrigamiPlatform.Application.DTOs.AdminConfiguration;

public record AdminUserResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    string Status,
    List<string> Roles,
    DateTime CreatedAt);
