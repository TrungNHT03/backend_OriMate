using OrigamiPlatform.Application.DTOs.AdminConfiguration;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record CreateUserByAdminCommand(Guid ActorId, CreateUserByAdminRequest Request);
