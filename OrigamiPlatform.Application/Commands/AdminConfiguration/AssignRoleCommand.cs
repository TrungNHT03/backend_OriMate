using OrigamiPlatform.Application.DTOs.AdminConfiguration;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record AssignRoleCommand(Guid ActorId, Guid UserId, AssignRoleRequest Request);
