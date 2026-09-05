using OrigamiPlatform.Application.DTOs.AdminConfiguration;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record SuspendUserCommand(Guid ActorId, Guid UserId, SuspendUserRequest Request);
