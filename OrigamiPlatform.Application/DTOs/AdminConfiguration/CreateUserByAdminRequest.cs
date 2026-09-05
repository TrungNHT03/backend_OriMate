namespace OrigamiPlatform.Application.DTOs.AdminConfiguration;

public record CreateUserByAdminRequest(string Email, string Password, string DisplayName, string Role);
