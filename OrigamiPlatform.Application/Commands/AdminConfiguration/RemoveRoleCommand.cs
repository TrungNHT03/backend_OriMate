using OrigamiPlatform.Application.DTOs.AdminConfiguration;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record RemoveRoleCommand(Guid ActorId, Guid UserId, RemoveRoleRequest Request);
